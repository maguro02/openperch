namespace OpenPerch.Format
{
    /// <summary>
    /// The variable fields of a <c>.opk</c> header. The manifest always starts at byte 64, so its offset is not stored.
    /// </summary>
    public readonly struct PackageHeader
    {
        public PackageHeader(ushort ver, uint manifestLen, ulong thumbOff, uint thumbLen, ulong bundleOff, ulong bundleLen)
        {
            ContainerVersion = ver;
            ManifestLength = manifestLen;
            ThumbnailOffset = thumbOff;
            ThumbnailLength = thumbLen;
            BundleOffset = bundleOff;
            BundleLength = bundleLen;
        }

        public ushort ContainerVersion { get; }

        public uint ManifestLength { get; }

        /// <summary>0 when the package has no thumbnail.</summary>
        public ulong ThumbnailOffset { get; }

        /// <summary>0 when the package has no thumbnail.</summary>
        public uint ThumbnailLength { get; }

        public ulong BundleOffset { get; }

        public ulong BundleLength { get; }
    }
}
