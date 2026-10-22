using System;

namespace OpenPerch.App.Core
{
    /// <summary>
    /// Behaviour state machine of one mascot: decides what it does on the desktop surfaces and moves its feet.
    /// </summary>
    /// <remarks>
    /// The owner ticks it at a fixed rate, feeds the returned <see cref="BrainOutput.FeetPx"/> back, and looks up
    /// <see cref="BrainInput.Standing"/> under the feet in the current surfaces. While dragged, the owner places the feet
    /// from the cursor every frame; the brain only reacts to the press and the release. The owner keeps ticking while
    /// <see cref="BrainInput.Fullscreen"/> holds the mascot hidden and for at least 2 s after it clears, because the
    /// brain counts that delay in its own ticks before it shows the mascot again. Coordinates are client pixels with
    /// y pointing down, so gravity acts along +y.
    /// </remarks>
    public sealed class MascotBrain
    {
        private const float GravityMetersPerSec2 = 9.81f;
        private const float TerminalSpeedPx = 3000f;
        private const float MaxThrowSpeedPx = 4000f;
        private const float UnhideDelaySec = 2f;
        private const float JumpApexAboveTargetRatio = 0.25f;

        // The landing is solved on the integrated path, so it misses the aimed point by rounding only; aiming this far
        // inside the span keeps a jump to its very end from landing just outside it.
        private const float JumpLandingInsetPx = 1f;

        // The owner may look the surface up by window and x alone, so a window that moved while the mascot was hidden
        // still resolves although the feet stayed behind. The slack covers a snapshot trailing a window being followed.
        private const float UnhideFootingToleranceRatio = 0.05f;

        // Sums of 1/30 s steps drift by a few ulps; without the slack a 0.5 s threshold could take one tick more.
        private const float TimeEpsilon = 1e-4f;

        private const int ActionWalk = 0;
        private const int ActionSit = 1;
        private const int ActionIdle = 2;
        private const int ActionVariant = 3;
        private const int ActionJump = 4;

        private readonly BehaviorConfig _config;
        private readonly IRandom _random;

        private MascotState _state = MascotState.Falling;
        private Vec2 _velocity;
        private float _stateTimer;
        private float _footingLostSec;
        private int _walkDirection = 1;
        private float _walkRemainingPx;
        private bool _walkOffEnd;
        private bool _variantPlaying;
        private bool _lostWhileHidden;
        private bool _hasLastStanding;
        private IntPtr _lastStandingOwner;
        private int _lastStandingY;
        private bool _skipLostSurface;
        private IntPtr _lostOwner;
        private int _lostY;
        private BrainOutput _last;

        /// <summary>Starts in <see cref="MascotState.Falling"/>, so the first ticks drop the mascot onto a surface.</summary>
        public MascotBrain(BehaviorConfig config, IRandom random)
        {
            _config = config ?? throw new ArgumentNullException(nameof(config));
            _random = random ?? throw new ArgumentNullException(nameof(random));
            _last.State = MascotState.Falling;
            _last.Role = LocomotionRole.Fall;
            _last.RoleSpeed = 1f;
        }

        /// <summary>Advances the state machine by <see cref="BrainInput.Dt"/> seconds.</summary>
        public BrainOutput Tick(in BrainInput input)
        {
            float dt = input.Dt > 0f && !float.IsInfinity(input.Dt) ? input.Dt : 0f;
            BrainOutput output;
            if (_state == MascotState.Hidden || input.Fullscreen)
            {
                output = TickHidden(input, dt);
            }
            else
            {
                if (_state != MascotState.Dragged && input.Pointer.LeftDown && input.PointerOnThisAvatar)
                {
                    _state = MascotState.Dragged;
                    _velocity = default;
                    _variantPlaying = false;
                }

                switch (_state)
                {
                    case MascotState.Dragged:
                        output = TickDragged(input);
                        break;
                    case MascotState.Falling:
                    case MascotState.Jump:
                        output = TickAirborne(input, dt);
                        break;
                    default:
                        output = TickGrounded(input, dt);
                        break;
                }
            }

            _last = output;
            return output;
        }

        private BrainOutput TickHidden(in BrainInput input, float dt)
        {
            if (_state != MascotState.Hidden)
            {
                _state = MascotState.Hidden;
                _stateTimer = 0f;
                _velocity = default;
                _variantPlaying = false;
                _lostWhileHidden = false;
            }

            // The owner reports a hard loss for a single tick, so one that arrives while hidden has to wait for the exit.
            _lostWhileHidden |= input.StandingLostHard;
            if (input.Fullscreen)
            {
                _stateTimer = 0f;
            }
            else
            {
                _stateTimer += dt;
                if (_stateTimer + TimeEpsilon >= UnhideDelaySec)
                {
                    _footingLostSec = 0f;
                    if (!_lostWhileHidden
                        && TryGetFooting(input, out Surface standing)
                        && Math.Abs(standing.Y - input.FeetPx.y) <= UnhideFootingToleranceRatio * input.DisplayHeightPx)
                    {
                        return EnterIdle(input, input.FeetPx);
                    }

                    return EnterFalling(input, input.FeetPx, default, _lostWhileHidden);
                }
            }

            return new BrainOutput
            {
                State = MascotState.Hidden,
                FeetPx = input.FeetPx,
                RootPx = input.FeetPx + (_last.RootPx - _last.FeetPx),
                Facing = _last.Facing,
                Role = ResolveRole(_last.Role, input.Motions),
                RoleSpeed = 0f,
            };
        }

        private BrainOutput TickDragged(in BrainInput input)
        {
            if (input.Pointer.LeftUp || !input.Pointer.LeftHeld)
            {
                return EnterFalling(input, input.FeetPx, LimitThrow(input.Pointer.VelocityPxPerSec), false);
            }

            return new BrainOutput
            {
                State = MascotState.Dragged,
                FeetPx = input.FeetPx,
                RootPx = input.FeetPx,
                Facing = 0,
                Role = ResolveRole(LocomotionRole.Dragged, input.Motions),
                RoleSpeed = 1f,
            };
        }

        private BrainOutput TickAirborne(in BrainInput input, float dt)
        {
            Vec2 start = input.FeetPx;
            float g = Gravity(input);
            float vx = _velocity.x;
            float vy = Math.Min(_velocity.y, TerminalSpeedPx);

            // Gravity stops adding speed at the terminal speed, which the fall can reach part way through the tick.
            bool reachesTerminal = vy + g * dt > TerminalSpeedPx;
            float acceleratingSec = reachesTerminal ? (TerminalSpeedPx - vy) / g : dt;
            float yAccelerated = start.y + vy * acceleratingSec + 0.5f * g * acceleratingSec * acceleratingSec;
            var end = new Vec2(start.x + vx * dt, yAccelerated + TerminalSpeedPx * (dt - acceleratingSec));
            if (TrySweep(input, start, vx, vy, g, acceleratingSec, yAccelerated, end.y, out Vec2 landing))
            {
                return Land(input, landing);
            }

            vy = reachesTerminal ? TerminalSpeedPx : vy + g * dt;
            if (TryGetFloor(input.Surfaces, out Surface floor))
            {
                if (end.x < floor.X0)
                {
                    end.x = floor.X0;
                    vx = Math.Abs(vx);
                }
                else if (end.x > floor.X1 - 1)
                {
                    end.x = floor.X1 - 1;
                    vx = -Math.Abs(vx);
                }

                if (end.y >= floor.Y)
                {
                    return Land(input, new Vec2(end.x, floor.Y));
                }
            }

            _velocity = new Vec2(vx, vy);
            return AirborneOutput(input, end);
        }

        private bool TrySweep(
            in BrainInput input,
            Vec2 start,
            float vx,
            float vy,
            float g,
            float acceleratingSec,
            float yAccelerated,
            float yEnd,
            out Vec2 landing)
        {
            landing = default;
            Surface[] items = input.Surfaces?.Items;
            if (items == null)
            {
                return false;
            }

            float best = float.PositiveInfinity;
            for (int k = 0; k < items.Length; k++)
            {
                Surface s = items[k];
                if (!IsLandable(s, input.DisplayHeightPx))
                {
                    continue;
                }

                float t = DescentTime(s.Y, start.y, vy, g, acceleratingSec, yAccelerated, yEnd);
                if (!(t >= 0f) || !(t < best))
                {
                    continue;
                }

                float x = start.x + vx * t;
                if (x >= s.X0 && x < s.X1)
                {
                    best = t;
                    landing = new Vec2(x, s.Y);
                }
            }

            return best < float.PositiveInfinity;
        }

        /// <summary>
        /// Seconds into the tick at which the feet come down through height <paramref name="y"/>, or -1 when they do not.
        /// </summary>
        /// <remarks>
        /// Solved on the path itself: the straight line between the tick's ends crosses short of it by a distance that
        /// grows with gravity, enough at high pixels per metre to make a jump to the near end of a span miss it.
        /// </remarks>
        private static float DescentTime(float y, float startY, float vy, float g, float acceleratingSec, float yAccelerated, float yEnd)
        {
            float below = y - startY;
            if (below < 0f || y > yEnd)
            {
                return -1f;
            }

            if (y > yAccelerated)
            {
                return acceleratingSec + (y - yAccelerated) / TerminalSpeedPx;
            }

            // The later root of startY + vy·t + g·t²/2 = y, in the form that does not cancel when vy > 0.
            float root = MathF.Sqrt(vy * vy + 2f * g * below);
            float t = vy > 0f ? 2f * below / (vy + root) : g > 0f ? (root - vy) / g : 0f;
            return Math.Min(t, acceleratingSec);
        }

        private bool IsLandable(in Surface s, float displayHeightPx)
        {
            if (s.Kind != SurfaceKind.WindowTop)
            {
                return true;
            }

            if (s.Headroom < displayHeightPx)
            {
                return false;
            }

            // The snapshot can still list the window the owner just reported lost, which would catch the mascot on its
            // first tick of falling.
            return !(_skipLostSurface && s.Owner == _lostOwner && s.Y == _lostY);
        }

        private BrainOutput Land(in BrainInput input, Vec2 at)
        {
            _velocity = default;
            _footingLostSec = 0f;
            _skipLostSurface = false;
            if (Has(input.Motions, MotionAvailability.Land))
            {
                _state = MascotState.Landing;
                return GroundedOutput(input, at);
            }

            return EnterIdle(input, at);
        }

        private BrainOutput TickGrounded(in BrainInput input, float dt)
        {
            Vec2 feet = input.FeetPx;
            bool footing = TryGetFooting(input, out Surface standing);
            if (footing)
            {
                _hasLastStanding = true;
                _lastStandingOwner = standing.Owner;
                _lastStandingY = standing.Y;
            }

            if (input.StandingLostHard)
            {
                return EnterFalling(input, feet, WalkVelocity(input), true);
            }

            if (footing)
            {
                _footingLostSec = 0f;
            }
            else
            {
                // A window passing over the feet only briefly must not drop the mascot.
                _footingLostSec += dt;
                if (_footingLostSec + TimeEpsilon >= _config.occludedFallSec)
                {
                    return EnterFalling(input, feet, WalkVelocity(input), false);
                }
            }

            switch (_state)
            {
                case MascotState.Idle:
                    return TickIdle(input, dt, feet, footing, standing);
                case MascotState.Walk:
                    return TickWalk(input, dt, feet, footing, standing);
                case MascotState.SitEdge:
                case MascotState.SitFloor:
                    return TickSit(input, dt, feet);
                default:
                    return TickLanding(input, feet);
            }
        }

        private BrainOutput TickIdle(in BrainInput input, float dt, Vec2 feet, bool footing, in Surface standing)
        {
            if (_variantPlaying)
            {
                if (input.OneShotDone || !Has(input.Motions, MotionAvailability.IdleVariant))
                {
                    return EnterIdle(input, feet);
                }

                return GroundedOutput(input, feet);
            }

            _stateTimer -= dt;
            if (_stateTimer > TimeEpsilon || !footing)
            {
                return GroundedOutput(input, feet);
            }

            return ChooseNext(input, feet, standing);
        }

        private BrainOutput ChooseNext(in BrainInput input, Vec2 feet, in Surface standing)
        {
            bool canWalk = CanWalk(input);
            if (input.Pomodoro == PomodoroMode.Focus && standing.Kind == SurfaceKind.WindowTop && canWalk)
            {
                int towardNearerEnd = feet.x - standing.X0 < standing.X1 - feet.x ? -1 : 1;
                return EnterWalk(input, feet, towardNearerEnd, float.PositiveInfinity, true);
            }

            switch (DrawAction(input.Pomodoro))
            {
                case ActionWalk:
                    if (canWalk)
                    {
                        int direction = _random.Next01() < 0.5f ? -1 : 1;
                        float ratio = Lerp(_config.walkMinRatio, _config.walkMaxRatio, _random.Next01());
                        return EnterWalk(input, feet, direction, ratio * input.DisplayHeightPx, false);
                    }

                    break;
                case ActionSit:
                    if (standing.Kind == SurfaceKind.WindowTop)
                    {
                        if (CanSitOnEdge(input, feet))
                        {
                            return EnterSit(input, feet, MascotState.SitEdge);
                        }
                    }
                    else if (Has(input.Motions, MotionAvailability.SitOnFloor))
                    {
                        return EnterSit(input, feet, MascotState.SitFloor);
                    }

                    break;
                case ActionVariant:
                    if (Has(input.Motions, MotionAvailability.IdleVariant))
                    {
                        _variantPlaying = true;
                        return GroundedOutput(input, feet);
                    }

                    break;
                case ActionJump:
                    if (CanJump(input) && TryPickJumpTarget(input, feet, out Vec2 target))
                    {
                        return EnterJump(input, feet, target);
                    }

                    break;
            }

            return EnterIdle(input, feet);
        }

        private int DrawAction(PomodoroMode mode)
        {
            float walkScale = mode == PomodoroMode.Focus ? _config.focusWalkScale : mode == PomodoroMode.Break ? _config.breakScale : 1f;
            float jumpScale = mode == PomodoroMode.Focus ? 0f : mode == PomodoroMode.Break ? _config.breakScale : 1f;
            float variantScale = mode == PomodoroMode.Break ? _config.breakScale : 1f;

            float walk = NonNegative(_config.wWalk * walkScale);
            float sit = walk + NonNegative(_config.wSit);
            float idle = sit + NonNegative(_config.wIdle);
            float variant = idle + NonNegative(_config.wVariant * variantScale);
            float jump = variant + NonNegative(_config.wJump * jumpScale);

            float r = _random.Next01() * jump;
            if (r < walk) return ActionWalk;
            if (r < sit) return ActionSit;
            if (r < idle) return ActionIdle;
            if (r < variant) return ActionVariant;
            return r < jump ? ActionJump : ActionIdle;
        }

        private BrainOutput EnterWalk(in BrainInput input, Vec2 feet, int direction, float distancePx, bool offTheEnd)
        {
            _state = MascotState.Walk;
            _walkDirection = direction;
            _walkRemainingPx = distancePx;
            _walkOffEnd = offTheEnd;
            return GroundedOutput(input, feet);
        }

        private BrainOutput TickWalk(in BrainInput input, float dt, Vec2 feet, bool footing, in Surface standing)
        {
            if (!CanWalk(input))
            {
                return EnterIdle(input, feet);
            }

            float step = Math.Min(input.WalkSpeedPx * dt, _walkRemainingPx);
            _walkRemainingPx -= step;
            feet.x += _walkDirection * step;
            if (footing)
            {
                if (_walkOffEnd)
                {
                    if (feet.x < standing.X0 || feet.x >= standing.X1)
                    {
                        return EnterFalling(input, feet, new Vec2(_walkDirection * input.WalkSpeedPx, 0f), false);
                    }
                }
                else
                {
                    float lo = standing.X0;
                    float hi = standing.X1 - 1;
                    if (feet.x > hi)
                    {
                        feet.x = Math.Max(lo, 2f * hi - feet.x);
                        _walkDirection = -1;
                    }
                    else if (feet.x < lo)
                    {
                        feet.x = Math.Min(hi, 2f * lo - feet.x);
                        _walkDirection = 1;
                    }
                }
            }

            if (_walkRemainingPx <= 0f)
            {
                return EnterIdle(input, feet);
            }

            return GroundedOutput(input, feet);
        }

        private BrainOutput EnterSit(in BrainInput input, Vec2 feet, MascotState state)
        {
            _state = state;
            _stateTimer = DrawDwellSec();
            return GroundedOutput(input, feet);
        }

        private BrainOutput TickSit(in BrainInput input, float dt, Vec2 feet)
        {
            _stateTimer -= dt;
            if (_stateTimer <= TimeEpsilon)
            {
                return EnterIdle(input, feet);
            }

            return GroundedOutput(input, feet);
        }

        private BrainOutput TickLanding(in BrainInput input, Vec2 feet)
        {
            if (input.OneShotDone || !Has(input.Motions, MotionAvailability.Land))
            {
                return EnterIdle(input, feet);
            }

            return GroundedOutput(input, feet);
        }

        private BrainOutput EnterIdle(in BrainInput input, Vec2 feet)
        {
            _state = MascotState.Idle;
            _variantPlaying = false;
            _velocity = default;
            _stateTimer = DrawDwellSec();
            return GroundedOutput(input, feet);
        }

        private float DrawDwellSec()
        {
            float min = Math.Max(0f, _config.idleMinSec);
            float max = Math.Max(min, _config.idleMaxSec);
            return Lerp(min, max, _random.Next01());
        }

        private BrainOutput EnterFalling(in BrainInput input, Vec2 feet, Vec2 velocity, bool skipLastStanding)
        {
            _state = MascotState.Falling;
            _velocity = velocity;
            _variantPlaying = false;
            _skipLostSurface = skipLastStanding && _hasLastStanding;
            _lostOwner = _lastStandingOwner;
            _lostY = _lastStandingY;
            _hasLastStanding = false;
            return AirborneOutput(input, feet);
        }

        private bool TryPickJumpTarget(in BrainInput input, Vec2 feet, out Vec2 target)
        {
            target = default;
            Surface[] items = input.Surfaces?.Items;
            if (items == null)
            {
                return false;
            }

            float total = 0f;
            for (int k = 0; k < items.Length; k++)
            {
                if (TryGetJumpLanding(items[k], input, feet, out _, out float weight))
                {
                    total += weight;
                }
            }

            if (!(total > 0f))
            {
                return false;
            }

            float r = _random.Next01() * total;
            float cumulative = 0f;
            for (int k = 0; k < items.Length; k++)
            {
                if (!TryGetJumpLanding(items[k], input, feet, out Vec2 landing, out float weight))
                {
                    continue;
                }

                cumulative += weight;
                target = landing;
                if (r < cumulative)
                {
                    break;
                }
            }

            return true;
        }

        private bool TryGetJumpLanding(in Surface s, in BrainInput input, Vec2 feet, out Vec2 landing, out float weight)
        {
            landing = default;
            weight = 0f;
            float height = input.DisplayHeightPx;
            if (s.Kind != SurfaceKind.WindowTop || s.X1 <= s.X0 || s.Headroom < height)
            {
                return false;
            }

            float rise = feet.y - s.Y;
            if (!(rise > 0f) || rise > _config.jumpMaxRiseRatio * height)
            {
                return false;
            }

            float inset = Math.Min(JumpLandingInsetPx, (s.X1 - 1 - s.X0) * 0.5f);
            float x = Math.Min(Math.Max(feet.x, s.X0 + inset), s.X1 - 1 - inset);
            float run = Math.Abs(x - feet.x);
            if (run > _config.jumpMaxRunRatio * height)
            {
                return false;
            }

            landing = new Vec2(x, s.Y);
            weight = 1f / (1f + run / height);
            return true;
        }

        private BrainOutput EnterJump(in BrainInput input, Vec2 feet, Vec2 target)
        {
            float g = Gravity(input);
            float drop = JumpApexAboveTargetRatio * input.DisplayHeightPx;
            float climb = feet.y - target.y + drop;
            float flightSec = MathF.Sqrt(2f * climb / g) + MathF.Sqrt(2f * drop / g);
            _state = MascotState.Jump;
            _velocity = new Vec2((target.x - feet.x) / flightSec, -MathF.Sqrt(2f * g * climb));
            _hasLastStanding = false;
            _skipLostSurface = false;
            return AirborneOutput(input, feet);
        }

        private bool CanWalk(in BrainInput input) =>
            Has(input.Motions, MotionAvailability.Walk) && !input.LocomotionBlockedByFx && input.WalkSpeedPx > 0f;

        private bool CanSitOnEdge(in BrainInput input, Vec2 feet) =>
            Has(input.Motions, MotionAvailability.SitOnEdge)
            && TryGetFloor(input.Surfaces, out Surface floor)
            && floor.Y - feet.y >= input.SeatHeightPx;

        private bool CanJump(in BrainInput input) =>
            Has(input.Motions, MotionAvailability.Fall)
            && Has(input.Motions, MotionAvailability.Land)
            && input.Pomodoro != PomodoroMode.Focus
            && !input.LocomotionBlockedByFx
            && Gravity(input) > 0f
            && input.DisplayHeightPx > 0f;

        private Vec2 WalkVelocity(in BrainInput input) =>
            _state == MascotState.Walk ? new Vec2(_walkDirection * input.WalkSpeedPx, 0f) : default;

        private BrainOutput GroundedOutput(in BrainInput input, Vec2 feet)
        {
            var output = new BrainOutput { State = _state, FeetPx = feet, RootPx = feet, Facing = 0, RoleSpeed = 1f };
            switch (_state)
            {
                case MascotState.Walk:
                    output.Role = ResolveRole(LocomotionRole.Walk, input.Motions);
                    output.Facing = _walkDirection;
                    break;
                case MascotState.SitEdge:
                    output.Role = ResolveRole(LocomotionRole.SitOnEdge, input.Motions);
                    output.RootPx = new Vec2(feet.x, feet.y + input.SeatHeightPx);
                    break;
                case MascotState.SitFloor:
                    output.Role = ResolveRole(LocomotionRole.SitOnFloor, input.Motions);
                    break;
                case MascotState.Landing:
                    output.Role = ResolveRole(LocomotionRole.Land, input.Motions);
                    break;
                default:
                    output.Role = ResolveRole(_variantPlaying ? LocomotionRole.IdleVariant : LocomotionRole.Idle, input.Motions);
                    break;
            }

            return output;
        }

        private BrainOutput AirborneOutput(in BrainInput input, Vec2 feet)
        {
            return new BrainOutput
            {
                State = _state,
                FeetPx = feet,
                RootPx = feet,
                Facing = Sign(_velocity.x),
                Role = ResolveRole(LocomotionRole.Fall, input.Motions),
                RoleSpeed = 1f,
            };
        }

        private static bool TryGetFooting(in BrainInput input, out Surface standing)
        {
            standing = default;
            SurfaceSet set = input.Surfaces;
            SurfaceRef r = input.Standing;
            if (set?.Items == null || r.IsNone || r.Seq != set.Seq || r.Index >= set.Items.Length)
            {
                return false;
            }

            standing = set.Items[r.Index];
            return input.FeetPx.x >= standing.X0 && input.FeetPx.x < standing.X1;
        }

        private static bool TryGetFloor(SurfaceSet set, out Surface floor)
        {
            Surface[] items = set?.Items;
            if (items != null)
            {
                for (int k = 0; k < items.Length; k++)
                {
                    if (items[k].Kind == SurfaceKind.Floor)
                    {
                        floor = items[k];
                        return true;
                    }
                }
            }

            floor = default;
            return false;
        }

        private static float Gravity(in BrainInput input) =>
            input.PixelsPerMeter > 0f && !float.IsInfinity(input.PixelsPerMeter) ? GravityMetersPerSec2 * input.PixelsPerMeter : 0f;

        private static LocomotionRole ResolveRole(LocomotionRole role, MotionAvailability motions)
        {
            if (role == LocomotionRole.Dragged && !Has(motions, MotionAvailability.Dragged))
            {
                role = LocomotionRole.Fall;
            }

            return role == LocomotionRole.Idle || Has(motions, MotionOf(role)) ? role : LocomotionRole.Idle;
        }

        private static MotionAvailability MotionOf(LocomotionRole role)
        {
            switch (role)
            {
                case LocomotionRole.IdleVariant: return MotionAvailability.IdleVariant;
                case LocomotionRole.Walk: return MotionAvailability.Walk;
                case LocomotionRole.SitOnEdge: return MotionAvailability.SitOnEdge;
                case LocomotionRole.SitOnFloor: return MotionAvailability.SitOnFloor;
                case LocomotionRole.Dragged: return MotionAvailability.Dragged;
                case LocomotionRole.Fall: return MotionAvailability.Fall;
                case LocomotionRole.Land: return MotionAvailability.Land;
                default: return MotionAvailability.Idle;
            }
        }

        private static Vec2 LimitThrow(Vec2 velocity)
        {
            float speed = velocity.Length();
            if (!(speed > 0f) || float.IsInfinity(speed))
            {
                return default;
            }

            return speed > MaxThrowSpeedPx ? velocity * (MaxThrowSpeedPx / speed) : velocity;
        }

        private static bool Has(MotionAvailability motions, MotionAvailability motion) => (motions & motion) != 0;

        private static float Lerp(float a, float b, float t) => a + (b - a) * t;

        private static float NonNegative(float value) => value > 0f ? value : 0f;

        private static int Sign(float value) => value > 0f ? 1 : value < 0f ? -1 : 0;
    }
}
