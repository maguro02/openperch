using System;

namespace OpenPerch.Format
{
    /// <summary>
    /// A set of comparators separated by spaces that must all hold, for example "&gt;=1.0.0 &lt;1.0.3". Each comparator is
    /// one of &gt;=, &gt;, &lt;=, &lt; or = followed directly by a <see cref="SemVer"/>; a bare version means =.
    /// Pre-release versions are compared by plain SemVer precedence, so "1.0.3-beta" falls inside "&lt;1.0.3".
    /// </summary>
    public readonly struct SemVerRange
    {
        private readonly string[] _operators;
        private readonly SemVer[] _bounds;
        private readonly string _text;

        private SemVerRange(string[] operators, SemVer[] bounds, string text)
        {
            _operators = operators;
            _bounds = bounds;
            _text = text;
        }

        public static bool TryParse(string s, out SemVerRange r)
        {
            r = default;
            if (s == null) return false;
            string[] tokens = s.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
            if (tokens.Length == 0) return false;

            var operators = new string[tokens.Length];
            var bounds = new SemVer[tokens.Length];
            for (int i = 0; i < tokens.Length; i++)
            {
                string op = OperatorOf(tokens[i]);
                if (!SemVer.TryParse(tokens[i].Substring(op.Length), out bounds[i])) return false;
                operators[i] = op.Length == 0 ? "=" : op;
            }
            r = new SemVerRange(operators, bounds, s);
            return true;
        }

        /// <summary>False for the default value, which holds no comparators.</summary>
        public bool Contains(SemVer v)
        {
            if (_operators == null) return false;
            for (int i = 0; i < _operators.Length; i++)
            {
                int c = v.CompareTo(_bounds[i]);
                bool holds;
                switch (_operators[i])
                {
                    case ">=": holds = c >= 0; break;
                    case ">": holds = c > 0; break;
                    case "<=": holds = c <= 0; break;
                    case "<": holds = c < 0; break;
                    default: holds = c == 0; break;
                }
                if (!holds) return false;
            }
            return true;
        }

        public override string ToString() => _text ?? "";

        private static string OperatorOf(string token)
        {
            if (token.StartsWith(">=", StringComparison.Ordinal)) return ">=";
            if (token.StartsWith("<=", StringComparison.Ordinal)) return "<=";
            if (token.StartsWith(">", StringComparison.Ordinal)) return ">";
            if (token.StartsWith("<", StringComparison.Ordinal)) return "<";
            if (token.StartsWith("=", StringComparison.Ordinal)) return "=";
            return "";
        }
    }
}
