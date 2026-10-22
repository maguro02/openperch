using System;

namespace OpenPerch.App.Core
{
    public readonly struct WindowInfo
    {
        public readonly IntPtr Hwnd;
        public readonly uint Pid;
        public readonly RectI Bounds;

        /// <summary>
        /// Virtual-screen physical pixels, like <see cref="Bounds"/>. DWM reports the caption buttons relative to the window,
        /// so the probe offsets them by the window position. Empty when DWM could not report them.
        /// </summary>
        public readonly RectI CaptionButtons;

        public readonly WindowClass Class;
        public readonly bool Maximized;

        /// <summary>Position in the snapshot's Z order; 0 is the topmost window.</summary>
        public readonly int Z;

        public WindowInfo(IntPtr hwnd, uint pid, RectI bounds, RectI captionButtons, WindowClass windowClass, bool maximized, int z)
        {
            Hwnd = hwnd;
            Pid = pid;
            Bounds = bounds;
            CaptionButtons = captionButtons;
            Class = windowClass;
            Maximized = maximized;
            Z = z;
        }
    }
}
