using System;

namespace OpenPerch.App.Core
{
    public readonly struct WindowInfo
    {
        public readonly IntPtr Hwnd;
        public readonly uint Pid;
        public readonly RectI Bounds;

        /// <summary>Empty when DWM could not report the caption button bounds.</summary>
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
