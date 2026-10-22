using System;
using NUnit.Framework;
using OpenPerch.App.Tests.Shared;
using OpenPerch.Format;

namespace OpenPerch.Core.Tests.Format
{
    [Category("T-FMT")]
    public class PngInfoTests
    {
        [Test]
        public void ReadsTheImageHeader()
        {
            byte[] png = MalformedPackageFactory.CreatePngHeaderOnly(300, 200, 16, 2, 1);

            Assert.That(PngInfo.TryReadIhdr(png, out int w, out int h, out byte depth, out byte colorType, out byte interlace), Is.True);
            Assert.That(w, Is.EqualTo(300));
            Assert.That(h, Is.EqualTo(200));
            Assert.That(depth, Is.EqualTo((byte)16));
            Assert.That(colorType, Is.EqualTo((byte)2));
            Assert.That(interlace, Is.EqualTo((byte)1));
        }

        [Test]
        public void ReadsTheFactoryThumbnail()
        {
            byte[] png = MalformedPackageFactory.CreateThumbnailPng(256, 256);

            Assert.That(PngInfo.TryReadIhdr(png, out int w, out int h, out _, out _, out _), Is.True);
            Assert.That(w, Is.EqualTo(256));
            Assert.That(h, Is.EqualTo(256));
            Assert.That(PngInfo.IsAcceptableThumbnail(png), Is.True);
        }

        [Test]
        public void OnlyTheIhdrPrefixIsNeeded()
        {
            byte[] png = MalformedPackageFactory.CreateThumbnailPng(16, 16);

            Assert.That(PngInfo.IsAcceptableThumbnail(new ReadOnlySpan<byte>(png, 0, PngInfo.IhdrPrefixLength)), Is.True);
            Assert.That(PngInfo.IsAcceptableThumbnail(new ReadOnlySpan<byte>(png, 0, PngInfo.IhdrPrefixLength - 1)), Is.False);
        }

        [TestCase(512u, 512u, (byte)8, (byte)6, (byte)0, true)]
        [TestCase(1u, 1u, (byte)8, (byte)2, (byte)0, true)]
        [TestCase(513u, 512u, (byte)8, (byte)6, (byte)0, false)]
        [TestCase(512u, 513u, (byte)8, (byte)6, (byte)0, false)]
        [TestCase(16384u, 16384u, (byte)8, (byte)6, (byte)0, false)]
        [TestCase(256u, 256u, (byte)16, (byte)6, (byte)0, false)]
        [TestCase(256u, 256u, (byte)8, (byte)3, (byte)0, false)]
        [TestCase(256u, 256u, (byte)8, (byte)4, (byte)0, false)]
        [TestCase(256u, 256u, (byte)8, (byte)0, (byte)0, false)]
        [TestCase(256u, 256u, (byte)8, (byte)6, (byte)1, false)]
        public void ThumbnailConditions(uint width, uint height, byte depth, byte colorType, byte interlace, bool expected)
        {
            byte[] png = MalformedPackageFactory.CreatePngHeaderOnly(width, height, depth, colorType, interlace);

            Assert.That(PngInfo.IsAcceptableThumbnail(png), Is.EqualTo(expected));
        }

        [TestCase(0u, 10u)]
        [TestCase(10u, 0u)]
        [TestCase(0x80000000u, 10u)]
        public void DimensionsOutsidePngRangeAreRejected(uint width, uint height)
        {
            byte[] png = MalformedPackageFactory.CreatePngHeaderOnly(width, height, 8, 6, 0);

            Assert.That(PngInfo.TryReadIhdr(png, out _, out _, out _, out _, out _), Is.False);
        }

        [Test]
        public void CorruptIhdrIsRejected()
        {
            byte[] badSignature = MalformedPackageFactory.CreatePngHeaderOnly(16, 16, 8, 6, 0);
            badSignature[1] = (byte)'X';
            byte[] badCrc = MalformedPackageFactory.CreatePngHeaderOnly(16, 16, 8, 6, 0);
            badCrc[30] ^= 0xFF;
            byte[] badType = MalformedPackageFactory.CreatePngHeaderOnly(16, 16, 8, 6, 0);
            badType[12] = (byte)'i';
            byte[] badLength = MalformedPackageFactory.CreatePngHeaderOnly(16, 16, 8, 6, 0);
            badLength[11] = 14;

            Assert.That(PngInfo.TryReadIhdr(badSignature, out _, out _, out _, out _, out _), Is.False);
            Assert.That(PngInfo.TryReadIhdr(badCrc, out _, out _, out _, out _, out _), Is.False);
            Assert.That(PngInfo.TryReadIhdr(badType, out _, out _, out _, out _, out _), Is.False);
            Assert.That(PngInfo.TryReadIhdr(badLength, out _, out _, out _, out _, out _), Is.False);
            Assert.That(PngInfo.TryReadIhdr(ReadOnlySpan<byte>.Empty, out _, out _, out _, out _, out _), Is.False);
        }
    }
}
