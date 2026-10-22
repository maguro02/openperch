using System;
using System.IO;
using NUnit.Framework;
using OpenPerch.App.Core;
using OpenPerch.App.Tests.Shared;
using OpenPerch.Format;

namespace OpenPerch.Core.Tests.Format
{
    /// <summary>Compatibility outcomes as seen through the whole verifier, with the manifest handed over by a stub.</summary>
    [Category("T-FMT")]
    public class PackageVerifierCompatTests
    {
        private MalformedPackageFactory _factory;
        private byte[] _package;

        [SetUp]
        public void SetUp()
        {
            _factory = new MalformedPackageFactory();
            _package = _factory.CreateValidPackage();
        }

        private VerifyResult Verify(Action<PackageManifest> change, AppProfile profile = null)
        {
            PackageManifest manifest = _factory.CreateValidManifest();
            change(manifest);
            using (var s = new MemoryStream(_package, false))
            {
                return PackageVerifier.Verify(s, (ulong)_package.Length, new StubManifestParser(manifest), profile ?? AppProfiles.Current);
            }
        }

        [Test]
        public void Exported22f1IsAcceptedByThe62f3App()
        {
            VerifyResult result = Verify(m => m.exporter.unityVersion = "2022.3.22f1");

            Assert.That(result.ManifestErrors, Is.Empty);
            Assert.That(result.Compat.Code, Is.EqualTo(CompatCode.Ok));
            Assert.That(result.Compat.Warnings, Is.Empty);
            Assert.That(result.Accept, Is.True);
        }

        [Test]
        public void FormatVersion2PassesValidationAndIsTooNew()
        {
            VerifyResult result = Verify(m => m.formatVersion = 2);

            Assert.That(result.ManifestErrors, Is.Empty);
            Assert.That(result.Compat.Code, Is.EqualTo(CompatCode.FormatTooNew));
            Assert.That(result.Accept, Is.False);
        }

        [Test]
        public void FormatVersion2WithV1OnlyFieldsBrokenIsStillTooNew()
        {
            VerifyResult result = Verify(m =>
            {
                m.formatVersion = 2;
                m.kind = "";
                m.exportId = "not-hex";
                m.bundle = null;
            });

            Assert.That(result.ManifestErrors, Is.Empty);
            Assert.That(result.Compat.Code, Is.EqualTo(CompatCode.FormatTooNew));
        }

        [Test]
        public void RuntimeSchema2IsTooNew()
        {
            Assert.That(Verify(m => m.runtimeSchemaVersion = 2).Compat.Code, Is.EqualTo(CompatCode.SchemaTooNew));
        }

        [Test]
        public void AndroidTargetIsWrong()
        {
            Assert.That(Verify(m => m.exporter.buildTarget = "Android").Compat.Code, Is.EqualTo(CompatCode.WrongTarget));
        }

        [Test]
        public void GammaColorSpaceIsWrong()
        {
            Assert.That(Verify(m => m.exporter.colorSpace = "Gamma").Compat.Code, Is.EqualTo(CompatCode.WrongColorSpace));
        }

        [Test]
        public void Unity2021IsADifferentStream()
        {
            Assert.That(Verify(m => m.exporter.unityVersion = "2021.3.45f1").Compat.Code, Is.EqualTo(CompatCode.DifferentStream));
        }

        [Test]
        public void RejectingKnownBadExporterIsRefused()
        {
            var profile = AppProfiles.Current;
            profile.KnownBadExporters = new[] { new KnownBad(">=1.0.0 <1.0.3", MessageCodes.E_COMPAT_KNOWN_BAD_EXPORTER, true) };

            VerifyResult result = Verify(m => m.exporter.version = "1.0.2", profile);

            Assert.That(result.Compat.Code, Is.EqualTo(CompatCode.KnownBadExporter));
            Assert.That(result.Accept, Is.False);
            Assert.That(CompatibilityChecker.TryFindKnownBad(result.Manifest, profile, out KnownBad bad), Is.True);
            Assert.That(bad.MessageCode, Is.EqualTo(MessageCodes.E_COMPAT_KNOWN_BAD_EXPORTER));
            Assert.That(bad.Reject, Is.True);
        }

        [Test]
        public void WarningKnownBadExporterIsAcceptedWithAWarning()
        {
            var profile = AppProfiles.Current;
            profile.KnownBadExporters = new[] { new KnownBad(">=1.1.0 <1.2.0", MessageCodes.W_COMPAT_KNOWN_BAD_EXPORTER, false) };

            VerifyResult result = Verify(m => m.exporter.version = "1.1.5", profile);

            Assert.That(result.Compat.Code, Is.EqualTo(CompatCode.Ok));
            Assert.That(result.Compat.Warnings, Is.EqualTo(new[] { CompatCode.KnownBadExporter }));
            Assert.That(result.Accept, Is.True);
            Assert.That(CompatibilityChecker.TryFindKnownBad(result.Manifest, profile, out KnownBad bad), Is.True);
            Assert.That(bad.MessageCode, Is.EqualTo(MessageCodes.W_COMPAT_KNOWN_BAD_EXPORTER));
            Assert.That(bad.Reject, Is.False);
        }

        [Test]
        public void FormatIsJudgedBeforeTarget()
        {
            VerifyResult result = Verify(m =>
            {
                m.formatVersion = 2;
                m.exporter.buildTarget = "Android";
            });

            Assert.That(result.Compat.Code, Is.EqualTo(CompatCode.FormatTooNew));
        }

        [Test]
        public void SchemaIsJudgedBeforeColorSpace()
        {
            VerifyResult result = Verify(m =>
            {
                m.runtimeSchemaVersion = 2;
                m.exporter.colorSpace = "Gamma";
            });

            Assert.That(result.Compat.Code, Is.EqualTo(CompatCode.SchemaTooNew));
        }
    }
}
