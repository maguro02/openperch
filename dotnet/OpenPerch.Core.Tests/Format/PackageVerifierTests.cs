using System;
using System.IO;
using System.Text;
using NUnit.Framework;
using OpenPerch.App.Core;
using OpenPerch.App.Tests.Shared;
using OpenPerch.Format;

namespace OpenPerch.Core.Tests.Format
{
    [Category("T-FMT")]
    public class PackageVerifierTests
    {
        private static VerifyResult Verify(byte[] package, IManifestParser parser, AppProfile profile = null)
        {
            using (var s = new MemoryStream(package, false))
            {
                return PackageVerifier.Verify(s, (ulong)package.Length, parser, profile ?? AppProfiles.Current);
            }
        }

        [Test]
        public void ValidPackageIsAccepted()
        {
            var factory = new MalformedPackageFactory();
            var parser = new StubManifestParser(factory.CreateValidManifest());

            VerifyResult result = Verify(factory.CreateValidPackage(), parser);

            Assert.That(result.HeaderError, Is.Null);
            Assert.That(result.ManifestErrors, Is.Empty);
            Assert.That(result.Compat.Code, Is.EqualTo(CompatCode.Ok));
            Assert.That(result.ThumbnailOk, Is.True);
            Assert.That(MessageCodes.ForThumbnail(result), Is.Null);
            Assert.That(result.Sha256Ok, Is.True);
            Assert.That(result.Accept, Is.True);
            Assert.That(result.Header.BundleLength, Is.EqualTo((ulong)MalformedPackageFactory.BundleLength));
            Assert.That(result.Header.BundleOffset, Is.EqualTo(4096UL));
        }

        [Test]
        public void ParserReceivesTheManifestSection()
        {
            byte[] manifestBytes = Encoding.UTF8.GetBytes("{\"formatVersion\":1}");
            var factory = new MalformedPackageFactory(_ => manifestBytes);
            var parser = new StubManifestParser(factory.CreateValidManifest());

            Verify(factory.CreateValidPackage(), parser);

            Assert.That(parser.LastBytes, Is.EqualTo(manifestBytes));
        }

        [TestCase(MalformedPackageCase.BadMagic, PackageError.BadMagic)]
        [TestCase(MalformedPackageCase.ContainerVersion2, PackageError.UnsupportedContainer)]
        [TestCase(MalformedPackageCase.NonZeroFlags, PackageError.BadFlags)]
        [TestCase(MalformedPackageCase.NonZeroReserved, PackageError.BadReserved)]
        [TestCase(MalformedPackageCase.CorruptHeaderCrc, PackageError.BadHeaderCrc)]
        [TestCase(MalformedPackageCase.ManifestOffset128, PackageError.BadManifestOffset)]
        [TestCase(MalformedPackageCase.BundleOffset2Pow63, PackageError.OutOfRange)]
        [TestCase(MalformedPackageCase.ThumbnailOverlapsBundle, PackageError.Overlap)]
        [TestCase(MalformedPackageCase.MisalignedBundle, PackageError.Misaligned)]
        [TestCase(MalformedPackageCase.TrailingByte, PackageError.LengthMismatch)]
        [TestCase(MalformedPackageCase.OversizedManifest, PackageError.ManifestTooLarge)]
        [TestCase(MalformedPackageCase.ThumbnailOffsetZero, PackageError.BadThumbnailFields)]
        public void ContainerDefectStopsAtTheHeader(MalformedPackageCase c, PackageError expected)
        {
            var factory = new MalformedPackageFactory();
            var parser = new StubManifestParser(factory.CreateManifest(c));

            VerifyResult result = Verify(factory.CreatePackage(c), parser);

            Assert.That(result.HeaderError, Is.EqualTo(expected));
            Assert.That(parser.Calls, Is.EqualTo(0));
            Assert.That(result.Manifest, Is.Null);
            Assert.That(result.Sha256Ok, Is.False);
            Assert.That(result.Accept, Is.False);
        }

        [Test]
        public void MisalignedBundleSitsAt4100()
        {
            byte[] package = new MalformedPackageFactory().CreatePackage(MalformedPackageCase.MisalignedBundle);

            Assert.That(MalformedPackageFactory.GetUInt64(package, MalformedPackageFactory.BundleOffsetOffset), Is.EqualTo(4100UL));
        }

        [Test]
        public void OversizedThumbnailIsDroppedButThePackageIsAccepted()
        {
            var factory = new MalformedPackageFactory();
            var parser = new StubManifestParser(factory.CreateManifest(MalformedPackageCase.HugeThumbnail));

            VerifyResult result = Verify(factory.CreatePackage(MalformedPackageCase.HugeThumbnail), parser);

            Assert.That(result.Header.ThumbnailLength, Is.GreaterThan(0u));
            Assert.That(result.ThumbnailOk, Is.False);
            Assert.That(result.Sha256Ok, Is.True);
            Assert.That(result.Accept, Is.True);
            Assert.That(MessageCodes.ForThumbnail(result), Is.EqualTo(MessageCodes.W_THUMB_REJECTED));
            Assert.That(MessageCatalog.TryGet(MessageCodes.W_THUMB_REJECTED, out MessageDef def), Is.True);
            Assert.That(def.Severity, Is.EqualTo(MessageSeverity.Warning));
        }

        [TestCase(MalformedPackageCase.FormatVersionZero, ManifestErrorCode.MissingField, "formatVersion")]
        [TestCase(MalformedPackageCase.BadExportId, ManifestErrorCode.BadExportId, "exportId")]
        [TestCase(MalformedPackageCase.BundleNameMismatch, ManifestErrorCode.BundleNameMismatch, "bundle.name")]
        [TestCase(MalformedPackageCase.LongDisplayName, ManifestErrorCode.TooLong, "displayName")]
        [TestCase(MalformedPackageCase.TooManyWarnings, ManifestErrorCode.TooMany, "warnings")]
        [TestCase(MalformedPackageCase.MotionWithDefinitionRoot, ManifestErrorCode.KindRootMismatch, "bundle.rootAsset")]
        [TestCase(MalformedPackageCase.BundleLengthMismatch, ManifestErrorCode.BundleLengthMismatch, "bundle.length")]
        [TestCase(MalformedPackageCase.UppercaseSha256, ManifestErrorCode.BadSha256, "bundle.sha256")]
        [TestCase(MalformedPackageCase.BadUnityVersion, ManifestErrorCode.BadUnityVersion, "exporter.unityVersion")]
        public void ManifestDefectIsTheOnlyError(MalformedPackageCase c, ManifestErrorCode expected, string field)
        {
            var factory = new MalformedPackageFactory();
            var parser = new StubManifestParser(factory.CreateManifest(c));

            VerifyResult result = Verify(factory.CreatePackage(c), parser);

            Assert.That(result.HeaderError, Is.Null);
            Assert.That(result.ManifestErrors, Has.Count.EqualTo(1), string.Join(", ", result.ManifestErrors));
            Assert.That(result.ManifestErrors[0].Code, Is.EqualTo(expected));
            Assert.That(result.ManifestErrors[0].Field, Is.EqualTo(field));
            Assert.That(result.Sha256Ok, Is.False, "verification must stop before hashing");
            Assert.That(result.Accept, Is.False);
        }

        [Test]
        public void TamperedBundleFailsTheHashCheck()
        {
            var factory = new MalformedPackageFactory();
            var parser = new StubManifestParser(factory.CreateManifest(MalformedPackageCase.TamperedBundle));

            VerifyResult result = Verify(factory.CreatePackage(MalformedPackageCase.TamperedBundle), parser);

            Assert.That(result.HeaderError, Is.Null);
            Assert.That(result.ManifestErrors, Is.Empty);
            Assert.That(result.Compat.Accept, Is.True);
            Assert.That(result.ThumbnailOk, Is.True);
            Assert.That(result.Sha256Ok, Is.False);
            Assert.That(result.Accept, Is.False);
        }

        [Test]
        public void ParserFailureIsReportedAsTheOnlyManifestError()
        {
            var factory = new MalformedPackageFactory();
            var parser = StubManifestParser.Failing(new ManifestError(ManifestErrorCode.JsonInvalid, ""));

            VerifyResult result = Verify(factory.CreateValidPackage(), parser);

            Assert.That(result.HeaderError, Is.Null);
            Assert.That(result.ManifestErrors, Has.Count.EqualTo(1));
            Assert.That(result.ManifestErrors[0].Code, Is.EqualTo(ManifestErrorCode.JsonInvalid));
            Assert.That(result.Manifest, Is.Null);
            Assert.That(result.Accept, Is.False);
        }

        [Test]
        public void ParserReturningNullIsJsonInvalid()
        {
            var factory = new MalformedPackageFactory();

            VerifyResult result = Verify(factory.CreateValidPackage(), new StubManifestParser(null));

            Assert.That(result.ManifestErrors, Has.Count.EqualTo(1));
            Assert.That(result.ManifestErrors[0].Code, Is.EqualTo(ManifestErrorCode.JsonInvalid));
            Assert.That(result.Accept, Is.False);
        }

        [Test]
        public void IncompatiblePackageStopsBeforeTheThumbnailAndHash()
        {
            var factory = new MalformedPackageFactory();
            PackageManifest manifest = factory.CreateValidManifest();
            manifest.exporter.buildTarget = "Android";

            VerifyResult result = Verify(factory.CreateValidPackage(), new StubManifestParser(manifest));

            Assert.That(result.Compat.Code, Is.EqualTo(CompatCode.WrongTarget));
            Assert.That(result.ThumbnailOk, Is.False);
            Assert.That(result.Sha256Ok, Is.False);
            Assert.That(result.Accept, Is.False);
            Assert.That(result.Header.ThumbnailLength, Is.GreaterThan(0u));
            Assert.That(MessageCodes.ForThumbnail(result), Is.Null, "the thumbnail was never examined");
        }

        [Test]
        public void PackageWithoutThumbnailIsAccepted()
        {
            var factory = new MalformedPackageFactory();
            byte[] package = MalformedPackageFactory.Assemble(Encoding.UTF8.GetBytes("{}"), null, factory.Bundle, 0);

            VerifyResult result = Verify(package, new StubManifestParser(factory.CreateValidManifest()));

            Assert.That(result.Header.ThumbnailOffset, Is.EqualTo(0UL));
            Assert.That(result.Header.ThumbnailLength, Is.EqualTo(0u));
            Assert.That(result.ThumbnailOk, Is.False);
            Assert.That(MessageCodes.ForThumbnail(result), Is.Null, "a package without a thumbnail gets no thumbnail warning");
            Assert.That(result.Accept, Is.True);
        }

        [Test]
        public void ThumbnailWarningNeedsAResult()
        {
            Assert.Throws<ArgumentNullException>(() => MessageCodes.ForThumbnail(null));
        }

        [Test]
        public void StreamShorterThanTheFileLengthIsALengthMismatch()
        {
            var factory = new MalformedPackageFactory();
            byte[] package = factory.CreateValidPackage();
            byte[] truncated = new byte[package.Length - 10];
            Array.Copy(package, truncated, truncated.Length);

            VerifyResult result;
            using (var s = new MemoryStream(truncated, false))
            {
                result = PackageVerifier.Verify(s, (ulong)package.Length, new StubManifestParser(factory.CreateValidManifest()), AppProfiles.Current);
            }

            Assert.That(result.HeaderError, Is.EqualTo(PackageError.LengthMismatch));
            Assert.That(result.Accept, Is.False);
        }
    }
}
