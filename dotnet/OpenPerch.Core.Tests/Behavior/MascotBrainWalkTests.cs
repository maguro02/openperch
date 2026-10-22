using System;
using NUnit.Framework;
using OpenPerch.App.Core;

namespace OpenPerch.Core.Tests.Behavior
{
    [Category("T-BRAIN")]
    public class MascotBrainWalkTests
    {
        // Random draws after landing: dwell, action (0.1 → walk), direction (0.9 → right), distance (0.5 → 0.95 × height = 342 px).
        private static readonly float[] WalkRight342 = { 0f, 0.1f, 0.9f, 0.5f };

        private const MotionAvailability Walking = MotionAvailability.Idle | MotionAvailability.Walk | MotionAvailability.Fall;

        [Test]
        public void WalksTheDrawnDistanceThenIdles()
        {
            var h = new BrainHarness(TestDesk.FixedDwell(1f), TestDesk.Desk(), new Vec2(700f, 1040f), Walking, WalkRight342);
            h.Settle();

            BrainOutput start = h.Steps(30);
            Assert.That(start.State, Is.EqualTo(MascotState.Walk));
            Assert.That(start.Role, Is.EqualTo(LocomotionRole.Walk));
            Assert.That(start.Facing, Is.EqualTo(1));

            BrainOutput second = h.Step();
            Assert.That(second.FeetPx.x - start.FeetPx.x, Is.EqualTo(TestDesk.WalkSpeedPx * BrainHarness.Dt).Within(0.001f));

            BrainOutput end = h.StepUntil(o => o.State != MascotState.Walk);
            Assert.That(end.State, Is.EqualTo(MascotState.Idle));
            Assert.That(end.FeetPx.x, Is.EqualTo(1042f).Within(0.05f));
            Assert.That(end.FeetPx.y, Is.EqualTo(1040f));
        }

        [Test]
        public void TurnsAroundAtTheScreenEdge()
        {
            var h = new BrainHarness(TestDesk.FixedDwell(1f), TestDesk.Desk(), new Vec2(1880f, 1040f), Walking, WalkRight342);
            h.Settle();
            Assert.That(h.Steps(30).State, Is.EqualTo(MascotState.Walk));

            float rightmost = 0f;
            bool turned = false;
            BrainOutput o = h.Step();
            while (o.State == MascotState.Walk)
            {
                rightmost = Math.Max(rightmost, o.FeetPx.x);
                turned |= o.Facing == -1;
                o = h.Step();
            }

            Assert.That(rightmost, Is.InRange(1914f, 1919f));
            Assert.That(turned, Is.True);
            // 39 px to the edge, then the remaining 303 px back.
            Assert.That(o.FeetPx.x, Is.EqualTo(1616f).Within(0.05f));
        }

        [Test]
        public void FallsAfterWalkingPastTheEndOfAWindowTop()
        {
            var h = new BrainHarness(
                TestDesk.FixedDwell(1f), TestDesk.Desk(TestDesk.Window(101, 600, 400, 1000)), new Vec2(950f, 600f), Walking, WalkRight342);
            h.Settle();
            Assert.That(h.Steps(30).State, Is.EqualTo(MascotState.Walk));

            BrainOutput off = h.StepUntil(o => o.State != MascotState.Walk);
            Assert.That(off.State, Is.EqualTo(MascotState.Falling));
            Assert.That(off.FeetPx.x, Is.InRange(1000f, 1005f));
            Assert.That(off.Facing, Is.EqualTo(1));
            Assert.That(off.Role, Is.EqualTo(LocomotionRole.Fall));

            BrainOutput landed = h.StepUntil(o => o.State != MascotState.Falling);
            Assert.That(landed.FeetPx.y, Is.EqualTo(1040f));
            Assert.That(landed.FeetPx.x, Is.GreaterThan(1050f));
        }

        [Test]
        public void FxLocomotionLockStopsAWalk()
        {
            var h = new BrainHarness(TestDesk.FixedDwell(1f), TestDesk.Desk(), new Vec2(700f, 1040f), Walking, WalkRight342);
            h.Settle();
            h.Steps(35);
            Assert.That(h.Last.State, Is.EqualTo(MascotState.Walk));
            float x = h.Last.FeetPx.x;

            h.Input.LocomotionBlockedByFx = true;
            BrainOutput stopped = h.Step();

            Assert.That(stopped.State, Is.EqualTo(MascotState.Idle));
            Assert.That(stopped.FeetPx.x, Is.EqualTo(x));
        }

        [Test]
        public void FxLocomotionLockKeepsAWalkFromStarting()
        {
            var h = new BrainHarness(TestDesk.FixedDwell(1f), TestDesk.Desk(), new Vec2(700f, 1040f), Walking, WalkRight342);
            h.Input.LocomotionBlockedByFx = true;
            h.Settle();

            for (int k = 0; k < 200; k++)
            {
                Assert.That(h.Step().State, Is.EqualTo(MascotState.Idle));
            }

            Assert.That(h.Last.FeetPx.x, Is.EqualTo(700f));
        }
    }
}
