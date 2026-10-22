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
        /// Moves <paramref name="path"/> to <c>&lt;path&gt;.bak-&lt;UTC time&gt;</c>, adding a counter when that name is taken.
        /// Returns false, leaving the file where it is, when the move fails with an error <see cref="IsFileError"/> accepts.
        /// </summary>
        public static bool TryMoveAside(string path, DateTime utcNow)
        {
            try
            {
                File.Move(path, BackupPath(path, utcNow));
                return true;
            }
            catch (Exception e) when (IsFileError(e))
            {
                return false;
            }
        }

        /// <summary>
        /// Copies <paramref name="path"/> to the same name <see cref="TryMoveAside"/> would move it to. Returns false when the
        /// copy fails with an error <see cref="IsFileError"/> accepts.
        /// </summary>
        public static bool TryCopyAside(string path, DateTime utcNow)
        {
            try
            {
                File.Copy(path, BackupPath(path, utcNow));
                return true;
            }
            catch (Exception e) when (IsFileError(e))
            {
                return false;
            }
        }

        /// <summary>
        /// True for the exceptions a file operation throws when the disk, the permissions or another process get in the way,
        /// as opposed to a bug in the caller.
        /// </summary>
        public static bool IsFileError(Exception e) => e is IOException || e is UnauthorizedAccessException;

        private static string BackupPath(string path, DateTime utcNow)
        {
            // ISO 8601 basic format, because ':' is not allowed in Windows file names.
            var stem = path + ".bak-" + utcNow.ToString("yyyyMMdd'T'HHmmss'Z'", CultureInfo.InvariantCulture);
            var target = stem;
            for (var n = 1; File.Exists(target); n++)
            {
                target = stem + "-" + n.ToString(CultureInfo.InvariantCulture);
            }
            return target;
        }
    }
}
