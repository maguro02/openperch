using System;
using System.Globalization;
using System.IO;
using System.Text;

namespace OpenPerch.App.Core
{
    /// <summary>File operations shared by the stores that keep JSON in the data folder.</summary>
    internal static class PersistedFile
    {
        private static readonly UTF8Encoding Utf8NoBom = new UTF8Encoding(false);

        /// <summary>
        /// Replaces <paramref name="path"/> with <paramref name="text"/> through <c>&lt;path&gt;.tmp</c>, so an interrupted
        /// save leaves either the old file or the new one, never a partial file.
        /// </summary>
        public static void WriteAtomic(string path, string text)
        {
            var directory = Path.GetDirectoryName(path);
            if (!string.IsNullOrEmpty(directory)) Directory.CreateDirectory(directory);

            var tmp = path + ".tmp";
            var bytes = Utf8NoBom.GetBytes(text);
            using (var stream = new FileStream(tmp, FileMode.Create, FileAccess.Write, FileShare.None))
            {
                stream.Write(bytes, 0, bytes.Length);
                // Without this the rename can reach the disk before the data, leaving an empty file after a power loss.
                stream.Flush(true);
            }

            if (File.Exists(path)) File.Replace(tmp, path, null);
            else File.Move(tmp, path);
        }

        /// <summary>
        /// Moves <paramref name="path"/> to <c>&lt;path&gt;.bak-&lt;UTC time&gt;</c>, adding a counter when that name is taken,
        /// and returns the new path.
        /// </summary>
        public static string MoveAside(string path, DateTime utcNow)
        {
            // ISO 8601 basic format, because ':' is not allowed in Windows file names.
            var stem = path + ".bak-" + utcNow.ToString("yyyyMMdd'T'HHmmss'Z'", CultureInfo.InvariantCulture);
            var target = stem;
            for (var n = 1; File.Exists(target); n++)
            {
                target = stem + "-" + n.ToString(CultureInfo.InvariantCulture);
            }
            File.Move(path, target);
            return target;
        }
    }
}
