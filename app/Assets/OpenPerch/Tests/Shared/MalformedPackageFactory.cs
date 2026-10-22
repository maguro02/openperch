using System;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using OpenPerch.Format;

namespace OpenPerch.App.Tests.Shared
{
    /// <summary>
    /// Builds a valid <c>.opk</c> and single-defect variants of it. The container is laid out here rather than by
    /// <see cref="PackageCodec.Write"/>, so layouts the codec refuses to write can be produced, and a layout bug in the
    /// codec cannot cancel itself out by being on both sides of a test.
    /// </summary>
    public sealed class MalformedPackageFactory
    {
        public const string ExportId = "3f2c9a0d6b7e4c1fa2b3c4d5e6f70812";
        public const string LineageId = "9a1b3c5d7e9f4a2b8c6d0e1f2a3b4c5d";
        public const uint BundleCrc = 2864434397;
        public const int BundleLength = 10000;

        /// <summary>Small enough that the header, manifest and thumbnail fit before the first 4096-byte boundary.</summary>
        public const int ThumbnailSize = 16;

        public const int HeaderLength = 64;
        public const int ContainerVersionOffset = 8;
        public const int FlagsOffset = 10;
        public const int ManifestLengthOffset = 12;
        public const int ManifestOffsetOffset = 16;
        public const int ThumbnailOffsetOffset = 24;
        public const int ThumbnailLengthOffset = 32;
        public const int Reserved0Offset = 36;
        public const int BundleOffsetOffset = 40;
        public const int BundleLengthOffset = 48;
        public const int HeaderCrcOffset = 56;
        public const int Reserved1Offset = 60;

        private const int OversizedManifestLength = 300 * 1024;
        private const uint HugeThumbnailSize = 16384;
        private const int StoredBlockMax = 65535;

        private static readonly byte[] Magic = { 0x89, 0x4F, 0x50, 0x4B, 0x0D, 0x0A, 0x1A, 0x0A };
        private static readonly byte[] PngSignature = { 0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A };

        private readonly Func<PackageManifest, byte[]> _serializeManifest;
        private readonly byte[] _bundle;
        private readonly byte[] _thumbnail;

        /// <summary>Embeds the placeholder "{}" as the manifest, for verifiers whose manifest parser is a stub.</summary>
        public MalformedPackageFactory()
            : this(null)
        {
        }

        /// <param name="serializeManifest">Encodes the manifest embedded in each package; null embeds "{}".</param>
        public MalformedPackageFactory(Func<PackageManifest, byte[]> serializeManifest)
        {
            _serializeManifest = serializeManifest ?? (_ => Encoding.UTF8.GetBytes("{}"));
            _bundle = CreateBundleBytes(BundleLength);
            _thumbnail = CreateThumbnailPng(ThumbnailSize, ThumbnailSize);
        }

        /// <summary>A copy of the bundle section every package carries.</summary>
        public byte[] Bundle => (byte[])_bundle.Clone();

        /// <summary>A copy of the thumbnail every package carries, a decodable RGBA PNG.</summary>
        public byte[] Thumbnail => (byte[])_thumbnail.Clone();

        /// <summary>A manifest that passes every check against <see cref="CreateValidPackage"/> and a 2022.3.62f3 app.</summary>
        public PackageManifest CreateValidManifest()
        {
            return new PackageManifest
            {
                formatVersion = 1,
                kind = "avatar",
                exportId = ExportId,
                lineageId = LineageId,
                displayName = "TestDummy",
                createdAtUtc = "2026-10-22T10:00:00Z",
                exporter = new ExporterInfo
                {
                    version = "1.0.0",
                    unityVersion = "2022.3.22f1",
                    buildTarget = "StandaloneWindows64",
                    colorSpace = "Linear",
                    vrcSdkVersion = "3.10.5",
                    tools = new[] { "nadena.dev.ndmf@1.15.0" },
                },
                runtimeSchemaVersion = 1,
                bundle = new BundleInfo
                {
                    name = "op-" + ExportId,
                    crc = BundleCrc,
                    sha256 = Sha256Hex(_bundle),
                    length = _bundle.Length,
                    rootAsset = "definition",
                },
                features = new FeatureInfo
                {
                    fx = true,
                    menuPages = 5,
                    physBones = 14,
                    constraints = 3,
                    contacts = 2,
                    eyeLook = true,
                    blink = true,
                },
                compositing = new CompositingInfo { requiresPrecise = true, reasons = new[] { "additive:1", "grabpass:1" } },
                stats = new StatsInfo
                {
                    triangles = 69000,
                    materials = 18,
                    skinnedMeshes = 6,
                    physBoneTransforms = 220,
                    estimatedTextureBytes = 412000000,
                    estimatedMeshBytes = 38000000,
                },
                warnings = new[] { new WarningInfo { code = "W_REMOVED_COMPONENT", detail = "VRCSpatialAudioSource x2" } },
            };
        }

        public byte[] CreateValidPackage() => Assemble(_serializeManifest(CreateValidManifest()), _thumbnail, _bundle, 0);

        /// <summary>
        /// The manifest for <paramref name="c"/>: changed for manifest defects, otherwise the valid one, which still
        /// describes the package of a container defect.
        /// </summary>
        public PackageManifest CreateManifest(MalformedPackageCase c)
        {
            PackageManifest m = CreateValidManifest();
            switch (c)
            {
                case MalformedPackageCase.FormatVersionZero:
                    m.formatVersion = 0;
                    break;
                case MalformedPackageCase.BadExportId:
                    m.exportId = "zz2c9a0d6b7e4c1fa2b3c4d5e6f70812";
                    m.bundle.name = "op-" + m.exportId;
                    break;
                case MalformedPackageCase.BundleNameMismatch:
                    m.bundle.name = "op-" + LineageId;
                    break;
                case MalformedPackageCase.LongDisplayName:
                    m.displayName = new string('a', 129);
                    break;
                case MalformedPackageCase.TooManyWarnings:
                    m.warnings = CreateWarnings(129);
                    break;
                case MalformedPackageCase.MotionWithDefinitionRoot:
                    m.kind = "motion";
                    break;
                case MalformedPackageCase.BundleLengthMismatch:
                    m.bundle.length = _bundle.Length + 1;
                    break;
                case MalformedPackageCase.UppercaseSha256:
                    m.bundle.sha256 = m.bundle.sha256.ToUpperInvariant();
                    break;
                case MalformedPackageCase.BadUnityVersion:
                    m.exporter.unityVersion = "abc";
                    break;
            }
            return m;
        }

        /// <summary>The package for <paramref name="c"/>, embedding the manifest from <see cref="CreateManifest"/>.</summary>
        public byte[] CreatePackage(MalformedPackageCase c)
        {
            byte[] manifest = _serializeManifest(CreateManifest(c));
            byte[] p;
            switch (c)
            {
                case MalformedPackageCase.BadMagic:
                    p = Assemble(manifest, _thumbnail, _bundle, 0);
                    p[1] = (byte)'X';
                    FixHeaderCrc(p);
                    return p;
                case MalformedPackageCase.ContainerVersion2:
                    p = Assemble(manifest, _thumbnail, _bundle, 0);
                    SetUInt16(p, ContainerVersionOffset, 2);
                    FixHeaderCrc(p);
                    return p;
                case MalformedPackageCase.NonZeroFlags:
                    p = Assemble(manifest, _thumbnail, _bundle, 0);
                    SetUInt16(p, FlagsOffset, 1);
                    FixHeaderCrc(p);
                    return p;
                case MalformedPackageCase.NonZeroReserved:
                    p = Assemble(manifest, _thumbnail, _bundle, 0);
                    SetUInt32(p, Reserved0Offset, 1);
                    FixHeaderCrc(p);
                    return p;
                case MalformedPackageCase.CorruptHeaderCrc:
                    p = Assemble(manifest, _thumbnail, _bundle, 0);
                    p[HeaderCrcOffset] ^= 0x01;
                    return p;
                case MalformedPackageCase.ManifestOffset128:
                    p = Assemble(manifest, _thumbnail, _bundle, 0);
                    SetUInt64(p, ManifestOffsetOffset, 128);
                    FixHeaderCrc(p);
                    return p;
                case MalformedPackageCase.BundleOffset2Pow63:
                    p = Assemble(manifest, _thumbnail, _bundle, 0);
                    SetUInt64(p, BundleOffsetOffset, 1UL << 63);
                    FixHeaderCrc(p);
                    return p;
                case MalformedPackageCase.ThumbnailOverlapsBundle:
                    p = Assemble(manifest, _thumbnail, _bundle, 0);
                    SetUInt64(p, ThumbnailOffsetOffset, GetUInt64(p, BundleOffsetOffset) - 8);
                    FixHeaderCrc(p);
                    return p;
                case MalformedPackageCase.MisalignedBundle:
                    return Assemble(manifest, _thumbnail, _bundle, 4);
                case MalformedPackageCase.TrailingByte:
                    p = Assemble(manifest, _thumbnail, _bundle, 0);
                    Array.Resize(ref p, p.Length + 1);
                    return p;
                case MalformedPackageCase.OversizedManifest:
                    return Assemble(PadWithSpaces(manifest, OversizedManifestLength), _thumbnail, _bundle, 0);
                case MalformedPackageCase.ThumbnailOffsetZero:
                    p = Assemble(manifest, _thumbnail, _bundle, 0);
                    SetUInt64(p, ThumbnailOffsetOffset, 0);
                    SetUInt32(p, ThumbnailLengthOffset, 10);
                    FixHeaderCrc(p);
                    return p;
                case MalformedPackageCase.HugeThumbnail:
                    return Assemble(manifest, CreatePngHeaderOnly(HugeThumbnailSize, HugeThumbnailSize, 8, 6, 0), _bundle, 0);
                case MalformedPackageCase.TamperedBundle:
                    p = Assemble(manifest, _thumbnail, _bundle, 0);
                    p[(long)GetUInt64(p, BundleOffsetOffset) + 100] ^= 0xFF;
                    return p;
                default:
                    return Assemble(manifest, _thumbnail, _bundle, 0);
            }
        }

        /// <summary>
        /// Lays out a version 1 package: the manifest at 64, the thumbnail right after it (if any), and the bundle at the
        /// next 4096-byte boundary plus <paramref name="bundleMisalignment"/>, ending the file.
        /// </summary>
        public static byte[] Assemble(byte[] manifest, byte[] thumbnail, byte[] bundle, int bundleMisalignment)
        {
            int thumbnailLength = thumbnail == null ? 0 : thumbnail.Length;
            long manifestEnd = HeaderLength + manifest.Length;
            long thumbnailOffset = thumbnailLength == 0 ? 0 : manifestEnd;
            long contentEnd = manifestEnd + thumbnailLength;
            long bundleOffset = (contentEnd + 4095) / 4096 * 4096 + bundleMisalignment;

            var p = new byte[bundleOffset + bundle.Length];
            byte[] header = CreateHeader((uint)manifest.Length, (ulong)thumbnailOffset, (uint)thumbnailLength, (ulong)bundleOffset, (ulong)bundle.Length);
            Array.Copy(header, 0, p, 0, HeaderLength);
            Array.Copy(manifest, 0, p, HeaderLength, manifest.Length);
            if (thumbnailLength != 0) Array.Copy(thumbnail, 0, p, thumbnailOffset, thumbnailLength);
            Array.Copy(bundle, 0, p, bundleOffset, bundle.Length);
            return p;
        }

        /// <summary>A 64-byte header with a correct CRC; every field can be set, including ones the format forbids.</summary>
        public static byte[] CreateHeader(
            uint manifestLength,
            ulong thumbnailOffset,
            uint thumbnailLength,
            ulong bundleOffset,
            ulong bundleLength,
            ushort containerVersion = 1,
            ushort flags = 0,
            ulong manifestOffset = 64,
            uint reserved0 = 0,
            uint reserved1 = 0)
        {
            var h = new byte[HeaderLength];
            Array.Copy(Magic, h, Magic.Length);
            SetUInt16(h, ContainerVersionOffset, containerVersion);
            SetUInt16(h, FlagsOffset, flags);
            SetUInt32(h, ManifestLengthOffset, manifestLength);
            SetUInt64(h, ManifestOffsetOffset, manifestOffset);
            SetUInt64(h, ThumbnailOffsetOffset, thumbnailOffset);
            SetUInt32(h, ThumbnailLengthOffset, thumbnailLength);
            SetUInt32(h, Reserved0Offset, reserved0);
            SetUInt64(h, BundleOffsetOffset, bundleOffset);
            SetUInt64(h, BundleLengthOffset, bundleLength);
            SetUInt32(h, Reserved1Offset, reserved1);
            FixHeaderCrc(h);
            return h;
        }

        /// <summary>Recomputes headerCrc32 after a field was changed, so that only the intended defect remains.</summary>
        public static void FixHeaderCrc(byte[] package)
        {
            SetUInt32(package, HeaderCrcOffset, Crc32.Compute(new ReadOnlySpan<byte>(package, 0, HeaderCrcOffset)));
        }

        public static void SetUInt16(byte[] b, int offset, ushort value)
        {
            b[offset] = (byte)value;
            b[offset + 1] = (byte)(value >> 8);
        }

        public static void SetUInt32(byte[] b, int offset, uint value)
        {
            for (int i = 0; i < 4; i++) b[offset + i] = (byte)(value >> (8 * i));
        }

        public static void SetUInt64(byte[] b, int offset, ulong value)
        {
            for (int i = 0; i < 8; i++) b[offset + i] = (byte)(value >> (8 * i));
        }

        public static ulong GetUInt64(byte[] b, int offset)
        {
            ulong value = 0;
            for (int i = 7; i >= 0; i--) value = (value << 8) | b[offset + i];
            return value;
        }

        /// <summary>A decodable 8-bit RGBA PNG filled with one opaque color; the pixel data is stored uncompressed.</summary>
        public static byte[] CreateThumbnailPng(int width, int height)
        {
            int stride = width * 4;
            var raw = new byte[(stride + 1) * height];
            for (int y = 0; y < height; y++)
            {
                int row = y * (stride + 1);
                for (int x = 0; x < width; x++)
                {
                    int px = row + 1 + x * 4;
                    raw[px] = 0x40;
                    raw[px + 1] = 0x80;
                    raw[px + 2] = 0xC0;
                    raw[px + 3] = 0xFF;
                }
            }
            using (var png = new MemoryStream())
            {
                png.Write(PngSignature, 0, PngSignature.Length);
                WriteChunk(png, "IHDR", CreateIhdrData((uint)width, (uint)height, 8, 6, 0));
                WriteChunk(png, "IDAT", ZlibStored(raw));
                WriteChunk(png, "IEND", Array.Empty<byte>());
                return png.ToArray();
            }
        }

        /// <summary>A PNG with only a well-formed IHDR and IEND, for dimensions too large to hold real pixel data.</summary>
        public static byte[] CreatePngHeaderOnly(uint width, uint height, byte bitDepth, byte colorType, byte interlace)
        {
            using (var png = new MemoryStream())
            {
                png.Write(PngSignature, 0, PngSignature.Length);
                WriteChunk(png, "IHDR", CreateIhdrData(width, height, bitDepth, colorType, interlace));
                WriteChunk(png, "IEND", Array.Empty<byte>());
                return png.ToArray();
            }
        }

        private static byte[] CreateIhdrData(uint width, uint height, byte bitDepth, byte colorType, byte interlace)
        {
            var data = new byte[13];
            WriteUInt32BigEndian(data, 0, width);
            WriteUInt32BigEndian(data, 4, height);
            data[8] = bitDepth;
            data[9] = colorType;
            data[12] = interlace;
            return data;
        }

        private static void WriteChunk(Stream s, string type, byte[] data)
        {
            byte[] typeBytes = Encoding.ASCII.GetBytes(type);
            var word = new byte[4];
            WriteUInt32BigEndian(word, 0, (uint)data.Length);
            s.Write(word, 0, 4);
            s.Write(typeBytes, 0, typeBytes.Length);
            s.Write(data, 0, data.Length);
            WriteUInt32BigEndian(word, 0, Crc32.Append(Crc32.Compute(typeBytes), data));
            s.Write(word, 0, 4);
        }

        // Stored (uncompressed) deflate blocks keep this free of System.IO.Compression, which not every Unity API
        // compatibility level references by default.
        private static byte[] ZlibStored(byte[] raw)
        {
            using (var z = new MemoryStream())
            {
                z.WriteByte(0x78);
                z.WriteByte(0x01);
                int offset = 0;
                do
                {
                    int length = Math.Min(StoredBlockMax, raw.Length - offset);
                    bool final = offset + length == raw.Length;
                    z.WriteByte(final ? (byte)1 : (byte)0);
                    z.WriteByte((byte)length);
                    z.WriteByte((byte)(length >> 8));
                    z.WriteByte((byte)~length);
                    z.WriteByte((byte)(~length >> 8));
                    z.Write(raw, offset, length);
                    offset += length;
                }
                while (offset < raw.Length);
                var word = new byte[4];
                WriteUInt32BigEndian(word, 0, Adler32(raw));
                z.Write(word, 0, 4);
                return z.ToArray();
            }
        }

        private static uint Adler32(byte[] data)
        {
            uint a = 1;
            uint b = 0;
            foreach (byte d in data)
            {
                a = (a + d) % 65521;
                b = (b + a) % 65521;
            }
            return (b << 16) | a;
        }

        private static void WriteUInt32BigEndian(byte[] b, int offset, uint value)
        {
            b[offset] = (byte)(value >> 24);
            b[offset + 1] = (byte)(value >> 16);
            b[offset + 2] = (byte)(value >> 8);
            b[offset + 3] = (byte)value;
        }

        private static byte[] CreateBundleBytes(int length)
        {
            var bytes = new byte[length];
            for (int i = 0; i < length; i++) bytes[i] = (byte)((i * 31) ^ (i >> 8));
            return bytes;
        }

        private static WarningInfo[] CreateWarnings(int count)
        {
            var warnings = new WarningInfo[count];
            for (int i = 0; i < count; i++) warnings[i] = new WarningInfo { code = "W_REMOVED_COMPONENT", detail = "Component" + i };
            return warnings;
        }

        private static byte[] PadWithSpaces(byte[] bytes, int length)
        {
            if (bytes.Length >= length) return bytes;
            var padded = new byte[length];
            Array.Copy(bytes, padded, bytes.Length);
            for (int i = bytes.Length; i < length; i++) padded[i] = (byte)' ';
            return padded;
        }

        private static string Sha256Hex(byte[] data)
        {
            using (SHA256 sha = SHA256.Create())
            {
                byte[] hash = sha.ComputeHash(data);
                var sb = new StringBuilder(hash.Length * 2);
                foreach (byte h in hash) sb.Append(h.ToString("x2"));
                return sb.ToString();
            }
        }
    }
}
