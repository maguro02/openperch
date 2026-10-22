using NUnit.Framework;
using OpenPerch.App.Core;
using static OpenPerch.Core.Tests.Menu.MenuFactory;

namespace OpenPerch.Core.Tests.Menu
{
    [TestFixture]
    [Category("T-MENU")]
    public class ExpressionMenuButtonTests
    {
        [TestCase(0.0, 3f)]
        [TestCase(0.59, 3f)]
        [TestCase(0.6, 0f)]
        [TestCase(5.0, 0f)]
        public void HoldsTheValueForPointSixSecondsThenReturnsToZero(double elapsed, float expected)
        {
            var sink = new RecordingParamSink();
            var time = Clock();
            var menu = new ExpressionMenuController(new[] { Page(Button("Wave", 3f)) }, sink, time);

            Assert.That(menu.Press(0), Is.True);
            Assert.That(sink.Get("Wave"), Is.EqualTo(3f));

            time.Advance(elapsed);
            menu.Update();

            Assert.That(sink.Get("Wave"), Is.EqualTo(expected));
        }

        [Test]
        public void ReturnsToZeroOnlyOnce()
        {
            var sink = new RecordingParamSink();
            var time = Clock();
            var menu = new ExpressionMenuController(new[] { Page(Button("Wave", 3f)) }, sink, time);

            menu.Press(0);
            time.Advance(0.7);
            menu.Update();
            time.Advance(0.7);
            menu.Update();

            Assert.That(sink.Writes, Is.EqualTo(new[] { "Wave=3", "Wave=0" }));
        }

        [Test]
        public void PressingAgainRestartsTheHold()
        {
            var sink = new RecordingParamSink();
            var time = Clock();
            var menu = new ExpressionMenuController(new[] { Page(Button("Wave", 3f)) }, sink, time);

            menu.Press(0);
            time.Advance(0.4);
            menu.Press(0);
            time.Advance(0.3);
            menu.Update();
            Assert.That(sink.Get("Wave"), Is.EqualTo(3f), "0.7 s after the first press, 0.3 s after the second");

            time.Advance(0.35);
            menu.Update();
            Assert.That(sink.Get("Wave"), Is.EqualTo(0f));
        }

        [Test]
        public void ALaterButtonOnTheSameParameterIsNotCutShort()
        {
            var sink = new RecordingParamSink();
            var time = Clock();
            var menu = new ExpressionMenuController(new[] { Page(Button("Emote", 1f), Button("Emote", 2f)) }, sink, time);

            menu.Press(0);
            time.Advance(0.4);
            menu.Press(1);
            time.Advance(0.3);
            menu.Update();
            Assert.That(sink.Get("Emote"), Is.EqualTo(2f));

            time.Advance(0.35);
            menu.Update();
            Assert.That(sink.Writes, Is.EqualTo(new[] { "Emote=1", "Emote=2", "Emote=0" }));
        }

        [Test]
        public void ButtonsOnDifferentParametersReturnIndependently()
        {
            var sink = new RecordingParamSink();
            var time = Clock();
            var menu = new ExpressionMenuController(new[] { Page(Button("Wave", 1f), Button("Clap", 1f)) }, sink, time);

            menu.Press(0);
            time.Advance(0.3);
            menu.Press(1);
            time.Advance(0.35);
            menu.Update();
            Assert.That(sink.Get("Wave"), Is.EqualTo(0f));
            Assert.That(sink.Get("Clap"), Is.EqualTo(1f));

            time.Advance(0.3);
            menu.Update();
            Assert.That(sink.Get("Clap"), Is.EqualTo(0f));
        }

        [Test]
        public void ReturnsToZeroAfterTheMenuIsClosed()
        {
            var sink = new RecordingParamSink();
            var time = Clock();
            var menu = new ExpressionMenuController(new[] { Page(SubMenu(1)), Page(Button("Wave", 3f)) }, sink, time);

            menu.Press(0);
            menu.Press(0);
            menu.Close();
            time.Advance(0.7);
            menu.Update();

            Assert.That(sink.Get("Wave"), Is.EqualTo(0f));
        }

        [Test]
        public void ButtonsWithoutAParameterDoNothing()
        {
            var sink = new RecordingParamSink();
            var time = Clock();
            var menu = new ExpressionMenuController(new[] { Page(Button("", 1f)) }, sink, time);

            Assert.That(menu.Press(0), Is.False);
            time.Advance(1.0);
            menu.Update();

            Assert.That(sink.Writes, Is.Empty);
        }
    }
}
