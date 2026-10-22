using System;
using OpenPerch.App.Core;

namespace OpenPerch.Core.Tests.Behavior
{
    /// <summary>A 1920x1040 work area seen as surfaces, and the avatar metrics the brain tests share.</summary>
    internal static class TestDesk
    {
        public const float DisplayHeightPx = 360f;
        public const float PixelsPerMeter = 240f;
        public const float WalkSpeedPx = 144f;
        public const float SeatHeightPx = 100f;
        public const int FloorY = 1040;
        public const int Width = 1920;

        public static float Gravity => 9.81f * PixelsPerMeter;

        public static Surface Floor() => new Surface(SurfaceKind.Floor, IntPtr.Zero, FloorY, 0, Width, FloorY, int.MaxValue);

        /// <summary>A window top whose headroom reaches the top of the client area.</summary>
        public static Surface Window(int owner, int y, int x0, int x1) => Window(owner, y, x0, x1, y);

        public static Surface Window(int owner, int y, int x0, int x1, int headroom) =>
            new Surface(SurfaceKind.WindowTop, new IntPtr(owner), y, x0, x1, headroom, owner);

        public static SurfaceSet Desk(params Surface[] windows) => Snapshot(1, windows);

        /// <summary>The floor followed by <paramref name="windows"/>, as a probe snapshot numbered <paramref name="seq"/>.</summary>
        public static SurfaceSet Snapshot(long seq, params Surface[] windows)
        {
            var items = new Surface[windows.Length + 1];
            items[0] = Floor();
            Array.Copy(windows, 0, items, 1, windows.Length);
            return new SurfaceSet { Seq = seq, Items = items };
        }

        /// <summary>Idle and sitting always last <paramref name="seconds"/>, so a decision falls on a known tick.</summary>
        public static BehaviorConfig FixedDwell(float seconds) => new BehaviorConfig { idleMinSec = seconds, idleMaxSec = seconds };
    }
}
