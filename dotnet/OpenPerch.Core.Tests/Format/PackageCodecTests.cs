using System;
using System.IO;
using System.Linq;
using System.Text;
using NUnit.Framework;
using OpenPerch.App.Tests.Shared;
using OpenPerch.Format;

namespace OpenPerch.Core.Tests.Format
{
    [Category("T-FMT")]
    public class PackageCodecTests
    {
        private static readonly byte[] Manifest = Encoding.UTF8.GetBytes("{\"formatVersion\":1,\"kind\":\"avatar\"}");

        private static byte[] Write(byte[] manifest, byte[] thumbnail, byte[] bundle)
        {
            using (var dst = new MemoryStream())
            using (var src = new MemoryStream(bundle, false))
            {
                PackageCodec.Write(dst, manifest, thumbnail, src, (ulong)bundle.Length);
                return dst.ToArray();
            }
        }

        private static PackageHeader ReadHeader(byte[] package) => ReadHeader(package, (ulong)package.Length);

        private static PackageHeader ReadHeader(byte[] bytes, ulong fileLength)
        {
            using (var s = new MemoryStream(bytes, false))
            {
                return PackageCodec.ReadHeader(s, fileLength);
            }
        }

        private static PackageError HeaderError(byte[] bytes, ulong fileLength)
        {
            var e = Assert.Throws<PackageFormatException>(() => ReadHeader(bytes, fileLength));
            return e.Error;
        }

        private static byte[] Slice(byte[] b, ulong offset, ulong length) => b.Skip((int)offset).Take((int)length).ToArray();

        [Test]
        public void WrittenPackageReadsBack()
        {
            var factory = new MalformedPackageFactory();
            byte[] thumbnail = factory.Thumbnail;
            byte[] bundle = factory.Bundle;

            byte[] package = Write(Manifest, thumbnail, bundle);
            PackageHeader h = ReadHeader(package);

            Assert.That(h.ContainerVersion, Is.EqualTo((ushort)1));
            Assert.That(h.ManifestLength, Is.EqualTo((uint)Manifest.Length));
            Assert.That(h.ThumbnailOffset, Is.EqualTo(64UL + (ulong)Manifest.Length));
            Assert.That(h.ThumbnailLength, Is.EqualTo((uint)thumbnail.Length));
            Assert.That(h.BundleOffset % 4096, Is.EqualTo(0UL));
            Assert.That(h.BundleLength, Is.EqualTo((ulong)bundle.Length));
            Assert.That((ulong)package.Length, Is.EqualTo(h.BundleOffset + h.BundleLength));
            Assert.That(Slice(package, h.ThumbnailOffset, h.ThumbnailLength), Is.EqualTo(thumbnail));
            Assert.That(Slice(package, h.BundleOffset, h.BundleLength), Is.EqualTo(bundle));
            ulong contentEnd = h.ThumbnailOffset + h.ThumbnailLength;
            Assert.That(Slice(package, contentEnd, h.BundleOffset - contentEnd).All(b => b == 0), Is.True);

            using (var s = new MemoryStream(package, false))
            {
                Assert.That(PackageCodec.ReadManifestBytes(s, h), Is.EqualTo(Manifest));
            }
        }

        [Test]
        public void PackageWithoutThumbnailHasZeroThumbnailFields()
        {
            byte[] package = Write(Manifest, null, new byte[] { 1, 2, 3 });
            PackageHeader h = ReadHeader(package);

            Assert.That(h.ThumbnailOffset, Is.EqualTo(0UL));
            Assert.That(h.ThumbnailLength, Is.EqualTo(0u));
            Assert.That(h.BundleOffset, Is.EqualTo(4096UL));
            Assert.That(package.Length, Is.EqualTo(4096 + 3));
        }

        [Test]
        public void EmptyThumbnailMeansNoThumbnail()
        {
            PackageHeader h = ReadHeader(Write(Manifest, Array.Empty<byte>(), new byte[] { 7 }));

            Assert.That(h.ThumbnailOffset, Is.EqualTo(0UL));
            Assert.That(h.ThumbnailLength, Is.EqualTo(0u));
        }

        [Test]
        public void WrittenPackagePassesTheVerifier()
        {
            var factory = new MalformedPackageFactory();
            byte[] package = Write(Manifest, factory.Thumbnail, factory.Bundle);

            VerifyResult result;
            using (var s = new MemoryStream(package, false))
            {
                result = PackageVerifier.Verify(s, (ulong)package.Length, new StubManifestParser(factory.CreateValidManifest()),
                    OpenPerch.App.Core.AppProfiles.Current);
            }

            Assert.That(result.Accept, Is.True);
            Assert.That(result.ThumbnailOk, Is.True);
        }

        [Test]
        public void ContentEndingOnABoundaryNeedsNoPadding()
        {
            byte[] manifest = Enumerable.Repeat((byte)' ', 4096 - 64).ToArray();

            PackageHeader h = ReadHeader(Write(manifest, null, new byte[] { 1 }));

            Assert.That(h.BundleOffset, Is.EqualTo(4096UL));
        }

        [Test]
        public void WriteRejectsOutOfRangeInputs()
        {
            var bundle = new byte[] { 1 };
            Assert.Throws<ArgumentOutOfRangeException>(() => Write(Array.Empty<byte>(), null, bundle));
            Assert.Throws<ArgumentOutOfRangeException>(() => Write(new byte[262145], null, bundle));
            Assert.Throws<ArgumentOutOfRangeException>(() => Write(Manifest, new byte[2 * 1024 * 1024 + 1], bundle));
            Assert.Throws<ArgumentNullException>(() => Write(null, null, bundle));

            using (var dst = new MemoryStream())
            using (var src = new MemoryStream(bundle, false))
            {
                Assert.Throws<ArgumentOutOfRangeException>(() => PackageCodec.Write(dst, Manifest, null, src, 0));
                Assert.Throws<ArgumentOutOfRangeException>(() => PackageCodec.Write(dst, Manifest, null, src, 1UL << 31));
            }
        }

        [Test]
        public void WriteAcceptsTheLargestManifestAndThumbnail()
        {
            byte[] package = Write(new byte[262144], new byte[2 * 1024 * 1024], new byte[] { 1 });

            PackageHeader h = ReadHeader(package);

            Assert.That(h.ManifestLength, Is.EqualTo(262144u));
            Assert.That(h.ThumbnailLength, Is.EqualTo(2u * 1024 * 1024));
        }

        [Test]
        public void WriteFailsWhenTheBundleStreamIsShort()
        {
            using (var dst = new MemoryStream())
            using (var src = new MemoryStream(new byte[10], false))
            {
                Assert.Throws<EndOfStreamException>(() => PackageCodec.Write(dst, Manifest, null, src, 11));
            }
        }

        [Test]
        public void WriteCopiesFromTheCurrentBundlePosition()
        {
            using (var dst = new MemoryStream())
            using (var src = new MemoryStream(new byte[] { 9, 9, 1, 2, 3 }, false))
            {
                src.Position = 2;
                PackageCodec.Write(dst, Manifest, null, src, 3);
                byte[] package = dst.ToArray();
                PackageHeader h = ReadHeader(package);
                Assert.That(Slice(package, h.BundleOffset, h.BundleLength), Is.EqualTo(new byte[] { 1, 2, 3 }));
            }
        }

        [Test]
        public void EmptyAndForeignFilesAreBadMagic()
        {
            Assert.That(HeaderError(Array.Empty<byte>(), 0), Is.EqualTo(PackageError.BadMagic));
            Assert.That(HeaderError(new byte[] { 0x89, 0x4F, 0x50 }, 3), Is.EqualTo(PackageError.BadMagic));
            Assert.That(HeaderError(Encoding.ASCII.GetBytes("PK\u0003\u0004 this is a zip file, not a package at all....................."), 70),
                Is.EqualTo(PackageError.BadMagic));
        }

        [Test]
        public void FileEndingInsideTheHeaderIsALengthMismatch()
        {
            byte[] header = MalformedPackageFactory.CreateHeader(2, 0, 0, 4096, 1);

            Assert.That(HeaderError(header.Take(40).ToArray(), 40), Is.EqualTo(PackageError.LengthMismatch));
            Assert.That(HeaderError(header.Take(40).ToArray(), 4097), Is.EqualTo(PackageError.LengthMismatch));
        }

        [Test]
        public void ReservedOneIsAlsoChecked()
        {
            byte[] header = MalformedPackageFactory.CreateHeader(2, 0, 0, 4096, 1, reserved1: 7);

            Assert.That(HeaderError(header, 4097), Is.EqualTo(PackageError.BadReserved));
        }

        [Test]
        public void ManifestLengthBoundaries()
        {
            Assert.That(ReadHeader(MalformedPackageFactory.CreateHeader(262144, 0, 0, 266240, 1), 266241).ManifestLength, Is.EqualTo(262144u));
            Assert.That(HeaderError(MalformedPackageFactory.CreateHeader(262145, 0, 0, 266240, 1), 266241), Is.EqualTo(PackageError.ManifestTooLarge));
            Assert.That(HeaderError(MalformedPackageFactory.CreateHeader(0, 0, 0, 4096, 1), 4097), Is.EqualTo(PackageError.OutOfRange));
        }

        [Test]
        public void ThumbnailLengthBoundaries()
        {
            const uint max = 2 * 1024 * 1024;
            Assert.That(ReadHeader(MalformedPackageFactory.CreateHeader(2, 66, max, 2101248, 1), 2101249).ThumbnailLength, Is.EqualTo(max));
            Assert.That(HeaderError(MalformedPackageFactory.CreateHeader(2, 66, max + 1, 2101248, 1), 2101249), Is.EqualTo(PackageError.BadThumbnailFields));
            Assert.That(HeaderError(MalformedPackageFactory.CreateHeader(2, 66, 0, 4096, 1), 4097), Is.EqualTo(PackageError.BadThumbnailFields));
        }

        [Test]
        public void BundleLengthBoundaries()
        {
            const ulong max = (1UL << 31) - 1;
            Assert.That(ReadHeader(MalformedPackageFactory.CreateHeader(2, 0, 0, 4096, max), 4096 + max).BundleLength, Is.EqualTo(max));
            Assert.That(HeaderError(MalformedPackageFactory.CreateHeader(2, 0, 0, 4096, max + 1), 4096 + max + 1), Is.EqualTo(PackageError.OutOfRange));
            Assert.That(HeaderError(MalformedPackageFactory.CreateHeader(2, 0, 0, 4096, 0), 4096), Is.EqualTo(PackageError.OutOfRange));
        }

        [Test]
        public void BundleRangeThatWrapsAroundIsOutOfRange()
        {
            ulong offset = ulong.MaxValue - 4095;
            byte[] header = MalformedPackageFactory.CreateHeader(2, 0, 0, offset, 8192);

            Assert.That(unchecked(offset + 8192), Is.LessThan(8192UL), "the sum wraps, which an additive check would accept");
            Assert.That(HeaderError(header, 4096), Is.EqualTo(PackageError.OutOfRange));
            Assert.That(HeaderError(header, ulong.MaxValue), Is.EqualTo(PackageError.OutOfRange));
        }

        [Test]
        public void ThumbnailRangeThatWrapsAroundIsOutOfRange()
        {
            byte[] header = MalformedPackageFactory.CreateHeader(2, ulong.MaxValue - 5, 10, 4096, 1);

            Assert.That(HeaderError(header, 4097), Is.EqualTo(PackageError.OutOfRange));
        }

        [Test]
        public void HugeButConsistentOffsetsAreAccepted()
        {
            ulong bundleOffset = 1UL << 62;
            const ulong bundleLength = (1UL << 31) - 1;

            PackageHeader h = ReadHeader(MalformedPackageFactory.CreateHeader(2, 0, 0, bundleOffset, bundleLength), bundleOffset + bundleLength);

            Assert.That(h.BundleOffset, Is.EqualTo(bundleOffset));
        }

        [Test]
        public void SectionsMustNotOverlapTheManifest()
        {
            Assert.That(HeaderError(MalformedPackageFactory.CreateHeader(100, 100, 10, 4096, 1), 4097), Is.EqualTo(PackageError.Overlap));
            Assert.That(HeaderError(MalformedPackageFactory.CreateHeader(2, 0, 0, 0, 100), 100), Is.EqualTo(PackageError.Overlap));
        }

        [Test]
        public void ThumbnailMayEndExactlyWhereTheBundleStarts()
        {
            PackageHeader h = ReadHeader(MalformedPackageFactory.CreateHeader(2, 4000, 96, 4096, 1), 4097);

            Assert.That(h.ThumbnailOffset + h.ThumbnailLength, Is.EqualTo(h.BundleOffset));
        }

        [Test]
        public void FileShorterThanTheBundleEndIsOutOfRange()
        {
            Assert.That(HeaderError(MalformedPackageFactory.CreateHeader(2, 0, 0, 4096, 100), 4100), Is.EqualTo(PackageError.OutOfRange));
        }

        [Test]
        public void ReadManifestBytesRejectsHeadersItCannotHonor()
        {
            using (var s = new MemoryStream(new byte[100], false))
            {
                var tooLarge = new PackageHeader(1, 262145, 0, 0, 4096, 1);
                var empty = new PackageHeader(1, 0, 0, 0, 4096, 1);
                var pastEnd = new PackageHeader(1, 1000, 0, 0, 4096, 1);
                Assert.That(Assert.Throws<PackageFormatException>(() => PackageCodec.ReadManifestBytes(s, tooLarge)).Error, Is.EqualTo(PackageError.ManifestTooLarge));
                Assert.That(Assert.Throws<PackageFormatException>(() => PackageCodec.ReadManifestBytes(s, empty)).Error, Is.EqualTo(PackageError.OutOfRange));
                Assert.That(Assert.Throws<PackageFormatException>(() => PackageCodec.ReadManifestBytes(s, pastEnd)).Error, Is.EqualTo(PackageError.LengthMismatch));
            }
        }
    }
}
