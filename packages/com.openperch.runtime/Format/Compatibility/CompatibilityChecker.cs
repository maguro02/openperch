using System;
using System.Collections.Generic;

namespace OpenPerch.Format
{
    /// <summary>Decides whether the app can use a package whose manifest has already passed <see cref="ManifestValidator"/>.</summary>
    public static class CompatibilityChecker
    {
        public const string RequiredBuildTarget = "StandaloneWindows64";
        public const string RequiredColorSpace = "Linear";

        /// <summary>
        /// Evaluates format, schema, target, color space, Unity stream, Unity version and known-bad exporters in that order
        /// and returns the first rejection. Without a rejection the result is <see cref="CompatCode.Ok"/> with the warnings
        /// in evaluation order.
        /// </summary>
        public static CompatResult Check(PackageManifest m, AppProfile p)
        {
            if (m == null) throw new ArgumentNullException(nameof(m));
            if (p == null) throw new ArgumentNullException(nameof(p));
            ExporterInfo exporter = m.exporter ?? new ExporterInfo();

            if (p.SupportedFormats == null || Array.IndexOf(p.SupportedFormats, m.formatVersion) < 0) return Reject(CompatCode.FormatTooNew);
            if (m.runtimeSchemaVersion > p.AppSchema) return Reject(CompatCode.SchemaTooNew);
            if (!string.Equals(exporter.buildTarget, RequiredBuildTarget, StringComparison.Ordinal)) return Reject(CompatCode.WrongTarget);
            if (!string.Equals(exporter.colorSpace, RequiredColorSpace, StringComparison.Ordinal)) return Reject(CompatCode.WrongColorSpace);

            var warnings = new List<CompatCode>();
            if (!UnityVersion.TryParse(exporter.unityVersion, out UnityVersion fileUnity)) return Reject(CompatCode.DifferentStream);
            if (fileUnity.SameStream(p.AppUnity))
            {
                if (fileUnity.CompareTo(p.AppUnity) > 0) return Reject(CompatCode.NewerThanApp);
            }
            else if (p.AcceptLegacy2022 && p.AppUnity.Major >= 6000 && fileUnity.Major == 2022 && fileUnity.Minor == 3)
            {
                warnings.Add(CompatCode.LegacyStreamAccepted);
            }
            else
            {
                return Reject(CompatCode.DifferentStream);
            }

            if (p.KnownBadExporters != null && SemVer.TryParse(exporter.version, out SemVer exporterVersion))
            {
                bool warned = false;
                foreach (KnownBad bad in p.KnownBadExporters)
                {
                    if (!SemVerRange.TryParse(bad.Range, out SemVerRange range) || !range.Contains(exporterVersion)) continue;
                    if (bad.Reject) return Reject(CompatCode.KnownBadExporter);
                    if (!warned) warnings.Add(CompatCode.KnownBadExporter);
                    warned = true;
                }
            }

            return new CompatResult(CompatCode.Ok, warnings);
        }

        private static CompatResult Reject(CompatCode code) => new CompatResult(code, Array.Empty<CompatCode>());
    }
}
