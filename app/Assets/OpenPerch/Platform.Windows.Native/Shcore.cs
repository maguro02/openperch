using System;
using System.Runtime.InteropServices;

namespace OpenPerch.App.Platform.Windows.Native
{
    /// <summary>shcore.dll declarations: per-monitor DPI.</summary>
    internal static class Shcore
    {
        private const string Dll = "shcore.dll";

        public const int MDT_EFFECTIVE_DPI = 0;

        /// <returns>An HRESULT. The DPI reflects the calling thread's awareness, so call it from a per-monitor v2 thread.</returns>
        [DllImport(Dll, CharSet = CharSet.Unicode, ExactSpelling = true)]
        public static extern int GetDpiForMonitor(IntPtr hmonitor, int dpiType, out uint dpiX, out uint dpiY);
    }
}
