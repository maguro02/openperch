using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;
using NUnit.Framework;
using OpenPerch.App.Core;
using OpenPerch.Core.Tests.Support;

namespace OpenPerch.Core.Tests.Localization
{
    [Category("T-L10N")]
    public class LocaleFilesTests
    {
        private static readonly Regex Placeholder = new Regex(@"\{([A-Za-z0-9_]+)\}");

        [TestCase("ja")]
        [TestCase("en")]
        public void FileDeclaresItsLanguageAndVersion(string lang)
        {
            var file = Read(lang);

            Assert.That(file.lang, Is.EqualTo(lang));
            Assert.That(file.schemaVersion, Is.EqualTo(1));
        }

        [TestCase("ja")]
        [TestCase("en")]
        public void KeysAreUniqueAndTextsAreNotEmpty(string lang)
        {
            var entries = Read(lang).entries;

            var repeated = entries.GroupBy(e => e.key).Where(g => g.Count() > 1).Select(g => g.Key);
            var blank = entries.Where(e => string.IsNullOrWhiteSpace(e.key) || string.IsNullOrWhiteSpace(e.text)).Select(e => e.key);
            Assert.That(repeated, Is.Empty);
            Assert.That(blank, Is.Empty);
        }

        [Test]
        public void JapaneseAndEnglishHaveTheSameKeys()
        {
            var ja = Read("ja").entries.Select(e => e.key).ToList();
            var en = Read("en").entries.Select(e => e.key).ToList();

            Assert.That(ja.Except(en), Is.Empty, "only in ja");
            Assert.That(en.Except(ja), Is.Empty, "only in en");
        }

        [Test]
        public void PlaceholdersMatchBetweenLanguages()
        {
            var en = Read("en").entries.ToDictionary(e => e.key, e => e.text);

            var mismatched = Read("ja").entries
                .Where(e => en.ContainsKey(e.key) && !Placeholders(e.text).SetEquals(Placeholders(en[e.key])))
                .Select(e => e.key);

            Assert.That(mismatched, Is.Empty);
        }

        [TestCase("ja")]
        [TestCase("en")]
        public void EveryL10nKeyIsInTheFile(string lang)
        {
            var keys = Read(lang).entries.Select(e => e.key);

            Assert.That(AllL10nKeys().Except(keys), Is.Empty);
        }

        [Test]
        public void L10nKeysAreDistinct()
        {
            var keys = AllL10nKeys().ToList();

            Assert.That(keys, Is.Not.Empty);
            Assert.That(keys, Is.Unique);
        }

        internal static IEnumerable<string> AllL10nKeys()
        {
            return typeof(L10nKeys).GetFields(BindingFlags.Public | BindingFlags.Static)
                .Where(f => f.IsLiteral && f.FieldType == typeof(string))
                .Select(f => (string)f.GetRawConstantValue());
        }

        internal static string LocalePath(string lang) =>
            RepoPaths.Combine("app", "Assets", "StreamingAssets", "i18n", lang + ".json");

        private static LocaleFile Read(string lang) => new StjCodec().Deserialize<LocaleFile>(File.ReadAllText(LocalePath(lang)));

        private static HashSet<string> Placeholders(string text) =>
            new HashSet<string>(Placeholder.Matches(text).Cast<Match>().Select(m => m.Groups[1].Value));
    }
}
