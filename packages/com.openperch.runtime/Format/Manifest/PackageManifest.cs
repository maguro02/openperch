using System;

namespace OpenPerch.Format
{
    /// <summary>
    /// The JSON manifest stored in every <c>.opk</c>. Field names are the JSON keys, so renaming one changes the format.
    /// Converted to and from JSON only by <c>OpenPerch.Format.Json.ManifestJson</c>.
    /// </summary>
    [Serializable]
    public sealed class PackageManifest
    {
        /// <summary>Raised when the meaning of the manifest changes incompatibly. 0 means the key was missing.</summary>
        public int formatVersion;

        /// <summary>"avatar" or "motion".</summary>
        public string kind = "";

        /// <summary>A new GUID for every export, as 32 lowercase hex digits.</summary>
        public string exportId = "";

        /// <summary>Shared by every export of the same avatar, as 32 lowercase hex digits.</summary>
        public string lineageId = "";

        public string displayName = "";

        /// <summary>ISO 8601 UTC time of the export, for example "2026-10-22T10:00:00Z".</summary>
        public string createdAtUtc = "";

        public ExporterInfo exporter = new ExporterInfo();

        /// <summary>The <c>RuntimeSchema.Current</c> of the runtime package the bundle was built with.</summary>
        public int runtimeSchemaVersion;

        public BundleInfo bundle = new BundleInfo();
        public FeatureInfo features = new FeatureInfo();
        public CompositingInfo compositing = new CompositingInfo();
        public StatsInfo stats = new StatsInfo();
        public WarningInfo[] warnings = Array.Empty<WarningInfo>();

        /// <summary>The clips of a motion pack; empty for an avatar.</summary>
        public MotionInfo[] motions = Array.Empty<MotionInfo>();
    }
}
