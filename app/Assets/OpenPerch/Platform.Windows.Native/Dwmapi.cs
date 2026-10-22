using System;
using System.Runtime.InteropServices;

namespace OpenPerch.App.Platform.Windows.Native
{
    /// <summary>
    /// dwmapi.dll declarations: visible frame bounds, cloaking and caption buttons of other windows, and the frame of our own.
    /// </summary>
    internal static class Dwmapi
    {
        private const string Dll = "dwmapi.dll";

        /// <summary>
        /// Caption button bounds relative to the top-left of <see cref="User32.GetWindowRect"/>, not of the extended frame
        /// bounds, whose left edge lies inside the invisible resize border. Undefined while the window is minimized,
        /// cloaked or hidden.
        /// </summary>
        public const uint DWMWA_CAPTION_BUTTON_BOUNDS = 5;

        public const uint DWMWA_EXTENDED_FRAME_BOUNDS = 9;
        public const uint DWMWA_CLOAKED = 14;

        // Windows 10 rejects these two attributes with E_INVALIDARG; only Windows 11 draws the rounded, colored frame they remove.
        public const uint DWMWA_WINDOW_CORNER_PREFERENCE = 33;
        public const uint DWMWA_BORDER_COLOR = 34;

        public const uint DWMWCP_DONOTROUND = 1;
        public const uint DWMWA_COLOR_NONE = 0xFFFFFFFE;

        /// <returns>An HRESULT.</returns>
        [DllImport(Dll, CharSet = CharSet.Unicode, ExactSpelling = true)]
        public static extern int DwmGetWindowAttribute(IntPtr hwnd, uint dwAttribute, out RECT pvAttribute, uint cbAttribute);

        /// <returns>An HRESULT.</returns>
        [DllImport(Dll, CharSet = CharSet.Unicode, ExactSpelling = true)]
        public static extern int DwmGetWindowAttribute(IntPtr hwnd, uint dwAttribute, out uint pvAttribute, uint cbAttribute);

        /// <returns>An HRESULT.</returns>
        [DllImport(Dll, CharSet = CharSet.Unicode, ExactSpelling = true)]
        public static extern int DwmSetWindowAttribute(IntPtr hwnd, uint dwAttribute, [In] ref uint pvAttribute, uint cbAttribute);
    }
}
