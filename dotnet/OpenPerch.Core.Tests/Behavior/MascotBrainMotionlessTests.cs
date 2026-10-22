using System.Collections.Generic;
using NUnit.Framework;
using OpenPerch.App.Core;

namespace OpenPerch.Core.Tests.Behavior
{
    [Category("T-BRAIN")]
    public class MascotBrainMotionlessTests
    {
        [Test]
        public void OnlyIdleDraggedAndFallingOccurWithoutMotions()
        {
            var h = new BrainHarness(
                new BehaviorConfig(),
                TestDesk.Desk(TestDesk.Window(101, 600, 400, 1000), TestDesk.Window(102, 840, 1200, 1700)),
                new Vec2(960f, 1040f),
                MotionAvailability.None,
                Draws(97));
            var states = new HashSet<MascotState>();
            var roles = new HashSet<LocomotionRole>();

            for (int k = 0; k < 5400; k++)
            {
                if (k % 300 == 100)
                {
                    Record(h.Press(new Vec2(h.Input.FeetPx.x, h.Input.FeetPx.y - 200f)), states, roles);
                    for (int held = 0; held < 9; held++)
                    {
                        Record(h.Step(), states, roles);
                    }

                    Record(h.Release(new Vec2(500f, -800f)), states, roles);
                    continue;
                }

                Record(h.Step(), states, roles);
            }

            Assert.That(states, Is.SubsetOf(new[] { MascotState.Idle, MascotState.Dragged, MascotState.Falling }));
            Assert.That(states, Does.Contain(MascotState.Dragged).And.Contain(MascotState.Falling));
            Assert.That(roles, Is.EquivalentTo(new[] { LocomotionRole.Idle }));
        }

        [Test]
        public void LandsStraightIntoIdleWithoutTheLandMotion()
        {
            var h = new BrainHarness(TestDesk.FixedDwell(100f), TestDesk.Desk(), new Vec2(700f, 900f), MotionAvailability.Idle | MotionAvailability.Fall, 0f);

            BrainOutput landed = h.StepUntil(o => o.State != MascotState.Falling);

            Assert.That(landed.State, Is.EqualTo(MascotState.Idle));
            Assert.That(landed.Role, Is.EqualTo(LocomotionRole.Idle));
        }

        [Test]
        public void FallingWithoutTheFallMotionPlaysIdle()
        {
            var h = new BrainHarness(TestDesk.FixedDwell(100f), TestDesk.Desk(), new Vec2(700f, 900f), MotionAvailability.Idle, 0f);

            BrainOutput falling = h.Step();

            Assert.That(falling.State, Is.EqualTo(MascotState.Falling));
            Assert.That(falling.Role, Is.EqualTo(LocomotionRole.Idle));
        }

        [Test]
        public void SkipsTheIdleVariantWithoutItsMotion()
        {
            // 0.867 draws the idle variant with the default weights.
            var h = new BrainHarness(TestDesk.FixedDwell(1f), TestDesk.Desk(), new Vec2(700f, 1040f), MotionAvailability.Idle, 0f, 0.867f);
            h.Settle();

            BrainOutput decided = h.Steps(30);

            Assert.That(decided.State, Is.EqualTo(MascotState.Idle));
            Assert.That(decided.Role, Is.EqualTo(LocomotionRole.Idle));
        }

        private static void Record(BrainOutput o, HashSet<MascotState> states, HashSet<LocomotionRole> roles)
        {
            states.Add(o.State);
            roles.Add(o.Role);
        }

        /// <summary>A fixed pseudo-random sequence in [0, 1), so the long run is reproducible.</summary>
        private static float[] Draws(int count)
        {
            var values = new float[count];
            long x = 12345;
            for (int k = 0; k < count; k++)
            {
                x = (1103515245L * x + 12345L) % 2147483648L;
                values[k] = (x % 10000L) / 10000f;
            }

            return values;
        }
    }
}
