using NUnit.Framework;
using OpenPerch.App.Core;

namespace OpenPerch.Core.Tests.Behavior
{
    [Category("T-BRAIN")]
    public class MascotBrainHiddenTests
    {
        private static readonly Surface Platform = TestDesk.Window(101, 600, 400, 1000);

        [TestCase(MascotState.Idle)]
        [TestCase(MascotState.Walk)]
        [TestCase(MascotState.SitEdge)]
        [TestCase(MascotState.Falling)]
        [TestCase(MascotState.Dragged)]
        public void HidesFromAnyState(MascotState from)
        {
            BrainHarness h = InState(from);

            h.Input.Fullscreen = true;
            BrainOutput hidden = h.Step();

            Assert.That(hidden.State, Is.EqualTo(MascotState.Hidden));
            Assert.That(hidden.RoleSpeed, Is.EqualTo(0f));
        }

        [Test]
        public void KeepsTheRoleAndTheSeatedRootWhileHidden()
        {
            BrainHarness h = InState(MascotState.SitEdge);

            h.Input.Fullscreen = true;
            BrainOutput hidden = h.Steps(5);

            Assert.That(hidden.Role, Is.EqualTo(LocomotionRole.SitOnEdge));
            Assert.That(hidden.RootPx.y, Is.EqualTo(600f + TestDesk.SeatHeightPx));
        }

        [Test]
        public void ReturnsToIdleTwoSecondsAfterUnhidingWhenTheFootingRemains()
        {
            BrainHarness h = InState(MascotState.Idle);
            h.Input.Fullscreen = true;
            h.Steps(10);

            h.Input.Fullscreen = false;
            Assert.That(h.Steps(59).State, Is.EqualTo(MascotState.Hidden));
            BrainOutput back = h.Step();

            Assert.That(back.State, Is.EqualTo(MascotState.Idle));
            Assert.That(back.FeetPx.y, Is.EqualTo(600f));
            Assert.That(back.RoleSpeed, Is.EqualTo(1f));
        }

        [Test]
        public void FallsTwoSecondsAfterUnhidingWhenTheFootingIsGone()
        {
            BrainHarness h = InState(MascotState.Idle);
            h.Input.Fullscreen = true;
            h.Step();
            h.SetSurfaces(TestDesk.Snapshot(2));

            h.Input.Fullscreen = false;
            Assert.That(h.Steps(59).State, Is.EqualTo(MascotState.Hidden));
            Assert.That(h.Step().State, Is.EqualTo(MascotState.Falling));

            BrainOutput landed = h.StepUntil(o => o.State != MascotState.Falling);
            Assert.That(landed.FeetPx.y, Is.EqualTo(1040f));
        }

        [Test]
        public void HidingAgainRestartsTheTwoSeconds()
        {
            BrainHarness h = InState(MascotState.Idle);
            h.Input.Fullscreen = true;
            h.Step();

            h.Input.Fullscreen = false;
            h.Steps(45);
            h.Input.Fullscreen = true;
            h.Step();
            h.Input.Fullscreen = false;

            Assert.That(h.Steps(59).State, Is.EqualTo(MascotState.Hidden));
            Assert.That(h.Step().State, Is.EqualTo(MascotState.Idle));
        }

        [Test]
        public void IgnoresPressesWhileHidden()
        {
            BrainHarness h = InState(MascotState.Idle);
            h.Input.Fullscreen = true;
            h.Step();

            Assert.That(h.Press(new Vec2(700f, 400f)).State, Is.EqualTo(MascotState.Hidden));
        }

        [Test]
        public void StaysPutWhileHiddenInMidAir()
        {
            BrainHarness h = InState(MascotState.Falling);
            float y = h.Last.FeetPx.y;

            h.Input.Fullscreen = true;
            Assert.That(h.Steps(30).FeetPx.y, Is.EqualTo(y));
        }

        private static BrainHarness InState(MascotState state)
        {
            var h = new BrainHarness(
                TestDesk.FixedDwell(1f),
                TestDesk.Desk(Platform),
                new Vec2(700f, state == MascotState.Falling ? 100f : 600f),
                MotionAvailability.Idle | MotionAvailability.Walk | MotionAvailability.SitOnEdge | MotionAvailability.Fall,
                0f, state == MascotState.SitEdge ? 0.4f : 0.1f, 0.9f, 0.5f);
            switch (state)
            {
                case MascotState.Falling:
                    h.Step();
                    break;
                case MascotState.Dragged:
                    h.Settle();
                    h.Press(new Vec2(700f, 500f));
                    break;
                case MascotState.Walk:
                case MascotState.SitEdge:
                    h.Settle();
                    h.Steps(30);
                    break;
                default:
                    h.Settle();
                    break;
            }

            Assert.That(h.Last.State, Is.EqualTo(state));
            return h;
        }
    }
}
