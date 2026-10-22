using System;
using System.Collections.Generic;

namespace OpenPerch.Format
{
    /// <summary>
    /// The outcome of <see cref="PackageVerifier.Verify"/>. Fields after the first failing step keep their defaults.
    /// </summary>
    public sealed class VerifyResult
    {
        public PackageHeader Header;

        /// <summary>Null when the header or the JSON could not be read.</summary>
        public PackageManifest Manifest;

        public PackageError? HeaderError;
        public IReadOnlyList<ManifestError> ManifestErrors = Array.Empty<ManifestError>();
        public CompatResult Compat;

        /// <summary>
        /// True when the package has a thumbnail whose image header passed <see cref="PngInfo.IsAcceptableThumbnail"/>.
        /// False also when the package has no thumbnail (<see cref="PackageHeader.ThumbnailLength"/> is 0) or verification
        /// stopped before the thumbnail step. False never rejects the package by itself. Report
        /// <see cref="MessageCodes.W_THUMB_REJECTED"/> only when <c>Header.ThumbnailLength != 0 &amp;&amp; !ThumbnailOk</c>
        /// after the thumbnail step, as <see cref="MessageCodes.ForThumbnail"/> does.
        /// </summary>
        public bool ThumbnailOk;

        /// <summary>Whether the SHA-256 of the bundle section matches the manifest.</summary>
        public bool Sha256Ok;

        public bool Accept => HeaderError == null && ManifestErrors.Count == 0 && Compat.Accept && Sha256Ok;
    }
}
