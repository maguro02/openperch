using System;
using NUnit.Framework;
using OpenPerch.App.Core;

namespace OpenPerch.Core.Tests.Behavior
{
    [Category("T-BRAIN")]
    public class MascotBrainFallingTests
    {
        [Test]
        public void StartsFallingAndLandsOnTheFloor()
        {
            var h = new BrainHarness(TestDesk.FixedDwell(100f), TestDesk.Desk(), new Vec2(960f, 1000f), MotionAvailability.None, 0.5f);

            Assert.That(h.Step().State, Is.EqualTo(MascotState.Falling));
            BrainOutput landed = h.StepUntil(o => o.State != MascotState.Falling, 30);

            Assert.That(landed.State, Is.EqualTo(MascotState.Idle));
            Assert.That(landed.FeetPx.x, Is.EqualTo(960f));
            Assert.That(landed.FeetPx.y, Is.EqualTo(1040f));
        }

        [Test]
        public void LandsOnAWindowTopWhenTheFeetCrossItsSpan()
        {
            var h = new BrainHarness(
                TestDesk.FixedDwell(100f), TestDesk.Desk(TestDesk.Window(101, 600, 400, 1000)), new Vec2(700f, 300f), MotionAvailability.None, 0.5f);

            BrainOutput landed = h.StepUntil(o => o.State != MascotState.Falling);

            Assert.That(landed.State, Is.EqualTo(MascotState.Idle));
            Assert.That(landed.FeetPx.x, Is.EqualTo(700f));
            Assert.That(landed.FeetPx.y, Is.EqualTo(600f));
        }

        [Test]
        public void FallsPastAWindowTopWhenTheFeetMissItsSpan()
        {
            var h = new BrainHarness(
                TestDesk.FixedDwell(100f), TestDesk.Desk(TestDesk.Window(101, 600, 400, 1000)), new Vec2(300f, 300f), MotionAvailability.None, 0.5f);

            BrainOutput landed = h.StepUntil(o => o.State != MascotState.Falling);

            Assert.That(landed.FeetPx.y, Is.EqualTo(1040f));
        }

        [Test]
        public void SweepCatchesASurfaceThatLiesBetweenTwoTicks()
        {
            var h = new BrainHarness(
                TestDesk.FixedDwell(100f), TestDesk.Desk(TestDesk.Window(101, 600, 400, 1000)), new Vec2(700f, 1040f), MotionAvailability.None, 0.5f);
            h.Settle();
            h.Press(new Vec2(700f, 130f));
            h.Release(new Vec2(0f, 4000f));

            // At terminal speed the feet advance 100 px per tick: 130, 230, ..., 530, then 630 if nothing stopped them.
            BrainOutput landed = h.StepUntil(o => o.State != MascotState.Falling, 10);

            Assert.That(landed.FeetPx.y, Is.EqualTo(600f));
            Assert.That(landed.FeetPx.x, Is.EqualTo(700f));
        }

        [Test]
        public void LandsWhereThePathOfTheFeetCrossesTheSurface()
        {
            // Thrown sideways from (300, 500) at 600 px/s, the feet fall 100 px in √(200 / g) ≈ 0.2915 s and cross y = 600
            // at x ≈ 474.87, in the tick that runs from x = 460 to x = 480. The straight line between that tick's ends
            // would cross at x ≈ 474.65. Only the crossing lies in [400, 477).
            var h = new BrainHarness(
                TestDesk.FixedDwell(100f), TestDesk.Desk(TestDesk.Window(101, 600, 400, 477)), new Vec2(300f, 1040f), MotionAvailability.None, 0.5f);
            h.Settle();
            h.Press(new Vec2(300f, 500f));
            h.Release(new Vec2(600f, 0f));

            BrainOutput landed = h.StepUntil(o => o.State != MascotState.Falling);

            Assert.That(landed.FeetPx.y, Is.EqualTo(600f));
            Assert.That(landed.FeetPx.x, Is.EqualTo(474.87f).Within(0.05f));
        }

        [Test]
        public void PassesASpanThatOnlyTheEndOfTheTickIsAbove()
        {
            // Same throw: the crossing at x ≈ 474.87 is left of [478, 1000) although the tick ends at x = 480 inside it.
            var h = new BrainHarness(
                TestDesk.FixedDwell(100f), TestDesk.Desk(TestDesk.Window(101, 600, 478, 1000)), new Vec2(300f, 1040f), MotionAvailability.None, 0.5f);
            h.Settle();
            h.Press(new Vec2(300f, 500f));
            h.Release(new Vec2(600f, 0f));

            BrainOutput landed = h.StepUntil(o => o.State != MascotState.Falling);

            Assert.That(landed.FeetPx.y, Is.EqualTo(1040f));
        }

        [Test]
        public void IgnoresAWindowTopWithLessHeadroomThanTheAvatarIsTall()
        {
            var h = new BrainHarness(
                TestDesk.FixedDwell(100f),
                TestDesk.Desk(TestDesk.Window(102, 200, 400, 1000, 200)),
                new Vec2(700f, 50f),
                MotionAvailability.None,
                0.5f);

            BrainOutput landed = h.StepUntil(o => o.State != MascotState.Falling);

            Assert.That(landed.FeetPx.y, Is.EqualTo(1040f));
        }

        [Test]
        public void RisesThroughAWindowTopAndLandsOnItOnTheWayDown()
        {
            var h = new BrainHarness(
                TestDesk.FixedDwell(100f), TestDesk.Desk(TestDesk.Window(101, 600, 400, 1000)), new Vec2(700f, 1040f), MotionAvailability.None, 0.5f);
            h.Settle();
            h.Press(new Vec2(700f, 700f));
            h.Release(new Vec2(0f, -1500f));

            float highest = 700f;
            BrainOutput o = h.Step();
            while (o.State == MascotState.Falling)
            {
                highest = Math.Min(highest, o.FeetPx.y);
                o = h.Step();
            }

            Assert.That(highest, Is.LessThan(300f));
            Assert.That(o.FeetPx.y, Is.EqualTo(600f));
        }

        [Test]
        public void ScreenEdgeStopsAThrowAndTurnsItAround()
        {
            var h = new BrainHarness(TestDesk.FixedDwell(100f), TestDesk.Desk(), new Vec2(1800f, 1040f), MotionAvailability.None, 0.5f);
            h.Settle();
            h.Press(new Vec2(1800f, 100f));
            Assert.That(h.Release(new Vec2(3000f, 0f)).Facing, Is.EqualTo(1));

            Assert.That(h.Step().FeetPx.x, Is.EqualTo(1900f).Within(0.01f));
            BrainOutput atEdge = h.Step();
            BrainOutput back = h.Step();

            Assert.That(atEdge.FeetPx.x, Is.EqualTo(1919f));
            Assert.That(atEdge.Facing, Is.EqualTo(-1));
            Assert.That(back.FeetPx.x, Is.EqualTo(1819f).Within(0.01f));

            BrainOutput landed = h.StepUntil(o => o.State != MascotState.Falling);
            Assert.That(landed.FeetPx.y, Is.EqualTo(1040f));
            Assert.That(landed.FeetPx.x, Is.InRange(0f, 1919f));
        }
    }
}
