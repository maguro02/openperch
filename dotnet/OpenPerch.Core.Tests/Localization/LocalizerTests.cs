using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using NUnit.Framework;
using OpenPerch.App.Core;
using OpenPerch.Core.Tests.Support;

namespace OpenPerch.Core.Tests.Localization
{
    [Category("T-L10N")]
    public class LocalizerTests
    {
        private const string En =
            "{\"schemaVersion\": 1, \"lang\": \"en\", \"entries\": [" +
            "{\"key\": \"greet\", \"text\": \"Hello, {name}\"}," +
            "{\"key\": \"onlyEn\", \"text\": \"English only\"}]}";

        private const string Ja =
            "{\"schemaVersion\": 1, \"lang\": \"ja\", \"entries\": [" +
            "{\"key\": \"greet\", \"text\": \"こんにちは、{name}\"}]}";

        [Test]
        public void UsesTheCurrentLanguage()
        {
            Assert.That(Create().Get("greet"), Is.EqualTo("こんにちは、{name}"));
        }

        [Test]
        public void FallsBackToEnglish()
        {
            Assert.That(Create().Get("onlyEn"), Is.EqualTo("English only"));
        }

        [Test]
        public void MissingKeyReturnsTheKey()
        {
            Assert.That(Create().Get("no.such.key"), Is.EqualTo("no.such.key"));
        }

        [Test]
        public void LanguageWithoutATableFallsBackToEnglish()
        {
            var localizer = Create();

            localizer.Lang = "fr";

            Assert.That(localizer.Get("greet"), Is.EqualTo("Hello, {name}"));
        }

        [Test]
        public void SwitchingTheLanguageChangesTheText()
        {
            var localizer = Create();

            localizer.Lang = "en";

            Assert.That(localizer.Get("greet"), Is.EqualTo("Hello, {name}"));
        }

        [Test]
        public void DefaultLanguageIsEnglish()
        {
            var localizer = new Localizer(new StjCodec());
            localizer.Load("en", En);

            Assert.That(localizer.Lang, Is.EqualTo("en"));
            Assert.That(localizer.Get("greet"), Is.EqualTo("Hello, {name}"));
        }

        [Test]
        public void ReplacesPlaceholders()
        {
            var args = new Dictionary<string, string> { ["name"] = "Perch" };

            Assert.That(Create().Get("greet", args), Is.EqualTo("こんにちは、Perch"));
        }

        [TestCase("{a} and {b}", "1 and {b}")]
        [TestCase("{a}{a}", "11")]
        [TestCase("{ a }", "{ a }")]
        [TestCase("{}", "{}")]
        [TestCase("{a", "{a")]
        [TestCase("{{a}}", "{1}")]
        [TestCase("{a-b}", "{a-b}")]
        public void OnlyKnownPlaceholdersAreReplaced(string text, string expected)
        {
            var localizer = new Localizer(new StjCodec());
            localizer.Load("en", "{\"entries\": [{\"key\": \"k\", \"text\": \"" + text + "\"}]}");

            var result = localizer.Get("k", new Dictionary<string, string> { ["a"] = "1" });

            Assert.That(result, Is.EqualTo(expected));
        }

        [Test]
        public void ReloadingReplacesTheTable()
        {
            var localizer = Create();

            localizer.Load("ja", "{\"lang\": \"ja\", \"entries\": [{\"key\": \"onlyEn\", \"text\": \"日本語\"}]}");

            Assert.That(localizer.Get("onlyEn"), Is.EqualTo("日本語"));
            Assert.That(localizer.Get("greet"), Is.EqualTo("Hello, {name}"));
        }

        [Test]
        public void EntriesWithoutKeyOrTextAreSkipped()
        {
            var localizer = new Localizer(new StjCodec());

            localizer.Load("en", "{\"entries\": [null, {\"key\": \"\", \"text\": \"x\"}, {\"key\": \"k\", \"text\": null}]}");

            Assert.That(localizer.Get("k"), Is.EqualTo("k"));
            Assert.That(localizer.Get(""), Is.EqualTo(""));
        }

        [Test]
        public void MalformedJsonThrowsFormatException()
        {
            var localizer = new Localizer(new StjCodec());

            Assert.Throws<FormatException>(() => localizer.Load("ja", "{\"entries\": ["));
        }

        [TestCase("ja")]
        [TestCase("en")]
        public void ShippedFilesResolveEveryKey(string lang)
        {
            var localizer = new Localizer(new StjCodec());
            localizer.Load(lang, File.ReadAllText(LocaleFilesTests.LocalePath(lang)));
            localizer.Lang = lang;

            var unresolved = LocaleFilesTests.AllL10nKeys().Where(key => localizer.Get(key) == key);

            Assert.That(unresolved, Is.Empty);
        }

        private static Localizer Create()
        {
            var localizer = new Localizer(new StjCodec());
            localizer.Load("en", En);
            localizer.Load("ja", Ja);
            localizer.Lang = "ja";
            return localizer;
        }
    }
}
