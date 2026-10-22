using System;
using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;

namespace OpenPerch.Format
{
    /// <summary>A Semantic Versioning 2.0.0 version, parsed strictly (no leading "v", no surrounding whitespace).</summary>
    public readonly struct SemVer : IComparable<SemVer>
    {
        private static readonly Regex Pattern = new Regex(
            @"^(0|[1-9][0-9]*)\.(0|[1-9][0-9]*)\.(0|[1-9][0-9]*)" +
            @"(?:-((?:0|[1-9][0-9]*|[0-9]*[a-zA-Z-][0-9a-zA-Z-]*)(?:\.(?:0|[1-9][0-9]*|[0-9]*[a-zA-Z-][0-9a-zA-Z-]*))*))?" +
            @"(?:\+([0-9a-zA-Z-]+(?:\.[0-9a-zA-Z-]+)*))?\z",
            RegexOptions.CultureInvariant);

        private readonly string _preRelease;
        private readonly string _buildMetadata;

        private SemVer(int major, int minor, int patch, string preRelease, string buildMetadata)
        {
            Major = major;
            Minor = minor;
            Patch = patch;
            _preRelease = preRelease;
            _buildMetadata = buildMetadata;
        }

        public int Major { get; }

        public int Minor { get; }

        public int Patch { get; }

        /// <summary>The part after "-", or "" for a release version.</summary>
        public string PreRelease => _preRelease ?? "";

        /// <summary>The part after "+", or "". Ignored when comparing.</summary>
        public string BuildMetadata => _buildMetadata ?? "";

        /// <summary>
        /// Parses "MAJOR.MINOR.PATCH" with an optional "-" pre-release and "+" build metadata as SemVer 2.0.0 defines them,
        /// for example "1.0.0" or "1.2.3-beta.1+build.5". Fails on a leading "v", any whitespace, leading zeros where
        /// SemVer forbids them, and MAJOR, MINOR or PATCH above <see cref="int.MaxValue"/>.
        /// </summary>
        public static bool TryParse(string s, out SemVer v)
        {
            v = default;
            if (s == null) return false;
            Match match = Pattern.Match(s);
            if (!match.Success) return false;
            if (!TryParseNumber(match.Groups[1].Value, out int major) ||
                !TryParseNumber(match.Groups[2].Value, out int minor) ||
                !TryParseNumber(match.Groups[3].Value, out int patch))
            {
                return false;
            }
            v = new SemVer(major, minor, patch, match.Groups[4].Value, match.Groups[5].Value);
            return true;
        }

        /// <summary>Orders by SemVer precedence; versions differing only in build metadata compare equal.</summary>
        public int CompareTo(SemVer o)
        {
            int c = Major.CompareTo(o.Major);
            if (c != 0) return c;
            c = Minor.CompareTo(o.Minor);
            if (c != 0) return c;
            c = Patch.CompareTo(o.Patch);
            return c != 0 ? c : ComparePreRelease(PreRelease, o.PreRelease);
        }

        public override string ToString()
        {
            var sb = new StringBuilder();
            sb.Append(Major.ToString(CultureInfo.InvariantCulture)).Append('.')
              .Append(Minor.ToString(CultureInfo.InvariantCulture)).Append('.')
              .Append(Patch.ToString(CultureInfo.InvariantCulture));
            if (PreRelease.Length > 0) sb.Append('-').Append(PreRelease);
            if (BuildMetadata.Length > 0) sb.Append('+').Append(BuildMetadata);
            return sb.ToString();
        }

        private static int ComparePreRelease(string a, string b)
        {
            if (a.Length == 0) return b.Length == 0 ? 0 : 1;
            if (b.Length == 0) return -1;
            string[] x = a.Split('.');
            string[] y = b.Split('.');
            int n = Math.Min(x.Length, y.Length);
            for (int i = 0; i < n; i++)
            {
                int c = CompareIdentifier(x[i], y[i]);
                if (c != 0) return c;
            }
            return x.Length.CompareTo(y.Length);
        }

        private static int CompareIdentifier(string a, string b)
        {
            bool aNumeric = IsNumeric(a);
            bool bNumeric = IsNumeric(b);
            if (aNumeric && bNumeric)
            {
                // Numeric identifiers have no leading zeros, so the longer one is larger; this avoids overflow on huge values.
                int c = a.Length.CompareTo(b.Length);
                return c != 0 ? c : Math.Sign(string.CompareOrdinal(a, b));
            }
            if (aNumeric) return -1;
            if (bNumeric) return 1;
            return Math.Sign(string.CompareOrdinal(a, b));
        }

        private static bool IsNumeric(string identifier)
        {
            foreach (char c in identifier)
            {
                if (c < '0' || c > '9') return false;
            }
            return true;
        }

        private static bool TryParseNumber(string digits, out int value) =>
            int.TryParse(digits, NumberStyles.None, CultureInfo.InvariantCulture, out value);
    }
}
