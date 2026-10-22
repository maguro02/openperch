using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using OpenPerch.App.Platform.Windows.Native;

namespace OpenPerch.Native.Smoke
{
    /// <summary>
    /// Walks the desktop the way the probe does. Window checks report counts only, nothing that identifies another
    /// application's window.
    /// </summary>
    internal static class DesktopChecks
    {
        private static readonly WNDENUMPROC EnumWindowsCallback = OnWindow;
        private static readonly MONITORENUMPROC EnumMonitorsCallback = OnMonitor;
        private static readonly List<IntPtr> FoundWindows = new List<IntPtr>();
        private static readonly List<IntPtr> FoundMonitors = new List<IntPtr>();
        private static readonly uint RectSize = (uint)Marshal.SizeOf<RECT>();

        public static void TopLevelWindows(SmokeReport report)
        {
            FoundWindows.Clear();
            bool completed = User32.EnumWindows(EnumWindowsCallback, IntPtr.Zero);
            int error = Marshal.GetLastWin32Error();
            report.Require(completed, $"EnumWindows completes ({FoundWindows.Count} top-level windows, error {error})");

            uint ownProcess = Kernel32.GetCurrentProcessId();
            int own = 0, visible = 0, iconic = 0, zoomed = 0, cloaked = 0, owned = 0;
            int frameBounds = 0, windowRects = 0, captionBounds = 0, layered = 0, layeredRead = 0;
            foreach (IntPtr hwnd in FoundWindows)
            {
                User32.GetWindowThreadProcessId(hwnd, out uint process);
                if (process == ownProcess)
                {
                    own++;
                }
                if (!User32.IsWindowVisible(hwnd))
                {
                    continue;
                }

                visible++;
                if (User32.IsIconic(hwnd))
                {
                    iconic++;
                }
                if (User32.IsZoomed(hwnd))
                {
                    zoomed++;
                }
                if (Dwmapi.DwmGetWindowAttribute(hwnd, Dwmapi.DWMWA_CLOAKED, out uint cloak, sizeof(uint)) == Kernel32.S_OK && cloak != 0)
                {
                    cloaked++;
                }
                if (Dwmapi.DwmGetWindowAttribute(hwnd, Dwmapi.DWMWA_EXTENDED_FRAME_BOUNDS, out RECT _, RectSize) == Kernel32.S_OK)
                {
                    frameBounds++;
                }
                if (User32.GetWindowRect(hwnd, out RECT _))
                {
                    windowRects++;
                }
                if (Dwmapi.DwmGetWindowAttribute(hwnd, Dwmapi.DWMWA_CAPTION_BUTTON_BOUNDS, out RECT _, RectSize) == Kernel32.S_OK)
                {
                    captionBounds++;
                }

                User32.GetWindowLongPtr(hwnd, User32.GWL_STYLE);
                long exStyle = User32.GetWindowLongPtr(hwnd, User32.GWL_EXSTYLE).ToInt64();
                if ((exStyle & User32.WS_EX_LAYERED) != 0)
                {
                    layered++;
                    if (User32.GetLayeredWindowAttributes(hwnd, out uint _, out byte _, out uint _))
                    {
                        layeredRead++;
                    }
                }
                if (User32.GetWindow(hwnd, User32.GW_OWNER) != IntPtr.Zero || User32.GetAncestor(hwnd, User32.GA_ROOTOWNER) != hwnd)
                {
                    owned++;
                }
                User32.GetLastActivePopup(hwnd);
            }

            report.Info($"visible {visible} (iconic {iconic}, zoomed {zoomed}, cloaked {cloaked}, owned {owned}); own process {own}");
            report.Info(
                $"extended frame bounds {frameBounds}, window rects {windowRects}, caption button bounds {captionBounds}, " +
                $"layered {layered} (attributes read {layeredRead})");
            report.Info("foreground window present: " + (User32.GetForegroundWindow() != IntPtr.Zero));
        }

        public static void ShellWindows(SmokeReport report)
        {
            IntPtr taskbar = User32.FindWindow("Shell_TrayWnd", null);
            IntPtr progman = User32.FindWindow("Progman", null);
            int secondaryTaskbars = CountTopLevel("Shell_SecondaryTrayWnd");
            int workers = CountTopLevel("WorkerW");

            int desktopViews = progman != IntPtr.Zero && HasDesktopView(progman) ? 1 : 0;
            IntPtr worker = IntPtr.Zero;
            while ((worker = User32.FindWindowEx(IntPtr.Zero, worker, "WorkerW", null)) != IntPtr.Zero)
            {
                if (HasDesktopView(worker))
                {
                    desktopViews++;
                }
            }

            report.Info(
                $"taskbar {taskbar != IntPtr.Zero}, secondary taskbars {secondaryTaskbars}, shell window {User32.GetShellWindow() != IntPtr.Zero}, " +
                $"Progman {progman != IntPtr.Zero}, WorkerW {workers}, SHELLDLL_DefView hosts {desktopViews}");
        }

        public static void DisplayMonitors(SmokeReport report)
        {
            FoundMonitors.Clear();
            bool completed = User32.EnumDisplayMonitors(IntPtr.Zero, IntPtr.Zero, EnumMonitorsCallback, IntPtr.Zero);
            report.Require(completed, $"EnumDisplayMonitors completes ({FoundMonitors.Count} monitors)");

            foreach (IntPtr monitor in FoundMonitors)
            {
                var info = new MONITORINFOEXW { cbSize = (uint)Marshal.SizeOf<MONITORINFOEXW>() };
                bool described = User32.GetMonitorInfo(monitor, ref info);
                report.Require(
                    described,
                    $"GetMonitorInfoW {info.szDevice}: monitor {SmokeReport.Describe(info.rcMonitor)}, work {SmokeReport.Describe(info.rcWork)}, " +
                    $"primary {(info.dwFlags & User32.MONITORINFOF_PRIMARY) != 0}");

                int hr = Shcore.GetDpiForMonitor(monitor, Shcore.MDT_EFFECTIVE_DPI, out uint dpiX, out uint dpiY);
                report.Require(hr == Kernel32.S_OK && dpiX > 0 && dpiY > 0, $"GetDpiForMonitor (hr 0x{hr:X8}, {dpiX}x{dpiY} dpi)");
            }

            IntPtr primary = User32.MonitorFromPoint(new POINT(0, 0), User32.MONITOR_DEFAULTTOPRIMARY);
            if (FoundMonitors.Count > 0)
            {
                report.Require(primary != IntPtr.Zero, "MonitorFromPoint falls back to the primary monitor");
            }
            else
            {
                report.Info("the session has no monitors; MonitorFromPoint returned " + (primary != IntPtr.Zero ? "a handle" : "null"));
            }

            var bar = new APPBARDATA { cbSize = (uint)Marshal.SizeOf<APPBARDATA>() };
            long state = Shell32.SHAppBarMessage(Shell32.ABM_GETSTATE, ref bar).ToInt64();
            bool positioned = Shell32.SHAppBarMessage(Shell32.ABM_GETTASKBARPOS, ref bar) != IntPtr.Zero;
            report.Info(
                $"taskbar auto-hide {(state & Shell32.ABS_AUTOHIDE) != 0}, " +
                (positioned ? $"bounds {SmokeReport.Describe(bar.rc)} on edge {bar.uEdge}" : "no taskbar position"));
        }

        private static int CountTopLevel(string className)
        {
            int count = 0;
            IntPtr after = IntPtr.Zero;
            while ((after = User32.FindWindowEx(IntPtr.Zero, after, className, null)) != IntPtr.Zero)
            {
                count++;
            }
            return count;
        }

        private static bool HasDesktopView(IntPtr host) => User32.FindWindowEx(host, IntPtr.Zero, "SHELLDLL_DefView", null) != IntPtr.Zero;

        [MonoPInvokeCallback(typeof(WNDENUMPROC))]
        private static bool OnWindow(IntPtr hwnd, IntPtr lParam)
        {
            FoundWindows.Add(hwnd);
            return true;
        }

        [MonoPInvokeCallback(typeof(MONITORENUMPROC))]
        private static bool OnMonitor(IntPtr hMonitor, IntPtr hdcMonitor, ref RECT lprcMonitor, IntPtr dwData)
        {
            FoundMonitors.Add(hMonitor);
            return true;
        }
    }
}
