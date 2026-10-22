using System;
using System.Diagnostics;
using System.Runtime.InteropServices;
using OpenPerch.App.Platform.Windows.Native;

namespace OpenPerch.Native.Smoke
{
    /// <summary>Checks that need no window: thread setup, single instance, elevation, notification state, timer and input.</summary>
    internal static class ProcessChecks
    {
        private const long RelativeTwoMilliseconds = -20_000;

        public static void PerMonitorDpiAwareness(SmokeReport report)
        {
            IntPtr previous = User32.SetThreadDpiAwarenessContext(User32.DPI_AWARENESS_CONTEXT_PER_MONITOR_AWARE_V2);

            report.Require(previous != IntPtr.Zero, "SetThreadDpiAwarenessContext switches this thread to per-monitor v2");
        }

        public static void SingleInstanceMutex(SmokeReport report)
        {
            string name = "Local\\OpenPerch.Smoke." + Guid.NewGuid().ToString("N");
            IntPtr first = Kernel32.CreateMutex(IntPtr.Zero, false, name);
            int firstError = Marshal.GetLastWin32Error();
            IntPtr second = Kernel32.CreateMutex(IntPtr.Zero, false, name);
            int secondError = Marshal.GetLastWin32Error();
            try
            {
                report.Require(
                    first != IntPtr.Zero && firstError != Kernel32.ERROR_ALREADY_EXISTS,
                    $"the first CreateMutexW creates the mutex (error {firstError})");
                report.Require(
                    second != IntPtr.Zero && secondError == Kernel32.ERROR_ALREADY_EXISTS,
                    $"the second CreateMutexW reports ERROR_ALREADY_EXISTS (error {secondError})");
            }
            finally
            {
                if (second != IntPtr.Zero)
                {
                    report.Require(Kernel32.CloseHandle(second), "CloseHandle releases the second mutex handle");
                }
                if (first != IntPtr.Zero)
                {
                    report.Require(Kernel32.CloseHandle(first), "CloseHandle releases the first mutex handle");
                }
            }
        }

        public static void TokenElevation(SmokeReport report)
        {
            bool opened = Advapi32.OpenProcessToken(Kernel32.GetCurrentProcess(), Advapi32.TOKEN_QUERY, out IntPtr token);
            int error = Marshal.GetLastWin32Error();
            report.Require(opened, $"OpenProcessToken opens this process's token (error {error})");
            if (!opened)
            {
                return;
            }

            try
            {
                uint size = (uint)Marshal.SizeOf<TOKEN_ELEVATION>();
                bool read = Advapi32.GetTokenInformation(token, Advapi32.TokenElevation, out TOKEN_ELEVATION elevation, size, out uint written);
                error = Marshal.GetLastWin32Error();
                report.Require(read && written == size, $"GetTokenInformation(TokenElevation) fills TOKEN_ELEVATION (error {error}, {written} bytes)");
                report.Info("elevated: " + (elevation.TokenIsElevated != 0));
            }
            finally
            {
                report.Require(Kernel32.CloseHandle(token), "CloseHandle releases the token");
            }
        }

        public static void NotificationState(SmokeReport report)
        {
            int hr = Shell32.SHQueryUserNotificationState(out int state);

            report.Require(
                hr == Kernel32.S_OK && state >= Shell32.QUNS_NOT_PRESENT && state <= Shell32.QUNS_APP,
                $"SHQueryUserNotificationState returns a QUNS value (hr 0x{hr:X8}, state {state})");
        }

        public static void HighResolutionTimer(SmokeReport report)
        {
            IntPtr timer = Kernel32.CreateWaitableTimerEx(
                IntPtr.Zero, null, Kernel32.CREATE_WAITABLE_TIMER_HIGH_RESOLUTION, Kernel32.TIMER_ALL_ACCESS);
            int error = Marshal.GetLastWin32Error();
            report.Require(timer != IntPtr.Zero, $"CreateWaitableTimerExW creates a high-resolution timer (error {error})");
            if (timer == IntPtr.Zero)
            {
                return;
            }

            try
            {
                long dueTime = RelativeTwoMilliseconds;
                bool armed = Kernel32.SetWaitableTimer(timer, ref dueTime, 0, IntPtr.Zero, IntPtr.Zero, false);
                error = Marshal.GetLastWin32Error();
                report.Require(armed, $"SetWaitableTimer arms the timer (error {error})");

                var elapsed = Stopwatch.StartNew();
                uint wait = Kernel32.WaitForSingleObject(timer, 1000);
                report.Require(
                    wait == Kernel32.WAIT_OBJECT_0,
                    $"WaitForSingleObject wakes on the timer (result 0x{wait:X}, {elapsed.Elapsed.TotalMilliseconds:F2} ms)");
            }
            finally
            {
                report.Require(Kernel32.CloseHandle(timer), "CloseHandle releases the timer");
            }
        }

        public static void InputState(SmokeReport report)
        {
            bool gotCursor = User32.GetCursorPos(out POINT cursor);
            int error = Marshal.GetLastWin32Error();
            report.Info(gotCursor ? $"cursor at ({cursor.x}, {cursor.y})" : $"GetCursorPos failed (error {error}); the session has no input desktop");
            report.Info(
                $"keys down: left {IsDown(User32.VK_LBUTTON)}, right {IsDown(User32.VK_RBUTTON)}, alt {IsDown(User32.VK_MENU)}, " +
                $"esc {IsDown(User32.VK_ESCAPE)}; buttons swapped {User32.GetSystemMetrics(User32.SM_SWAPBUTTON) != 0}");

            uint doubleClick = User32.GetDoubleClickTime();
            report.Require(doubleClick > 0, $"GetDoubleClickTime returns the interval ({doubleClick} ms)");
        }

        private static bool IsDown(int virtualKey) => (User32.GetAsyncKeyState(virtualKey) & 0x8000) != 0;
    }
}
