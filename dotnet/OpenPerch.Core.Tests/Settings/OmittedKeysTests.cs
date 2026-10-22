using NUnit.Framework;
using OpenPerch.App.Core;
using OpenPerch.App.Tests.Shared;
using OpenPerch.Core.Tests.Support;

namespace OpenPerch.Core.Tests.Settings
{
    [Category("T-SET")]
    public class OmittedKeysTests
    {
        private readonly StjCodec _codec = new StjCodec();

        [Test]
        public void MissingFieldsOfAvatarsKeepInitializerValues()
        {
            var settings = _codec.Deserialize<AppSettings>(OmittedKeySamples.SettingsJson);

            Assert.That(PersistComparer.Differences(OmittedKeySamples.ExpectedSettings(), settings), Is.Empty);
        }

        [Test]
        public void MissingFieldsOfIndexEntriesKeepInitializerValues()
        {
            var index = _codec.Deserialize<LibraryIndex>(OmittedKeySamples.IndexJson);

            Assert.That(PersistComparer.Differences(OmittedKeySamples.ExpectedIndex(), index), Is.Empty);
        }
    }
}
