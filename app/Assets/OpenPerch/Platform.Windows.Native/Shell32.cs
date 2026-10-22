using System;
using System.Runtime.InteropServices;

namespace OpenPerch.App.Platform.Windows.Native
{
    /// <summary>
    /// shell32.dll declarations: the notification-area icon, taskbar state and the user notification state.
    /// </summary>
    internal static class Shell32
    {
        private const string Dll = "shell32.dll";

        public const uint NIM_ADD = 0x0;
        public const uint NIM_MODIFY = 0x1;
        public const uint NIM_DELETE = 0x2;
        public const uint NIM_SETVERSION = 0x4;

        public const uint NIF_MESSAGE = 0x01;
        public const uint NIF_ICON = 0x02;
        public const uint NIF_TIP = 0x04;
        public const uint NIF_INFO = 0x10;
        public const uint NIF_SHOWTIP = 0x80;

        public const uint NOTIFYICON_VERSION_4 = 4;

        public const uint NIIF_NONE = 0x00;
        public const uint NIIF_INFO = 0x01;
        public const uint NIIF_NOSOUND = 0x10;
        public const uint NIIF_RESPECT_QUIET_TIME = 0x80;

        public const uint NIN_SELECT = User32.WM_USER + 0;
        public const uint NIN_KEYSELECT = User32.WM_USER + 1;
        public const uint NIN_BALLOONSHOW = User32.WM_USER + 2;
        public const uint NIN_BALLOONHIDE = User32.WM_USER + 3;
        public const uint NIN_BALLOONTIMEOUT = User32.WM_USER + 4;
        public const uint NIN_BALLOONUSERCLICK = User32.WM_USER + 5;

        public const uint ABM_GETSTATE = 0x4;
        public const uint ABM_GETTASKBARPOS = 0x5;
        public const uint ABM_GETAUTOHIDEBAREX = 0xB;

        public const uint ABS_AUTOHIDE = 0x1;

        public const uint ABE_LEFT = 0;
        public const uint ABE_TOP = 1;
        public const uint ABE_RIGHT = 2;
        public const uint ABE_BOTTOM = 3;

        public const int QUNS_NOT_PRESENT = 1;
        public const int QUNS_BUSY = 2;
        public const int QUNS_RUNNING_D3D_FULL_SCREEN = 3;
        public const int QUNS_PRESENTATION_MODE = 4;
        public const int QUNS_ACCEPTS_NOTIFICATIONS = 5;
        public const int QUNS_QUIET_TIME = 6;
        public const int QUNS_APP = 7;

        /// <summary>Registered message Explorer broadcasts after it restarts; tray icons must be added again.</summary>
        public const string TaskbarCreatedMessage = "TaskbarCreated";

        /// <summary>Window property that tells the shell a topmost, monitor-sized window is not a full-screen app.</summary>
        public const string NonRudeHwndProperty = "NonRudeHWND";

        [DllImport(Dll, EntryPoint = "Shell_NotifyIconW", CharSet = CharSet.Unicode, ExactSpelling = true, SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern bool Shell_NotifyIcon(uint dwMessage, [In] ref NOTIFYICONDATAW lpData);

        [DllImport(Dll, CharSet = CharSet.Unicode, ExactSpelling = true)]
        public static extern IntPtr SHAppBarMessage(uint dwMessage, ref APPBARDATA pData);

        /// <returns>An HRESULT; <paramref name="pquns"/> receives one of the <c>QUNS_*</c> values.</returns>
        [DllImport(Dll, CharSet = CharSet.Unicode, ExactSpelling = true)]
        public static extern int SHQueryUserNotificationState(out int pquns);
    }
}
