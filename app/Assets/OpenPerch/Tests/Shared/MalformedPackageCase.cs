namespace OpenPerch.App.Tests.Shared
{
    /// <summary>A single defect that <see cref="MalformedPackageFactory"/> can build into an otherwise valid package.</summary>
    public enum MalformedPackageCase
    {
        /// <summary>The second signature byte is changed.</summary>
        BadMagic = 0,

        /// <summary>containerVersion is 2.</summary>
        ContainerVersion2 = 1,

        /// <summary>flags is 1.</summary>
        NonZeroFlags = 2,

        /// <summary>reserved0 is 1.</summary>
        NonZeroReserved = 3,

        /// <summary>headerCrc32 has one bit flipped.</summary>
        CorruptHeaderCrc = 4,

        /// <summary>manifestOffset is 128.</summary>
        ManifestOffset128 = 5,

        /// <summary>bundleOffset is 2^63.</summary>
        BundleOffset2Pow63 = 6,

        /// <summary>The thumbnail starts 8 bytes before the bundle, so the two overlap.</summary>
        ThumbnailOverlapsBundle = 7,

        /// <summary>The bundle starts 4 bytes after the 4096-byte boundary, at 4100 in the default layout.</summary>
        MisalignedBundle = 8,

        /// <summary>One byte is appended after the bundle.</summary>
        TrailingByte = 9,

        /// <summary>The manifest section is 300 KiB.</summary>
        OversizedManifest = 10,

        /// <summary>thumbnailOffset is 0 while thumbnailLength is 10.</summary>
        ThumbnailOffsetZero = 11,

        /// <summary>The thumbnail's IHDR declares 16384x16384.</summary>
        HugeThumbnail = 12,

        /// <summary>formatVersion is 0, as JsonUtility leaves it when the key is absent.</summary>
        FormatVersionZero = 13,

        /// <summary>exportId is not hex; bundle.name follows it so that only exportId is wrong.</summary>
        BadExportId = 14,

        /// <summary>bundle.name names a different export.</summary>
        BundleNameMismatch = 15,

        /// <summary>displayName is 129 characters.</summary>
        LongDisplayName = 16,

        /// <summary>warnings has 129 entries.</summary>
        TooManyWarnings = 17,

        /// <summary>kind is "motion" while bundle.rootAsset stays "definition".</summary>
        MotionWithDefinitionRoot = 18,

        /// <summary>bundle.length is one more than the header's bundleLength.</summary>
        BundleLengthMismatch = 19,

        /// <summary>bundle.sha256 is written in uppercase.</summary>
        UppercaseSha256 = 20,

        /// <summary>exporter.unityVersion is "abc".</summary>
        BadUnityVersion = 21,

        /// <summary>One byte of the bundle is changed after the hash was recorded.</summary>
        TamperedBundle = 22,
    }
}
