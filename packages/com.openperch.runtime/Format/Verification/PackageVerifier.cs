using System;
using System.Collections.Generic;
using System.IO;
using System.Security.Cryptography;

namespace OpenPerch.Format
{
    /// <summary>Checks a whole <c>.opk</c> before it is registered in the library.</summary>
    public static class PackageVerifier
    {
        private const int HashBufferSize = 81920;

        /// <summary>
        /// Runs the header, manifest JSON, <see cref="ManifestValidator"/>, <see cref="CompatibilityChecker"/>, thumbnail and
        /// bundle SHA-256 checks in that order and stops at the first failure. An unacceptable thumbnail only clears
        /// <see cref="VerifyResult.ThumbnailOk"/>; <see cref="MessageCodes.ForThumbnail"/> tells whether that deserves a warning.
        /// </summary>
        /// <param name="s">A readable, seekable stream over the package.</param>
        /// <param name="fileLength">The length of the package file.</param>
        public static VerifyResult Verify(Stream s, ulong fileLength, IManifestParser parser, AppProfile profile)
        {
            if (s == null) throw new ArgumentNullException(nameof(s));
            if (parser == null) throw new ArgumentNullException(nameof(parser));
            if (profile == null) throw new ArgumentNullException(nameof(profile));

            var result = new VerifyResult();
            try
            {
                result.Header = PackageCodec.ReadHeader(s, fileLength);
                byte[] manifestBytes = PackageCodec.ReadManifestBytes(s, result.Header);

                if (!parser.TryParse(manifestBytes, out PackageManifest manifest, out ManifestError parseError))
                {
                    result.ManifestErrors = new[] { parseError };
                    return result;
                }
                if (manifest == null)
                {
                    result.ManifestErrors = new[] { new ManifestError(ManifestErrorCode.JsonInvalid, "") };
                    return result;
                }
                result.Manifest = manifest;

                IReadOnlyList<ManifestError> errors = ManifestValidator.Validate(manifest, result.Header);
                if (errors.Count > 0)
                {
                    result.ManifestErrors = errors;
                    return result;
                }

                result.Compat = CompatibilityChecker.Check(manifest, profile);
                if (!result.Compat.Accept) return result;

                result.ThumbnailOk = IsThumbnailAcceptable(s, result.Header);
                result.Sha256Ok = string.Equals(ComputeBundleSha256(s, result.Header), manifest.bundle?.sha256, StringComparison.Ordinal);
            }
            catch (PackageFormatException e)
            {
                result.HeaderError = e.Error;
            }
            return result;
        }

        private static bool IsThumbnailAcceptable(Stream s, in PackageHeader h)
        {
            if (h.ThumbnailLength == 0) return false;
            int length = h.ThumbnailLength < PngInfo.IhdrPrefixLength ? (int)h.ThumbnailLength : PngInfo.IhdrPrefixLength;
            var prefix = new byte[length];
            PackageCodec.Seek(s, h.ThumbnailOffset);
            PackageCodec.ReadExactly(s, prefix, 0, length);
            return PngInfo.IsAcceptableThumbnail(prefix);
        }

        private static string ComputeBundleSha256(Stream s, in PackageHeader h)
        {
            PackageCodec.Seek(s, h.BundleOffset);
            using (SHA256 sha = SHA256.Create())
            {
                var buffer = new byte[HashBufferSize];
                ulong remaining = h.BundleLength;
                while (remaining > 0)
                {
                    int want = remaining < (ulong)buffer.Length ? (int)remaining : buffer.Length;
                    int n = s.Read(buffer, 0, want);
                    if (n <= 0) throw new PackageFormatException(PackageError.LengthMismatch);
                    sha.TransformBlock(buffer, 0, n, null, 0);
                    remaining -= (ulong)n;
                }
                sha.TransformFinalBlock(Array.Empty<byte>(), 0, 0);
                return ToLowerHex(sha.Hash);
            }
        }

        private static string ToLowerHex(byte[] bytes)
        {
            const string digits = "0123456789abcdef";
            var chars = new char[bytes.Length * 2];
            for (int i = 0; i < bytes.Length; i++)
            {
                chars[i * 2] = digits[bytes[i] >> 4];
                chars[i * 2 + 1] = digits[bytes[i] & 0xF];
            }
            return new string(chars);
        }
    }
}
