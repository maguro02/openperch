using NUnit.Framework;
using OpenPerch.App.Core;
using static OpenPerch.Core.Tests.Menu.MenuFactory;

namespace OpenPerch.Core.Tests.Menu
{
    [TestFixture]
    [Category("T-MENU")]
    public class ExpressionMenuPuppetTests
    {
        [TestCase(ExpressionMenuControlType.RadialPuppet)]
        [TestCase(ExpressionMenuControlType.TwoAxisPuppet)]
        [TestCase(ExpressionMenuControlType.FourAxisPuppet)]
        public void ParameterIsOneOnlyWhileThePuppetIsOpen(ExpressionMenuControlType type)
        {
            var sink = new RecordingParamSink();
            var puppet = Puppet(type, "PuppetOpen", "A", "B", "C", "D");
            var menu = Controller(sink, Page(puppet));

            Assert.That(menu.Press(0), Is.True);
            Assert.That(menu.ActivePuppet, Is.SameAs(puppet));
            Assert.That(sink.Get("PuppetOpen"), Is.EqualTo(1f));

            Assert.That(menu.Back(), Is.True);
            Assert.That(menu.ActivePuppet, Is.Null);
            Assert.That(menu.CurrentPageIndex, Is.EqualTo(0));
            Assert.That(sink.Writes, Is.EqualTo(new[] { "PuppetOpen=1", "PuppetOpen=0" }));
        }

        [TestCase(0.25f, 0.25f)]
        [TestCase(0f, 0f)]
        [TestCase(1f, 1f)]
        [TestCase(1.5f, 1f)]
        [TestCase(-0.5f, 0f)]
        [TestCase(float.PositiveInfinity, 1f)]
        [TestCase(float.NegativeInfinity, 0f)]
        public void RadialIsClampedToZeroToOne(float input, float expected)
        {
            var sink = new RecordingParamSink();
            var menu = Controller(sink, Page(Puppet(ExpressionMenuControlType.RadialPuppet, "", "Size")));
            menu.Press(0);

            menu.SetRadial(input);

            Assert.That(sink.Get("Size"), Is.EqualTo(expected));
        }

        [TestCase(0.5f, -0.25f, 0.5f, -0.25f)]
        [TestCase(1f, -1f, 1f, -1f)]
        [TestCase(3f, -2f, 1f, -1f)]
        [TestCase(-1.5f, 1.5f, -1f, 1f)]
        public void TwoAxisIsClampedToMinusOneToOne(float x, float y, float expectedHorizontal, float expectedVertical)
        {
            var sink = new RecordingParamSink();
            var menu = Controller(sink, Page(Puppet(ExpressionMenuControlType.TwoAxisPuppet, "", "LookX", "LookY")));
            menu.Press(0);

            menu.SetAxes(new Vec2(x, y));

            Assert.That(sink.Get("LookX"), Is.EqualTo(expectedHorizontal));
            Assert.That(sink.Get("LookY"), Is.EqualTo(expectedVertical));
        }

        [TestCase(0.5f, 0.8f, 0.8f, 0.5f, 0f, 0f)]
        [TestCase(-2f, -0.3f, 0f, 0f, 0.3f, 1f)]
        [TestCase(0f, 0f, 0f, 0f, 0f, 0f)]
        public void FourAxisSplitsTheStickIntoDirectionsFromZeroToOne(
            float x, float y, float up, float right, float down, float left)
        {
            var sink = new RecordingParamSink();
            var menu = Controller(sink, Page(Puppet(ExpressionMenuControlType.FourAxisPuppet, "", "Up", "Right", "Down", "Left")));
            menu.Press(0);

            menu.SetAxes(new Vec2(x, y));

            Assert.That(sink.Get("Up"), Is.EqualTo(up));
            Assert.That(sink.Get("Right"), Is.EqualTo(right));
            Assert.That(sink.Get("Down"), Is.EqualTo(down));
            Assert.That(sink.Get("Left"), Is.EqualTo(left));
        }

        [Test]
        public void FourAxisWritesZeroNotNegativeZeroForACentredStick()
        {
            var sink = new RecordingParamSink();
            var menu = Controller(sink, Page(Puppet(ExpressionMenuControlType.FourAxisPuppet, "", "Up", "Right", "Down", "Left")));
            menu.Press(0);

            menu.SetAxes(new Vec2(0f, 0f));

            Assert.That(sink.Writes, Is.EqualTo(new[] { "Up=0", "Right=0", "Down=0", "Left=0" }));
        }

        [Test]
        public void ValuesAreKeptAfterClosingAndReopening()
        {
            var sink = new RecordingParamSink();
            var menu = Controller(sink,
                Page(Puppet(ExpressionMenuControlType.RadialPuppet, "SizeOpen", "Size"),
                    Puppet(ExpressionMenuControlType.TwoAxisPuppet, "LookOpen", "LookX", "LookY")));

            menu.Press(0);
            menu.SetRadial(0.6f);
            menu.Back();
            menu.Press(1);
            menu.SetAxes(new Vec2(-0.5f, 0.75f));
            menu.Close();

            Assert.That(sink.Get("SizeOpen"), Is.EqualTo(0f));
            Assert.That(sink.Get("LookOpen"), Is.EqualTo(0f));
            Assert.That(sink.Get("Size"), Is.EqualTo(0.6f));
            Assert.That(sink.Get("LookX"), Is.EqualTo(-0.5f));
            Assert.That(sink.Get("LookY"), Is.EqualTo(0.75f));

            sink.Writes.Clear();
            menu.Press(0);

            Assert.That(sink.Writes, Is.EqualTo(new[] { "SizeOpen=1" }), "reopening writes only the open flag");
            Assert.That(sink.Get("Size"), Is.EqualTo(0.6f));
        }

        [Test]
        public void ClosingTheMenuClosesThePuppetAndLeavesSubMenus()
        {
            var sink = new RecordingParamSink();
            var menu = Controller(sink,
                Page(SubMenu(1, "Outer", 1f)),
                Page(Puppet(ExpressionMenuControlType.RadialPuppet, "SizeOpen", "Size")));
            menu.Press(0);
            menu.Press(0);

            menu.Close();

            Assert.That(menu.ActivePuppet, Is.Null);
            Assert.That(menu.CurrentPageIndex, Is.EqualTo(0));
            Assert.That(sink.Writes, Is.EqualTo(new[] { "Outer=1", "SizeOpen=1", "SizeOpen=0", "Outer=0" }));
        }

        [Test]
        public void BackInsideASubMenuClosesOnlyThePuppet()
        {
            var sink = new RecordingParamSink();
            var menu = Controller(sink,
                Page(SubMenu(1, "Outer", 1f)),
                Page(Puppet(ExpressionMenuControlType.RadialPuppet, "SizeOpen", "Size")));
            menu.Press(0);
            menu.Press(0);

            Assert.That(menu.Back(), Is.True);

            Assert.That(menu.ActivePuppet, Is.Null);
            Assert.That(menu.Depth, Is.EqualTo(1));
            Assert.That(menu.CurrentPageIndex, Is.EqualTo(1));
            Assert.That(sink.Writes, Is.EqualTo(new[] { "Outer=1", "SizeOpen=1", "SizeOpen=0" }));

            Assert.That(menu.Back(), Is.True);

            Assert.That(menu.Depth, Is.EqualTo(0));
            Assert.That(sink.Writes, Is.EqualTo(new[] { "Outer=1", "SizeOpen=1", "SizeOpen=0", "Outer=0" }));
        }

        [Test]
        public void OtherControlsCannotBePressedWhileAPuppetIsOpen()
        {
            var sink = new RecordingParamSink();
            var menu = Controller(sink, Page(Puppet(ExpressionMenuControlType.RadialPuppet, "", "Size"), Toggle("Hat", 1f)));
            menu.Press(0);

            Assert.That(menu.Press(1), Is.False);
            Assert.That(menu.Press(0), Is.False);
            Assert.That(sink.TryGet("Hat", out _), Is.False);

            menu.Back();
            Assert.That(menu.Press(1), Is.True);
            Assert.That(sink.Get("Hat"), Is.EqualTo(1f));
        }

        [Test]
        public void InputForAnotherKindOfPuppetIsIgnored()
        {
            var sink = new RecordingParamSink();
            var menu = Controller(sink,
                Page(Puppet(ExpressionMenuControlType.TwoAxisPuppet, "", "LookX", "LookY"),
                    Puppet(ExpressionMenuControlType.RadialPuppet, "", "Size")));

            menu.SetRadial(0.5f);
            menu.SetAxes(new Vec2(0.5f, 0.5f));
            menu.Press(0);
            menu.SetRadial(0.5f);
            menu.Back();
            menu.Press(1);
            menu.SetAxes(new Vec2(0.5f, 0.5f));

            Assert.That(sink.Writes, Is.Empty);
        }

        [Test]
        public void PuppetsWithoutAParameterStillOpen()
        {
            var sink = new RecordingParamSink();
            var menu = Controller(sink, Page(Puppet(ExpressionMenuControlType.RadialPuppet, "", "Size")));

            Assert.That(menu.Press(0), Is.True);
            Assert.That(menu.ActivePuppet, Is.Not.Null);
            menu.SetRadial(0.5f);
            Assert.That(menu.Back(), Is.True);

            Assert.That(sink.Writes, Is.EqualTo(new[] { "Size=0.5" }));
        }

        [Test]
        public void MissingOrEmptySubParametersAreSkipped()
        {
            var sink = new RecordingParamSink();
            var menu = Controller(sink,
                Page(Puppet(ExpressionMenuControlType.FourAxisPuppet, "", "Up", ""),
                    Puppet(ExpressionMenuControlType.RadialPuppet, "")));

            menu.Press(0);
            menu.SetAxes(new Vec2(-1f, 1f));
            menu.Back();
            menu.Press(1);
            menu.SetRadial(0.5f);

            Assert.That(sink.Writes, Is.EqualTo(new[] { "Up=1" }));
        }

        [Test]
        public void NaNInputIsIgnored()
        {
            var sink = new RecordingParamSink();
            var menu = Controller(sink,
                Page(Puppet(ExpressionMenuControlType.RadialPuppet, "", "Size"),
                    Puppet(ExpressionMenuControlType.TwoAxisPuppet, "", "LookX", "LookY")));

            menu.Press(0);
            menu.SetRadial(float.NaN);
            menu.Back();
            menu.Press(1);
            menu.SetAxes(new Vec2(float.NaN, 0.5f));
            menu.SetAxes(new Vec2(0.5f, float.NaN));

            Assert.That(sink.Writes, Is.Empty);
        }
    }
}
