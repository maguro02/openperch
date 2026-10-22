using System;
using System.IO;
using System.Linq;
using OpenPerch.App.Core;
using OpenPerch.Core.Tests.Support;

namespace OpenPerch.Core.Tests.Desktop
{
    /// <summary>
    /// Loads the geometry fixtures and renders surfaces as text, so a failing comparison prints every field of every surface.
    /// </summary>
    public static class GeometryTestData
    {
        public static readonly RectI PrimaryMonitor = new RectI(0, 0, 1920, 1080);
        public static readonly RectI PrimaryWork = new RectI(0, 0, 1920, 1032);

        public static string FixtureDirectory => RepoPaths.Combine("dotnet", "OpenPerch.Core.Tests", "Fixtures", "Geometry");

        public static GeometryFixture Load(string name) =>
            new StjCodec().Deserialize<GeometryFixture>(File.ReadAllText(Path.Combine(FixtureDirectory, name + ".json")));

        public static GeometryOptions Options(float scale = 1f, int footWidthPx = 40) =>
            new GeometryOptions { Scale = scale, FootWidthPx = footWidthPx };

        public static string[] Compute(GeometryFixture fixture, GeometryOptions options = null) =>
            Compute(fixture.ToSnapshot(), fixture.client, options);

        public static string[] Compute(DesktopSnapshot snapshot, RectI client, GeometryOptions options = null) =>
            Describe(DesktopGeometry.ComputeSurfaces(snapshot, client, options ?? Options()));

        public static string[] Describe(SurfaceSet set) => set.Items.Select(item => Describe(item)).ToArray();

        public static string Describe(Surface s) =>
            FormattableString.Invariant($"{s.Kind}:{s.Owner.ToInt64()} y={s.Y} x=[{s.X0},{s.X1}) h={s.Headroom} z={s.Z}");

        public static WindowInfo Window(int id, WindowClass kind, RectI bounds, int z) =>
            new WindowInfo(new IntPtr(id), (uint)id, bounds, default, kind, false, z);

        public static WindowInfo WithCaptionButtons(WindowInfo w, RectI captionButtons) =>
            new WindowInfo(w.Hwnd, w.Pid, w.Bounds, captionButtons, w.Class, w.Maximized, w.Z);

        public static WindowInfo WithMaximized(WindowInfo w, bool maximized) =>
            new WindowInfo(w.Hwnd, w.Pid, w.Bounds, w.CaptionButtons, w.Class, maximized, w.Z);

        public static MonitorInfo Monitor(RectI monitor, RectI work, bool primary = false) =>
            new MonitorInfo(monitor, work, 96, primary, "");
    }
}
