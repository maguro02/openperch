using System;

namespace OpenPerch.Format
{
    /// <summary>CRC-32 with the IEEE 802.3 polynomial, the variant used by zlib and PNG.</summary>
    public static class Crc32
    {
        private static readonly uint[] Table = CreateTable();

        /// <summary>Returns the CRC-32 of <paramref name="data"/>.</summary>
        public static uint Compute(ReadOnlySpan<byte> data) => Append(0u, data);

        /// <summary>
        /// Continues a CRC-32 over more data: <c>Append(Compute(a), b)</c> equals the CRC-32 of <c>a</c> followed by <c>b</c>.
        /// </summary>
        public static uint Append(uint crc, ReadOnlySpan<byte> data)
        {
            uint c = ~crc;
            for (int i = 0; i < data.Length; i++)
            {
                c = Table[(c ^ data[i]) & 0xFF] ^ (c >> 8);
            }
            return ~c;
        }

        private static uint[] CreateTable()
        {
            var table = new uint[256];
            for (uint n = 0; n < 256; n++)
            {
                uint c = n;
                for (int k = 0; k < 8; k++)
                {
                    c = (c & 1) != 0 ? 0xEDB88320u ^ (c >> 1) : c >> 1;
                }
                table[n] = c;
            }
            return table;
        }
    }
}
