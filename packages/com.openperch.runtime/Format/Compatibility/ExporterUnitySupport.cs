namespace OpenPerch.Format
{
    /// <summary>Whether the exporter may run in a given Unity editor.</summary>
    public enum ExporterUnitySupport
    {
        Supported = 0,

        /// <summary>Unity 6: the export continues, but only a Unity 6 build of the app can read the result.</summary>
        Unity6AppOnly = 1,

        /// <summary>The export is stopped.</summary>
        Unsupported = 2,
    }
}
