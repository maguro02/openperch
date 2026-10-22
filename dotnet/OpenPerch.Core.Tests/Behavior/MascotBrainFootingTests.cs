using NUnit.Framework;
using OpenPerch.App.Core;

namespace OpenPerch.Core.Tests.Behavior
{
    [Category("T-BRAIN")]
    public class MascotBrainFootingTests
    {
        private static readonly Surface Platform = TestDesk.Window(101, 600, 400, 1000);

        // Another window covering x in [650, 750) just above the edge splits the walkable span around the feet at 700.
        private static readonly SurfaceSet Covered =
            TestDesk.Snapshot(2, TestDesk.Window(101, 600, 400, 650), TestDesk.Window(101, 600, 750, 1000));

        [Test]
        public void FallsOnceTheFootingHasBeenCoveredForHalfASecond()
        {
            BrainHarness h = IdleOnPlatform();
            h.SetSurfaces(Covered);

            Assert.That(h.Steps(14).State, Is.EqualTo(MascotState.Idle));
            Assert.That(h.Step().State, Is.EqualTo(MascotState.Falling));

            BrainOutput landed = h.StepUntil(o => o.State != MascotState.Falling);
            Assert.That(landed.FeetPx.y, Is.EqualTo(1040f));
        }

        [Test]
        public void BriefCoveringsDoNotAddUp()
        {
            BrainHarness h = IdleOnPlatform();

            h.SetSurfaces(Covered);
            h.Steps(14);
            h.SetSurfaces(TestDesk.Snapshot(3, Platform));
            h.Step();
            h.SetSurfaces(Covered);

            Assert.That(h.Steps(14).State, Is.EqualTo(MascotState.Idle));
        }

        [Test]
        public void SittingOnAnEdgeAlsoFallsWhenTheFootingStaysCovered()
        {
            var h = new BrainHarness(
                TestDesk.FixedDwell(1f), TestDesk.Desk(Platform), new Vec2(700f, 600f), MotionAvailability.Idle | MotionAvailability.SitOnEdge, 0f, 0.4f, 0f);
            h.Settle();
            Assert.That(h.Steps(30).State, Is.EqualTo(MascotState.SitEdge));
            h.SetSurfaces(Covered);

            Assert.That(h.Steps(14).State, Is.EqualTo(MascotState.SitEdge));
            BrainOutput falling = h.Step();

            Assert.That(falling.State, Is.EqualTo(MascotState.Falling));
            Assert.That(falling.RootPx.y, Is.EqualTo(falling.FeetPx.y));
        }

        [Test]
        public void HardLossFallsAtOnceThroughTheWindowStillInTheSnapshot()
        {
            BrainHarness h = IdleOnPlatform();
            h.Step();

            h.Input.StandingLostHard = true;
            BrainOutput falling = h.Step();
            Assert.That(falling.State, Is.EqualTo(MascotState.Falling));

            BrainOutput landed = h.StepUntil(o => o.State != MascotState.Falling);
            Assert.That(landed.FeetPx.y, Is.EqualTo(1040f));
        }

        private static BrainHarness IdleOnPlatform()
        {
            var h = new BrainHarness(TestDesk.FixedDwell(100f), TestDesk.Desk(Platform), new Vec2(700f, 600f), MotionAvailability.Idle, 0f);
            Assert.That(h.Settle().State, Is.EqualTo(MascotState.Idle));
            Assert.That(h.Input.Standing.IsNone, Is.False);
            return h;
        }
    }
}
