using System;
using System.Text;
using NUnit.Framework;
using OpenPerch.Format;

namespace OpenPerch.Core.Tests.Format
{
    [Category("T-FMT")]
    public class Crc32Tests
    {
        [Test]
        public void MatchesTheStandardCheckValue()
        {
            Assert.That(Crc32.Compute(Encoding.ASCII.GetBytes("123456789")), Is.EqualTo(0xCBF43926u));
        }

        [Test]
        public void EmptyInputIsZero()
        {
            Assert.That(Crc32.Compute(new byte[0]), Is.EqualTo(0u));
        }

        [Test]
        public void MatchesTheIendChunkCrc()
        {
            Assert.That(Crc32.Compute(Encoding.ASCII.GetBytes("IEND")), Is.EqualTo(0xAE426082u));
        }

        [Test]
        public void AppendContinuesAPreviousCrc()
        {
            byte[] all = Encoding.ASCII.GetBytes("The quick brown fox jumps over the lazy dog");

            uint split = Crc32.Append(Crc32.Compute(new ReadOnlySpan<byte>(all, 0, 10)), new ReadOnlySpan<byte>(all, 10, all.Length - 10));

            Assert.That(split, Is.EqualTo(Crc32.Compute(all)));
            Assert.That(Crc32.Compute(all), Is.EqualTo(0x414FA339u));
        }
    }
}
