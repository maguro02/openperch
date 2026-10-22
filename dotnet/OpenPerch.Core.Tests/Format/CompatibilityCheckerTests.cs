using System;
using NUnit.Framework;
using OpenPerch.App.Tests.Shared;
using OpenPerch.Format;

namespace OpenPerch.Core.Tests.Format
{
    [Category("T-VER")]
    public class CompatibilityCheckerTests
    {
        private static UnityVersion U(string s)
        {
            Assert.That(UnityVersion.TryParse(s, out UnityVersion v), Is.True, s);
            return v;
        }

        private static AppProfile App(string unity, bool acceptLegacy2022 = false, params KnownBad[] knownBad) => new AppProfile
        {
            AppUnity = U(unity),
            AppSchema = 1,
            SupportedFormats = new[] { 1 },
            AcceptLegacy2022 = acceptLegacy2022,
            KnownBadExporters = knownBad,
        };

        private static PackageManifest Exported(string unity, string exporterVersion = "1.0.0")
        {
            PackageManifest m = new MalformedPackageFactory().CreateValidManifest();
            m.exporter.unityVersion = unity;
            m.exporter.version = exporterVersion;
            return m;
        }

        [Test]
        public void OlderPatchInTheSameStreamIsAccepted()
        {
            CompatResult r = CompatibilityChecker.Check(Exported("2022.3.22f1"), App("2022.3.62f3"));

            Assert.That(r.Code, Is.EqualTo(CompatCode.Ok));
            Assert.That(r.Warnings, Is.Empty);
            Assert.That(r.Accept, Is.True);
        }

        [Test]
        public void SameVersionIsAccepted()
        {
            Assert.That(CompatibilityChecker.Check(Exported("2022.3.62f3"), App("2022.3.62f3")).Code, Is.EqualTo(CompatCode.Ok));
        }

        [Test]
        public void ChinaBuildOfTheSameVersionIsAccepted()
        {
            Assert.That(CompatibilityChecker.Check(Exported("2022.3.62f3c1"), App("2022.3.62f3")).Code, Is.EqualTo(CompatCode.Ok));
        }

        [Test]
        public void NewerFileInTheSameStreamIsNewerThanApp()
        {
            Assert.That(CompatibilityChecker.Check(Exported("2022.3.63f1"), App("2022.3.62f3")).Code, Is.EqualTo(CompatCode.NewerThanApp));
            Assert.That(CompatibilityChecker.Check(Exported("2022.3.62p1"), App("2022.3.62f3")).Code, Is.EqualTo(CompatCode.NewerThanApp));
            Assert.That(CompatibilityChecker.Check(Exported("6000.3.1f1"), App("6000.0.23f1")).Code, Is.EqualTo(CompatCode.NewerThanApp));
        }

        [Test]
        public void Unity6MinorVersionsAreOneStream()
        {
            Assert.That(CompatibilityChecker.Check(Exported("6000.0.23f1"), App("6000.3.0f1")).Code, Is.EqualTo(CompatCode.Ok));
        }

        [Test]
        public void Legacy2022FileOnUnity6AppDependsOnAcceptLegacy2022()
        {
            CompatResult accepted = CompatibilityChecker.Check(Exported("2022.3.22f1"), App("6000.3.0f1", acceptLegacy2022: true));
            CompatResult refused = CompatibilityChecker.Check(Exported("2022.3.22f1"), App("6000.3.0f1", acceptLegacy2022: false));

            Assert.That(accepted.Code, Is.EqualTo(CompatCode.Ok));
            Assert.That(accepted.Warnings, Is.EqualTo(new[] { CompatCode.LegacyStreamAccepted }));
            Assert.That(refused.Code, Is.EqualTo(CompatCode.DifferentStream));
        }

        [Test]
        public void Unity6FileOn2022AppIsADifferentStreamEvenWithAcceptLegacy2022()
        {
            Assert.That(CompatibilityChecker.Check(Exported("6000.0.23f1"), App("2022.3.62f3", acceptLegacy2022: true)).Code,
                Is.EqualTo(CompatCode.DifferentStream));
        }

        [Test]
        public void OnlyUnity2022Point3IsALegacyStream()
        {
            Assert.That(CompatibilityChecker.Check(Exported("2021.3.45f1"), App("6000.3.0f1", acceptLegacy2022: true)).Code,
                Is.EqualTo(CompatCode.DifferentStream));
            Assert.That(CompatibilityChecker.Check(Exported("2022.2.0f1"), App("6000.3.0f1", acceptLegacy2022: true)).Code,
                Is.EqualTo(CompatCode.DifferentStream));
        }

        [Test]
        public void OtherMinorStreamsBefore6000AreDifferent()
        {
            Assert.That(CompatibilityChecker.Check(Exported("2022.2.20f1"), App("2022.3.62f3")).Code, Is.EqualTo(CompatCode.DifferentStream));
            Assert.That(CompatibilityChecker.Check(Exported("2023.2.20f1"), App("2022.3.62f3")).Code, Is.EqualTo(CompatCode.DifferentStream));
        }

        [Test]
        public void UnparsableUnityVersionIsADifferentStream()
        {
            Assert.That(CompatibilityChecker.Check(Exported("abc"), App("2022.3.62f3")).Code, Is.EqualTo(CompatCode.DifferentStream));
        }

        [Test]
        public void KnownBadRejectionWinsOverAWarningListedEarlier()
        {
            AppProfile app = App("2022.3.62f3", false,
                new KnownBad(">=1.0.0 <2.0.0", MessageCodes.W_COMPAT_KNOWN_BAD_EXPORTER, false),
                new KnownBad(">=1.0.0 <1.0.3", MessageCodes.E_COMPAT_KNOWN_BAD_EXPORTER, true));

            CompatResult r = CompatibilityChecker.Check(Exported("2022.3.22f1", "1.0.1"), app);

            Assert.That(r.Code, Is.EqualTo(CompatCode.KnownBadExporter));
            Assert.That(r.Accept, Is.False);
            Assert.That(r.Warnings, Is.Empty);
        }

        [Test]
        public void KnownBadWarningIsListedOnceAfterTheStreamWarning()
        {
            AppProfile app = App("6000.3.0f1", true,
                new KnownBad(">=1.0.0 <2.0.0", MessageCodes.W_COMPAT_KNOWN_BAD_EXPORTER, false),
                new KnownBad(">=1.0.0 <1.5.0", MessageCodes.W_COMPAT_KNOWN_BAD_EXPORTER, false));

            CompatResult r = CompatibilityChecker.Check(Exported("2022.3.22f1", "1.0.1"), app);

            Assert.That(r.Code, Is.EqualTo(CompatCode.Ok));
            Assert.That(r.Warnings, Is.EqualTo(new[] { CompatCode.LegacyStreamAccepted, CompatCode.KnownBadExporter }));
        }

        [Test]
        public void VersionsOutsideEveryKnownBadRangePass()
        {
            AppProfile app = App("2022.3.62f3", false, new KnownBad(">=1.0.0 <1.0.3", MessageCodes.E_COMPAT_KNOWN_BAD_EXPORTER, true));

            CompatResult r = CompatibilityChecker.Check(Exported("2022.3.22f1", "1.0.3"), app);

            Assert.That(r.Code, Is.EqualTo(CompatCode.Ok));
            Assert.That(r.Warnings, Is.Empty);
        }

        [Test]
        public void StreamIsJudgedBeforeKnownBad()
        {
            AppProfile app = App("2022.3.62f3", false, new KnownBad(">=1.0.0", MessageCodes.E_COMPAT_KNOWN_BAD_EXPORTER, true));

            Assert.That(CompatibilityChecker.Check(Exported("2022.3.63f1"), app).Code, Is.EqualTo(CompatCode.NewerThanApp));
            Assert.That(CompatibilityChecker.TryFindKnownBad(Exported("2022.3.63f1"), app, out _), Is.False);
        }

        [Test]
        public void TryFindKnownBadReturnsTheRejectingEntryOverAnEarlierWarning()
        {
            var warning = new KnownBad(">=1.0.0 <2.0.0", MessageCodes.W_COMPAT_KNOWN_BAD_EXPORTER, false);
            var rejection = new KnownBad(">=1.0.0 <1.0.3", MessageCodes.E_COMPAT_KNOWN_BAD_EXPORTER, true);
            AppProfile app = App("2022.3.62f3", false, warning, rejection);
            PackageManifest m = Exported("2022.3.22f1", "1.0.1");

            Assert.That(CompatibilityChecker.Check(m, app).Code, Is.EqualTo(CompatCode.KnownBadExporter));
            Assert.That(CompatibilityChecker.TryFindKnownBad(m, app, out KnownBad bad), Is.True);
            Assert.That(bad.Range, Is.EqualTo(rejection.Range));
            Assert.That(bad.MessageCode, Is.EqualTo(rejection.MessageCode));
            Assert.That(bad.Reject, Is.True);
        }

        [Test]
        public void TryFindKnownBadReturnsTheFirstMatchingWarning()
        {
            var first = new KnownBad(">=1.0.0 <2.0.0", MessageCodes.W_COMPAT_KNOWN_BAD_EXPORTER, false);
            var second = new KnownBad(">=1.0.0 <1.5.0", MessageCodes.W_COMPAT_KNOWN_BAD_EXPORTER, false);
            var unrelated = new KnownBad(">=3.0.0", MessageCodes.E_COMPAT_KNOWN_BAD_EXPORTER, true);
            AppProfile app = App("2022.3.62f3", false, unrelated, first, second);
            PackageManifest m = Exported("2022.3.22f1", "1.0.1");

            CompatResult r = CompatibilityChecker.Check(m, app);
            Assert.That(r.Code, Is.EqualTo(CompatCode.Ok));
            Assert.That(r.Warnings, Is.EqualTo(new[] { CompatCode.KnownBadExporter }));
            Assert.That(CompatibilityChecker.TryFindKnownBad(m, app, out KnownBad bad), Is.True);
            Assert.That(bad.Range, Is.EqualTo(first.Range));
            Assert.That(bad.Reject, Is.False);
        }

        [Test]
        public void TryFindKnownBadFindsNothingOutsideEveryRange()
        {
            AppProfile app = App("2022.3.62f3", false, new KnownBad(">=1.0.0 <1.0.3", MessageCodes.E_COMPAT_KNOWN_BAD_EXPORTER, true));

            Assert.That(CompatibilityChecker.TryFindKnownBad(Exported("2022.3.22f1", "1.0.3"), app, out KnownBad bad), Is.False);
            Assert.That(bad.MessageCode, Is.Null);
        }

        [Test]
        public void TargetIsJudgedBeforeStream()
        {
            PackageManifest m = Exported("2021.3.45f1");
            m.exporter.buildTarget = "StandaloneOSX";

            Assert.That(CompatibilityChecker.Check(m, App("2022.3.62f3")).Code, Is.EqualTo(CompatCode.WrongTarget));
        }

        [Test]
        public void ColorSpaceIsJudgedBeforeStream()
        {
            PackageManifest m = Exported("2021.3.45f1");
            m.exporter.colorSpace = "Gamma";

            Assert.That(CompatibilityChecker.Check(m, App("2022.3.62f3")).Code, Is.EqualTo(CompatCode.WrongColorSpace));
        }

        [Test]
        public void OlderSchemaIsAccepted()
        {
            AppProfile app = App("2022.3.62f3");
            app.AppSchema = 3;

            Assert.That(CompatibilityChecker.Check(Exported("2022.3.22f1"), app).Code, Is.EqualTo(CompatCode.Ok));
        }

        [Test]
        public void FormatsOutsideTheSupportedListAreTooNew()
        {
            AppProfile app = App("2022.3.62f3");
            app.SupportedFormats = new[] { 1, 2 };
            PackageManifest m = Exported("2022.3.22f1");
            m.formatVersion = 3;

            Assert.That(CompatibilityChecker.Check(m, app).Code, Is.EqualTo(CompatCode.FormatTooNew));
            m.formatVersion = 2;
            Assert.That(CompatibilityChecker.Check(m, app).Code, Is.EqualTo(CompatCode.Ok));
        }

        [Test]
        public void TargetAndColorSpaceAreCaseSensitive()
        {
            PackageManifest m = Exported("2022.3.22f1");
            m.exporter.buildTarget = "standalonewindows64";

            Assert.That(CompatibilityChecker.Check(m, App("2022.3.62f3")).Code, Is.EqualTo(CompatCode.WrongTarget));
        }

        [Test]
        public void NullArgumentsThrow()
        {
            Assert.Throws<ArgumentNullException>(() => CompatibilityChecker.Check(null, App("2022.3.62f3")));
            Assert.Throws<ArgumentNullException>(() => CompatibilityChecker.Check(Exported("2022.3.22f1"), null));
            Assert.Throws<ArgumentNullException>(() => CompatibilityChecker.TryFindKnownBad(null, App("2022.3.62f3"), out _));
            Assert.Throws<ArgumentNullException>(() => CompatibilityChecker.TryFindKnownBad(Exported("2022.3.22f1"), null, out _));
        }

        [Test]
        public void DefaultResultHasNoWarnings()
        {
            Assert.That(default(CompatResult).Warnings, Is.Empty);
        }
    }
}
