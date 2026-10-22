using System;
using NUnit.Framework;
using OpenPerch.App.Core;

namespace OpenPerch.Core.Tests.Behavior
{
    [Category("T-BRAIN")]
    public class MascotBrainJumpTests
    {
        private const float JumpDraw = 0.95f;
        private const MotionAvailability Jumping = MotionAvailability.Idle | MotionAvailability.Fall | MotionAvailability.Land;

        // Seen from feet at (700, 1040) with a 360 px tall avatar: rise 300, straight above.
        private static readonly Surface Reachable = TestDesk.Window(1, 740, 600, 900);

        // Rise 600 > 1.5 × 360.
        private static readonly Surface TooHigh = TestDesk.Window(2, 440, 600, 900);

        // Rise 0.
        private static readonly Surface Level = TestDesk.Window(3, 1040, 1000, 1200);

        // Rise 200, but the nearest landing point x = 1701 is 1001 px > 2.5 × 360 away.
        private static readonly Surface TooFar = TestDesk.Window(4, 840, 1700, 1900);

        // Rise 200 and 402 px away, but only 300 px of headroom.
        private static readonly Surface Cramped = TestDesk.Window(5, 840, 100, 300, 300);

        // Rise 200; the nearest landing point x = 1060 is exactly one avatar height away, so it weighs 1/2.
        private static readonly Surface OneHeightAway = TestDesk.Window(6, 840, 1059, 1400);

        [TestCase(0f)]
        [TestCase(0.5f)]
        [TestCase(0.99f)]
        public void JumpsOnlyToAWindowTopThatMeetsEveryCondition(float targetDraw)
        {
            BrainHarness h = Scene(targetDraw, Reachable, TooHigh, Level, TooFar, Cramped);

            Assert.That(h.Steps(30).State, Is.EqualTo(MascotState.Jump));
            BrainOutput landed = h.StepUntil(o => o.State != MascotState.Jump);

            Assert.That(landed.State, Is.EqualTo(MascotState.Landing));
            Assert.That(landed.FeetPx.x, Is.EqualTo(700f));
            Assert.That(landed.FeetPx.y, Is.EqualTo(740f));
        }

        // The limits for a 360 px tall avatar are a rise of 540, a run of 900 and 360 px of headroom, all included.
        [TestCase(540, 900, 360, true)]
        [TestCase(541, 900, 360, false)]
        [TestCase(540, 901, 360, false)]
        [TestCase(540, 900, 359, false)]
        public void JumpLimitsIncludeTheirBounds(int rise, int run, int headroom, bool jumps)
        {
            int y = 1040 - rise;
            int landingX = 700 + run;
            BrainHarness h = Scene(0.5f, TestDesk.Window(8, y, landingX - 1, landingX + 199, headroom));

            BrainOutput decided = h.Steps(30);

            if (!jumps)
            {
                Assert.That(decided.State, Is.EqualTo(MascotState.Idle));
                AssertNeverJumps(h, 120);
                return;
            }

            Assert.That(decided.State, Is.EqualTo(MascotState.Jump));
            BrainOutput landed = h.StepUntil(o => o.State != MascotState.Jump);
            Assert.That(landed.State, Is.EqualTo(MascotState.Landing));
            Assert.That(landed.FeetPx.x, Is.EqualTo((float)landingX).Within(0.01f));
            Assert.That(landed.FeetPx.y, Is.EqualTo((float)y));
        }

        // Weights 1 and 1/2: draws below 2/3 pick the window straight above.
        [TestCase(0.6f, 700f, 740f)]
        [TestCase(0.7f, 1060f, 840f)]
        public void NearerTargetsWeighMore(float targetDraw, float landingX, float landingY)
        {
            BrainHarness h = Scene(targetDraw, Reachable, OneHeightAway);

            Assert.That(h.Steps(30).State, Is.EqualTo(MascotState.Jump));
            BrainOutput landed = h.StepUntil(o => o.State != MascotState.Jump);

            Assert.That(landed.FeetPx.x, Is.EqualTo(landingX).Within(0.5f));
            Assert.That(landed.FeetPx.y, Is.EqualTo(landingY));
        }

        [Test]
        public void TrajectoryPeaksAQuarterHeightAboveTheTargetAndLandsOnIt()
        {
            BrainHarness h = Scene(0.7f, Reachable, OneHeightAway);
            BrainOutput takeoff = h.Steps(30);
            Assert.That(takeoff.State, Is.EqualTo(MascotState.Jump));
            BrainOutput first = h.Step();

            float g = TestDesk.Gravity;
            float dt = BrainHarness.Dt;
            float drop = 0.25f * TestDesk.DisplayHeightPx;
            float climb = 1040f - 840f + drop;
            float flightSec = MathF.Sqrt(2f * climb / g) + MathF.Sqrt(2f * drop / g);
            float vy0 = (first.FeetPx.y - takeoff.FeetPx.y) / dt - 0.5f * g * dt;
            float vx = (first.FeetPx.x - takeoff.FeetPx.x) / dt;

            Assert.That(vy0, Is.EqualTo(-MathF.Sqrt(2f * g * climb)).Within(0.5f));
            Assert.That(vx, Is.EqualTo((1060f - 700f) / flightSec).Within(0.5f));
            Assert.That(first.Facing, Is.EqualTo(1));
            Assert.That(first.Role, Is.EqualTo(LocomotionRole.Fall));

            float apex = first.FeetPx.y;
            int ticks = 1;
            BrainOutput o = first;
            while (o.State == MascotState.Jump)
            {
                o = h.Step();
                ticks++;
                apex = Math.Min(apex, o.FeetPx.y);
            }

            Assert.That(apex, Is.EqualTo(840f - drop).Within(1f));
            Assert.That(o.State, Is.EqualTo(MascotState.Landing));
            Assert.That(o.Role, Is.EqualTo(LocomotionRole.Land));
            Assert.That(o.FeetPx.y, Is.EqualTo(840f));
            Assert.That(o.FeetPx.x, Is.EqualTo(1060f).Within(0.5f));
            Assert.That(ticks * dt, Is.EqualTo(flightSec).Within(dt));
        }

        [Test]
        public void LandsOnTheNearEndOfASpanAtHighPixelsPerMetre()
        {
            // A 1 m avatar shown 720 px tall jumps 1798 px, close to the 1800 px limit, onto a window top 58 px up. In its
            // last tick the straight line between the tick's ends crosses y = 982 at x ≈ 1855.7, outside [1857, 1919);
            // the path itself comes down at the aimed x = 1858.
            var h = new BrainHarness(
                TestDesk.FixedDwell(1f), TestDesk.Desk(TestDesk.Window(7, 982, 1857, 1919)), new Vec2(60f, 1040f), Jumping, 0f, JumpDraw, 0.5f, 0f);
            h.Input.DisplayHeightPx = 720f;
            h.Input.PixelsPerMeter = 720f;
            Assert.That(h.Settle().State, Is.EqualTo(MascotState.Idle));

            Assert.That(h.Steps(30).State, Is.EqualTo(MascotState.Jump));
            BrainOutput landed = h.StepUntil(o => o.State != MascotState.Jump);

            Assert.That(landed.State, Is.EqualTo(MascotState.Landing));
            Assert.That(landed.FeetPx.y, Is.EqualTo(982f));
            Assert.That(landed.FeetPx.x, Is.EqualTo(1858f).Within(0.01f));
        }

        [TestCase(MotionAvailability.Idle | MotionAvailability.Fall)]
        [TestCase(MotionAvailability.Idle | MotionAvailability.Land)]
        public void DoesNotJumpWithoutBothFallAndLand(MotionAvailability motions)
        {
            var h = new BrainHarness(TestDesk.FixedDwell(1f), TestDesk.Desk(Reachable), new Vec2(700f, 1040f), motions, 0f, JumpDraw, 0.5f, 0f);
            h.Settle();

            AssertNeverJumps(h, 120);
        }

        [Test]
        public void FxLocomotionLockPreventsJumping()
        {
            BrainHarness h = Scene(0.5f, Reachable);
            h.Input.LocomotionBlockedByFx = true;

            AssertNeverJumps(h, 120);
        }

        private static BrainHarness Scene(float targetDraw, params Surface[] windows)
        {
            var h = new BrainHarness(TestDesk.FixedDwell(1f), TestDesk.Desk(windows), new Vec2(700f, 1040f), Jumping, 0f, JumpDraw, targetDraw, 0f);
            Assert.That(h.Settle().State, Is.EqualTo(MascotState.Idle));
            return h;
        }

        private static void AssertNeverJumps(BrainHarness h, int ticks)
        {
            for (int k = 0; k < ticks; k++)
            {
                Assert.That(h.Step().State, Is.EqualTo(MascotState.Idle));
            }
        }
    }
}
