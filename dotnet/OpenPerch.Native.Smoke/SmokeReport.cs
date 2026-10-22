using System;
using System.Collections.Generic;
using OpenPerch.App.Platform.Windows.Native;

namespace OpenPerch.Native.Smoke
{
    /// <summary>
    /// Collects check results so a run reports every failure before it exits, instead of stopping at the first one.
    /// </summary>
    internal sealed class SmokeReport
    {
        private readonly List<string> _failures = new List<string>();
        private int _passed;

        public static string Describe(RECT rect) => $"[{rect.left}, {rect.top}, {rect.right}, {rect.bottom})";

        /// <summary>Runs one group of checks; an exception, such as a missing entry point, fails the group.</summary>
        public void Section(string title, Action checks)
        {
            Console.WriteLine();
            Console.WriteLine("== " + title);
            try
            {
                checks();
            }
            catch (Exception e)
            {
                Fail(title + " threw " + e);
            }
        }

        public void Require(bool condition, string description)
        {
            if (condition)
            {
                _passed++;
                Console.WriteLine("  pass  " + description);
            }
            else
            {
                Fail(description);
            }
        }

        /// <summary>Reports a result that depends on the session, such as whether a taskbar exists.</summary>
        public void Info(string message)
        {
            Console.WriteLine("  info  " + message);
        }

        /// <returns>The process exit code: 0 when every check passed.</returns>
        public int Finish()
        {
            Console.WriteLine();
            if (_failures.Count == 0)
            {
                Console.WriteLine($"native-smoke: OK ({_passed} checks)");
                return 0;
            }

            Console.WriteLine($"native-smoke: FAILED ({_failures.Count} failed, {_passed} passed)");
            foreach (string failure in _failures)
            {
                Console.WriteLine("  - " + failure);
            }
            return 1;
        }

        private void Fail(string description)
        {
            _failures.Add(description);
            Console.WriteLine("  FAIL  " + description);
        }
    }
}
