using System;
using System.Runtime.InteropServices;

namespace OpenPerch.Native.Smoke
{
    internal static class Program
    {
        // The tray host runs its window and menus on an STA thread; the smoke mirrors that.
        [STAThread]
        private static int Main()
        {
            if (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                Console.Error.WriteLine("native-smoke must run on Windows.");
                return 2;
            }

            var report = new SmokeReport();
            report.Section("Declared entry points", () => DeclarationCheck.Run(report));
            report.Section("Per-monitor DPI awareness", () => ProcessChecks.PerMonitorDpiAwareness(report));
            report.Section("Single-instance mutex", () => ProcessChecks.SingleInstanceMutex(report));
            bool elevated = false;
            report.Section("Token elevation", () => elevated = ProcessChecks.TokenElevation(report));
            report.Section("User notification state", () => ProcessChecks.NotificationState(report));
            report.Section("High-resolution wait timer", () => ProcessChecks.HighResolutionTimer(report));
            report.Section("Input state", () => ProcessChecks.InputState(report));
            report.Section("Zone.Identifier", () => ZoneIdentifierCheck.Run(report));
            report.Section("Zone.Identifier on volumes without named streams", () => StreamlessVolumeCheck.Run(report, elevated));
            report.Section("Window enumeration", () => DesktopChecks.TopLevelWindows(report));
            report.Section("Shell windows", () => DesktopChecks.ShellWindows(report));
            report.Section("Monitor enumeration", () => DesktopChecks.DisplayMonitors(report));
            report.Section("Own window, tray and message loop", () => MessageWindowCheck.Run(report));
            return report.Finish();
        }
    }
}
