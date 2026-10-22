using System;
using NUnit.Framework;
using OpenPerch.App.Core;

namespace OpenPerch.Core.Tests.Desktop
{
    public class DetectFullscreenTests
    {
        private static readonly RectI Primary = GeometryTestData.PrimaryMonitor;

        [TestCase(3)]
        [TestCase(4)]
        [Category("T-GEO")]
        public void DesktopInTheForegroundIsNotFullscreen(int foregroundId)
        {
            var snapshot = GeometryTestData.Load("foreground-desktop").ToSnapshot();
            snapshot.ForegroundHwnd = new IntPtr(foregroundId);

            Assert.That(DesktopGeometry.DetectFullscreen(snapshot, Primary), Is.EqualTo(FullscreenState.None));
        }

        [TestCase(NotificationState.Busy, FullscreenState.Fullscreen)]
        [TestCase(NotificationState.D3DFullScreen, FullscreenState.Fullscreen)]
        [TestCase(NotificationState.PresentationMode, FullscreenState.Fullscreen)]
        [TestCase(NotificationState.AcceptsNotifications, FullscreenState.None)]
        [TestCase(NotificationState.QuietTime, FullscreenState.None)]
        [TestCase(NotificationState.NotPresent, FullscreenState.None)]
        [TestCase(NotificationState.App, FullscreenState.None)]
        [Category("T-GEO")]
        public void NotificationStateDecidesWhileTheDesktopIsInTheForeground(NotificationState notify, FullscreenState expected)
        {
            var snapshot = GeometryTestData.Load("foreground-desktop").ToSnapshot();
            snapshot.ForegroundHwnd = new IntPtr(4);
            snapshot.Notify = notify;

            Assert.That(DesktopGeometry.DetectFullscreen(snapshot, Primary), Is.EqualTo(expected));
        }

        [TestCase(0, FullscreenState.Fullscreen)]
        [TestCase(2, FullscreenState.Fullscreen)]
        [TestCase(-2, FullscreenState.Fullscreen)]
        [TestCase(3, FullscreenState.None)]
        [TestCase(-3, FullscreenState.None)]
        [Category("T-GEO")]
        public void ForegroundMustMatchTheMonitorWithinTwoPixels(int grow, FullscreenState expected)
        {
            var game = GeometryTestData.Window(1, WindowClass.Platform, new RectI(-grow, -grow, 1920 + grow, 1080 + grow), 0);

            Assert.That(DesktopGeometry.DetectFullscreen(WithForeground(game), Primary), Is.EqualTo(expected));
        }

        [Test]
        [Category("T-GEO")]
        public void MaximizedWindowAboveTheTaskbarIsNotFullscreen()
        {
            var editor = GeometryTestData.WithMaximized(
                GeometryTestData.Window(1, WindowClass.OccluderOnly, GeometryTestData.PrimaryWork, 0), true);

            Assert.That(DesktopGeometry.DetectFullscreen(WithForeground(editor), Primary), Is.EqualTo(FullscreenState.None));
        }

        [Test]
        [Category("T-GEO")]
        public void MaximizedWindowCoveringTheMonitorWithAnAutoHiddenTaskbarIsNotFullscreen()
        {
            var browser = GeometryTestData.WithMaximized(
                GeometryTestData.Window(1, WindowClass.OccluderOnly, GeometryTestData.PrimaryMonitor, 0), true);

            Assert.That(DesktopGeometry.DetectFullscreen(WithForeground(browser), Primary), Is.EqualTo(FullscreenState.None));
        }

        [TestCase(WindowClass.Platform, FullscreenState.Fullscreen)]
        [TestCase(WindowClass.OccluderOnly, FullscreenState.Fullscreen)]
        [TestCase(WindowClass.Self, FullscreenState.None)]
        [TestCase(WindowClass.Shell, FullscreenState.None)]
        [TestCase(WindowClass.Desktop, FullscreenState.None)]
        [TestCase(WindowClass.Taskbar, FullscreenState.None)]
        [TestCase(WindowClass.Ignored, FullscreenState.None)]
        [Category("T-GEO")]
        public void OnlyApplicationWindowsCanBeFullscreen(WindowClass kind, FullscreenState expected)
        {
            var window = GeometryTestData.Window(1, kind, Primary, 0);

            Assert.That(DesktopGeometry.DetectFullscreen(WithForeground(window), Primary), Is.EqualTo(expected));
        }

        [Test]
        [Category("T-GEO")]
        public void FullscreenWindowOnAnotherMonitorOnlyCountsForThatMonitor()
        {
            var secondary = new RectI(-1920, -120, 0, 960);
            var video = GeometryTestData.Window(1, WindowClass.OccluderOnly, new RectI(-1919, -121, 1, 959), 0);
            var snapshot = WithForeground(video);

            Assert.That(DesktopGeometry.DetectFullscreen(snapshot, Primary), Is.EqualTo(FullscreenState.None));
            Assert.That(DesktopGeometry.DetectFullscreen(snapshot, secondary), Is.EqualTo(FullscreenState.Fullscreen));
        }

        [Test]
        [Category("T-GEO")]
        public void ForegroundMissingFromTheSnapshotIsNotFullscreen()
        {
            var snapshot = WithForeground(GeometryTestData.Window(1, WindowClass.Platform, Primary, 0));

            snapshot.ForegroundHwnd = new IntPtr(99);
            Assert.That(DesktopGeometry.DetectFullscreen(snapshot, Primary), Is.EqualTo(FullscreenState.None));

            snapshot.ForegroundHwnd = IntPtr.Zero;
            Assert.That(DesktopGeometry.DetectFullscreen(snapshot, Primary), Is.EqualTo(FullscreenState.None));
        }

        private static DesktopSnapshot WithForeground(WindowInfo foreground)
        {
            return new DesktopSnapshot
            {
                Monitors = new[] { GeometryTestData.Monitor(Primary, GeometryTestData.PrimaryWork, primary: true) },
                Taskbars = new[] { new TaskbarInfo(new RectI(0, 1032, 1920, 1080), Edge.Bottom, false) },
                Windows = new[]
                {
                    foreground,
                    GeometryTestData.Window(90, WindowClass.Taskbar, new RectI(0, 1032, 1920, 1080), 1),
                },
                ForegroundHwnd = foreground.Hwnd,
            };
        }
    }
}
