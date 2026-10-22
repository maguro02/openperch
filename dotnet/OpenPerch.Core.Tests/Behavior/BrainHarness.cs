using System;
using OpenPerch.App.Core;
using OpenPerch.App.Tests.Shared;

namespace OpenPerch.Core.Tests.Behavior
{
    /// <summary>
    /// Ticks a <see cref="MascotBrain"/> the way its owner does: fixed 30 Hz steps, the feet fed back, one-tick flags
    /// cleared, and the standing surface looked up under the feet after every tick.
    /// </summary>
    /// <remarks>
    /// Like the owner, it finds the surface just landed on by the feet, and from then on follows that window by its
    /// handle and the feet x, whatever its y has become.
    /// </remarks>
    internal sealed class BrainHarness
    {
        public const float Dt = 1f / 30f;

        public BrainInput Input;

        private bool _following;
        private IntPtr _followedOwner;
        private SurfaceKind _followedKind;

        public BrainHarness(BehaviorConfig config, SurfaceSet surfaces, Vec2 feet, MotionAvailability motions, params float[] randomSequence)
        {
            Random = new FakeRandom(randomSequence);
            Brain = new MascotBrain(config, Random);
            Input = new BrainInput
            {
                FeetPx = feet,
                Standing = SurfaceRef.None,
                Surfaces = surfaces,
                Motions = motions,
                WalkSpeedPx = TestDesk.WalkSpeedPx,
                DisplayHeightPx = TestDesk.DisplayHeightPx,
                SeatHeightPx = TestDesk.SeatHeightPx,
                PixelsPerMeter = TestDesk.PixelsPerMeter,
            };
        }

        public MascotBrain Brain { get; }

        public FakeRandom Random { get; }

        public BrainOutput Last { get; private set; }

        public int Ticks { get; private set; }

        public BrainOutput Step()
        {
            Input.Dt = Dt;
            Last = Brain.Tick(Input);
            Ticks++;
            Input.FeetPx = Last.FeetPx;
            Input.Pointer.LeftDown = false;
            Input.Pointer.LeftUp = false;
            Input.OneShotDone = false;
            Input.StandingLostHard = false;
            Input.Standing = FindStanding();
            return Last;
        }

        public BrainOutput Steps(int count)
        {
            for (int k = 0; k < count; k++)
            {
                Step();
            }

            return Last;
        }

        public BrainOutput StepUntil(Func<BrainOutput, bool> done, int maxTicks = 3000)
        {
            for (int k = 0; k < maxTicks; k++)
            {
                if (done(Step()))
                {
                    return Last;
                }
            }

            throw new InvalidOperationException($"Condition not met within {maxTicks} ticks; last state {Last.State}.");
        }

        /// <summary>Lets the initial fall land and finishes the landing reaction, leaving the brain idle.</summary>
        public BrainOutput Settle()
        {
            StepUntil(o => o.State != MascotState.Falling);
            return Last.State == MascotState.Landing ? FinishOneShot() : Last;
        }

        public BrainOutput FinishOneShot()
        {
            Input.OneShotDone = true;
            return Step();
        }

        public void SetSurfaces(SurfaceSet surfaces)
        {
            Input.Surfaces = surfaces;
            Input.Standing = FindStanding();
        }

        /// <summary>Presses on the avatar, with the owner placing the feet at <paramref name="feet"/>.</summary>
        public BrainOutput Press(Vec2 feet)
        {
            Input.FeetPx = feet;
            Input.Pointer.LeftDown = true;
            Input.Pointer.LeftHeld = true;
            Input.PointerOnThisAvatar = true;
            return Step();
        }

        public BrainOutput Release(Vec2 cursorVelocity)
        {
            Input.Pointer.LeftUp = true;
            Input.Pointer.LeftHeld = false;
            Input.Pointer.VelocityPxPerSec = cursorVelocity;
            return Step();
        }

        private SurfaceRef FindStanding()
        {
            SurfaceSet set = Input.Surfaces;
            if (Ticks == 0 || set == null)
            {
                return SurfaceRef.None;
            }

            switch (Last.State)
            {
                case MascotState.Falling:
                case MascotState.Jump:
                case MascotState.Dragged:
                    _following = false;
                    return SurfaceRef.None;
            }

            Vec2 feet = Last.FeetPx;
            for (int k = 0; k < set.Items.Length; k++)
            {
                Surface s = set.Items[k];
                bool candidate = _following ? s.Owner == _followedOwner && s.Kind == _followedKind : Math.Abs(s.Y - feet.y) <= 0.5f;
                if (candidate && feet.x >= s.X0 && feet.x < s.X1)
                {
                    _following = true;
                    _followedOwner = s.Owner;
                    _followedKind = s.Kind;
                    return new SurfaceRef(k, set.Seq);
                }
            }

            return SurfaceRef.None;
        }
    }
}
