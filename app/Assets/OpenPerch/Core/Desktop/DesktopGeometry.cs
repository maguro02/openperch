using System;
using System.Collections.Generic;

namespace OpenPerch.App.Core
{
    /// <summary>
    /// Derives the surfaces a mascot can stand on, the rectangles that hide it and the fullscreen state from a <see cref="DesktopSnapshot"/>.
    /// Every coordinate is in physical pixels of the virtual screen, so values can be negative.
    /// </summary>
    public static class DesktopGeometry
    {
        /// <summary>The most rectangles <see cref="CollectOccluders"/> writes.</summary>
        public const int MaxOccluders = 16;

        private const int FullscreenTolerancePx = 2;

        /// <summary>Computes the floor and the window tops visible inside <paramref name="clientPx"/>.</summary>
        /// <remarks>
        /// <para>
        /// <c>Items[0]</c> is always the floor, with <see cref="Surface.Owner"/> set to <see cref="IntPtr.Zero"/> and <see cref="Surface.Z"/> set to -1.
        /// Window tops follow from the topmost window down and, within a window, from left to right.
        /// </para>
        /// <para>
        /// A window top comes from a <see cref="WindowClass.Platform"/> window that is not maximized, is at least
        /// <see cref="GeometryOptions.MinPlatformWidthDip"/> wide and whose top edge lies inside the client with at least one row above it.
        /// <see cref="WindowInfo.CaptionButtons"/> is read in the same virtual-screen space as <see cref="WindowInfo.Bounds"/>.
        /// </para>
        /// </remarks>
        /// <param name="s">Snapshot to read.</param>
        /// <param name="clientPx">Client rectangle of the mascot window.</param>
        /// <param name="o">DPI scale and size limits.</param>
        /// <exception cref="ArgumentNullException"><paramref name="s"/> or <paramref name="o"/> is null.</exception>
        /// <exception cref="ArgumentOutOfRangeException"><see cref="GeometryOptions.Scale"/> is not positive.</exception>
        public static SurfaceSet ComputeSurfaces(DesktopSnapshot s, RectI clientPx, GeometryOptions o)
        {
            if (s == null) throw new ArgumentNullException(nameof(s));
            if (o == null) throw new ArgumentNullException(nameof(o));
            if (!(o.Scale > 0f)) throw new ArgumentOutOfRangeException(nameof(o), o.Scale, "GeometryOptions.Scale must be positive.");

            var windows = s.Windows ?? Array.Empty<WindowInfo>();
            int inset = DipToPx(o.CornerInsetDip, o.Scale);
            int captionFallback = DipToPx(o.CaptionFallbackDip, o.Scale);
            int minPlatformWidth = DipToPx(o.MinPlatformWidthDip, o.Scale);

            var items = new List<Surface> { ComputeFloor(s, clientPx) };
            var spans = new List<(int X0, int X1)>();
            foreach (var w in windows)
            {
                // The probe already demotes maximized and narrow windows, but a fixture can still label them Platform.
                if (w.Class != WindowClass.Platform || w.Maximized || w.Bounds.Width() < minPlatformWidth) continue;

                int y = w.Bounds.top;
                if (y <= clientPx.top || y >= clientPx.bottom) continue;

                int x0 = Math.Max(w.Bounds.left + inset, clientPx.left);
                int x1 = Math.Min(w.Bounds.right - inset, clientPx.right);
                if (x1 <= x0) continue;

                spans.Clear();
                spans.Add((x0, x1));
                if (w.CaptionButtons.IsEmpty()) Subtract(spans, w.Bounds.right - captionFallback, w.Bounds.right);
                else Subtract(spans, w.CaptionButtons.left, w.CaptionButtons.right);

                // The feet are drawn on the row just above the edge, so only windows covering that row hide them.
                int feetRow = y - 1;
                foreach (var above in windows)
                {
                    var r = above.Bounds;
                    if (above.Z < w.Z && IsOccluder(above.Class) && r.top <= feetRow && feetRow < r.bottom) Subtract(spans, r.left, r.right);
                }

                foreach (var (a, b) in spans)
                {
                    if (b - a < o.FootWidthPx) continue;
                    items.Add(new Surface(SurfaceKind.WindowTop, w.Hwnd, y, a, b, Headroom(windows, w.Z, y, a, b, clientPx.top), w.Z));
                }
            }

            return new SurfaceSet { Seq = s.Seq, Items = items.ToArray() };
        }

        /// <summary>Writes the parts of <paramref name="avatarRectPx"/> covered by windows above the one the avatar stands on.</summary>
        /// <remarks>
        /// Only a <see cref="SurfaceKind.WindowTop"/> surface has occluders: windows with a smaller Z than <paramref name="standing"/>.
        /// The rectangles are clipped to <paramref name="avatarRectPx"/> and written topmost first, at most <see cref="MaxOccluders"/>
        /// and at most <c>dst.Length</c> of them.
        /// </remarks>
        /// <param name="s">Snapshot the surface was computed from.</param>
        /// <param name="standing">Surface the avatar stands on.</param>
        /// <param name="avatarRectPx">Screen rectangle the avatar is drawn into.</param>
        /// <param name="dst">Receives the occluder rectangles.</param>
        /// <returns>
        /// The number of occluding windows. It can exceed the number written; a value above <see cref="MaxOccluders"/>
        /// means the occlusion cannot be represented.
        /// </returns>
        /// <exception cref="ArgumentNullException"><paramref name="s"/> is null.</exception>
        public static int CollectOccluders(DesktopSnapshot s, Surface standing, RectI avatarRectPx, Span<RectI> dst)
        {
            if (s == null) throw new ArgumentNullException(nameof(s));
            if (standing.Kind != SurfaceKind.WindowTop) return 0;

            int limit = Math.Min(MaxOccluders, dst.Length);
            int count = 0;
            foreach (var w in s.Windows ?? Array.Empty<WindowInfo>())
            {
                if (w.Z >= standing.Z || !IsOccluder(w.Class)) continue;
                var clipped = RectI.Intersect(w.Bounds, avatarRectPx);
                if (clipped.IsEmpty()) continue;
                if (count < limit) dst[count] = clipped;
                count++;
            }
            return count;
        }

        /// <summary>Reports whether a fullscreen application or a presentation is running.</summary>
        /// <remarks>
        /// The busy, Direct3D fullscreen and presentation notification states count on every monitor. Otherwise the foreground window
        /// must be an application window whose bounds match <paramref name="monitorPx"/> within 2 px on every edge.
        /// </remarks>
        /// <param name="s">Snapshot to read.</param>
        /// <param name="monitorPx">Monitor rectangle the mascot is shown on.</param>
        /// <exception cref="ArgumentNullException"><paramref name="s"/> is null.</exception>
        public static FullscreenState DetectFullscreen(DesktopSnapshot s, RectI monitorPx)
        {
            if (s == null) throw new ArgumentNullException(nameof(s));
            if (s.Notify == NotificationState.Busy || s.Notify == NotificationState.D3DFullScreen || s.Notify == NotificationState.PresentationMode)
            {
                return FullscreenState.Fullscreen;
            }
            if (s.ForegroundHwnd == IntPtr.Zero) return FullscreenState.None;

            foreach (var w in s.Windows ?? Array.Empty<WindowInfo>())
            {
                if (w.Hwnd != s.ForegroundHwnd) continue;
                // Tool windows fail the OccluderOnly test and end up Ignored, so the two application classes are all that remain.
                bool application = w.Class == WindowClass.Platform || w.Class == WindowClass.OccluderOnly;
                return application && Matches(w.Bounds, monitorPx, FullscreenTolerancePx) ? FullscreenState.Fullscreen : FullscreenState.None;
            }
            return FullscreenState.None;
        }

        private static Surface ComputeFloor(DesktopSnapshot s, RectI clientPx)
        {
            int y = clientPx.bottom;
            // Auto-hide is one setting for every taskbar, and a hidden taskbar's bounds can reach into the neighbouring monitor,
            // so the setting is not attributed to a monitor by position.
            if (!AnyAutoHide(s.Taskbars))
            {
                var monitors = s.Monitors ?? Array.Empty<MonitorInfo>();
                int m = FindMonitor(monitors, clientPx);
                if (m >= 0) y = monitors[m].Work.bottom;
            }
            return new Surface(SurfaceKind.Floor, IntPtr.Zero, y, clientPx.left, clientPx.right, y - clientPx.top, -1);
        }

        private static int Headroom(WindowInfo[] windows, int z, int y, int x0, int x1, int clientTop)
        {
            int headroom = y - clientTop;
            // Windows behind the platform do not count: an avatar standing on it is drawn over them.
            foreach (var above in windows)
            {
                var r = above.Bounds;
                if (above.Z >= z || !IsOccluder(above.Class) || r.IsEmpty()) continue;
                if (r.bottom < y && r.left < x1 && r.right > x0) headroom = Math.Min(headroom, y - r.bottom);
            }
            return headroom;
        }

        private static void Subtract(List<(int X0, int X1)> spans, int cut0, int cut1)
        {
            if (cut1 <= cut0) return;
            for (int k = spans.Count - 1; k >= 0; k--)
            {
                var (x0, x1) = spans[k];
                if (cut1 <= x0 || cut0 >= x1) continue;
                spans.RemoveAt(k);
                if (cut1 < x1) spans.Insert(k, (cut1, x1));
                if (x0 < cut0) spans.Insert(k, (x0, cut0));
            }
        }

        private static bool IsOccluder(WindowClass c) =>
            c == WindowClass.Platform || c == WindowClass.OccluderOnly || c == WindowClass.Taskbar;

        private static bool AnyAutoHide(TaskbarInfo[] taskbars)
        {
            if (taskbars == null) return false;
            foreach (var t in taskbars)
            {
                if (t.AutoHide) return true;
            }
            return false;
        }

        private static int FindMonitor(MonitorInfo[] monitors, RectI rect)
        {
            int best = -1;
            long bestArea = 0;
            for (int i = 0; i < monitors.Length; i++)
            {
                var overlap = RectI.Intersect(monitors[i].Monitor, rect);
                long area = (long)overlap.Width() * overlap.Height();
                if (area > bestArea)
                {
                    best = i;
                    bestArea = area;
                }
            }
            return best;
        }

        private static bool Matches(RectI a, RectI b, int tolerance) =>
            Math.Abs((long)a.left - b.left) <= tolerance && Math.Abs((long)a.top - b.top) <= tolerance &&
            Math.Abs((long)a.right - b.right) <= tolerance && Math.Abs((long)a.bottom - b.bottom) <= tolerance;

        private static int DipToPx(int dip, float scale) => (int)Math.Round(dip * (double)scale, MidpointRounding.AwayFromZero);
    }
}
