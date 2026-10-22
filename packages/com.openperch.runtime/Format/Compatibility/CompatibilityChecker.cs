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
        public static CompatResult Check(PackageManifest m, AppProfile p) => Evaluate(m, p, out _);

        /// <summary>
        /// Finds the <see cref="KnownBad"/> entry behind the <see cref="CompatCode.KnownBadExporter"/> verdict of
        /// <see cref="Check"/>, so that its <see cref="KnownBad.MessageCode"/> can be shown with the generic code.
        /// </summary>
        /// <returns>
        /// True when <see cref="Check"/> reports <see cref="CompatCode.KnownBadExporter"/> as the rejection or as a warning.
        /// <paramref name="bad"/> is then the first matching entry with <see cref="KnownBad.Reject"/> set, or, when no such
        /// entry matches, the first matching entry. False, with <paramref name="bad"/> left default, otherwise.
        /// </returns>
        public static bool TryFindKnownBad(PackageManifest m, AppProfile p, out KnownBad bad)
        {
            Evaluate(m, p, out int index);
            if (index < 0)
            {
                bad = default;
                return false;
            }
            bad = p.KnownBadExporters[index];
            return true;
        }

        private static CompatResult Evaluate(PackageManifest m, AppProfile p, out int knownBadIndex)
        {
            if (m == null) throw new ArgumentNullException(nameof(m));
            if (p == null) throw new ArgumentNullException(nameof(p));
            knownBadIndex = -1;
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

            KnownBad[] knownBad = p.KnownBadExporters;
            if (knownBad != null && SemVer.TryParse(exporter.version, out SemVer exporterVersion))
            {
                for (int i = 0; i < knownBad.Length; i++)
                {
                    if (!SemVerRange.TryParse(knownBad[i].Range, out SemVerRange range) || !range.Contains(exporterVersion)) continue;
                    if (knownBad[i].Reject)
                    {
                        knownBadIndex = i;
                        return Reject(CompatCode.KnownBadExporter);
                    }
                    if (knownBadIndex < 0) knownBadIndex = i;
                }
                if (knownBadIndex >= 0) warnings.Add(CompatCode.KnownBadExporter);
            }

            return new CompatResult(CompatCode.Ok, warnings);
        }

        private static CompatResult Reject(CompatCode code) => new CompatResult(code, Array.Empty<CompatCode>());
    }
}
