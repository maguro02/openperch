namespace OpenPerch.Format
{
    /// <summary>Why a manifest was rejected.</summary>
    public enum ManifestErrorCode
    {
        /// <summary>The manifest is not valid UTF-8 JSON, or its root is not an object.</summary>
        JsonInvalid = 0,

        /// <summary>A required field is absent, 0 or an empty string.</summary>
        MissingField = 1,

        /// <summary>exportId is not 32 lowercase hex digits.</summary>
        BadExportId = 2,

        /// <summary>lineageId is not 32 lowercase hex digits.</summary>
        BadLineageId = 3,

        /// <summary>bundle.sha256 is not 64 lowercase hex digits.</summary>
        BadSha256 = 4,

        /// <summary>bundle.name is not "op-" followed by the exportId.</summary>
        BundleNameMismatch = 5,

        /// <summary>bundle.length differs from the header's bundleLength.</summary>
        BundleLengthMismatch = 6,

        /// <summary>bundle.rootAsset does not belong to the kind.</summary>
        KindRootMismatch = 7,

        /// <summary>kind is neither "avatar" nor "motion".</summary>
        UnknownKind = 8,

        /// <summary>exporter.unityVersion is not a Unity version string.</summary>
        BadUnityVersion = 9,

        /// <summary>exporter.version is not SemVer.</summary>
        BadExporterVersion = 10,

        /// <summary>A number is negative.</summary>
        NegativeNumber = 11,

        /// <summary>A string is longer than its limit.</summary>
        TooLong = 12,

        /// <summary>An array has more elements than its limit.</summary>
        TooMany = 13,
    }
}
