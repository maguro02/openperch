using System;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using OpenPerch.App.Tests.Shared;
using OpenPerch.Format;

namespace OpenPerch.Core.Tests.Format
{
    [Category("T-FMT")]
    public class ManifestValidatorTests
    {
        private static readonly PackageHeader Header =
            new PackageHeader(1, 2, 0, 0, 4096, MalformedPackageFactory.BundleLength);

        private static PackageManifest Valid() => new MalformedPackageFactory().CreateValidManifest();

        private static IReadOnlyList<ManifestError> Validate(Action<PackageManifest> change)
        {
            PackageManifest m = Valid();
            change(m);
            return ManifestValidator.Validate(m, Header);
        }

        private static void AssertSingle(IReadOnlyList<ManifestError> errors, ManifestErrorCode code, string field)
        {
            Assert.That(errors, Has.Count.EqualTo(1), string.Join(", ", errors));
            Assert.That(errors[0].Code, Is.EqualTo(code));
            Assert.That(errors[0].Field, Is.EqualTo(field));
        }

        [Test]
        public void ValidManifestHasNoErrors()
        {
            Assert.That(ManifestValidator.Validate(Valid(), Header), Is.Empty);
        }

        [Test]
        public void FreshManifestReportsEveryRequiredFormatIndependentField()
        {
            IReadOnlyList<ManifestError> errors = ManifestValidator.Validate(new PackageManifest(), Header);

            Assert.That(errors.Select(e => e.Field), Is.EquivalentTo(new[]
            {
                "formatVersion", "runtimeSchemaVersion", "exporter.unityVersion", "exporter.version",
                "exporter.buildTarget", "exporter.colorSpace",
            }));
            Assert.That(errors.All(e => e.Code == ManifestErrorCode.MissingField), Is.True);
        }

        [Test]
        public void FreshVersion1ManifestReportsEveryRequiredField()
        {
            IReadOnlyList<ManifestError> errors = ManifestValidator.Validate(new PackageManifest { formatVersion = 1 }, Header);

            Assert.That(errors.Select(e => e.Field), Is.EquivalentTo(new[]
            {
                "runtimeSchemaVersion", "exporter.unityVersion", "exporter.version", "exporter.buildTarget",
                "exporter.colorSpace", "bundle.rootAsset", "kind", "exportId", "lineageId", "bundle.name", "bundle.crc",
                "bundle.sha256", "bundle.length",
            }));
            Assert.That(errors.All(e => e.Code == ManifestErrorCode.MissingField), Is.True);
        }

        [Test]
        public void NewerFormatIsOnlyCheckedForFormatIndependentFields()
        {
            IReadOnlyList<ManifestError> errors = Validate(m =>
            {
                m.formatVersion = 2;
                m.kind = "";
                m.bundle = null;
                m.displayName = new string('x', 1000);
                m.exporter.version = "one";
            });

            AssertSingle(errors, ManifestErrorCode.BadExporterVersion, "exporter.version");
        }

        [Test]
        public void NegativeVersionsAreNegativeNumbers()
        {
            AssertSingle(Validate(m => m.formatVersion = -1), ManifestErrorCode.NegativeNumber, "formatVersion");
            AssertSingle(Validate(m => m.runtimeSchemaVersion = -3), ManifestErrorCode.NegativeNumber, "runtimeSchemaVersion");
        }

        [Test]
        public void MissingExporterIsReportedOnce()
        {
            AssertSingle(Validate(m => m.exporter = null), ManifestErrorCode.MissingField, "exporter");
        }

        [Test]
        public void EmptyFormatIndependentStringsAreMissing()
        {
            AssertSingle(Validate(m => m.exporter.buildTarget = ""), ManifestErrorCode.MissingField, "exporter.buildTarget");
            AssertSingle(Validate(m => m.exporter.colorSpace = null), ManifestErrorCode.MissingField, "exporter.colorSpace");
            AssertSingle(Validate(m => m.exporter.unityVersion = ""), ManifestErrorCode.MissingField, "exporter.unityVersion");
        }

        [Test]
        public void ExporterVersionMustBeSemVer()
        {
            AssertSingle(Validate(m => m.exporter.version = "v1.0.0"), ManifestErrorCode.BadExporterVersion, "exporter.version");
        }

        [Test]
        public void KindRules()
        {
            AssertSingle(Validate(m => m.kind = "scene"), ManifestErrorCode.UnknownKind, "kind");
            AssertSingle(Validate(m => m.kind = "Avatar"), ManifestErrorCode.UnknownKind, "kind");
            AssertSingle(Validate(m => m.bundle.rootAsset = "motionpack"), ManifestErrorCode.KindRootMismatch, "bundle.rootAsset");
            AssertSingle(Validate(m => m.bundle.rootAsset = ""), ManifestErrorCode.MissingField, "bundle.rootAsset");
            Assert.That(Validate(m =>
            {
                m.kind = "motion";
                m.bundle.rootAsset = "motionpack";
            }), Is.Empty);
        }

        [Test]
        public void IdsMustBeLowercaseHex()
        {
            AssertSingle(Validate(m => m.lineageId = m.lineageId.ToUpperInvariant()), ManifestErrorCode.BadLineageId, "lineageId");
            AssertSingle(Validate(m => m.lineageId = m.lineageId.Substring(1)), ManifestErrorCode.BadLineageId, "lineageId");
            AssertSingle(Validate(m => m.bundle.sha256 = m.bundle.sha256 + "0"), ManifestErrorCode.BadSha256, "bundle.sha256");
        }

        [Test]
        public void BundleChecks()
        {
            AssertSingle(Validate(m => m.bundle = null), ManifestErrorCode.MissingField, "bundle");
            AssertSingle(Validate(m => m.bundle.crc = 0), ManifestErrorCode.MissingField, "bundle.crc");
            AssertSingle(Validate(m => m.bundle.length = 0), ManifestErrorCode.MissingField, "bundle.length");
            AssertSingle(Validate(m => m.bundle.length = -1), ManifestErrorCode.NegativeNumber, "bundle.length");
            AssertSingle(Validate(m => m.bundle.name = ""), ManifestErrorCode.MissingField, "bundle.name");
            Assert.That(Validate(m => m.bundle.crc = uint.MaxValue), Is.Empty);
        }

        [Test]
        public void LimitsAreInclusive()
        {
            Assert.That(Validate(m => m.displayName = new string('a', 128)), Is.Empty);
            Assert.That(Validate(m => m.warnings = Enumerable.Range(0, 128).Select(i => new WarningInfo { code = "W_X", detail = new string('d', 256) }).ToArray()), Is.Empty);
            AssertSingle(Validate(m => m.warnings[0].detail = new string('d', 257)), ManifestErrorCode.TooLong, "warnings[0].detail");
        }

        [Test]
        public void DisplayNameMayBeEmpty()
        {
            Assert.That(Validate(m => m.displayName = ""), Is.Empty);
        }

        [Test]
        public void EveryCountMustBeNonNegative()
        {
            AssertSingle(Validate(m => m.features.physBones = -1), ManifestErrorCode.NegativeNumber, "features.physBones");
            AssertSingle(Validate(m => m.features.menuPages = -1), ManifestErrorCode.NegativeNumber, "features.menuPages");
            AssertSingle(Validate(m => m.features.constraints = -1), ManifestErrorCode.NegativeNumber, "features.constraints");
            AssertSingle(Validate(m => m.features.contacts = -1), ManifestErrorCode.NegativeNumber, "features.contacts");
            AssertSingle(Validate(m => m.stats.triangles = -1), ManifestErrorCode.NegativeNumber, "stats.triangles");
            AssertSingle(Validate(m => m.stats.materials = -1), ManifestErrorCode.NegativeNumber, "stats.materials");
            AssertSingle(Validate(m => m.stats.skinnedMeshes = -1), ManifestErrorCode.NegativeNumber, "stats.skinnedMeshes");
            AssertSingle(Validate(m => m.stats.physBoneTransforms = -1), ManifestErrorCode.NegativeNumber, "stats.physBoneTransforms");
            AssertSingle(Validate(m => m.stats.estimatedTextureBytes = -1), ManifestErrorCode.NegativeNumber, "stats.estimatedTextureBytes");
            AssertSingle(Validate(m => m.stats.estimatedMeshBytes = long.MinValue), ManifestErrorCode.NegativeNumber, "stats.estimatedMeshBytes");
        }

        [Test]
        public void ZeroCountsAreValid()
        {
            Assert.That(Validate(m =>
            {
                m.features = new FeatureInfo();
                m.stats = new StatsInfo();
                m.warnings = Array.Empty<WarningInfo>();
            }), Is.Empty);
        }

        [Test]
        public void MotionLengthsMustBeNonNegative()
        {
            AssertSingle(Validate(m => m.motions = new[] { new MotionInfo { id = "a", role = "Walk", length = -0.5f } }),
                ManifestErrorCode.NegativeNumber, "motions[0].length");
            AssertSingle(Validate(m => m.motions = new[] { new MotionInfo(), new MotionInfo { length = float.NaN } }),
                ManifestErrorCode.NegativeNumber, "motions[1].length");
            AssertSingle(Validate(m => m.motions = new MotionInfo[] { null }), ManifestErrorCode.MissingField, "motions[0]");
        }

        [Test]
        public void OptionalSectionsMayBeNull()
        {
            Assert.That(Validate(m =>
            {
                m.features = null;
                m.stats = null;
                m.compositing = null;
                m.warnings = null;
                m.motions = null;
            }), Is.Empty);
        }

        [Test]
        public void NullWarningIsMissing()
        {
            AssertSingle(Validate(m => m.warnings = new WarningInfo[] { null }), ManifestErrorCode.MissingField, "warnings[0]");
        }

        [Test]
        public void NullManifestThrows()
        {
            Assert.Throws<ArgumentNullException>(() => ManifestValidator.Validate(null, Header));
        }
    }
}
