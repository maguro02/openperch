namespace OpenPerch.App.Core
{
    public readonly struct MonitorInfo
    {
        public readonly RectI Monitor;
        public readonly RectI Work;
        public readonly int Dpi;
        public readonly bool Primary;
        public readonly string DeviceName;

        public MonitorInfo(RectI monitor, RectI work, int dpi, bool primary, string deviceName)
        {
            Monitor = monitor;
            Work = work;
            Dpi = dpi;
            Primary = primary;
            DeviceName = deviceName ?? "";
        }
    }
}
