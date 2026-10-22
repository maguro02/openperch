using NUnit.Framework;
using OpenPerch.App.Core;

namespace OpenPerch.Core.Tests.Behavior
{
    [Category("T-BRAIN")]
    public class MascotBrainDragTests
    {
        [Test]
        public void PressOnTheAvatarStartsADragThatFollowsTheOwnersPlacement()
        {
            BrainHarness h = IdleOnFloor(MotionAvailability.Idle | MotionAvailability.Dragged);

            BrainOutput grabbed = h.Press(new Vec2(500f, 300f));
            Assert.That(grabbed.State, Is.EqualTo(MascotState.Dragged));
            Assert.That(grabbed.Role, Is.EqualTo(LocomotionRole.Dragged));

            h.Input.FeetPx = new Vec2(520f, 310f);
            BrainOutput moved = h.Steps(10);

            Assert.That(moved.State, Is.EqualTo(MascotState.Dragged));
            Assert.That(moved.FeetPx.x, Is.EqualTo(520f));
            Assert.That(moved.FeetPx.y, Is.EqualTo(310f));
            Assert.That(moved.RootPx.y, Is.EqualTo(310f));
        }

        [Test]
        public void PressOffTheAvatarDoesNotDrag()
        {
            BrainHarness h = IdleOnFloor(MotionAvailability.Idle);
            h.Input.Pointer.LeftDown = true;
            h.Input.Pointer.LeftHeld = true;
            h.Input.PointerOnThisAvatar = false;

            Assert.That(h.Step().State, Is.EqualTo(MascotState.Idle));
        }

        [Test]
        public void CatchesTheMascotInMidAir()
        {
            var h = new BrainHarness(TestDesk.FixedDwell(100f), TestDesk.Desk(), new Vec2(700f, 100f), MotionAvailability.None, 0f);
            h.Steps(3);

            Assert.That(h.Press(h.Input.FeetPx).State, Is.EqualTo(MascotState.Dragged));
        }

        [Test]
        public void ReleaseThrowsWithTheCursorVelocity()
        {
            BrainHarness h = IdleOnFloor(MotionAvailability.Idle);
            h.Press(new Vec2(500f, 300f));

            BrainOutput thrown = h.Release(new Vec2(-900f, -300f));
            Assert.That(thrown.State, Is.EqualTo(MascotState.Falling));
            Assert.That(thrown.Facing, Is.EqualTo(-1));
            Assert.That(thrown.FeetPx.x, Is.EqualTo(500f));

            BrainOutput next = h.Step();
            float dt = BrainHarness.Dt;
            Assert.That(next.FeetPx.x, Is.EqualTo(500f - 900f * dt).Within(0.01f));
            Assert.That(next.FeetPx.y, Is.EqualTo(300f - 300f * dt + 0.5f * TestDesk.Gravity * dt * dt).Within(0.01f));
        }

        [Test]
        public void ThrowSpeedIsCappedAt4000PxPerSecond()
        {
            BrainHarness h = IdleOnFloor(MotionAvailability.Idle);
            h.Press(new Vec2(500f, 300f));
            h.Release(new Vec2(8000f, 0f));

            BrainOutput next = h.Step();

            Assert.That(next.FeetPx.x - 500f, Is.EqualTo(4000f * BrainHarness.Dt).Within(0.01f));
        }

        [Test]
        public void FallSpeedIsCappedAt3000PxPerSecond()
        {
            BrainHarness h = IdleOnFloor(MotionAvailability.Idle);
            h.Press(new Vec2(700f, 0f));
            h.Release(new Vec2(0f, 4000f));

            float y = 0f;
            BrainOutput o = h.Step();
            while (o.State == MascotState.Falling)
            {
                Assert.That(o.FeetPx.y - y, Is.EqualTo(3000f * BrainHarness.Dt).Within(0.01f));
                y = o.FeetPx.y;
                o = h.Step();
            }

            Assert.That(o.FeetPx.y, Is.EqualTo(1040f));
        }

        [TestCase(MotionAvailability.Dragged | MotionAvailability.Fall, LocomotionRole.Dragged)]
        [TestCase(MotionAvailability.Fall, LocomotionRole.Fall)]
        [TestCase(MotionAvailability.None, LocomotionRole.Idle)]
        public void DraggedFallsBackToFallThenIdle(MotionAvailability motions, LocomotionRole role)
        {
            BrainHarness h = IdleOnFloor(motions);

            Assert.That(h.Press(new Vec2(700f, 900f)).Role, Is.EqualTo(role));
        }

        private static BrainHarness IdleOnFloor(MotionAvailability motions)
        {
            var h = new BrainHarness(TestDesk.FixedDwell(100f), TestDesk.Desk(), new Vec2(700f, 1040f), motions, 0f);
            Assert.That(h.Settle().State, Is.EqualTo(MascotState.Idle));
            return h;
        }
    }
}
