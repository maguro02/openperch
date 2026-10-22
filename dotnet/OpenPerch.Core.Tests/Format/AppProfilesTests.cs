using System;
using System.IO;
using System.Linq;
using NUnit.Framework;
using OpenPerch.App.Core;
using OpenPerch.Core.Tests.Support;
using OpenPerch.Format;

namespace OpenPerch.Core.Tests.Format
{
    [Category("T-VER")]
    public class AppProfilesTests
    {
        [Test]
        public void CurrentProfileValues()
        {
            AppProfile p = AppProfiles.Current;

            Assert.That(p.AppUnity.ToString(), Is.EqualTo("2022.3.62f3"));
            Assert.That(p.AppSchema, Is.EqualTo(1));
            Assert.That(p.SupportedFormats, Is.EqualTo(new[] { 1 }));
            Assert.That(p.AcceptLegacy2022, Is.False);
            Assert.That(p.KnownBadExporters, Is.Empty);
        }

        [Test]
        public void AppUnityMatchesTheProjectEditorVersion()
        {
            string line = File.ReadAllLines(RepoPaths.Combine("app", "ProjectSettings", "ProjectVersion.txt"))
                .Single(l => l.StartsWith("m_EditorVersion:", StringComparison.Ordinal));
            string editorVersion = line.Substring("m_EditorVersion:".Length).Trim();

            Assert.That(AppProfiles.Current.AppUnity.ToString(), Is.EqualTo(editorVersion));
        }

        [Test]
        public void EachCallReturnsAnIndependentProfile()
        {
            AppProfile first = AppProfiles.Current;
            first.SupportedFormats[0] = 99;
            first.AppSchema = 42;

            Assert.That(AppProfiles.Current.SupportedFormats, Is.EqualTo(new[] { 1 }));
            Assert.That(AppProfiles.Current.AppSchema, Is.EqualTo(1));
        }

        [Test]
        public void KnownBadMessageCodesAreInTheCatalog()
        {
            foreach (KnownBad bad in AppProfiles.Current.KnownBadExporters)
            {
                Assert.That(MessageCatalog.TryGet(bad.MessageCode, out _), Is.True, bad.MessageCode);
            }
        }
    }
}
