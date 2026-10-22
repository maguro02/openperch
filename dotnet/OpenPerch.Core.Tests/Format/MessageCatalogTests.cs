using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;
using NUnit.Framework;
using OpenPerch.Core.Tests.Support;
using OpenPerch.Format;

namespace OpenPerch.Core.Tests.Format
{
    [Category("T-MSG")]
    public class MessageCatalogTests
    {
        private static readonly Regex CodeHeading = new Regex(@"^#{1,6}[ \t]+([EWI]_[A-Z0-9_]+)[ \t]*$");
        private static readonly Regex AnyHeading = new Regex(@"^#{1,6}[ \t]");
        private static readonly Regex CodeShape = new Regex(@"^[EWI]_[A-Z0-9]+(?:_[A-Z0-9]+)*$");

        private static string[] DocLines() => File.ReadAllLines(RepoPaths.Combine(MessageCatalog.TroubleshootingPath.Split('/')));

        /// <summary>Code headings in the troubleshooting document, with the text of their sections.</summary>
        private static List<KeyValuePair<string, string>> DocSections()
        {
            var sections = new List<KeyValuePair<string, string>>();
            string code = null;
            var body = new StringBuilder();
            foreach (string line in DocLines())
            {
                if (AnyHeading.IsMatch(line))
                {
                    if (code != null) sections.Add(new KeyValuePair<string, string>(code, body.ToString()));
                    Match m = CodeHeading.Match(line);
                    code = m.Success ? m.Groups[1].Value : null;
                    body.Clear();
                    continue;
                }
                body.AppendLine(line);
            }
            if (code != null) sections.Add(new KeyValuePair<string, string>(code, body.ToString()));
            return sections;
        }

        // GitHub's rule for heading anchors: lowercase, drop punctuation other than "-" and "_", spaces become "-".
        private static string GitHubAnchor(string heading)
        {
            var sb = new StringBuilder();
            foreach (char c in heading.Trim().ToLowerInvariant())
            {
                if (char.IsLetterOrDigit(c) || c == '-' || c == '_') sb.Append(c);
                else if (c == ' ') sb.Append('-');
            }
            return sb.ToString();
        }

        [Test]
        public void EveryCodeHasATroubleshootingSectionAtItsAnchor()
        {
            Dictionary<string, string> anchors = DocSections().ToDictionary(s => s.Key, s => GitHubAnchor(s.Key));

            foreach (MessageDef def in MessageCatalog.All)
            {
                Assert.That(anchors.ContainsKey(def.Code), Is.True, def.Code + " has no section in " + MessageCatalog.TroubleshootingPath);
                Assert.That(def.RemedyAnchor, Is.EqualTo(anchors[def.Code]), def.Code);
            }
        }

        [Test]
        public void EverySectionBelongsToACatalogCode()
        {
            foreach (KeyValuePair<string, string> section in DocSections())
            {
                Assert.That(MessageCatalog.TryGet(section.Key, out _), Is.True, section.Key + " is documented but not in the catalog");
            }
        }

        [Test]
        public void SectionsAreUniqueAndExplainCauseAndRemedy()
        {
            List<KeyValuePair<string, string>> sections = DocSections();

            Assert.That(sections.Select(s => s.Key), Is.Unique);
            foreach (KeyValuePair<string, string> section in sections)
            {
                Assert.That(section.Value, Does.Contain("原因"), section.Key);
                Assert.That(section.Value, Does.Contain("対処"), section.Key);
            }
        }

        [Test]
        public void CodesAreUniqueAndShapedBySeverity()
        {
            Assert.That(MessageCatalog.All.Select(d => d.Code), Is.Unique);
            Assert.That(MessageCatalog.All.Select(d => d.RemedyAnchor), Is.Unique);
            foreach (MessageDef def in MessageCatalog.All)
            {
                Assert.That(CodeShape.IsMatch(def.Code), Is.True, def.Code);
                char expectedPrefix = def.Severity == MessageSeverity.Error ? 'E' : def.Severity == MessageSeverity.Warning ? 'W' : 'I';
                Assert.That(def.Code[0], Is.EqualTo(expectedPrefix), def.Code);
                Assert.That(def.Ja, Is.Not.Empty, def.Code);
                Assert.That(def.En, Is.Not.Empty, def.Code);
                Assert.That(def.Ja, Is.Not.EqualTo(def.En), def.Code);
            }
        }

        [Test]
        public void ConstantsAndCatalogListTheSameCodes()
        {
            string[] constants = typeof(MessageCodes)
                .GetFields(BindingFlags.Public | BindingFlags.Static)
                .Where(f => f.IsLiteral && f.FieldType == typeof(string))
                .Select(f => (string)f.GetRawConstantValue())
                .ToArray();

            Assert.That(constants, Is.EquivalentTo(MessageCatalog.All.Select(d => d.Code)));
            foreach (FieldInfo field in typeof(MessageCodes).GetFields(BindingFlags.Public | BindingFlags.Static).Where(fi => fi.IsLiteral))
            {
                Assert.That(field.GetRawConstantValue(), Is.EqualTo(field.Name), "constant name and value differ");
            }
        }

        [Test]
        public void CodesNamedForTheAppAndExporterExist()
        {
            AssertSeverity(MessageCodes.E_ELEVATED, MessageSeverity.Error);
            AssertSeverity(MessageCodes.E_DEF_INVALID, MessageSeverity.Error);
            AssertSeverity(MessageCodes.W_DEF_CLAMPED, MessageSeverity.Warning);
            AssertSeverity(MessageCodes.W_REF_OUTSIDE, MessageSeverity.Warning);
            AssertSeverity(MessageCodes.W_THUMB_REJECTED, MessageSeverity.Warning);
            AssertSeverity(MessageCodes.W_REMOVED_COMPONENT, MessageSeverity.Warning);
        }

        [Test]
        public void EveryPackageErrorHasAnErrorMessage()
        {
            foreach (PackageError e in Enum.GetValues(typeof(PackageError)))
            {
                AssertSeverity(MessageCodes.For(e), MessageSeverity.Error);
            }
        }

        [Test]
        public void EveryManifestErrorHasAnErrorMessage()
        {
            foreach (ManifestErrorCode c in Enum.GetValues(typeof(ManifestErrorCode)))
            {
                AssertSeverity(MessageCodes.For(c), MessageSeverity.Error);
            }
        }

        [Test]
        public void EveryCompatOutcomeHasAMessage()
        {
            foreach (CompatCode c in Enum.GetValues(typeof(CompatCode)))
            {
                if (c == CompatCode.Ok || c == CompatCode.LegacyStreamAccepted)
                {
                    Assert.Throws<ArgumentOutOfRangeException>(() => MessageCodes.ForRejection(c), c.ToString());
                    continue;
                }
                AssertSeverity(MessageCodes.ForRejection(c), MessageSeverity.Error);
            }
            AssertSeverity(MessageCodes.ForWarning(CompatCode.LegacyStreamAccepted), MessageSeverity.Warning);
            AssertSeverity(MessageCodes.ForWarning(CompatCode.KnownBadExporter), MessageSeverity.Warning);
            Assert.Throws<ArgumentOutOfRangeException>(() => MessageCodes.ForWarning(CompatCode.WrongTarget));
        }

        [Test]
        public void ExporterUnityVerdictsHaveMessages()
        {
            AssertSeverity(MessageCodes.For(ExporterUnitySupport.Unity6AppOnly), MessageSeverity.Warning);
            AssertSeverity(MessageCodes.For(ExporterUnitySupport.Unsupported), MessageSeverity.Error);
            Assert.Throws<ArgumentOutOfRangeException>(() => MessageCodes.For(ExporterUnitySupport.Supported));
        }

        [Test]
        public void ImportAndLoadErrorsHaveMessages()
        {
            string[] codes =
            {
                MessageCodes.E_ELEVATED, MessageCodes.I_IMPORT_UNTRUSTED_DECLINED, MessageCodes.E_IMPORT_COPY_FAILED,
                MessageCodes.E_IMPORT_DISK_FULL, MessageCodes.E_IMPORT_HEADER, MessageCodes.E_IMPORT_MANIFEST,
                MessageCodes.E_IMPORT_INCOMPATIBLE, MessageCodes.E_IMPORT_SHA256_MISMATCH, MessageCodes.E_LOAD_CRC,
                MessageCodes.E_LOAD_DEFINITION_MISSING, MessageCodes.E_LOAD_EXPORT_ID_MISMATCH,
                MessageCodes.E_LOAD_BUNDLE_NAME_COLLISION, MessageCodes.E_DEF_INVALID, MessageCodes.E_LOAD_SANITIZE_REJECTED,
            };

            foreach (string code in codes)
            {
                Assert.That(MessageCatalog.TryGet(code, out _), Is.True, code);
            }
        }

        [Test]
        public void UnknownCodesAreNotFound()
        {
            Assert.That(MessageCatalog.TryGet("E_DOES_NOT_EXIST", out MessageDef def), Is.False);
            Assert.That(def, Is.Null);
            Assert.That(MessageCatalog.TryGet(null, out _), Is.False);
            Assert.That(MessageCatalog.TryGet("e_elevated", out _), Is.False);
        }

        private static void AssertSeverity(string code, MessageSeverity severity)
        {
            Assert.That(MessageCatalog.TryGet(code, out MessageDef def), Is.True, code);
            Assert.That(def.Severity, Is.EqualTo(severity), code);
        }
    }
}
