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

        /// <summary>False when there is no thumbnail or its image header is not acceptable; neither rejects the package.</summary>
        public bool ThumbnailOk;

        /// <summary>Whether the SHA-256 of the bundle section matches the manifest.</summary>
        public bool Sha256Ok;

        public bool Accept => HeaderError == null && ManifestErrors.Count == 0 && Compat.Accept && Sha256Ok;
    }
}
