using System;
using System.Globalization;
using System.Text.RegularExpressions;

namespace OpenPerch.Format
{
    /// <summary>
    /// A Unity editor version such as "2022.3.22f1". The China release suffix ("2022.3.22f1c1") is accepted but plays no
    /// part in ordering.
    /// </summary>
    public readonly struct UnityVersion : IComparable<UnityVersion>
    {
        private static readonly Regex Pattern = new Regex(
            @"^([0-9]+)\.([0-9]+)\.([0-9]+)([abfp])([0-9]+)(?:c[0-9]+)?\z",
            RegexOptions.CultureInvariant);

        public readonly int Major, Minor, Patch, Build;

        /// <summary>Release kind, ordered a (alpha) &lt; b (beta) &lt; f (final) &lt; p (patch).</summary>
        public readonly char Kind;

        /// <exception cref="ArgumentOutOfRangeException">A number is negative, or <paramref name="kind"/> is not a, b, f or p.</exception>
        public UnityVersion(int major, int minor, int patch, char kind, int build)
        {
            if (major < 0) throw new ArgumentOutOfRangeException(nameof(major));
            if (minor < 0) throw new ArgumentOutOfRangeException(nameof(minor));
            if (patch < 0) throw new ArgumentOutOfRangeException(nameof(patch));
            if (KindRank(kind) < 0) throw new ArgumentOutOfRangeException(nameof(kind), kind, "Kind must be a, b, f or p.");
            if (build < 0) throw new ArgumentOutOfRangeException(nameof(build));
            Major = major;
            Minor = minor;
            Patch = patch;
            Kind = kind;
            Build = build;
        }

        /// <summary>
        /// Parses "MAJOR.MINOR.PATCH" followed directly by the release kind (a, b, f or p) and the build number, optionally
        /// followed by the China release suffix "c" and digits, for example "2022.3.22f1" or "2022.3.22f1c1". Fails on any
        /// whitespace and on numbers above <see cref="int.MaxValue"/>.
        /// </summary>
        public static bool TryParse(string s, out UnityVersion v)
        {
            v = default;
            if (s == null) return false;
            Match match = Pattern.Match(s);
            if (!match.Success) return false;
            if (!TryParseNumber(match.Groups[1].Value, out int major) ||
                !TryParseNumber(match.Groups[2].Value, out int minor) ||
                !TryParseNumber(match.Groups[3].Value, out int patch) ||
                !TryParseNumber(match.Groups[5].Value, out int build))
            {
                return false;
            }
            v = new UnityVersion(major, minor, patch, match.Groups[4].Value[0], build);
            return true;
        }

        /// <summary>Orders by major, minor, patch, kind (a &lt; b &lt; f &lt; p) and build; the China suffix plays no part.</summary>
        public int CompareTo(UnityVersion o)
        {
            int c = Major.CompareTo(o.Major);
            if (c != 0) return c;
            c = Minor.CompareTo(o.Minor);
            if (c != 0) return c;
            c = Patch.CompareTo(o.Patch);
            if (c != 0) return c;
            c = KindRank(Kind).CompareTo(KindRank(o.Kind));
            return c != 0 ? c : Build.CompareTo(o.Build);
        }

        /// <summary>
        /// Whether bundles can move between the two versions: the same major.minor up to 2022, the same major from 6000 on.
        /// </summary>
        public bool SameStream(UnityVersion o) => Major == o.Major && (Major >= 6000 || Minor == o.Minor);

        /// <summary>Formats as "2022.3.22f1"; a China suffix that was parsed is not kept.</summary>
        public override string ToString() =>
            string.Format(CultureInfo.InvariantCulture, "{0}.{1}.{2}{3}{4}", Major, Minor, Patch, Kind, Build);

        private static int KindRank(char kind)
        {
            switch (kind)
            {
                case 'a': return 0;
                case 'b': return 1;
                case 'f': return 2;
                case 'p': return 3;
                default: return -1;
            }
        }

        private static bool TryParseNumber(string digits, out int value) =>
            int.TryParse(digits, NumberStyles.None, CultureInfo.InvariantCulture, out value);
    }
}
