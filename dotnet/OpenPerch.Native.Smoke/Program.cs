using System;
using System.Runtime.InteropServices;

namespace OpenPerch.Native.Smoke
{
    internal static class Program
    {
        private static int Main()
        {
            if (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                Console.Error.WriteLine("native-smoke must run on Windows.");
                return 2;
            }

            Console.WriteLine("native-smoke: OK");
            return 0;
        }
    }
}
