using System;
using System.Globalization;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;

namespace OpenPerch.App.Platform.Windows.Native
{
    /// <summary>
    /// Reads and parses the Mark-of-the-Web: the <c>Zone.Identifier</c> alternate data stream that browsers and mail
    /// clients attach to downloaded files.
    /// </summary>
    internal static class ZoneIdentifier
    {
        public const string StreamSuffix = ":Zone.Identifier";

        /// <summary>
        /// <c>URLZONE_INTERNET</c>. Marks at this value or above are the Internet zone and the Restricted Sites zone.
        /// </summary>
        public const int UrlZoneInternet = 3;

        /// <summary>
        /// Reading stops here. Real marks are a few hundred bytes, but the stream arrives with an untrusted file.
        /// </summary>
        public const int MaxStreamBytes = 64 * 1024;

        private static readonly char[] Padding = { ' ', '\t', '\0' };

        /// <summary>Reads the <c>Zone.Identifier</c> stream of <paramref name="filePath"/>.</summary>
        /// <returns>
        /// <see cref="Kernel32.ERROR_SUCCESS"/> with at most <see cref="MaxStreamBytes"/> bytes in <paramref name="content"/>,
        /// or the Win32 error with an empty <paramref name="content"/>. <see cref="MeansUnmarked"/> separates the errors
        /// that mean the file carries no mark from those that leave the mark unknown.
        /// </returns>
        public static int ReadStream(string filePath, out byte[] content)
        {
            if (filePath == null) throw new ArgumentNullException(nameof(filePath));
            content = Array.Empty<byte>();

            // Mono's file APIs may reject a path containing ':', so the stream is opened through CreateFileW.
            // Full sharing keeps the check from failing while a browser or scanner still holds the file open.
            IntPtr handle = Kernel32.CreateFile(
                filePath + StreamSuffix,
                Kernel32.GENERIC_READ,
                Kernel32.FILE_SHARE_READ | Kernel32.FILE_SHARE_WRITE | Kernel32.FILE_SHARE_DELETE,
                IntPtr.Zero,
                Kernel32.OPEN_EXISTING,
                Kernel32.FILE_ATTRIBUTE_NORMAL,
                IntPtr.Zero);
            if (handle == Kernel32.INVALID_HANDLE_VALUE)
            {
                return Marshal.GetLastWin32Error();
            }

            try
            {
                var chunk = new byte[4096];
                using (var collected = new MemoryStream())
                {
                    while (collected.Length < MaxStreamBytes)
                    {
                        uint wanted = (uint)Math.Min(chunk.Length, MaxStreamBytes - collected.Length);
                        if (!Kernel32.ReadFile(handle, chunk, wanted, out uint read, IntPtr.Zero))
                        {
                            return Marshal.GetLastWin32Error();
                        }
                        if (read == 0)
                        {
                            break;
                        }
                        collected.Write(chunk, 0, (int)read);
                    }
                    content = collected.ToArray();
                }
                return Kernel32.ERROR_SUCCESS;
            }
            finally
            {
                Kernel32.CloseHandle(handle);
            }
        }

        /// <summary>
        /// True when a <see cref="ReadStream"/> result means the file carries no mark: it has no <c>Zone.Identifier</c>
        /// stream (<see cref="Kernel32.ERROR_FILE_NOT_FOUND"/>), or it sits on a volume that cannot hold named streams,
        /// such as FAT32 or exFAT (<see cref="Kernel32.ERROR_INVALID_NAME"/>).
        /// </summary>
        public static bool MeansUnmarked(int result) =>
            result == Kernel32.ERROR_FILE_NOT_FOUND || result == Kernel32.ERROR_INVALID_NAME;

        /// <summary>
        /// Finds <c>ZoneId</c> in the <c>[ZoneTransfer]</c> section of a stream returned by <see cref="ReadStream"/>.
        /// Accepts UTF-16 LE text with a byte order mark, and UTF-8 or ANSI text with or without one; section and key
        /// names in any case; and any line ending.
        /// </summary>
        /// <returns>False when the section or key is missing or the value is not a decimal integer.</returns>
        public static bool TryParseZoneId(byte[] content, out int zoneId)
        {
            zoneId = 0;
            if (content == null || content.Length == 0)
            {
                return false;
            }

            bool inZoneTransfer = false;
            foreach (string rawLine in Decode(content).Split('\r', '\n'))
            {
                string line = rawLine.Trim(Padding);
                if (line.Length == 0)
                {
                    continue;
                }

                if (line[0] == '[')
                {
                    int close = line.IndexOf(']');
                    inZoneTransfer = close > 0 &&
                        string.Equals(line.Substring(1, close - 1).Trim(Padding), "ZoneTransfer", StringComparison.OrdinalIgnoreCase);
                    continue;
                }

                int separator = line.IndexOf('=');
                if (!inZoneTransfer || separator <= 0 ||
                    !string.Equals(line.Substring(0, separator).Trim(Padding), "ZoneId", StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }

                return int.TryParse(line.Substring(separator + 1).Trim(Padding), NumberStyles.Integer, CultureInfo.InvariantCulture, out zoneId);
            }
            return false;
        }

        private static string Decode(byte[] content)
        {
            if (content.Length >= 2 && content[0] == 0xFF && content[1] == 0xFE)
            {
                return Encoding.Unicode.GetString(content, 2, (content.Length - 2) & ~1);
            }

            // ANSI marks decode as UTF-8 too: the section, key and digits are ASCII, and only URL bytes may turn into U+FFFD.
            int start = content.Length >= 3 && content[0] == 0xEF && content[1] == 0xBB && content[2] == 0xBF ? 3 : 0;
            return Encoding.UTF8.GetString(content, start, content.Length - start);
        }
    }
}
