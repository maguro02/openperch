using System;

namespace OpenPerch.EnvDriver
{
    internal static class Program
    {
        [STAThread]
        private static int Main(string[] args)
        {
            Console.WriteLine("OpenPerch.EnvDriver");
            Console.WriteLine("Drives test windows for the W-ENV checks in docs/windows-test.md.");
            return 0;
        }
    }
}
