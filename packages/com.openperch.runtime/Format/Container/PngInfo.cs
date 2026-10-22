using System;

namespace OpenPerch.Format
{
    /// <summary>Reads the PNG image header, so a thumbnail can be judged before anything decodes it.</summary>
    public static class PngInfo
    {
        /// <summary>Number of leading bytes <see cref="TryReadIhdr"/> looks at: the signature and the whole IHDR chunk.</summary>
        public const int IhdrPrefixLength = 33;

        /// <summary>Largest width and height accepted for a package thumbnail.</summary>
        public const int MaxThumbnailSize = 512;

        private static readonly byte[] Signature = { 0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A };

        /// <summary>
        /// Reads the IHDR chunk, which PNG requires to come first. Fails unless the signature, chunk length, chunk type and
        /// chunk CRC are valid and both dimensions are between 1 and 2^31 - 1.
        /// </summary>
        public static bool TryReadIhdr(ReadOnlySpan<byte> png, out int w, out int h, out byte depth, out byte colorType, out byte interlace)
        {
            w = 0;
            h = 0;
            depth = 0;
            colorType = 0;
            interlace = 0;
            if (png.Length < IhdrPrefixLength) return false;
            for (int i = 0; i < Signature.Length; i++)
            {
                if (png[i] != Signature[i]) return false;
            }
            if (ReadUInt32BigEndian(png, 8) != 13) return false;
            if (png[12] != (byte)'I' || png[13] != (byte)'H' || png[14] != (byte)'D' || png[15] != (byte)'R') return false;
            if (Crc32.Compute(png.Slice(12, 17)) != ReadUInt32BigEndian(png, 29)) return false;

            uint width = ReadUInt32BigEndian(png, 16);
            uint height = ReadUInt32BigEndian(png, 20);
            if (width == 0 || height == 0 || width > int.MaxValue || height > int.MaxValue) return false;

            w = (int)width;
            h = (int)height;
            depth = png[24];
            colorType = png[25];
            interlace = png[28];
            return true;
        }

        /// <summary>
        /// True when the image is at most 512x512, 8 bits per channel, truecolor with or without alpha (color type 2 or 6)
        /// and not interlaced. Anything else is shown as "no thumbnail" rather than decoded.
        /// </summary>
        public static bool IsAcceptableThumbnail(ReadOnlySpan<byte> png)
        {
            return TryReadIhdr(png, out int w, out int h, out byte depth, out byte colorType, out byte interlace) &&
                   w <= MaxThumbnailSize &&
                   h <= MaxThumbnailSize &&
                   depth == 8 &&
                   (colorType == 2 || colorType == 6) &&
                   interlace == 0;
        }

        private static uint ReadUInt32BigEndian(ReadOnlySpan<byte> b, int o) =>
            ((uint)b[o] << 24) | ((uint)b[o + 1] << 16) | ((uint)b[o + 2] << 8) | b[o + 3];
    }
}
