using NUnit.Framework;
using OpenPerch.App.Core;
using static OpenPerch.Core.Tests.Menu.MenuFactory;

namespace OpenPerch.Core.Tests.Menu
{
    [TestFixture]
    [Category("T-MENU")]
    public class ExpressionMenuSubMenuTests
    {
        private static ExpressionMenuPage[] ThreeLevels() => new[]
        {
            Page(SubMenu(1, "Outer", 1f)),
            Page(SubMenu(2, "Inner", 2f)),
            Page(Toggle("Hat", 1f)),
        };

        [Test]
        public void StartsOnTheRootPage()
        {
            var pages = ThreeLevels();
            var menu = Controller(new RecordingParamSink(), pages);

            Assert.That(menu.CurrentPageIndex, Is.EqualTo(0));
            Assert.That(menu.CurrentPage, Is.SameAs(pages[0]));
            Assert.That(menu.Depth, Is.EqualTo(0));
        }

        [Test]
        public void EnteringWritesTheValueAndShowsTheTargetPage()
        {
            var sink = new RecordingParamSink();
            var pages = ThreeLevels();
            var menu = Controller(sink, pages);

            Assert.That(menu.Press(0), Is.True);

            Assert.That(menu.CurrentPageIndex, Is.EqualTo(1));
            Assert.That(menu.CurrentPage, Is.SameAs(pages[1]));
            Assert.That(menu.GetControl(0), Is.SameAs(pages[1].Controls[0]));
            Assert.That(menu.Depth, Is.EqualTo(1));
            Assert.That(sink.Writes, Is.EqualTo(new[] { "Outer=1" }));
        }

        [Test]
        public void BackReturnsToTheParentAndResetsTheParameter()
        {
            var sink = new RecordingParamSink();
            var menu = Controller(sink, ThreeLevels());
            menu.Press(0);

            Assert.That(menu.Back(), Is.True);

            Assert.That(menu.CurrentPageIndex, Is.EqualTo(0));
            Assert.That(menu.Depth, Is.EqualTo(0));
            Assert.That(sink.Writes, Is.EqualTo(new[] { "Outer=1", "Outer=0" }));
        }

        [Test]
        public void NestedSubMenusAreLeftInnermostFirst()
        {
            var sink = new RecordingParamSink();
            var menu = Controller(sink, ThreeLevels());

            menu.Press(0);
            menu.Press(0);
            Assert.That(menu.CurrentPageIndex, Is.EqualTo(2));
            Assert.That(menu.Depth, Is.EqualTo(2));
            Assert.That(sink.Writes, Is.EqualTo(new[] { "Outer=1", "Inner=2" }));

            menu.Back();
            Assert.That(menu.CurrentPageIndex, Is.EqualTo(1));
            Assert.That(sink.Get("Outer"), Is.EqualTo(1f), "still inside the outer sub-menu");
            Assert.That(sink.Get("Inner"), Is.EqualTo(0f));

            menu.Back();
            Assert.That(menu.CurrentPageIndex, Is.EqualTo(0));
            Assert.That(sink.Writes, Is.EqualTo(new[] { "Outer=1", "Inner=2", "Inner=0", "Outer=0" }));
        }

        [Test]
        public void CloseLeavesEverySubMenu()
        {
            var sink = new RecordingParamSink();
            var menu = Controller(sink, ThreeLevels());
            menu.Press(0);
            menu.Press(0);

            menu.Close();

            Assert.That(menu.CurrentPageIndex, Is.EqualTo(0));
            Assert.That(menu.Depth, Is.EqualTo(0));
            Assert.That(sink.Writes, Is.EqualTo(new[] { "Outer=1", "Inner=2", "Inner=0", "Outer=0" }));
        }

        [Test]
        public void SubMenusWithoutAParameterOnlyNavigate()
        {
            var sink = new RecordingParamSink();
            var menu = Controller(sink, Page(SubMenu(1)), Page(Toggle("Hat", 1f)));

            Assert.That(menu.Press(0), Is.True);
            Assert.That(menu.CurrentPageIndex, Is.EqualTo(1));
            Assert.That(menu.Back(), Is.True);
            Assert.That(menu.CurrentPageIndex, Is.EqualTo(0));
            Assert.That(sink.Writes, Is.Empty);
        }

        [TestCase(-1)]
        [TestCase(2)]
        [TestCase(int.MaxValue)]
        [TestCase(int.MinValue)]
        public void OutOfRangeSubMenuIndexIsIgnored(int subMenuIndex)
        {
            var sink = new RecordingParamSink();
            var menu = Controller(sink, Page(SubMenu(subMenuIndex, "Open", 1f)), Page(Toggle("Hat", 1f)));

            Assert.That(menu.Press(0), Is.False);

            Assert.That(menu.CurrentPageIndex, Is.EqualTo(0));
            Assert.That(menu.Depth, Is.EqualTo(0));
            Assert.That(sink.Writes, Is.Empty);
        }

        [Test]
        public void BackOnTheRootPageDoesNothing()
        {
            var sink = new RecordingParamSink();
            var menu = Controller(sink, ThreeLevels());

            Assert.That(menu.Back(), Is.False);

            Assert.That(menu.CurrentPageIndex, Is.EqualTo(0));
            Assert.That(sink.Writes, Is.Empty);
        }
    }
}
