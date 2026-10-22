namespace OpenPerch.Format
{
    /// <summary>The outcome of <see cref="CompatibilityChecker.Check"/>.</summary>
    public enum CompatCode
    {
        Ok = 0,

        /// <summary>formatVersion is not in <see cref="AppProfile.SupportedFormats"/>.</summary>
        FormatTooNew = 1,

        /// <summary>runtimeSchemaVersion is greater than <see cref="AppProfile.AppSchema"/>.</summary>
        SchemaTooNew = 2,

        /// <summary>The bundle was not built for StandaloneWindows64.</summary>
        WrongTarget = 3,

        /// <summary>The project's color space was not Linear.</summary>
        WrongColorSpace = 4,

        /// <summary>The exporter's Unity version is not in the app's stream.</summary>
        DifferentStream = 5,

        /// <summary>The exporter's Unity version is in the app's stream but newer than the app's.</summary>
        NewerThanApp = 6,

        /// <summary>The exporter version matches a <see cref="KnownBad"/> range; a rejection or a warning depending on it.</summary>
        KnownBadExporter = 7,

        /// <summary>Warning: a Unity 2022.3 package accepted by a Unity 6 build of the app.</summary>
        LegacyStreamAccepted = 8,
    }
}
