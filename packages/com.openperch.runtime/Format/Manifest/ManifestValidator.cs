using System;
using System.Collections.Generic;
using System.Globalization;

namespace OpenPerch.Format
{
    /// <summary>
    /// Checks manifest values. JsonUtility fills absent keys with 0 or "", so a required field holding 0 or "" counts as
    /// missing.
    /// </summary>
    public static class ManifestValidator
    {
        /// <summary>The only formatVersion whose fields beyond the format-independent ones this validator understands.</summary>
        public const int CurrentFormatVersion = 1;

        public const string KindAvatar = "avatar";
        public const string KindMotion = "motion";
        public const string RootAssetDefinition = "definition";
        public const string RootAssetMotionPack = "motionpack";
        public const string BundleNamePrefix = "op-";

        /// <summary>Limits on string lengths count UTF-16 code units.</summary>
        public const int MaxDisplayNameLength = 128;

        public const int MaxWarnings = 128;
        public const int MaxWarningDetailLength = 256;

        /// <summary>
        /// Returns every problem found. The format-independent fields are checked first; when formatVersion is not 1 the
        /// check stops there, so that <see cref="CompatibilityChecker"/> can report <see cref="CompatCode.FormatTooNew"/>
        /// instead of the newer fields being rejected as malformed.
        /// </summary>
        public static IReadOnlyList<ManifestError> Validate(PackageManifest m, in PackageHeader h)
        {
            if (m == null) throw new ArgumentNullException(nameof(m));
            var errors = new List<ManifestError>();
            ValidateFormatIndependent(m, errors);
            if (m.formatVersion != CurrentFormatVersion) return errors;
            ValidateVersion1(m, h, errors);
            return errors;
        }

        private static void ValidateFormatIndependent(PackageManifest m, List<ManifestError> errors)
        {
            RequirePositive(m.formatVersion, "formatVersion", errors);
            RequirePositive(m.runtimeSchemaVersion, "runtimeSchemaVersion", errors);

            ExporterInfo exporter = m.exporter;
            if (exporter == null)
            {
                Add(errors, ManifestErrorCode.MissingField, "exporter");
                return;
            }
            if (IsMissing(exporter.unityVersion)) Add(errors, ManifestErrorCode.MissingField, "exporter.unityVersion");
            else if (!UnityVersion.TryParse(exporter.unityVersion, out _)) Add(errors, ManifestErrorCode.BadUnityVersion, "exporter.unityVersion");
            if (IsMissing(exporter.version)) Add(errors, ManifestErrorCode.MissingField, "exporter.version");
            else if (!SemVer.TryParse(exporter.version, out _)) Add(errors, ManifestErrorCode.BadExporterVersion, "exporter.version");
            if (IsMissing(exporter.buildTarget)) Add(errors, ManifestErrorCode.MissingField, "exporter.buildTarget");
            if (IsMissing(exporter.colorSpace)) Add(errors, ManifestErrorCode.MissingField, "exporter.colorSpace");
        }

        private static void ValidateVersion1(PackageManifest m, in PackageHeader h, List<ManifestError> errors)
        {
            BundleInfo bundle = m.bundle;
            ValidateKind(m.kind, bundle, errors);
            ValidateHex(m.exportId, 32, "exportId", ManifestErrorCode.BadExportId, errors);
            ValidateHex(m.lineageId, 32, "lineageId", ManifestErrorCode.BadLineageId, errors);
            if (m.displayName != null && m.displayName.Length > MaxDisplayNameLength) Add(errors, ManifestErrorCode.TooLong, "displayName");

            if (bundle == null)
            {
                Add(errors, ManifestErrorCode.MissingField, "bundle");
            }
            else
            {
                if (IsMissing(bundle.name)) Add(errors, ManifestErrorCode.MissingField, "bundle.name");
                else if (!string.Equals(bundle.name, BundleNamePrefix + m.exportId, StringComparison.Ordinal)) Add(errors, ManifestErrorCode.BundleNameMismatch, "bundle.name");
                // Unity treats a CRC of 0 as "do not check", so a missing CRC would silently skip the load-time check.
                if (bundle.crc == 0) Add(errors, ManifestErrorCode.MissingField, "bundle.crc");
                ValidateHex(bundle.sha256, 64, "bundle.sha256", ManifestErrorCode.BadSha256, errors);
                if (bundle.length == 0) Add(errors, ManifestErrorCode.MissingField, "bundle.length");
                else if (bundle.length < 0) Add(errors, ManifestErrorCode.NegativeNumber, "bundle.length");
                else if ((ulong)bundle.length != h.BundleLength) Add(errors, ManifestErrorCode.BundleLengthMismatch, "bundle.length");
            }

            FeatureInfo features = m.features;
            if (features != null)
            {
                RequireNonNegative(features.menuPages, "features.menuPages", errors);
                RequireNonNegative(features.physBones, "features.physBones", errors);
                RequireNonNegative(features.constraints, "features.constraints", errors);
                RequireNonNegative(features.contacts, "features.contacts", errors);
            }

            StatsInfo stats = m.stats;
            if (stats != null)
            {
                RequireNonNegative(stats.triangles, "stats.triangles", errors);
                RequireNonNegative(stats.materials, "stats.materials", errors);
                RequireNonNegative(stats.skinnedMeshes, "stats.skinnedMeshes", errors);
                RequireNonNegative(stats.physBoneTransforms, "stats.physBoneTransforms", errors);
                RequireNonNegative(stats.estimatedTextureBytes, "stats.estimatedTextureBytes", errors);
                RequireNonNegative(stats.estimatedMeshBytes, "stats.estimatedMeshBytes", errors);
            }

            MotionInfo[] motions = m.motions;
            if (motions != null)
            {
                for (int i = 0; i < motions.Length; i++)
                {
                    string path = "motions[" + i.ToString(CultureInfo.InvariantCulture) + "]";
                    if (motions[i] == null) Add(errors, ManifestErrorCode.MissingField, path);
                    else if (!(motions[i].length >= 0f)) Add(errors, ManifestErrorCode.NegativeNumber, path + ".length");
                }
            }

            WarningInfo[] warnings = m.warnings;
            if (warnings != null)
            {
                if (warnings.Length > MaxWarnings) Add(errors, ManifestErrorCode.TooMany, "warnings");
                for (int i = 0; i < warnings.Length; i++)
                {
                    string path = "warnings[" + i.ToString(CultureInfo.InvariantCulture) + "]";
                    if (warnings[i] == null) Add(errors, ManifestErrorCode.MissingField, path);
                    else if (warnings[i].detail != null && warnings[i].detail.Length > MaxWarningDetailLength) Add(errors, ManifestErrorCode.TooLong, path + ".detail");
                }
            }
        }

        private static void ValidateKind(string kind, BundleInfo bundle, List<ManifestError> errors)
        {
            string rootAsset = bundle?.rootAsset;
            bool rootMissing = IsMissing(rootAsset);
            if (bundle != null && rootMissing) Add(errors, ManifestErrorCode.MissingField, "bundle.rootAsset");

            string expectedRoot;
            if (IsMissing(kind))
            {
                Add(errors, ManifestErrorCode.MissingField, "kind");
                return;
            }
            if (string.Equals(kind, KindAvatar, StringComparison.Ordinal)) expectedRoot = RootAssetDefinition;
            else if (string.Equals(kind, KindMotion, StringComparison.Ordinal)) expectedRoot = RootAssetMotionPack;
            else
            {
                Add(errors, ManifestErrorCode.UnknownKind, "kind");
                return;
            }
            if (!rootMissing && !string.Equals(rootAsset, expectedRoot, StringComparison.Ordinal))
            {
                Add(errors, ManifestErrorCode.KindRootMismatch, "bundle.rootAsset");
            }
        }

        private static void ValidateHex(string value, int length, string field, ManifestErrorCode badCode, List<ManifestError> errors)
        {
            if (IsMissing(value)) Add(errors, ManifestErrorCode.MissingField, field);
            else if (!IsLowerHex(value, length)) Add(errors, badCode, field);
        }

        private static void RequirePositive(int value, string field, List<ManifestError> errors)
        {
            if (value == 0) Add(errors, ManifestErrorCode.MissingField, field);
            else if (value < 0) Add(errors, ManifestErrorCode.NegativeNumber, field);
        }

        private static void RequireNonNegative(long value, string field, List<ManifestError> errors)
        {
            if (value < 0) Add(errors, ManifestErrorCode.NegativeNumber, field);
        }

        private static bool IsMissing(string value) => string.IsNullOrEmpty(value);

        private static bool IsLowerHex(string value, int length)
        {
            if (value.Length != length) return false;
            foreach (char c in value)
            {
                if (!((c >= '0' && c <= '9') || (c >= 'a' && c <= 'f'))) return false;
            }
            return true;
        }

        private static void Add(List<ManifestError> errors, ManifestErrorCode code, string field) =>
            errors.Add(new ManifestError(code, field));
    }
}
