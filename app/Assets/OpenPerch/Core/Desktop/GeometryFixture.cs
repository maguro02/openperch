using System;
using System.Collections.Generic;
using System.Linq;

namespace OpenPerch.App.Core
{
    /// <summary>
    /// Persisted, anonymized form of a <see cref="DesktopSnapshot"/>: diagnostics save it and the geometry tests replay it.
    /// </summary>
    /// <remarks>
    /// Windows keep only their geometry, with handles and process IDs replaced by sequential numbers. Caption button bounds,
    /// the foreground window and the notification state are not stored, so a replayed snapshot uses the caption fallback.
    /// </remarks>
    [Serializable]
    public sealed class GeometryFixture
    {
        public int schemaVersion = 1;

        /// <summary>Client rectangle of the mascot window. <see cref="From"/> leaves it empty for the caller to fill in.</summary>
        public RectI client;

        public List<FixtureMonitor> monitors = new();
        public List<FixtureTaskbar> taskbars = new();
        public List<FixtureWindow> windows = new();

        /// <summary>Rebuilds a snapshot whose window handles and process IDs are the stored sequential numbers, ordered by Z.</summary>
        public DesktopSnapshot ToSnapshot()
        {
            return new DesktopSnapshot
            {
                Monitors = Present(monitors).Select(m => new MonitorInfo(m.monitor, m.work, m.dpi, m.primary, m.deviceName)).ToArray(),
                Taskbars = Present(taskbars).Select(t => new TaskbarInfo(t.bounds, t.edge, t.autoHide)).ToArray(),
                Windows = Present(windows)
                    .OrderBy(w => w.z)
                    .Select(w => new WindowInfo(new IntPtr(w.id), (uint)w.pid, w.rect, default, w.kind, w.maximized, w.z))
                    .ToArray(),
            };
        }

        /// <summary>
        /// Captures <paramref name="s"/>, numbering windows from 1 in Z order and processes from 1 in order of first appearance.
        /// </summary>
        /// <exception cref="ArgumentNullException"><paramref name="s"/> is null.</exception>
        public static GeometryFixture From(DesktopSnapshot s)
        {
            if (s == null) throw new ArgumentNullException(nameof(s));

            var fixture = new GeometryFixture();
            foreach (var m in s.Monitors ?? Array.Empty<MonitorInfo>())
            {
                fixture.monitors.Add(new FixtureMonitor { monitor = m.Monitor, work = m.Work, dpi = m.Dpi, primary = m.Primary, deviceName = m.DeviceName ?? "" });
            }
            foreach (var t in s.Taskbars ?? Array.Empty<TaskbarInfo>())
            {
                fixture.taskbars.Add(new FixtureTaskbar { bounds = t.Bounds, edge = t.Edge, autoHide = t.AutoHide });
            }

            var pids = new Dictionary<uint, int>();
            var snapshotWindows = s.Windows ?? Array.Empty<WindowInfo>();
            for (int i = 0; i < snapshotWindows.Length; i++)
            {
                var w = snapshotWindows[i];
                if (!pids.TryGetValue(w.Pid, out int pid))
                {
                    pid = pids.Count + 1;
                    pids.Add(w.Pid, pid);
                }
                fixture.windows.Add(new FixtureWindow { id = i + 1, pid = pid, rect = w.Bounds, kind = w.Class, z = w.Z, maximized = w.Maximized });
            }
            return fixture;
        }

        private static IEnumerable<T> Present<T>(List<T> items) where T : class =>
            items == null ? Enumerable.Empty<T>() : items.Where(item => item != null);
    }
}
