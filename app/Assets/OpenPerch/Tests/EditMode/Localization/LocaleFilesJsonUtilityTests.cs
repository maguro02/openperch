using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using OpenPerch.App.Boot;
using OpenPerch.App.Core;
using UnityEngine;

namespace OpenPerch.App.Tests.EditMode
{
    [Category("T-L10N-U")]
    public class LocaleFilesJsonUtilityTests
    {
        [TestCase("ja")]
        [TestCase("en")]
        public void FileReadsThroughJsonUtility(string lang)
        {
            var file = new JsonUtilityCodec().Deserialize<LocaleFile>(ReadLocale(lang));

            Assert.That(file.lang, Is.EqualTo(lang));
            Assert.That(file.schemaVersion, Is.EqualTo(1));
            Assert.That(file.entries.Where(e => string.IsNullOrEmpty(e.key) || string.IsNullOrEmpty(e.text)), Is.Empty);
            Assert.That(AllL10nKeys().Except(file.entries.Select(e => e.key)), Is.Empty);
        }

        [TestCase("ja")]
        [TestCase("en")]
        public void LocalizerResolvesEveryKey(string lang)
        {
            var localizer = new Localizer(new JsonUtilityCodec());
            localizer.Load(lang, ReadLocale(lang));
            localizer.Lang = lang;

            var unresolved = AllL10nKeys().Where(key => localizer.Get(key) == key);

            Assert.That(unresolved, Is.Empty);
        }

        private static string ReadLocale(string lang)
        {
            return File.ReadAllText(Path.Combine(Application.streamingAssetsPath, "i18n", lang + ".json"));
        }

        private static IEnumerable<string> AllL10nKeys()
        {
            return typeof(L10nKeys).GetFields(BindingFlags.Public | BindingFlags.Static)
                .Where(f => f.IsLiteral && f.FieldType == typeof(string))
                .Select(f => (string)f.GetRawConstantValue());
        }
    }
}
