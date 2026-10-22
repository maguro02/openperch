using NUnit.Framework;
using static OpenPerch.Core.Tests.Menu.MenuFactory;

namespace OpenPerch.Core.Tests.Menu
{
    [TestFixture]
    [Category("T-MENU")]
    public class ExpressionMenuToggleTests
    {
        [Test]
        public void ShowsOnOnlyWhileTheParameterEqualsTheValue()
        {
            var sink = new RecordingParamSink();
            var menu = Controller(sink, Page(Toggle("Outfit", 2f)));

            Assert.That(menu.IsOn(0), Is.False, "parameter not written yet");
            sink.Set("Outfit", 1f);
            Assert.That(menu.IsOn(0), Is.False);
            sink.Set("Outfit", 2f);
            Assert.That(menu.IsOn(0), Is.True);
        }

        [Test]
        public void PressSwitchesBetweenTheValueAndZero()
        {
            var sink = new RecordingParamSink();
            var menu = Controller(sink, Page(Toggle("Outfit", 2f)));

            Assert.That(menu.Press(0), Is.True);
            Assert.That(menu.IsOn(0), Is.True);
            Assert.That(menu.Press(0), Is.True);
            Assert.That(menu.IsOn(0), Is.False);
            Assert.That(sink.Writes, Is.EqualTo(new[] { "Outfit=2", "Outfit=0" }));
        }

        [Test]
        public void PressTurnsOnWhenTheParameterHoldsAnotherValue()
        {
            var sink = new RecordingParamSink();
            var menu = Controller(sink, Page(Toggle("Outfit", 2f)));
            sink.Set("Outfit", 3f);

            menu.Press(0);

            Assert.That(sink.Get("Outfit"), Is.EqualTo(2f));
        }

        [Test]
        public void TogglesSharingAParameterSelectOneValue()
        {
            var sink = new RecordingParamSink();
            var menu = Controller(sink, Page(Toggle("Outfit", 1f), Toggle("Outfit", 2f)));

            menu.Press(0);
            menu.Press(1);

            Assert.That(sink.Get("Outfit"), Is.EqualTo(2f));
            Assert.That(menu.IsOn(0), Is.False);
            Assert.That(menu.IsOn(1), Is.True);
        }

        [Test]
        public void TogglesWithoutAParameterDoNothing()
        {
            var sink = new RecordingParamSink();
            var menu = Controller(sink, Page(Toggle("", 1f)));

            Assert.That(menu.Press(0), Is.False);
            Assert.That(menu.IsOn(0), Is.False);
            Assert.That(sink.Writes, Is.Empty);
        }

        [Test]
        public void OnlyTogglesShowAsOn()
        {
            var sink = new RecordingParamSink();
            var menu = Controller(sink, Page(Button("Wave", 1f), SubMenu(0, "Wave", 1f)));
            sink.Set("Wave", 1f);

            Assert.That(menu.IsOn(0), Is.False);
            Assert.That(menu.IsOn(1), Is.False);
        }
    }
}
