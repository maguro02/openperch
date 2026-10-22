using System;
using System.Text;
using UnityEngine;

namespace OpenPerch.Format.Json
{
    /// <summary>Reads and writes <see cref="PackageManifest"/> with JsonUtility; the only JSON code of the format.</summary>
    public sealed class ManifestJson : IManifestParser
    {
        public static readonly ManifestJson Instance = new ManifestJson();

        private static readonly UTF8Encoding StrictUtf8 = new UTF8Encoding(false, true);
        private static readonly UTF8Encoding Utf8NoBom = new UTF8Encoding(false);
        private static readonly char[] JsonWhitespace = { ' ', '\t', '\n', '\r' };

        private ManifestJson()
        {
        }

        /// <summary>
        /// Parses UTF-8 JSON, skipping a leading byte order mark. Keys absent from the JSON keep the values of a new
        /// <see cref="PackageManifest"/> (0 and ""), which <see cref="ManifestValidator"/> reports as missing.
        /// Unknown keys are ignored.
        /// </summary>
        public bool TryParse(byte[] utf8, out PackageManifest manifest, out ManifestError error)
        {
            manifest = null;
            error = new ManifestError(ManifestErrorCode.JsonInvalid, "");
            if (utf8 == null) return false;
            try
            {
                int start = HasBom(utf8) ? 3 : 0;
                string text = StrictUtf8.GetString(utf8, start, utf8.Length - start).Trim(JsonWhitespace);
                // How JsonUtility treats a root that is not an object (such as "null" or "1") is undocumented.
                if (text.Length < 2 || text[0] != '{' || text[text.Length - 1] != '}') return false;

                var parsed = new PackageManifest();
                JsonUtility.FromJsonOverwrite(text, parsed);
                manifest = parsed;
                error = default;
                return true;
            }
            catch (Exception)
            {
                return false;
            }
        }

        /// <summary>Encodes the manifest as UTF-8 JSON without a byte order mark.</summary>
        public static byte[] ToUtf8(PackageManifest m)
        {
            if (m == null) throw new ArgumentNullException(nameof(m));
            return Utf8NoBom.GetBytes(JsonUtility.ToJson(m));
        }

        private static bool HasBom(byte[] b) => b.Length >= 3 && b[0] == 0xEF && b[1] == 0xBB && b[2] == 0xBF;
    }
}
