using NUnit.Framework;
using OpenPerch.App.Core;

namespace OpenPerch.Core.Tests.Behavior
{
    [Category("T-BRAIN")]
    public class MascotBrainOneShotTests
    {
        [Test]
        public void LandingPlaysLandUntilTheOneShotFinishes()
        {
            var h = new BrainHarness(
                TestDesk.FixedDwell(100f), TestDesk.Desk(), new Vec2(700f, 1000f), MotionAvailability.Idle | MotionAvailability.Fall | MotionAvailability.Land, 0f);

            BrainOutput landed = h.StepUntil(o => o.State != MascotState.Falling);
            Assert.That(landed.State, Is.EqualTo(MascotState.Landing));
            Assert.That(landed.Role, Is.EqualTo(LocomotionRole.Land));
            Assert.That(landed.FeetPx.y, Is.EqualTo(1040f));

            Assert.That(h.Steps(60).State, Is.EqualTo(MascotState.Landing));

            BrainOutput idle = h.FinishOneShot();
            Assert.That(idle.State, Is.EqualTo(MascotState.Idle));
            Assert.That(idle.Role, Is.EqualTo(LocomotionRole.Idle));
        }

        [Test]
        public void IdleVariantPlaysUntilTheOneShotFinishes()
        {
            // 0.867 draws the idle variant with the default weights.
            var h = new BrainHarness(
                TestDesk.FixedDwell(1f), TestDesk.Desk(), new Vec2(700f, 1040f), MotionAvailability.Idle | MotionAvailability.IdleVariant, 0f, 0.867f);
            h.Settle();

            BrainOutput playing = h.Steps(30);
            Assert.That(playing.State, Is.EqualTo(MascotState.Idle));
            Assert.That(playing.Role, Is.EqualTo(LocomotionRole.IdleVariant));

            Assert.That(h.Steps(60).Role, Is.EqualTo(LocomotionRole.IdleVariant));

            Assert.That(h.FinishOneShot().Role, Is.EqualTo(LocomotionRole.Idle));
        }
    }
}
