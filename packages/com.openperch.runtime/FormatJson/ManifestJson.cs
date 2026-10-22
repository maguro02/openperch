using System;
using System.Text;
using UnityEngine;

namespace OpenPerch.Format.Json
{
    /// <summary>Reads and writes <see cref="PackageManifest"/> with JsonUtility; the only JSON code of the format.</summary>
    public sealed class ManifestJson : IManifestParser
    {
        /// <summary>
        /// The deepest nesting of objects and arrays, counting the root object, that <see cref="TryParse"/> accepts. A
        /// version 1 manifest nests 3 deep.
        /// </summary>
        public const int MaxNestingDepth = 32;

        /// <summary>The shared instance; a <see cref="ManifestJson"/> keeps no state between calls.</summary>
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
        /// Unknown keys are ignored. Nesting deeper than <see cref="MaxNestingDepth"/> is <see cref="ManifestErrorCode.JsonInvalid"/>.
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
                // The manifest is untrusted. If JsonUtility's native parser recurses once per level, deep nesting would
                // overflow the stack and end the process instead of raising an exception that is caught here.
                if (!IsNestingWithinLimit(text)) return false;

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

        private static bool IsNestingWithinLimit(string json)
        {
            int depth = 0;
            bool inString = false;
            for (int i = 0; i < json.Length; i++)
            {
                char c = json[i];
                if (inString)
                {
                    if (c == '\\') i++;
                    else if (c == '"') inString = false;
                }
                else if (c == '"')
                {
                    inString = true;
                }
                else if (c == '{' || c == '[')
                {
                    if (++depth > MaxNestingDepth) return false;
                }
                else if (c == '}' || c == ']')
                {
                    // A closer without an opener is malformed anyway, and a negative count would let later openers pass.
                    if (--depth < 0) return false;
                }
            }
            return true;
        }
    }
}
