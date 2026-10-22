using NUnit.Framework;
using OpenPerch.App.Core;
using OpenPerch.App.Tests.Shared;

namespace OpenPerch.Core.Tests.Settings
{
    [Category("T-SET")]
    public class PersistComparerTests
    {
        [Test]
        public void EqualGraphsHaveNoDifferences()
        {
            Assert.That(PersistComparer.Differences(new AppSettings(), new AppSettings()), Is.Empty);
        }

        [Test]
        public void ReportsNestedFieldsWithTheirPath()
        {
            var expected = new AppSettings();
            expected.avatars.Add(new AvatarPlacement());
            var actual = new AppSettings();
            actual.avatars.Add(new AvatarPlacement { xNorm = 0.25f });
            actual.widgetPosDip = new Vec2I(3, 0);

            var differences = PersistComparer.Differences(expected, actual);

            Assert.That(differences, Is.EquivalentTo(new[]
            {
                "AppSettings.avatars[0].xNorm: expected 0 but was 0.25",
                "AppSettings.widgetPosDip.x: expected 0 but was 3",
            }));
        }

        [Test]
        public void ReportsCollectionLengthAndNulls()
        {
            var expected = new LibraryIndex();
            expected.entries.Add(new LibraryEntry());
            var actual = new LibraryIndex();
            actual.entries.Add(new LibraryEntry { warningCodes = null, status = null });
            actual.entries.Add(new LibraryEntry());

            Assert.That(PersistComparer.Differences(expected, actual), Is.EqualTo(new[] { "LibraryIndex.entries: expected 1 items but was 2" }));

            actual.entries.RemoveAt(1);
            Assert.That(PersistComparer.Differences(expected, actual), Is.EquivalentTo(new[]
            {
                "LibraryIndex.entries[0].status: expected \"ok\" but was null",
                "LibraryIndex.entries[0].warningCodes: expected [0 items] but was null",
            }));
        }
    }
}
