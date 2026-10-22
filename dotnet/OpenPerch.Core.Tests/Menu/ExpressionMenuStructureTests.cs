using System;
using NUnit.Framework;
using OpenPerch.App.Core;
using static OpenPerch.Core.Tests.Menu.MenuFactory;

namespace OpenPerch.Core.Tests.Menu
{
    [TestFixture]
    [Category("T-MENU")]
    public class ExpressionMenuStructureTests
    {
        [Test]
        public void ControlTypeValuesMatchTheRuntimeMenuControlType()
        {
            Assert.That((int)ExpressionMenuControlType.Button, Is.EqualTo(0));
            Assert.That((int)ExpressionMenuControlType.Toggle, Is.EqualTo(1));
            Assert.That((int)ExpressionMenuControlType.SubMenu, Is.EqualTo(2));
            Assert.That((int)ExpressionMenuControlType.TwoAxisPuppet, Is.EqualTo(3));
            Assert.That((int)ExpressionMenuControlType.FourAxisPuppet, Is.EqualTo(4));
            Assert.That((int)ExpressionMenuControlType.RadialPuppet, Is.EqualTo(5));
            Assert.That(Enum.GetValues(typeof(ExpressionMenuControlType)), Has.Length.EqualTo(6));
        }

        [Test]
        public void OnlyTheFirstEightControlsOfAPageAreUsed()
        {
            var controls = new ExpressionMenuControl[10];
            for (var i = 0; i < controls.Length; i++) controls[i] = Toggle("P" + i, 1f);
            var sink = new RecordingParamSink();
            var menu = Controller(sink, Page(controls));
            sink.Set("P8", 1f);
            sink.Writes.Clear();

            Assert.That(ExpressionMenuPage.MaxControls, Is.EqualTo(8));
            Assert.That(menu.ControlCount, Is.EqualTo(8));
            Assert.That(menu.GetControl(7), Is.SameAs(controls[7]));
            Assert.That(menu.GetControl(8), Is.Null);
            Assert.That(menu.IsOn(8), Is.False);
            Assert.That(menu.Press(8), Is.False);
            Assert.That(menu.Press(9), Is.False);
            Assert.That(sink.Writes, Is.Empty);
        }

        [TestCase(-1)]
        [TestCase(3)]
        [TestCase(7)]
        public void EmptySlotsAreIgnored(int index)
        {
            var sink = new RecordingParamSink();
            var menu = Controller(sink, Page(Toggle("A", 1f), Toggle("B", 1f), Toggle("C", 1f)));

            Assert.That(menu.ControlCount, Is.EqualTo(3));
            Assert.That(menu.GetControl(index), Is.Null);
            Assert.That(menu.IsOn(index), Is.False);
            Assert.That(menu.Press(index), Is.False);
            Assert.That(sink.Writes, Is.Empty);
        }

        [Test]
        public void AMenuWithoutPagesIsInert()
        {
            var sink = new RecordingParamSink();
            var menu = new ExpressionMenuController(Array.Empty<ExpressionMenuPage>(), sink, Clock());

            Assert.That(menu.CurrentPageIndex, Is.EqualTo(-1));
            Assert.That(menu.CurrentPage, Is.Null);
            Assert.That(menu.ControlCount, Is.EqualTo(0));
            Assert.That(menu.Press(0), Is.False);
            Assert.That(menu.Back(), Is.False);
            menu.Close();
            menu.Update();
            Assert.That(sink.Writes, Is.Empty);
        }

        [Test]
        public void MissingPagesControlsAndNamesAreTreatedAsEmpty()
        {
            var unknownType = new ExpressionMenuControl { Type = (ExpressionMenuControlType)99, Parameter = "Unknown", Value = 1f };
            var toggleWithNullParameter = new ExpressionMenuControl
            {
                Type = ExpressionMenuControlType.Toggle,
                Parameter = null,
                Value = 1f,
            };
            var puppetWithNullNames = new ExpressionMenuControl
            {
                Type = ExpressionMenuControlType.RadialPuppet,
                Parameter = null,
                SubParameters = null,
            };
            var sink = new RecordingParamSink();
            var menu = Controller(sink,
                Page(null, unknownType, toggleWithNullParameter, puppetWithNullNames, SubMenu(1), SubMenu(2)),
                null,
                new ExpressionMenuPage { Controls = null });

            Assert.That(menu.GetControl(0), Is.Null);
            Assert.That(menu.Press(0), Is.False);
            Assert.That(menu.Press(1), Is.False);
            Assert.That(menu.Press(2), Is.False);
            Assert.That(menu.IsOn(2), Is.False);

            Assert.That(menu.Press(3), Is.True);
            menu.SetRadial(0.5f);
            Assert.That(menu.Back(), Is.True);

            Assert.That(menu.Press(4), Is.True);
            Assert.That(menu.CurrentPage, Is.Null);
            Assert.That(menu.ControlCount, Is.EqualTo(0));
            Assert.That(menu.Back(), Is.True);

            Assert.That(menu.Press(5), Is.True);
            Assert.That(menu.ControlCount, Is.EqualTo(0));

            Assert.That(sink.Writes, Is.Empty);
        }

        [Test]
        public void ConstructorRejectsMissingDependencies()
        {
            var pages = new[] { Page(Toggle("Hat", 1f)) };
            var sink = new RecordingParamSink();
            var time = Clock();

            Assert.Throws<ArgumentNullException>(() => new ExpressionMenuController(null, sink, time));
            Assert.Throws<ArgumentNullException>(() => new ExpressionMenuController(pages, null, time));
            Assert.Throws<ArgumentNullException>(() => new ExpressionMenuController(pages, sink, null));
        }
    }
}
