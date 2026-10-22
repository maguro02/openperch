using System;
using System.Collections.Generic;
using System.Text;

namespace OpenPerch.App.Core
{
    /// <summary>
    /// Looks up UI strings by key. A key missing from <see cref="Lang"/> falls back to English, and a key missing from
    /// English as well is returned unchanged so the gap is visible on screen.
    /// </summary>
    public sealed class Localizer
    {
        private const string FallbackLang = "en";

        private readonly ITextCodec _codec;
        private readonly Dictionary<string, Dictionary<string, string>> _tables =
            new Dictionary<string, Dictionary<string, string>>(StringComparer.Ordinal);
        private string _lang = FallbackLang;

        /// <param name="codec">JSON codec used to parse <see cref="LocaleFile"/> texts.</param>
        public Localizer(ITextCodec codec)
        {
            _codec = codec ?? throw new ArgumentNullException(nameof(codec));
        }

        /// <summary>The language looked up first, such as <c>"ja"</c> or <c>"en"</c>. Defaults to <c>"en"</c>.</summary>
        public string Lang
        {
            get => _lang;
            set => _lang = value ?? "";
        }

        /// <summary>
        /// Parses a <see cref="LocaleFile"/> JSON text and makes it the table for <paramref name="lang"/>, replacing any table
        /// loaded for that language before. Entries without a key or text are skipped; for a repeated key the last one wins.
        /// </summary>
        /// <exception cref="FormatException"><paramref name="json"/> cannot be parsed.</exception>
        public void Load(string lang, string json)
        {
            if (lang == null) throw new ArgumentNullException(nameof(lang));

            var file = _codec.Deserialize<LocaleFile>(json);
            var table = new Dictionary<string, string>(StringComparer.Ordinal);
            if (file != null && file.entries != null)
            {
                foreach (var entry in file.entries)
                {
                    if (entry == null || string.IsNullOrEmpty(entry.key) || entry.text == null) continue;
                    table[entry.key] = entry.text;
                }
            }
            _tables[lang] = table;
        }

        /// <summary>
        /// Returns the text for <paramref name="key"/>, replacing each <c>{name}</c> placeholder (letters, digits and
        /// underscores) with the value of <paramref name="args"/> under that name. Placeholders without a value stay as written.
        /// </summary>
        public string Get(string key, IReadOnlyDictionary<string, string> args = null)
        {
            if (key == null) throw new ArgumentNullException(nameof(key));
            if (!TryGetText(_lang, key, out var text) && !TryGetText(FallbackLang, key, out text)) return key;
            return args == null || args.Count == 0 ? text : Substitute(text, args);
        }

        private bool TryGetText(string lang, string key, out string text)
        {
            text = null;
            return _tables.TryGetValue(lang, out var table) && table.TryGetValue(key, out text);
        }

        private static string Substitute(string text, IReadOnlyDictionary<string, string> args)
        {
            StringBuilder sb = null;
            var copied = 0;
            for (var i = 0; i < text.Length; i++)
            {
                if (text[i] != '{') continue;

                var end = i + 1;
                while (end < text.Length && IsNameChar(text[end])) end++;
                if (end == i + 1 || end >= text.Length || text[end] != '}') continue;

                var name = text.Substring(i + 1, end - i - 1);
                if (args.TryGetValue(name, out var value))
                {
                    if (sb == null) sb = new StringBuilder(text.Length + 16);
                    sb.Append(text, copied, i - copied).Append(value);
                    copied = end + 1;
                }
                i = end;
            }
            return sb == null ? text : sb.Append(text, copied, text.Length - copied).ToString();
        }

        private static bool IsNameChar(char c) =>
            (c >= 'a' && c <= 'z') || (c >= 'A' && c <= 'Z') || (c >= '0' && c <= '9') || c == '_';
    }
}
