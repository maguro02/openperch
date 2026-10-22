using System;
using System.IO;

namespace OpenPerch.Format
{
    /// <summary>
    /// Reads and writes the <c>.opk</c> container: a 64-byte little-endian header, the UTF-8 manifest, an optional PNG
    /// thumbnail and the AssetBundle, which is the last section of the file.
    /// </summary>
    public static class PackageCodec
    {
        /// <summary>Size of the fixed header in bytes.</summary>
        public const int HeaderLength = 64;

        /// <summary>The only containerVersion this codec reads and writes.</summary>
        public const ushort CurrentContainerVersion = 1;

        /// <summary>Byte offset of the manifest, right after the header; any other manifestOffset is rejected.</summary>
        public const ulong ManifestOffset = 64;

        /// <summary>Largest manifest in bytes (256 KiB); the smallest is 1.</summary>
        public const uint MaxManifestLength = 262144;

        /// <summary>Largest thumbnail in bytes (2 MiB).</summary>
        public const uint MaxThumbnailLength = 2 * 1024 * 1024;

        /// <summary>bundleOffset must be a multiple of this many bytes.</summary>
        public const ulong BundleAlignment = 4096;

        /// <summary>Largest bundle in bytes, 2^31 - 1: bundles must be shorter than 2^31 bytes.</summary>
        public const ulong MaxBundleLength = (1UL << 31) - 1;

        private const int CrcCoveredLength = 56;
        private const int CopyBufferSize = 81920;

        private static readonly byte[] Magic = { 0x89, 0x4F, 0x50, 0x4B, 0x0D, 0x0A, 0x1A, 0x0A };

        /// <summary>Reads the header at the start of <paramref name="s"/> and checks every layout rule.</summary>
        /// <param name="s">A readable, seekable stream positioned anywhere.</param>
        /// <param name="fileLength">The length of the whole file.</param>
        /// <exception cref="PackageFormatException">The header breaks a layout rule.</exception>
        public static PackageHeader ReadHeader(Stream s, ulong fileLength)
        {
            RequireReadableSeekable(s);
            var b = new byte[HeaderLength];
            s.Position = 0;
            int read = ReadUpTo(s, b, 0, fileLength < HeaderLength ? (int)fileLength : HeaderLength);
            if (read < Magic.Length || !StartsWithMagic(b)) throw new PackageFormatException(PackageError.BadMagic);
            if (read < HeaderLength) throw new PackageFormatException(PackageError.LengthMismatch);

            // The version comes before the CRC because a different container version may place the CRC elsewhere.
            ushort version = ReadUInt16(b, 8);
            if (version != CurrentContainerVersion) throw new PackageFormatException(PackageError.UnsupportedContainer);
            if (Crc32.Compute(new ReadOnlySpan<byte>(b, 0, CrcCoveredLength)) != ReadUInt32(b, 56))
            {
                throw new PackageFormatException(PackageError.BadHeaderCrc);
            }
            if (ReadUInt16(b, 10) != 0) throw new PackageFormatException(PackageError.BadFlags);
            if (ReadUInt32(b, 36) != 0 || ReadUInt32(b, 60) != 0) throw new PackageFormatException(PackageError.BadReserved);
            if (ReadUInt64(b, 16) != ManifestOffset) throw new PackageFormatException(PackageError.BadManifestOffset);

            uint manifestLength = ReadUInt32(b, 12);
            ulong thumbnailOffset = ReadUInt64(b, 24);
            uint thumbnailLength = ReadUInt32(b, 32);
            ulong bundleOffset = ReadUInt64(b, 40);
            ulong bundleLength = ReadUInt64(b, 48);

            if (manifestLength == 0) throw new PackageFormatException(PackageError.OutOfRange);
            if (manifestLength > MaxManifestLength) throw new PackageFormatException(PackageError.ManifestTooLarge);
            bool hasThumbnail = thumbnailLength != 0;
            if ((thumbnailOffset != 0) != hasThumbnail || thumbnailLength > MaxThumbnailLength)
            {
                throw new PackageFormatException(PackageError.BadThumbnailFields);
            }
            if (bundleLength == 0 || bundleLength > MaxBundleLength) throw new PackageFormatException(PackageError.OutOfRange);
            if (bundleOffset % BundleAlignment != 0) throw new PackageFormatException(PackageError.Misaligned);

            if (!IsInsideFile(ManifestOffset, manifestLength, fileLength) ||
                (hasThumbnail && !IsInsideFile(thumbnailOffset, thumbnailLength, fileLength)) ||
                !IsInsideFile(bundleOffset, bundleLength, fileLength))
            {
                throw new PackageFormatException(PackageError.OutOfRange);
            }

            // Every section now lies inside the file, so these sums cannot overflow.
            ulong manifestEnd = ManifestOffset + manifestLength;
            if (hasThumbnail && thumbnailOffset < manifestEnd) throw new PackageFormatException(PackageError.Overlap);
            ulong previousEnd = hasThumbnail ? thumbnailOffset + thumbnailLength : manifestEnd;
            if (bundleOffset < previousEnd) throw new PackageFormatException(PackageError.Overlap);

            if (fileLength - bundleOffset != bundleLength) throw new PackageFormatException(PackageError.LengthMismatch);

            return new PackageHeader(version, manifestLength, thumbnailOffset, thumbnailLength, bundleOffset, bundleLength);
        }

        /// <summary>Reads the manifest bytes described by <paramref name="h"/>.</summary>
        /// <exception cref="PackageFormatException">The manifest length is out of range or the file ends early.</exception>
        public static byte[] ReadManifestBytes(Stream s, in PackageHeader h)
        {
            RequireReadableSeekable(s);
            if (h.ManifestLength == 0) throw new PackageFormatException(PackageError.OutOfRange);
            if (h.ManifestLength > MaxManifestLength) throw new PackageFormatException(PackageError.ManifestTooLarge);
            var bytes = new byte[h.ManifestLength];
            Seek(s, ManifestOffset);
            ReadExactly(s, bytes, 0, bytes.Length);
            return bytes;
        }

        /// <summary>
        /// Writes a package to <paramref name="dst"/>: the manifest right after the header, the thumbnail right after the
        /// manifest, then zero padding up to the next 4096-byte boundary and <paramref name="bundleLength"/> bytes copied
        /// from the current position of <paramref name="bundle"/>.
        /// </summary>
        /// <param name="thumbnailPng">Null or empty to write a package without a thumbnail.</param>
        /// <exception cref="EndOfStreamException"><paramref name="bundle"/> ends before <paramref name="bundleLength"/> bytes.</exception>
        public static void Write(Stream dst, byte[] manifestUtf8, byte[] thumbnailPng, Stream bundle, ulong bundleLength)
        {
            if (dst == null) throw new ArgumentNullException(nameof(dst));
            if (!dst.CanWrite) throw new ArgumentException("The destination stream must be writable.", nameof(dst));
            if (manifestUtf8 == null) throw new ArgumentNullException(nameof(manifestUtf8));
            if (manifestUtf8.Length == 0 || (uint)manifestUtf8.Length > MaxManifestLength)
            {
                throw new ArgumentOutOfRangeException(nameof(manifestUtf8), manifestUtf8.Length, "The manifest must be 1 to 262144 bytes.");
            }
            int thumbnailLength = thumbnailPng == null ? 0 : thumbnailPng.Length;
            if ((uint)thumbnailLength > MaxThumbnailLength)
            {
                throw new ArgumentOutOfRangeException(nameof(thumbnailPng), thumbnailLength, "The thumbnail must not exceed 2 MiB.");
            }
            if (bundle == null) throw new ArgumentNullException(nameof(bundle));
            if (!bundle.CanRead) throw new ArgumentException("The bundle stream must be readable.", nameof(bundle));
            if (bundleLength == 0 || bundleLength > MaxBundleLength)
            {
                throw new ArgumentOutOfRangeException(nameof(bundleLength), bundleLength, "The bundle must be 1 to 2^31 - 1 bytes.");
            }

            ulong manifestEnd = ManifestOffset + (ulong)manifestUtf8.Length;
            ulong thumbnailOffset = thumbnailLength == 0 ? 0 : manifestEnd;
            ulong contentEnd = manifestEnd + (ulong)thumbnailLength;
            ulong bundleOffset = (contentEnd + BundleAlignment - 1) / BundleAlignment * BundleAlignment;

            var header = new byte[HeaderLength];
            Array.Copy(Magic, header, Magic.Length);
            WriteUInt16(header, 8, CurrentContainerVersion);
            WriteUInt32(header, 12, (uint)manifestUtf8.Length);
            WriteUInt64(header, 16, ManifestOffset);
            WriteUInt64(header, 24, thumbnailOffset);
            WriteUInt32(header, 32, (uint)thumbnailLength);
            WriteUInt64(header, 40, bundleOffset);
            WriteUInt64(header, 48, bundleLength);
            WriteUInt32(header, 56, Crc32.Compute(new ReadOnlySpan<byte>(header, 0, CrcCoveredLength)));

            dst.Write(header, 0, header.Length);
            dst.Write(manifestUtf8, 0, manifestUtf8.Length);
            if (thumbnailLength != 0) dst.Write(thumbnailPng, 0, thumbnailLength);
            int padding = (int)(bundleOffset - contentEnd);
            if (padding > 0) dst.Write(new byte[padding], 0, padding);
            CopyExactly(bundle, dst, bundleLength);
        }

        internal static void Seek(Stream s, ulong offset)
        {
            // Offsets were checked against the caller's file length only; no real stream extends past long.MaxValue.
            if (offset > long.MaxValue) throw new PackageFormatException(PackageError.OutOfRange);
            s.Position = (long)offset;
        }

        internal static void ReadExactly(Stream s, byte[] buffer, int offset, int count)
        {
            if (ReadUpTo(s, buffer, offset, count) < count) throw new PackageFormatException(PackageError.LengthMismatch);
        }

        private static int ReadUpTo(Stream s, byte[] buffer, int offset, int count)
        {
            int total = 0;
            while (total < count)
            {
                int n = s.Read(buffer, offset + total, count - total);
                if (n <= 0) break;
                total += n;
            }
            return total;
        }

        private static void CopyExactly(Stream src, Stream dst, ulong length)
        {
            var buffer = new byte[CopyBufferSize];
            ulong remaining = length;
            while (remaining > 0)
            {
                int want = remaining < (ulong)buffer.Length ? (int)remaining : buffer.Length;
                int n = src.Read(buffer, 0, want);
                if (n <= 0) throw new EndOfStreamException("The bundle stream ended before the declared bundle length.");
                dst.Write(buffer, 0, n);
                remaining -= (ulong)n;
            }
        }

        private static void RequireReadableSeekable(Stream s)
        {
            if (s == null) throw new ArgumentNullException(nameof(s));
            if (!s.CanRead || !s.CanSeek) throw new ArgumentException("The package stream must be readable and seekable.", nameof(s));
        }

        private static bool IsInsideFile(ulong offset, ulong length, ulong fileLength) =>
            offset <= fileLength && length <= fileLength - offset;

        private static bool StartsWithMagic(byte[] b)
        {
            for (int i = 0; i < Magic.Length; i++)
            {
                if (b[i] != Magic[i]) return false;
            }
            return true;
        }

        private static ushort ReadUInt16(byte[] b, int o) => (ushort)(b[o] | (b[o + 1] << 8));

        private static uint ReadUInt32(byte[] b, int o) =>
            (uint)b[o] | ((uint)b[o + 1] << 8) | ((uint)b[o + 2] << 16) | ((uint)b[o + 3] << 24);

        private static ulong ReadUInt64(byte[] b, int o) => ReadUInt32(b, o) | ((ulong)ReadUInt32(b, o + 4) << 32);

        private static void WriteUInt16(byte[] b, int o, ushort v)
        {
            b[o] = (byte)v;
            b[o + 1] = (byte)(v >> 8);
        }

        private static void WriteUInt32(byte[] b, int o, uint v)
        {
            b[o] = (byte)v;
            b[o + 1] = (byte)(v >> 8);
            b[o + 2] = (byte)(v >> 16);
            b[o + 3] = (byte)(v >> 24);
        }

        private static void WriteUInt64(byte[] b, int o, ulong v)
        {
            WriteUInt32(b, o, (uint)v);
            WriteUInt32(b, o + 4, (uint)(v >> 32));
        }
    }
}
