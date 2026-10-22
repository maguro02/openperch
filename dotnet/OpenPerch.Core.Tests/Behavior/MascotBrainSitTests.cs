using NUnit.Framework;
using OpenPerch.App.Core;

namespace OpenPerch.Core.Tests.Behavior
{
    [Category("T-BRAIN")]
    public class MascotBrainSitTests
    {
        // Random draws after landing: dwell, then 0.4 → sit, then the sitting time.
        private static readonly float[] SitDraws = { 0f, 0.4f, 0f };

        [Test]
        public void SitsOnAWindowEdgeWithTheRootTheSeatHeightBelowIt()
        {
            var h = new BrainHarness(
                TestDesk.FixedDwell(1f),
                TestDesk.Desk(TestDesk.Window(101, 600, 400, 1000)),
                new Vec2(700f, 600f),
                MotionAvailability.Idle | MotionAvailability.SitOnEdge,
                SitDraws);
            h.Settle();

            BrainOutput sitting = h.Steps(30);

            Assert.That(sitting.State, Is.EqualTo(MascotState.SitEdge));
            Assert.That(sitting.Role, Is.EqualTo(LocomotionRole.SitOnEdge));
            Assert.That(sitting.FeetPx.y, Is.EqualTo(600f));
            Assert.That(sitting.RootPx.x, Is.EqualTo(700f));
            Assert.That(sitting.RootPx.y, Is.EqualTo(600f + TestDesk.SeatHeightPx));
            Assert.That(sitting.Facing, Is.EqualTo(0));
        }

        // The floor is at 1040 and the legs need 100 px below the edge.
        [TestCase(940, MascotState.SitEdge)]
        [TestCase(980, MascotState.Idle)]
        public void SitsOnAnEdgeOnlyWithRoomForTheLegsBelowIt(int edgeY, MascotState expected)
        {
            var h = new BrainHarness(
                TestDesk.FixedDwell(1f),
                TestDesk.Desk(TestDesk.Window(101, edgeY, 400, 1000)),
                new Vec2(700f, edgeY),
                MotionAvailability.Idle | MotionAvailability.SitOnEdge,
                SitDraws);
            h.Settle();

            Assert.That(h.Steps(30).State, Is.EqualTo(expected));
        }

        [Test]
        public void SittingOnAnEdgeNeedsTheSitOnEdgeMotion()
        {
            var h = new BrainHarness(
                TestDesk.FixedDwell(1f),
                TestDesk.Desk(TestDesk.Window(101, 600, 400, 1000)),
                new Vec2(700f, 600f),
                MotionAvailability.Idle | MotionAvailability.SitOnFloor,
                SitDraws);
            h.Settle();

            Assert.That(h.Steps(30).State, Is.EqualTo(MascotState.Idle));
        }

        [Test]
        public void SitsOnTheFloorWithTheRootAtTheFeet()
        {
            var h = new BrainHarness(
                TestDesk.FixedDwell(1f), TestDesk.Desk(), new Vec2(700f, 1040f), MotionAvailability.Idle | MotionAvailability.SitOnFloor, SitDraws);
            h.Settle();

            BrainOutput sitting = h.Steps(30);

            Assert.That(sitting.State, Is.EqualTo(MascotState.SitFloor));
            Assert.That(sitting.Role, Is.EqualTo(LocomotionRole.SitOnFloor));
            Assert.That(sitting.RootPx.x, Is.EqualTo(sitting.FeetPx.x));
            Assert.That(sitting.RootPx.y, Is.EqualTo(sitting.FeetPx.y));
        }

        [Test]
        public void TheFloorNeverUsesTheEdgeSittingMotion()
        {
            var h = new BrainHarness(
                TestDesk.FixedDwell(1f), TestDesk.Desk(), new Vec2(700f, 1040f), MotionAvailability.Idle | MotionAvailability.SitOnEdge, SitDraws);
            h.Settle();

            Assert.That(h.Steps(30).State, Is.EqualTo(MascotState.Idle));
        }

        [Test]
        public void StandsUpWhenTheSittingTimeRunsOut()
        {
            var h = new BrainHarness(
                TestDesk.FixedDwell(1f),
                TestDesk.Desk(TestDesk.Window(101, 600, 400, 1000)),
                new Vec2(700f, 600f),
                MotionAvailability.Idle | MotionAvailability.SitOnEdge,
                SitDraws);
            h.Settle();
            Assert.That(h.Steps(30).State, Is.EqualTo(MascotState.SitEdge));

            Assert.That(h.Steps(29).State, Is.EqualTo(MascotState.SitEdge));
            BrainOutput standing = h.Step();

            Assert.That(standing.State, Is.EqualTo(MascotState.Idle));
            Assert.That(standing.RootPx.y, Is.EqualTo(600f));
        }
    }
}
