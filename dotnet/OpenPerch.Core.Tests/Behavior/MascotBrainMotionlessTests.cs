using System;
using System.Collections.Generic;
using NUnit.Framework;
using OpenPerch.App.Core;

namespace OpenPerch.Core.Tests.Behavior
{
    [Category("T-BRAIN")]
    public class MascotBrainMotionlessTests
    {
        // With the default weights these draws pick walk, sit, idle, the idle variant and jump.
        private static readonly float[] OneDrawPerAction = { 0.1f, 0.33f, 0.77f, 0.867f, 0.99f };

        [Test]
        public void OnlyIdleDraggedAndFallingOccurWithoutMotions()
        {
            var h = new BrainHarness(
                TestDesk.FixedDwell(1f),
                TestDesk.Desk(TestDesk.Window(101, 600, 400, 1000), TestDesk.Window(102, 840, 1200, 1700)),
                new Vec2(960f, 1040f),
                MotionAvailability.None,
                OneDrawPerAction);
            var states = new HashSet<MascotState>();
            var roles = new HashSet<LocomotionRole>();
            var actionDraws = new HashSet<float>();

            for (int k = 0; k < 5400; k++)
            {
                if (k % 300 == 100)
                {
                    Record(h, () => h.Press(new Vec2(h.Input.FeetPx.x, h.Input.FeetPx.y - 200f)), states, roles, actionDraws);
                    for (int held = 0; held < 9; held++)
                    {
                        Record(h, h.Step, states, roles, actionDraws);
                    }

                    Record(h, () => h.Release(new Vec2(500f, -800f)), states, roles, actionDraws);
                    continue;
                }

                Record(h, h.Step, states, roles, actionDraws);
            }

            Assert.That(actionDraws, Is.EquivalentTo(OneDrawPerAction));
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

        private static void Record(
            BrainHarness h, Func<BrainOutput> tick, HashSet<MascotState> states, HashSet<LocomotionRole> roles, HashSet<float> actionDraws)
        {
            int before = h.Random.Calls;
            BrainOutput o = tick();

            // Without motions a decision draws the action and then the next idle time; a landing draws the idle time only.
            if (h.Random.Calls - before == 2)
            {
                actionDraws.Add(OneDrawPerAction[before % OneDrawPerAction.Length]);
            }

            states.Add(o.State);
            roles.Add(o.Role);
        }
    }
}
