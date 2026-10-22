using System;

namespace OpenPerch.App.Core
{
    /// <summary>Persisted form of a <see cref="MonitorInfo"/> inside a <see cref="GeometryFixture"/>.</summary>
    [Serializable]
    public sealed class FixtureMonitor
    {
        public RectI monitor;
        public RectI work;
        public int dpi = 96;
        public bool primary;
        public string deviceName = "";
    }
}
