using System;
using System.Runtime.InteropServices;

namespace OpenPerch.App.Platform.Windows.Native
{
    /// <summary>
    /// user32.dll declarations for the mascot window, input polling, desktop probing and the tray host.
    /// </summary>
    internal static class User32
    {
        private const string Dll = "user32.dll";

        public const uint WM_NULL = 0x0000;
        public const uint WM_DESTROY = 0x0002;
        public const uint WM_CLOSE = 0x0010;
        public const uint WM_QUERYENDSESSION = 0x0011;
        public const uint WM_QUIT = 0x0012;
        public const uint WM_ENDSESSION = 0x0016;
        public const uint WM_SETTINGCHANGE = 0x001A;
        public const uint WM_CONTEXTMENU = 0x007B;
        public const uint WM_DISPLAYCHANGE = 0x007E;
        public const uint WM_COMMAND = 0x0111;
        public const uint WM_POWERBROADCAST = 0x0218;
        public const uint WM_WTSSESSION_CHANGE = 0x02B1;
        public const uint WM_HOTKEY = 0x0312;
        public const uint WM_USER = 0x0400;
        public const uint WM_APP = 0x8000;

        public const uint PBT_APMSUSPEND = 0x0004;
        public const uint PBT_APMRESUMESUSPEND = 0x0007;
        public const uint PBT_APMRESUMEAUTOMATIC = 0x0012;
        public const uint PBT_POWERSETTINGCHANGE = 0x8013;

        public const uint SPI_SETWORKAREA = 0x002F;

        public const uint DEVICE_NOTIFY_WINDOW_HANDLE = 0x0;

        /// <summary>Power setting whose DWORD value is 0 (display off), 1 (on) or 2 (dimmed).</summary>
        public static readonly Guid GUID_CONSOLE_DISPLAY_STATE = new Guid("6FE69556-704A-47A0-8F24-C28D936FDA47");

        public const int GWL_STYLE = -16;
        public const int GWL_EXSTYLE = -20;

        public const uint WS_POPUP = 0x80000000;
        public const uint WS_EX_TOPMOST = 0x00000008;
        public const uint WS_EX_TRANSPARENT = 0x00000020;
        public const uint WS_EX_TOOLWINDOW = 0x00000080;
        public const uint WS_EX_APPWINDOW = 0x00040000;
        public const uint WS_EX_LAYERED = 0x00080000;
        public const uint WS_EX_NOACTIVATE = 0x08000000;

        public static readonly IntPtr HWND_TOPMOST = new IntPtr(-1);
        public static readonly IntPtr HWND_NOTOPMOST = new IntPtr(-2);

        public const uint SWP_NOSIZE = 0x0001;
        public const uint SWP_NOMOVE = 0x0002;
        public const uint SWP_NOZORDER = 0x0004;
        public const uint SWP_NOACTIVATE = 0x0010;
        public const uint SWP_FRAMECHANGED = 0x0020;
        public const uint SWP_SHOWWINDOW = 0x0040;
        public const uint SWP_HIDEWINDOW = 0x0080;
        public const uint SWP_NOOWNERZORDER = 0x0200;

        public const int SW_HIDE = 0;
        public const int SW_SHOWNOACTIVATE = 4;

        public const uint GW_OWNER = 4;
        public const uint GA_ROOTOWNER = 3;

        public const uint LWA_COLORKEY = 0x1;
        public const uint LWA_ALPHA = 0x2;

        public const uint MONITOR_DEFAULTTONULL = 0;
        public const uint MONITOR_DEFAULTTOPRIMARY = 1;
        public const uint MONITOR_DEFAULTTONEAREST = 2;
        public const uint MONITORINFOF_PRIMARY = 1;

        public static readonly IntPtr DPI_AWARENESS_CONTEXT_PER_MONITOR_AWARE_V2 = new IntPtr(-4);

        public const int SM_SWAPBUTTON = 23;
        public const int SM_CXSMICON = 49;
        public const int SM_CYSMICON = 50;

        public const int VK_LBUTTON = 0x01;
        public const int VK_RBUTTON = 0x02;
        public const int VK_MENU = 0x12;
        public const int VK_ESCAPE = 0x1B;
        public const int VK_H = 0x48;
        public const int VK_Q = 0x51;

        public const uint MOD_ALT = 0x0001;
        public const uint MOD_CONTROL = 0x0002;
        public const uint MOD_SHIFT = 0x0004;
        public const uint MOD_NOREPEAT = 0x4000;

        public const uint MF_STRING = 0x0000;
        public const uint MF_GRAYED = 0x0001;
        public const uint MF_CHECKED = 0x0008;
        public const uint MF_POPUP = 0x0010;
        public const uint MF_SEPARATOR = 0x0800;

        public const uint TPM_LEFTALIGN = 0x0000;
        public const uint TPM_RIGHTBUTTON = 0x0002;
        public const uint TPM_RIGHTALIGN = 0x0008;
        public const uint TPM_BOTTOMALIGN = 0x0020;
        public const uint TPM_NONOTIFY = 0x0080;
        public const uint TPM_RETURNCMD = 0x0100;

        public const uint IMAGE_ICON = 1;
        public const uint LR_DEFAULTCOLOR = 0x0000;
        public const uint LR_LOADFROMFILE = 0x0010;
        public const uint LR_DEFAULTSIZE = 0x0040;
        public const uint LR_SHARED = 0x8000;
        public const int IDI_APPLICATION = 32512;

        [DllImport(Dll, EntryPoint = "FindWindowW", CharSet = CharSet.Unicode, ExactSpelling = true, SetLastError = true)]
        public static extern IntPtr FindWindow(string lpClassName, string lpWindowName);

        [DllImport(Dll, EntryPoint = "FindWindowExW", CharSet = CharSet.Unicode, ExactSpelling = true, SetLastError = true)]
        public static extern IntPtr FindWindowEx(IntPtr hWndParent, IntPtr hWndChildAfter, string lpszClass, string lpszWindow);

        [DllImport(Dll, EntryPoint = "RegisterWindowMessageW", CharSet = CharSet.Unicode, ExactSpelling = true, SetLastError = true)]
        public static extern uint RegisterWindowMessage(string lpString);

        [DllImport(Dll, EntryPoint = "PostMessageW", CharSet = CharSet.Unicode, ExactSpelling = true, SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern bool PostMessage(IntPtr hWnd, uint Msg, IntPtr wParam, IntPtr lParam);

        [DllImport(Dll, EntryPoint = "PostThreadMessageW", CharSet = CharSet.Unicode, ExactSpelling = true, SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern bool PostThreadMessage(uint idThread, uint Msg, IntPtr wParam, IntPtr lParam);

        // user32 exports the LongPtr pair only from its 64-bit build; on x86 they are macros over GetWindowLongW.
        // The player ships for Win64 alone, so binding the 64-bit export directly is safe.
        [DllImport(Dll, EntryPoint = "GetWindowLongPtrW", CharSet = CharSet.Unicode, ExactSpelling = true, SetLastError = true)]
        public static extern IntPtr GetWindowLongPtr(IntPtr hWnd, int nIndex);

        [DllImport(Dll, EntryPoint = "SetWindowLongPtrW", CharSet = CharSet.Unicode, ExactSpelling = true, SetLastError = true)]
        public static extern IntPtr SetWindowLongPtr(IntPtr hWnd, int nIndex, IntPtr dwNewLong);

        [DllImport(Dll, CharSet = CharSet.Unicode, ExactSpelling = true, SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern bool SetWindowPos(IntPtr hWnd, IntPtr hWndInsertAfter, int X, int Y, int cx, int cy, uint uFlags);

        /// <returns>Whether the window was visible before the call, not whether the call succeeded.</returns>
        [DllImport(Dll, CharSet = CharSet.Unicode, ExactSpelling = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern bool ShowWindow(IntPtr hWnd, int nCmdShow);

        [DllImport(Dll, EntryPoint = "SetPropW", CharSet = CharSet.Unicode, ExactSpelling = true, SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern bool SetProp(IntPtr hWnd, string lpString, IntPtr hData);

        [DllImport(Dll, EntryPoint = "RemovePropW", CharSet = CharSet.Unicode, ExactSpelling = true)]
        public static extern IntPtr RemoveProp(IntPtr hWnd, string lpString);

        [DllImport(Dll, CharSet = CharSet.Unicode, ExactSpelling = true, SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern bool GetCursorPos(out POINT lpPoint);

        [DllImport(Dll, CharSet = CharSet.Unicode, ExactSpelling = true)]
        public static extern short GetAsyncKeyState(int vKey);

        [DllImport(Dll, CharSet = CharSet.Unicode, ExactSpelling = true)]
        public static extern int GetSystemMetrics(int nIndex);

        [DllImport(Dll, CharSet = CharSet.Unicode, ExactSpelling = true)]
        public static extern uint GetDoubleClickTime();

        /// <returns>The previous context, or <see cref="IntPtr.Zero"/> when <paramref name="dpiContext"/> was rejected.</returns>
        [DllImport(Dll, CharSet = CharSet.Unicode, ExactSpelling = true)]
        public static extern IntPtr SetThreadDpiAwarenessContext(IntPtr dpiContext);

        [DllImport(Dll, CharSet = CharSet.Unicode, ExactSpelling = true, SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern bool EnumWindows(WNDENUMPROC lpEnumFunc, IntPtr lParam);

        [DllImport(Dll, CharSet = CharSet.Unicode, ExactSpelling = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern bool IsWindow(IntPtr hWnd);

        [DllImport(Dll, CharSet = CharSet.Unicode, ExactSpelling = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern bool IsWindowVisible(IntPtr hWnd);

        [DllImport(Dll, CharSet = CharSet.Unicode, ExactSpelling = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern bool IsIconic(IntPtr hWnd);

        [DllImport(Dll, CharSet = CharSet.Unicode, ExactSpelling = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern bool IsZoomed(IntPtr hWnd);

        [DllImport(Dll, CharSet = CharSet.Unicode, ExactSpelling = true, SetLastError = true)]
        public static extern uint GetWindowThreadProcessId(IntPtr hWnd, out uint lpdwProcessId);

        [DllImport(Dll, CharSet = CharSet.Unicode, ExactSpelling = true, SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern bool GetWindowRect(IntPtr hWnd, out RECT lpRect);

        [DllImport(Dll, CharSet = CharSet.Unicode, ExactSpelling = true, SetLastError = true)]
        public static extern IntPtr GetWindow(IntPtr hWnd, uint uCmd);

        [DllImport(Dll, CharSet = CharSet.Unicode, ExactSpelling = true)]
        public static extern IntPtr GetAncestor(IntPtr hwnd, uint gaFlags);

        [DllImport(Dll, CharSet = CharSet.Unicode, ExactSpelling = true)]
        public static extern IntPtr GetLastActivePopup(IntPtr hWnd);

        [DllImport(Dll, CharSet = CharSet.Unicode, ExactSpelling = true)]
        public static extern IntPtr GetShellWindow();

        [DllImport(Dll, CharSet = CharSet.Unicode, ExactSpelling = true)]
        public static extern IntPtr GetForegroundWindow();

        [DllImport(Dll, CharSet = CharSet.Unicode, ExactSpelling = true, SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern bool GetLayeredWindowAttributes(IntPtr hwnd, out uint pcrKey, out byte pbAlpha, out uint pdwFlags);

        [DllImport(Dll, CharSet = CharSet.Unicode, ExactSpelling = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern bool EnumDisplayMonitors(IntPtr hdc, IntPtr lprcClip, MONITORENUMPROC lpfnEnum, IntPtr dwData);

        [DllImport(Dll, EntryPoint = "GetMonitorInfoW", CharSet = CharSet.Unicode, ExactSpelling = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern bool GetMonitorInfo(IntPtr hMonitor, ref MONITORINFOEXW lpmi);

        [DllImport(Dll, CharSet = CharSet.Unicode, ExactSpelling = true)]
        public static extern IntPtr MonitorFromPoint(POINT pt, uint dwFlags);

        [DllImport(Dll, EntryPoint = "RegisterClassExW", CharSet = CharSet.Unicode, ExactSpelling = true, SetLastError = true)]
        public static extern ushort RegisterClassEx([In] ref WNDCLASSEXW lpwcx);

        [DllImport(Dll, EntryPoint = "UnregisterClassW", CharSet = CharSet.Unicode, ExactSpelling = true, SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern bool UnregisterClass(string lpClassName, IntPtr hInstance);

        [DllImport(Dll, EntryPoint = "CreateWindowExW", CharSet = CharSet.Unicode, ExactSpelling = true, SetLastError = true)]
        public static extern IntPtr CreateWindowEx(
            uint dwExStyle,
            string lpClassName,
            string lpWindowName,
            uint dwStyle,
            int X,
            int Y,
            int nWidth,
            int nHeight,
            IntPtr hWndParent,
            IntPtr hMenu,
            IntPtr hInstance,
            IntPtr lpParam);

        [DllImport(Dll, CharSet = CharSet.Unicode, ExactSpelling = true, SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern bool DestroyWindow(IntPtr hWnd);

        [DllImport(Dll, EntryPoint = "DefWindowProcW", CharSet = CharSet.Unicode, ExactSpelling = true)]
        public static extern IntPtr DefWindowProc(IntPtr hWnd, uint Msg, IntPtr wParam, IntPtr lParam);

        /// <returns>Positive for a message, 0 for <see cref="WM_QUIT"/>, -1 on error.</returns>
        [DllImport(Dll, EntryPoint = "GetMessageW", CharSet = CharSet.Unicode, ExactSpelling = true, SetLastError = true)]
        public static extern int GetMessage(out MSG lpMsg, IntPtr hWnd, uint wMsgFilterMin, uint wMsgFilterMax);

        [DllImport(Dll, CharSet = CharSet.Unicode, ExactSpelling = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern bool TranslateMessage([In] ref MSG lpMsg);

        [DllImport(Dll, EntryPoint = "DispatchMessageW", CharSet = CharSet.Unicode, ExactSpelling = true)]
        public static extern IntPtr DispatchMessage([In] ref MSG lpMsg);

        [DllImport(Dll, CharSet = CharSet.Unicode, ExactSpelling = true)]
        public static extern void PostQuitMessage(int nExitCode);

        [DllImport(Dll, CharSet = CharSet.Unicode, ExactSpelling = true, SetLastError = true)]
        public static extern IntPtr CreatePopupMenu();

        /// <param name="uIDNewItem">The command id, or the submenu handle when <paramref name="uFlags"/> has <see cref="MF_POPUP"/>.</param>
        [DllImport(Dll, EntryPoint = "AppendMenuW", CharSet = CharSet.Unicode, ExactSpelling = true, SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern bool AppendMenu(IntPtr hMenu, uint uFlags, IntPtr uIDNewItem, string lpNewItem);

        [DllImport(Dll, CharSet = CharSet.Unicode, ExactSpelling = true, SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern bool DestroyMenu(IntPtr hMenu);

        /// <returns>With <see cref="TPM_RETURNCMD"/>, the chosen command id, or 0 when the menu was dismissed.</returns>
        [DllImport(Dll, CharSet = CharSet.Unicode, ExactSpelling = true, SetLastError = true)]
        public static extern int TrackPopupMenuEx(IntPtr hMenu, uint uFlags, int x, int y, IntPtr hwnd, IntPtr lptpm);

        [DllImport(Dll, CharSet = CharSet.Unicode, ExactSpelling = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern bool SetForegroundWindow(IntPtr hWnd);

        [DllImport(Dll, CharSet = CharSet.Unicode, ExactSpelling = true, SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern bool RegisterHotKey(IntPtr hWnd, int id, uint fsModifiers, uint vk);

        [DllImport(Dll, CharSet = CharSet.Unicode, ExactSpelling = true, SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern bool UnregisterHotKey(IntPtr hWnd, int id);

        [DllImport(Dll, CharSet = CharSet.Unicode, ExactSpelling = true, SetLastError = true)]
        public static extern IntPtr RegisterPowerSettingNotification(IntPtr hRecipient, [In] ref Guid PowerSettingGuid, uint Flags);

        [DllImport(Dll, CharSet = CharSet.Unicode, ExactSpelling = true, SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern bool UnregisterPowerSettingNotification(IntPtr Handle);

        /// <param name="name">A file path together with <see cref="LR_LOADFROMFILE"/>, or a resource name in <paramref name="hInst"/>.</param>
        [DllImport(Dll, EntryPoint = "LoadImageW", CharSet = CharSet.Unicode, ExactSpelling = true, SetLastError = true)]
        public static extern IntPtr LoadImage(IntPtr hInst, string name, uint type, int cx, int cy, uint fuLoad);

        /// <param name="name">A resource ordinal; with a null <paramref name="hInst"/>, a stock image such as <see cref="IDI_APPLICATION"/>.</param>
        [DllImport(Dll, EntryPoint = "LoadImageW", CharSet = CharSet.Unicode, ExactSpelling = true, SetLastError = true)]
        public static extern IntPtr LoadImage(IntPtr hInst, IntPtr name, uint type, int cx, int cy, uint fuLoad);

        [DllImport(Dll, CharSet = CharSet.Unicode, ExactSpelling = true, SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern bool DestroyIcon(IntPtr hIcon);
    }
}
