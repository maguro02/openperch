using System;
using System.Runtime.InteropServices;
using System.Threading;
using OpenPerch.App.Platform.Windows.Native;

namespace OpenPerch.Native.Smoke
{
    /// <summary>
    /// Drives a top-level window of its own through what the mascot window and the tray host do: DWM attributes,
    /// mascot styles, the NonRudeHWND property, menus, the notification icon, notifications and a message loop.
    /// </summary>
    internal static class MessageWindowCheck
    {
        private const uint WS_OVERLAPPEDWINDOW = 0x00CF0000;
        private const uint TrayCallbackMessage = User32.WM_APP + 1;
        private const int HotkeyId = 1;
        private const int PingValue = 42;
        private const int LoopWatchdogMilliseconds = 10_000;

        private static readonly WNDPROC WindowProcedure = WndProc;
        private static readonly uint RectSize = (uint)Marshal.SizeOf<RECT>();

        private static uint _pingMessage;
        private static bool _pinged;
        private static string _displayState = "not reported";

        public static void Run(SmokeReport report)
        {
            IntPtr module = Kernel32.GetModuleHandle(null);
            report.Require(module != IntPtr.Zero, "GetModuleHandleW returns the executable module");

            string className = "OpenPerch.Smoke." + Guid.NewGuid().ToString("N");
            var windowClass = new WNDCLASSEXW
            {
                cbSize = (uint)Marshal.SizeOf<WNDCLASSEXW>(),
                lpfnWndProc = Marshal.GetFunctionPointerForDelegate(WindowProcedure),
                hInstance = module,
                lpszClassName = className,
            };
            ushort atom = User32.RegisterClassEx(ref windowClass);
            int error = Marshal.GetLastWin32Error();
            report.Require(atom != 0, $"RegisterClassExW registers the window class (error {error})");
            if (atom == 0)
            {
                return;
            }

            IntPtr hwnd = IntPtr.Zero;
            try
            {
                hwnd = User32.CreateWindowEx(
                    0, className, "OpenPerch native smoke", WS_OVERLAPPEDWINDOW, 64, 64, 480, 270, IntPtr.Zero, IntPtr.Zero, module, IntPtr.Zero);
                error = Marshal.GetLastWin32Error();
                report.Require(hwnd != IntPtr.Zero, $"CreateWindowExW creates a top-level window (error {error})");
                if (hwnd == IntPtr.Zero)
                {
                    return;
                }

                // Windows may replace the show command of a process's first ShowWindow with the launcher's STARTUPINFO,
                // so a second call is what guarantees a shown window here.
                User32.ShowWindow(hwnd, User32.SW_SHOWNOACTIVATE);
                if (!User32.IsWindowVisible(hwnd))
                {
                    User32.ShowWindow(hwnd, User32.SW_SHOWNOACTIVATE);
                }
                report.Require(User32.FindWindow(className, null) == hwnd, "FindWindowW finds the window by its class name");

                WindowState(report, hwnd);
                DwmAttributes(report, hwnd);
                MascotStyles(report, hwnd);
                NonRudeProperty(report, hwnd);
                PopupMenu(report);
                NotificationIcon(report, hwnd);
                MessageLoop(report, hwnd);
            }
            finally
            {
                if (hwnd != IntPtr.Zero)
                {
                    report.Require(User32.DestroyWindow(hwnd), "DestroyWindow destroys the window");
                }
                report.Require(User32.UnregisterClass(className, module), "UnregisterClassW removes the window class");
            }
        }

        private static void WindowState(SmokeReport report, IntPtr hwnd)
        {
            report.Require(
                User32.IsWindow(hwnd) && User32.IsWindowVisible(hwnd) && !User32.IsIconic(hwnd) && !User32.IsZoomed(hwnd),
                "IsWindow, IsWindowVisible, IsIconic and IsZoomed describe a shown, restored window");

            uint thread = User32.GetWindowThreadProcessId(hwnd, out uint process);
            report.Require(
                thread == Kernel32.GetCurrentThreadId() && process == Kernel32.GetCurrentProcessId(),
                "GetWindowThreadProcessId reports this thread and process");

            report.Require(
                User32.GetWindowRect(hwnd, out RECT bounds) && bounds.right > bounds.left && bounds.bottom > bounds.top,
                $"GetWindowRect returns {SmokeReport.Describe(bounds)}");
        }

        private static void DwmAttributes(SmokeReport report, IntPtr hwnd)
        {
            int hr = Dwmapi.DwmGetWindowAttribute(hwnd, Dwmapi.DWMWA_EXTENDED_FRAME_BOUNDS, out RECT frame, RectSize);
            report.Require(
                hr == Kernel32.S_OK && frame.right > frame.left && frame.bottom > frame.top,
                $"DWMWA_EXTENDED_FRAME_BOUNDS returns {SmokeReport.Describe(frame)} (hr 0x{hr:X8})");

            hr = Dwmapi.DwmGetWindowAttribute(hwnd, Dwmapi.DWMWA_CLOAKED, out uint cloaked, sizeof(uint));
            report.Require(hr == Kernel32.S_OK && cloaked == 0, $"DWMWA_CLOAKED is 0 for a shown window (hr 0x{hr:X8}, value {cloaked})");

            hr = Dwmapi.DwmGetWindowAttribute(hwnd, Dwmapi.DWMWA_CAPTION_BUTTON_BOUNDS, out RECT caption, RectSize);
            report.Require(hr == Kernel32.S_OK, $"DWMWA_CAPTION_BUTTON_BOUNDS returns {SmokeReport.Describe(caption)} (hr 0x{hr:X8})");

            uint corner = Dwmapi.DWMWCP_DONOTROUND;
            int cornerHr = Dwmapi.DwmSetWindowAttribute(hwnd, Dwmapi.DWMWA_WINDOW_CORNER_PREFERENCE, ref corner, sizeof(uint));
            uint border = Dwmapi.DWMWA_COLOR_NONE;
            int borderHr = Dwmapi.DwmSetWindowAttribute(hwnd, Dwmapi.DWMWA_BORDER_COLOR, ref border, sizeof(uint));
            report.Info($"Windows 11 frame attributes: square corners hr 0x{cornerHr:X8}, no border color hr 0x{borderHr:X8}");
        }

        private static void MascotStyles(SmokeReport report, IntPtr hwnd)
        {
            const long wanted = User32.WS_EX_TOOLWINDOW | User32.WS_EX_NOACTIVATE | User32.WS_EX_TOPMOST;

            long before = User32.GetWindowLongPtr(hwnd, User32.GWL_EXSTYLE).ToInt64();
            User32.SetWindowLongPtr(hwnd, User32.GWL_EXSTYLE, new IntPtr(before | User32.WS_EX_TOOLWINDOW | User32.WS_EX_NOACTIVATE));
            bool placed = User32.SetWindowPos(
                hwnd, User32.HWND_TOPMOST, 96, 96, 480, 270, User32.SWP_NOACTIVATE | User32.SWP_FRAMECHANGED | User32.SWP_SHOWWINDOW);
            int error = Marshal.GetLastWin32Error();
            report.Require(placed, $"SetWindowPos(HWND_TOPMOST) applies (error {error})");

            long after = User32.GetWindowLongPtr(hwnd, User32.GWL_EXSTYLE).ToInt64();
            report.Require((after & wanted) == wanted, $"GetWindowLongPtrW reads back the tool, no-activate and topmost styles (0x{after:X})");

            bool wasVisible = User32.ShowWindow(hwnd, User32.SW_HIDE);
            report.Require(wasVisible && !User32.IsWindowVisible(hwnd), "ShowWindow(SW_HIDE) hides the shown window");
            User32.ShowWindow(hwnd, User32.SW_SHOWNOACTIVATE);
            report.Require(User32.IsWindowVisible(hwnd), "ShowWindow(SW_SHOWNOACTIVATE) shows it again");
        }

        private static void NonRudeProperty(SmokeReport report, IntPtr hwnd)
        {
            report.Require(User32.SetProp(hwnd, Shell32.NonRudeHwndProperty, new IntPtr(1)), "SetPropW sets NonRudeHWND");
            report.Require(User32.RemoveProp(hwnd, Shell32.NonRudeHwndProperty) == new IntPtr(1), "RemovePropW returns the stored value");
        }

        private static void PopupMenu(SmokeReport report)
        {
            IntPtr avatars = User32.CreatePopupMenu();
            IntPtr menu = User32.CreatePopupMenu();
            report.Require(avatars != IntPtr.Zero && menu != IntPtr.Zero, "CreatePopupMenu creates a menu and a submenu");
            if (avatars == IntPtr.Zero || menu == IntPtr.Zero)
            {
                return;
            }

            bool built = User32.AppendMenu(avatars, User32.MF_STRING | User32.MF_CHECKED, new IntPtr(101), "Avatar") &&
                User32.AppendMenu(menu, User32.MF_POPUP, avatars, "Avatars") &&
                User32.AppendMenu(menu, User32.MF_SEPARATOR, IntPtr.Zero, null) &&
                User32.AppendMenu(menu, User32.MF_STRING, new IntPtr(102), "Quit");
            report.Require(built, "AppendMenuW adds a checked item, a submenu, a separator and a command");
            report.Require(User32.DestroyMenu(menu), "DestroyMenu destroys the menu with its submenu");
        }

        private static void NotificationIcon(SmokeReport report, IntPtr hwnd)
        {
            int size = User32.GetSystemMetrics(User32.SM_CXSMICON);
            IntPtr icon = User32.LoadImage(IntPtr.Zero, new IntPtr(User32.IDI_APPLICATION), User32.IMAGE_ICON, size, size, User32.LR_SHARED);
            int error = Marshal.GetLastWin32Error();
            report.Require(icon != IntPtr.Zero, $"LoadImageW loads the stock application icon (error {error})");

            var data = new NOTIFYICONDATAW
            {
                cbSize = (uint)Marshal.SizeOf<NOTIFYICONDATAW>(),
                hWnd = hwnd,
                uID = 1,
                uFlags = Shell32.NIF_MESSAGE | Shell32.NIF_ICON | Shell32.NIF_TIP | Shell32.NIF_SHOWTIP,
                uCallbackMessage = TrayCallbackMessage,
                hIcon = icon,
                szTip = "OpenPerch native smoke",
            };

            // A session without Explorer has no notification area, so only the steps after a successful add are required.
            bool added = Shell32.Shell_NotifyIcon(Shell32.NIM_ADD, ref data);
            error = Marshal.GetLastWin32Error();
            if (!added)
            {
                report.Info($"Shell_NotifyIconW(NIM_ADD) failed (error {error}); the session has no notification area");
                return;
            }

            data.uVersion = Shell32.NOTIFYICON_VERSION_4;
            report.Require(Shell32.Shell_NotifyIcon(Shell32.NIM_SETVERSION, ref data), "Shell_NotifyIconW selects NOTIFYICON_VERSION_4");
            report.Require(Shell32.Shell_NotifyIcon(Shell32.NIM_DELETE, ref data), "Shell_NotifyIconW removes the icon");
        }

        private static void MessageLoop(SmokeReport report, IntPtr hwnd)
        {
            bool sessionRegistered = Wtsapi32.WTSRegisterSessionNotification(hwnd, Wtsapi32.NOTIFY_FOR_THIS_SESSION);
            int sessionError = Marshal.GetLastWin32Error();
            Guid displayState = User32.GUID_CONSOLE_DISPLAY_STATE;
            IntPtr powerRegistration = User32.RegisterPowerSettingNotification(hwnd, ref displayState, User32.DEVICE_NOTIFY_WINDOW_HANDLE);
            int powerError = Marshal.GetLastWin32Error();
            bool hotkey = User32.RegisterHotKey(hwnd, HotkeyId, User32.MOD_CONTROL | User32.MOD_ALT | User32.MOD_SHIFT | User32.MOD_NOREPEAT, User32.VK_Q);
            int hotkeyError = Marshal.GetLastWin32Error();
            report.Info(
                $"session notifications {Outcome(sessionRegistered, sessionError)}, display power notifications " +
                $"{Outcome(powerRegistration != IntPtr.Zero, powerError)}, Ctrl+Alt+Shift+Q hotkey {Outcome(hotkey, hotkeyError)}");

            try
            {
                _pingMessage = User32.RegisterWindowMessage("OpenPerch.Smoke.Ping");
                report.Require(_pingMessage != 0, "RegisterWindowMessageW returns a message id");

                uint thread = Kernel32.GetCurrentThreadId();
                report.Require(User32.PostThreadMessage(thread, User32.WM_NULL, IntPtr.Zero, IntPtr.Zero), "PostThreadMessageW queues a thread message");
                report.Require(User32.PostMessage(hwnd, _pingMessage, new IntPtr(PingValue), IntPtr.Zero), "PostMessageW queues the registered message");

                int result;
                // If the ping never arrives GetMessageW would block forever; the watchdog turns that into a failure, not a hung job.
                using (new Timer(_ => User32.PostThreadMessage(thread, User32.WM_QUIT, IntPtr.Zero, IntPtr.Zero), null, LoopWatchdogMilliseconds, Timeout.Infinite))
                {
                    while ((result = User32.GetMessage(out MSG message, IntPtr.Zero, 0, 0)) > 0)
                    {
                        User32.TranslateMessage(ref message);
                        User32.DispatchMessage(ref message);
                    }
                }

                report.Require(result == 0, $"GetMessageW returns 0 for WM_QUIT (result {result})");
                report.Require(_pinged, "the window procedure receives the posted message and PostQuitMessage ends the loop");
                report.Info("console display state: " + _displayState);
            }
            finally
            {
                if (hotkey)
                {
                    report.Require(User32.UnregisterHotKey(hwnd, HotkeyId), "UnregisterHotKey releases the hotkey");
                }
                if (powerRegistration != IntPtr.Zero)
                {
                    report.Require(User32.UnregisterPowerSettingNotification(powerRegistration), "UnregisterPowerSettingNotification releases the registration");
                }
                if (sessionRegistered)
                {
                    report.Require(Wtsapi32.WTSUnRegisterSessionNotification(hwnd), "WTSUnRegisterSessionNotification releases the registration");
                }
            }
        }

        private static string Outcome(bool registered, int error) => registered ? "registered" : $"failed (error {error})";

        [MonoPInvokeCallback(typeof(WNDPROC))]
        private static IntPtr WndProc(IntPtr hwnd, uint message, IntPtr wParam, IntPtr lParam)
        {
            if (_pingMessage != 0 && message == _pingMessage)
            {
                _pinged = wParam.ToInt64() == PingValue;
                User32.PostQuitMessage(0);
                return IntPtr.Zero;
            }

            if (message == User32.WM_POWERBROADCAST && wParam.ToInt64() == User32.PBT_POWERSETTINGCHANGE && lParam != IntPtr.Zero)
            {
                var setting = Marshal.PtrToStructure<POWERBROADCAST_SETTING>(lParam);
                if (setting.PowerSetting == User32.GUID_CONSOLE_DISPLAY_STATE)
                {
                    _displayState = $"{setting.Data} ({setting.DataLength}-byte value)";
                }
                return new IntPtr(1);
            }

            return User32.DefWindowProc(hwnd, message, wParam, lParam);
        }
    }
}
