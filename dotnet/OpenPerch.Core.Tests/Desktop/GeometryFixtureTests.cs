using System;
using System.IO;
using System.Linq;
using NUnit.Framework;
using OpenPerch.App.Core;
using OpenPerch.Core.Tests.Support;

namespace OpenPerch.Core.Tests.Desktop
{
    public class GeometryFixtureTests
    {
        [Test]
        [Category("T-GEO")]
        public void EveryFixtureFileIsWellFormed()
        {
            var codec = new StjCodec();
            var files = Directory.GetFiles(GeometryTestData.FixtureDirectory, "*.json");

            Assert.That(files, Has.Length.GreaterThanOrEqualTo(13));
            Assert.Multiple(() =>
            {
                foreach (var file in files)
                {
                    var name = Path.GetFileName(file);
                    var fixture = codec.Deserialize<GeometryFixture>(File.ReadAllText(file));
                    Assert.That(fixture.schemaVersion, Is.EqualTo(1), name);
                    Assert.That(fixture.client.IsEmpty(), Is.False, name);
                    Assert.That(fixture.monitors, Is.Not.Empty, name);
                    Assert.That(fixture.windows.Select(w => w.id), Is.Unique, name);
                    Assert.That(fixture.windows.Select(w => w.id), Has.All.GreaterThan(0), name);
                    Assert.That(fixture.windows.Select(w => w.pid), Has.All.GreaterThan(0), name);
                    Assert.That(fixture.windows.Select(w => w.z), Is.Unique, name);
                }
            });
        }

        [Test]
        [Category("T-GEO")]
        public void FromReplacesHandlesAndProcessIdsWithSequentialNumbers()
        {
            var fixture = GeometryFixture.From(DesktopWithThreeWindows());

            Assert.That(fixture.windows.Select(w => w.id), Is.EqualTo(new[] { 1, 2, 3 }));
            Assert.That(fixture.windows.Select(w => w.pid), Is.EqualTo(new[] { 1, 2, 1 }));
            Assert.That(fixture.windows.Select(w => w.z), Is.EqualTo(new[] { 0, 1, 2 }));
            Assert.That(fixture.client, Is.EqualTo(default(RectI)));
        }

        [Test]
        [Category("T-GEO")]
        public void RoundTripThroughStjCodecKeepsTheGeometry()
        {
            var codec = new StjCodec();
            var fixture = GeometryFixture.From(DesktopWithThreeWindows());
            fixture.client = GeometryTestData.PrimaryWork;

            var restored = codec.Deserialize<GeometryFixture>(codec.Serialize(fixture));
            var snapshot = restored.ToSnapshot();

            Assert.That(restored.client, Is.EqualTo(GeometryTestData.PrimaryWork));
            Assert.That(snapshot.Monitors.Select(m => Describe(m)), Is.EqualTo(new[] { @"[0, 0, 1920, 1080) [0, 0, 1920, 1032) dpi=120 primary=True \\.\DISPLAY1" }));
            Assert.That(snapshot.Taskbars.Select(t => Describe(t)), Is.EqualTo(new[] { "[0, 1032, 1920, 1080) Bottom autoHide=False" }));
            Assert.That(snapshot.Windows.Select(w => Describe(w)), Is.EqualTo(new[]
            {
                "1 pid=1 [0, 1032, 1920, 1080) caption=[0, 0, 0, 0) Taskbar z=0 maximized=False",
                "2 pid=2 [240, 180, 1460, 940) caption=[0, 0, 0, 0) Platform z=1 maximized=False",
                "3 pid=1 [0, 0, 1920, 1080) caption=[0, 0, 0, 0) Desktop z=2 maximized=False",
            }));
            Assert.That(snapshot.ForegroundHwnd, Is.EqualTo(IntPtr.Zero));
            Assert.That(snapshot.Notify, Is.EqualTo(NotificationState.AcceptsNotifications));
        }

        [Test]
        [Category("T-GEO")]
        public void ToSnapshotOrdersWindowsTopmostFirst()
        {
            var fixture = new GeometryFixture();
            fixture.windows.Add(new FixtureWindow { id = 1, pid = 1, rect = new RectI(0, 0, 400, 300), z = 2 });
            fixture.windows.Add(new FixtureWindow { id = 2, pid = 1, rect = new RectI(0, 0, 400, 300), z = 0 });
            fixture.windows.Add(new FixtureWindow { id = 3, pid = 2, rect = new RectI(0, 0, 400, 300), z = 1 });

            var snapshot = fixture.ToSnapshot();

            Assert.That(snapshot.Windows.Select(w => w.Hwnd.ToInt64()), Is.EqualTo(new long[] { 2, 3, 1 }));
            Assert.That(snapshot.Windows.Select(w => w.Z), Is.EqualTo(new[] { 0, 1, 2 }));
        }

        private static DesktopSnapshot DesktopWithThreeWindows()
        {
            return new DesktopSnapshot
            {
                Seq = 77,
                Monitors = new[] { new MonitorInfo(GeometryTestData.PrimaryMonitor, GeometryTestData.PrimaryWork, 120, true, @"\\.\DISPLAY1") },
                Taskbars = new[] { new TaskbarInfo(new RectI(0, 1032, 1920, 1080), Edge.Bottom, false) },
                Windows = new[]
                {
                    new WindowInfo(new IntPtr(0x10254), 9120, new RectI(0, 1032, 1920, 1080), default, WindowClass.Taskbar, false, 0),
                    new WindowInfo(new IntPtr(0x3A0F2E), 15532, new RectI(240, 180, 1460, 940), new RectI(1322, 181, 1459, 213), WindowClass.Platform, false, 1),
                    new WindowInfo(new IntPtr(0x2071C), 9120, GeometryTestData.PrimaryMonitor, default, WindowClass.Desktop, false, 2),
                },
                Notify = NotificationState.QuietTime,
                ForegroundHwnd = new IntPtr(0x3A0F2E),
            };
        }

        private static string Describe(MonitorInfo m) => $"{m.Monitor} {m.Work} dpi={m.Dpi} primary={m.Primary} {m.DeviceName}";

        private static string Describe(TaskbarInfo t) => $"{t.Bounds} {t.Edge} autoHide={t.AutoHide}";

        private static string Describe(WindowInfo w) =>
            $"{w.Hwnd.ToInt64()} pid={w.Pid} {w.Bounds} caption={w.CaptionButtons} {w.Class} z={w.Z} maximized={w.Maximized}";
    }
}
