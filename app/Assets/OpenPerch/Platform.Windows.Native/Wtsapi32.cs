using System;
using System.Runtime.InteropServices;

namespace OpenPerch.App.Platform.Windows.Native
{
    /// <summary>wtsapi32.dll declarations: session lock and unlock notifications.</summary>
    internal static class Wtsapi32
    {
        private const string Dll = "wtsapi32.dll";

        public const uint NOTIFY_FOR_THIS_SESSION = 0;

        public const uint WTS_SESSION_LOCK = 0x7;
        public const uint WTS_SESSION_UNLOCK = 0x8;

        [DllImport(Dll, CharSet = CharSet.Unicode, ExactSpelling = true, SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern bool WTSRegisterSessionNotification(IntPtr hWnd, uint dwFlags);

        [DllImport(Dll, CharSet = CharSet.Unicode, ExactSpelling = true, SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern bool WTSUnRegisterSessionNotification(IntPtr hWnd);
    }
}
