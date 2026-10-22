using NUnit.Framework;
using OpenPerch.App.Core;
using OpenPerch.App.Tests.Shared;
using OpenPerch.Core.Tests.Support;
using OpenPerch.Format;

namespace OpenPerch.Core.Tests.Settings
{
    [Category("T-SET")]
    public class PersistRoundTripTests
    {
        private readonly StjCodec _codec = new StjCodec();

        [Test]
        public void AppSettingsKeepsEveryField()
        {
            AssertRoundTrip<AppSettings>();
        }

        [Test]
        public void LibraryIndexKeepsEveryField()
        {
            AssertRoundTrip<LibraryIndex>();
        }

        [Test]
        public void LocaleFileKeepsEveryField()
        {
            AssertRoundTrip<LocaleFile>();
        }

        [Test]
        public void GeometryFixtureKeepsEveryField()
        {
            AssertRoundTrip<GeometryFixture>();
        }

        [Test]
        public void PackageManifestKeepsEveryField()
        {
            AssertRoundTrip<PackageManifest>();
        }

        private void AssertRoundTrip<T>() where T : class, new()
        {
            var original = PersistFiller.Fill<T>();

            var restored = _codec.Deserialize<T>(_codec.Serialize(original));

            Assert.That(PersistComparer.Differences(original, restored), Is.Empty);
        }
    }
}
