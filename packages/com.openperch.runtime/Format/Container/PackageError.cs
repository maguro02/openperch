namespace OpenPerch.Format
{
    /// <summary>Why the container header of a <c>.opk</c> file was rejected.</summary>
    public enum PackageError
    {
        /// <summary>The file does not start with the <c>.opk</c> signature.</summary>
        BadMagic = 0,

        /// <summary>containerVersion is not 1, so the rest of the header layout cannot be trusted.</summary>
        UnsupportedContainer = 1,

        /// <summary>flags is not 0.</summary>
        BadFlags = 2,

        /// <summary>reserved0 or reserved1 is not 0.</summary>
        BadReserved = 3,

        /// <summary>headerCrc32 does not match bytes 0..55.</summary>
        BadHeaderCrc = 4,

        /// <summary>manifestOffset is not 64.</summary>
        BadManifestOffset = 5,

        /// <summary>A length is outside its allowed range, or a section does not lie inside the file.</summary>
        OutOfRange = 6,

        /// <summary>Sections overlap, or are not in the order manifest, thumbnail, bundle.</summary>
        Overlap = 7,

        /// <summary>bundleOffset is not a multiple of 4096.</summary>
        Misaligned = 8,

        /// <summary>The file length differs from bundleOffset + bundleLength, or the file ends early.</summary>
        LengthMismatch = 9,

        /// <summary>manifestLength exceeds 262144 bytes.</summary>
        ManifestTooLarge = 10,

        /// <summary>Only one of thumbnailOffset and thumbnailLength is 0, or the thumbnail exceeds 2 MiB.</summary>
        BadThumbnailFields = 11,
    }
}
