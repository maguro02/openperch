using NUnit.Framework;
using OpenPerch.App.Core;

namespace OpenPerch.Core.Tests.Behavior
{
    [Category("T-BRAIN")]
    public class MascotBrainPomodoroTests
    {
        private const MotionAvailability Everything =
            MotionAvailability.Idle | MotionAvailability.IdleVariant | MotionAvailability.Walk | MotionAvailability.SitOnFloor
            | MotionAvailability.Fall | MotionAvailability.Land;

        // Default weights walk 1, sit 0.6, idle 1, variant 0.3, jump 0.4. Focus scales walk by 0.2 and drops jump;
        // break scales walk, variant and jump by 1.5. Each draw lands well inside one action's share.
        [TestCase(PomodoroMode.None, 0.1f, MascotState.Walk, LocomotionRole.Walk)]
        [TestCase(PomodoroMode.None, 0.33f, MascotState.SitFloor, LocomotionRole.SitOnFloor)]
        [TestCase(PomodoroMode.None, 0.77f, MascotState.Idle, LocomotionRole.Idle)]
        [TestCase(PomodoroMode.None, 0.867f, MascotState.Idle, LocomotionRole.IdleVariant)]
        [TestCase(PomodoroMode.None, 0.99f, MascotState.Jump, LocomotionRole.Fall)]
        [TestCase(PomodoroMode.Focus, 0.05f, MascotState.Walk, LocomotionRole.Walk)]
        [TestCase(PomodoroMode.Focus, 0.1f, MascotState.SitFloor, LocomotionRole.SitOnFloor)]
        [TestCase(PomodoroMode.Focus, 0.99f, MascotState.Idle, LocomotionRole.IdleVariant)]
        [TestCase(PomodoroMode.Break, 0.33f, MascotState.Walk, LocomotionRole.Walk)]
        [TestCase(PomodoroMode.Break, 0.77f, MascotState.Idle, LocomotionRole.IdleVariant)]
        [TestCase(PomodoroMode.Break, 0.867f, MascotState.Jump, LocomotionRole.Fall)]
        public void ModeReweightsTheIdleDecision(PomodoroMode mode, float actionDraw, MascotState state, LocomotionRole role)
        {
            var h = new BrainHarness(
                TestDesk.FixedDwell(1f),
                TestDesk.Desk(TestDesk.Window(1, 740, 600, 900)),
                new Vec2(700f, 1040f),
                Everything,
                0f, actionDraw, 0.5f, 0.5f);
            h.Input.Pomodoro = mode;
            h.Settle();
            Assert.That(h.Steps(29).Role, Is.EqualTo(LocomotionRole.Idle));

            BrainOutput decided = h.Step();

            Assert.That(decided.State, Is.EqualTo(state));
            Assert.That(decided.Role, Is.EqualTo(role));
        }

        [TestCase(900f, 1)]
        [TestCase(450f, -1)]
        public void FocusWalksOffTheNearerEndOfAWindowTopDownToTheFloor(float startX, int direction)
        {
            var h = new BrainHarness(
                TestDesk.FixedDwell(1f),
                TestDesk.Desk(TestDesk.Window(101, 600, 400, 1000)),
                new Vec2(startX, 600f),
                MotionAvailability.Idle | MotionAvailability.Walk | MotionAvailability.Fall,
                0f);
            h.Input.Pomodoro = PomodoroMode.Focus;
            h.Settle();

            BrainOutput walking = h.Steps(30);
            Assert.That(walking.State, Is.EqualTo(MascotState.Walk));
            Assert.That(walking.Facing, Is.EqualTo(direction));

            BrainOutput off = h.StepUntil(o => o.State != MascotState.Walk);
            Assert.That(off.State, Is.EqualTo(MascotState.Falling));

            BrainOutput landed = h.StepUntil(o => o.State != MascotState.Falling);
            Assert.That(landed.FeetPx.y, Is.EqualTo(1040f));
        }
    }
}
