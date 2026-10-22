using System.Linq;
using System.Text;
using NUnit.Framework;
using OpenPerch.App.Platform.Windows.Native;

namespace OpenPerch.Core.Tests.Native
{
    public class ZoneIdentifierTests
    {
        [Test]
        public void ReadsTheZoneAWindowsBrowserWrites()
        {
            byte[] content = Encoding.ASCII.GetBytes(
                "[ZoneTransfer]\r\nZoneId=3\r\nReferrerUrl=https://example.com/\r\nHostUrl=https://example.com/avatar.opk\r\n");

            Assert.That(ZoneIdentifier.TryParseZoneId(content, out int zoneId), Is.True);
            Assert.That(zoneId, Is.EqualTo(ZoneIdentifier.UrlZoneInternet));
        }

        [Test]
        public void IgnoresCaseSpacingAndLineEndings()
        {
            byte[] content = Encoding.ASCII.GetBytes("[ zonetransfer ]\n  zoneid = 4 \n");

            Assert.That(ZoneIdentifier.TryParseZoneId(content, out int zoneId), Is.True);
            Assert.That(zoneId, Is.EqualTo(4));
        }

        [Test]
        public void ReadsUtf16TextWithAByteOrderMark()
        {
            byte[] content = Encoding.Unicode.GetPreamble().Concat(Encoding.Unicode.GetBytes("[ZoneTransfer]\r\nZoneId=3\r\n")).ToArray();

            Assert.That(ZoneIdentifier.TryParseZoneId(content, out int zoneId), Is.True);
            Assert.That(zoneId, Is.EqualTo(3));
        }

        [Test]
        public void ReadsUtf16TextCutInTheMiddleOfACharacter()
        {
            byte[] whole = Encoding.Unicode.GetPreamble().Concat(Encoding.Unicode.GetBytes("[ZoneTransfer]\r\nZoneId=3\r\nHostUrl=x")).ToArray();

            Assert.That(ZoneIdentifier.TryParseZoneId(whole.Take(whole.Length - 1).ToArray(), out int zoneId), Is.True);
            Assert.That(zoneId, Is.EqualTo(3));
        }

        [Test]
        public void ReadsUtf8TextWithAByteOrderMark()
        {
            byte[] content = Encoding.UTF8.GetPreamble().Concat(Encoding.UTF8.GetBytes("[ZoneTransfer]\nZoneId=3\n")).ToArray();

            Assert.That(ZoneIdentifier.TryParseZoneId(content, out int zoneId), Is.True);
            Assert.That(zoneId, Is.EqualTo(3));
        }

        [Test]
        public void ToleratesNonUtf8BytesInUrls()
        {
            byte[] content = Encoding.ASCII.GetBytes("[ZoneTransfer]\r\nHostUrl=https://example.com/")
                .Concat(new byte[] { 0x83, 0x41, 0x83, 0x6F })
                .Concat(Encoding.ASCII.GetBytes("\r\nZoneId=3\r\n"))
                .ToArray();

            Assert.That(ZoneIdentifier.TryParseZoneId(content, out int zoneId), Is.True);
            Assert.That(zoneId, Is.EqualTo(3));
        }

        [Test]
        public void TakesTheZoneIdKeyRatherThanTextInsideUrls()
        {
            byte[] content = Encoding.ASCII.GetBytes("[ZoneTransfer]\r\nReferrerUrl=https://example.com/?ZoneId=0\r\nZoneId=3\r\n");

            Assert.That(ZoneIdentifier.TryParseZoneId(content, out int zoneId), Is.True);
            Assert.That(zoneId, Is.EqualTo(3));
        }

        [Test]
        public void IgnoresAZoneIdOutsideTheZoneTransferSection()
        {
            byte[] content = Encoding.ASCII.GetBytes("ZoneId=3\r\n[Other]\r\nZoneId=3\r\n[ZoneTransfer]\r\nHostUrl=about:internet\r\n");

            Assert.That(ZoneIdentifier.TryParseZoneId(content, out _), Is.False);
        }

        [TestCase("[ZoneTransfer]\r\nZoneId=\r\n")]
        [TestCase("[ZoneTransfer]\r\nZoneId=three\r\n")]
        [TestCase("[ZoneTransfer]\r\nZoneId=3abc\r\n")]
        [TestCase("[ZoneTransfer\r\nZoneId=3\r\n")]
        [TestCase("")]
        public void RejectsMalformedMarks(string text)
        {
            Assert.That(ZoneIdentifier.TryParseZoneId(Encoding.ASCII.GetBytes(text), out int zoneId), Is.False);
            Assert.That(zoneId, Is.EqualTo(0));
        }

        [Test]
        public void RejectsMissingContent()
        {
            Assert.That(ZoneIdentifier.TryParseZoneId(null, out _), Is.False);
        }
    }
}
