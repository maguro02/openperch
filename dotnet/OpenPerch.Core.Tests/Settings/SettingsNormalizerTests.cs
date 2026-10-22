using System;
using System.Collections.Generic;
using System.Reflection;
using NUnit.Framework;
using OpenPerch.App.Core;
using OpenPerch.App.Tests.Shared;

namespace OpenPerch.Core.Tests.Settings
{
    [Category("T-SET")]
    public class SettingsNormalizerTests
    {
        [Test]
        public void NullBecomesDefaults()
        {
            Assert.That(PersistComparer.Differences(new AppSettings(), SettingsNormalizer.Normalize(null)), Is.Empty);
        }

        [Test]
        public void DefaultsStayAsTheyAre()
        {
            var settings = WithOneOfEach();

            var normalized = SettingsNormalizer.Normalize(settings);

            Assert.That(normalized, Is.SameAs(settings));
            Assert.That(PersistComparer.Differences(WithOneOfEach(), normalized), Is.Empty);
        }

        [Test]
        public void NullFieldsBecomeDefaults()
        {
            var settings = new AppSettings
            {
                language = null,
                monitorDeviceName = null,
                avatars = null,
                pomodoro = null,
                notify = null,
                behavior = null,
                dynamics = null,
            };

            SettingsNormalizer.Normalize(settings);

            Assert.That(PersistComparer.Differences(new AppSettings(), settings), Is.Empty);
        }

        [Test]
        public void NullsInsidePlacementsBecomeDefaults()
        {
            var settings = new AppSettings();
            settings.notify.soundFile = null;
            settings.avatars.Add(null);
            settings.avatars.Add(new AvatarPlacement
            {
                instanceId = null,
                exportId = null,
                lineageId = null,
                motionPackId = null,
                savedParams = null,
                pomodoroBindings = null,
            });
            var placement = new AvatarPlacement();
            placement.savedParams.Add(null);
            placement.savedParams.Add(new SavedParam { name = null });
            placement.pomodoroBindings.Add(null);
            placement.pomodoroBindings.Add(new PomodoroParamBinding { menuPath = null, controlName = null, parameter = null });
            settings.avatars.Add(placement);

            SettingsNormalizer.Normalize(settings);

            var expected = new AppSettings();
            expected.avatars.Add(new AvatarPlacement());
            var expectedPlacement = new AvatarPlacement();
            expectedPlacement.savedParams.Add(new SavedParam());
            expectedPlacement.pomodoroBindings.Add(new PomodoroParamBinding());
            expected.avatars.Add(expectedPlacement);
            Assert.That(PersistComparer.Differences(expected, settings), Is.Empty);
        }

        [Test]
        public void NonFiniteFloatsBecomeDefaults([Values(float.NaN, float.PositiveInfinity, float.NegativeInfinity)] float bad)
        {
            var settings = WithOneOfEach();
            foreach (var holder in FloatHolders(settings))
            {
                foreach (var field in holder.GetType().GetFields(BindingFlags.Instance | BindingFlags.Public))
                {
                    if (field.FieldType == typeof(float)) field.SetValue(holder, bad);
                }
            }

            SettingsNormalizer.Normalize(settings);

            Assert.That(PersistComparer.Differences(WithOneOfEach(), settings), Is.Empty);
        }

        [TestCaseSource(nameof(RangeCases))]
        public void ValuesAreBroughtIntoRange(Action<AppSettings> set, Func<AppSettings, object> get, object expected)
        {
            var settings = WithOneOfEach();
            set(settings);

            SettingsNormalizer.Normalize(settings);

            Assert.That(get(settings), Is.EqualTo(expected));
        }

        private static IEnumerable<TestCaseData> RangeCases()
        {
            yield return Case("schemaVersion 0", s => s.schemaVersion = 0, s => s.schemaVersion, 1);
            yield return Case("schemaVersion negative", s => s.schemaVersion = -3, s => s.schemaVersion, 1);
            yield return Case("schemaVersion newer", s => s.schemaVersion = 2, s => s.schemaVersion, 1);

            yield return Case("targetFpsCap low", s => s.targetFpsCap = 0, s => s.targetFpsCap, 10);
            yield return Case("targetFpsCap high", s => s.targetFpsCap = 1000, s => s.targetFpsCap, 240);
            yield return Case("targetFpsCap kept", s => s.targetFpsCap = 144, s => s.targetFpsCap, 144);
            yield return Case("checkUpdates on kept", s => s.checkUpdates = 1, s => s.checkUpdates, 1);
            yield return Case("checkUpdates off kept", s => s.checkUpdates = 0, s => s.checkUpdates, 0);
            yield return Case("checkUpdates unknown", s => s.checkUpdates = 2, s => s.checkUpdates, -1);
            yield return Case("checkUpdates below", s => s.checkUpdates = -2, s => s.checkUpdates, -1);
            yield return Case("ambientIntensity low", s => s.ambientIntensity = -1f, s => s.ambientIntensity, 0f);
            yield return Case("ambientIntensity high", s => s.ambientIntensity = 5f, s => s.ambientIntensity, 2f);
            yield return Case("msaa 0", s => s.msaa = 0, s => s.msaa, 1);
            yield return Case("msaa negative", s => s.msaa = -4, s => s.msaa, 1);
            yield return Case("msaa 1", s => s.msaa = 1, s => s.msaa, 1);
            yield return Case("msaa 2", s => s.msaa = 2, s => s.msaa, 2);
            yield return Case("msaa 3", s => s.msaa = 3, s => s.msaa, 2);
            yield return Case("msaa 6", s => s.msaa = 6, s => s.msaa, 4);
            yield return Case("msaa 8", s => s.msaa = 8, s => s.msaa, 8);
            yield return Case("msaa 16", s => s.msaa = 16, s => s.msaa, 8);

            yield return Case("displayHeightDip low", s => s.avatars[0].displayHeightDip = 10, s => s.avatars[0].displayHeightDip, 100);
            yield return Case("displayHeightDip high", s => s.avatars[0].displayHeightDip = 5000, s => s.avatars[0].displayHeightDip, 1080);
            yield return Case("xNorm low", s => s.avatars[0].xNorm = -0.5f, s => s.avatars[0].xNorm, 0f);
            yield return Case("xNorm high", s => s.avatars[0].xNorm = 1.5f, s => s.avatars[0].xNorm, 1f);
            yield return Case("compositingOverride kept", s => s.avatars[0].compositingOverride = 1, s => s.avatars[0].compositingOverride, 1);
            yield return Case("compositingOverride unknown", s => s.avatars[0].compositingOverride = 2, s => s.avatars[0].compositingOverride, -1);
            yield return Case("compositingOverride below", s => s.avatars[0].compositingOverride = -2, s => s.avatars[0].compositingOverride, -1);
            yield return Case("savedParam low", s => s.avatars[0].savedParams[0].value = -2f, s => s.avatars[0].savedParams[0].value, -1f);
            yield return Case("savedParam high", s => s.avatars[0].savedParams[0].value = 300f, s => s.avatars[0].savedParams[0].value, 255f);
            yield return Case("binding value low", s => s.avatars[0].pomodoroBindings[0].value = -2f, s => s.avatars[0].pomodoroBindings[0].value, -1f);
            yield return Case("binding value high", s => s.avatars[0].pomodoroBindings[0].value = 300f, s => s.avatars[0].pomodoroBindings[0].value, 255f);
            yield return Case("binding mode kept", s => s.avatars[0].pomodoroBindings[0].mode = PomodoroMode.Break, s => s.avatars[0].pomodoroBindings[0].mode, PomodoroMode.Break);
            yield return Case("binding mode unknown", s => s.avatars[0].pomodoroBindings[0].mode = (PomodoroMode)9, s => s.avatars[0].pomodoroBindings[0].mode, PomodoroMode.None);

            yield return Case("workMin low", s => s.pomodoro.workMin = 0, s => s.pomodoro.workMin, 1);
            yield return Case("workMin high", s => s.pomodoro.workMin = 500, s => s.pomodoro.workMin, 180);
            yield return Case("shortBreakMin low", s => s.pomodoro.shortBreakMin = -5, s => s.pomodoro.shortBreakMin, 1);
            yield return Case("shortBreakMin high", s => s.pomodoro.shortBreakMin = 90, s => s.pomodoro.shortBreakMin, 60);
            yield return Case("longBreakMin low", s => s.pomodoro.longBreakMin = 0, s => s.pomodoro.longBreakMin, 1);
            yield return Case("longBreakMin high", s => s.pomodoro.longBreakMin = 200, s => s.pomodoro.longBreakMin, 120);
            yield return Case("longBreakEvery low", s => s.pomodoro.longBreakEvery = 0, s => s.pomodoro.longBreakEvery, 1);
            yield return Case("longBreakEvery high", s => s.pomodoro.longBreakEvery = 50, s => s.pomodoro.longBreakEvery, 12);

            yield return Case("soundKind kept", s => s.notify.soundKind = 3, s => s.notify.soundKind, 3);
            yield return Case("soundKind none kept", s => s.notify.soundKind = 0, s => s.notify.soundKind, 0);
            yield return Case("soundKind unknown", s => s.notify.soundKind = 4, s => s.notify.soundKind, 1);
            yield return Case("soundKind negative", s => s.notify.soundKind = -1, s => s.notify.soundKind, 1);
            yield return Case("volume low", s => s.notify.volume = -0.5f, s => s.notify.volume, 0f);
            yield return Case("volume high", s => s.notify.volume = 1.5f, s => s.notify.volume, 1f);

            yield return Case("idleMinSec low", s => s.behavior.idleMinSec = 0f, s => s.behavior.idleMinSec, 1f);
            yield return Case("idleMinSec high", s => s.behavior.idleMinSec = 1000f, s => s.behavior.idleMinSec, 300f);
            yield return Case("idleMaxSec high", s => s.behavior.idleMaxSec = 1000f, s => s.behavior.idleMaxSec, 300f);
            yield return Case("idleMaxSec below idleMinSec", s => { s.behavior.idleMinSec = 20f; s.behavior.idleMaxSec = 10f; }, s => s.behavior.idleMaxSec, 20f);
            yield return Case("idleMaxSec NaN above idleMinSec", s => { s.behavior.idleMinSec = 20f; s.behavior.idleMaxSec = float.NaN; }, s => s.behavior.idleMaxSec, 20f);
            yield return Case("wWalk low", s => s.behavior.wWalk = -1f, s => s.behavior.wWalk, 0f);
            yield return Case("wWalk high", s => s.behavior.wWalk = 11f, s => s.behavior.wWalk, 10f);
            yield return Case("wSit high", s => s.behavior.wSit = 11f, s => s.behavior.wSit, 10f);
            yield return Case("wIdle low", s => s.behavior.wIdle = -1f, s => s.behavior.wIdle, 0f);
            yield return Case("wVariant high", s => s.behavior.wVariant = 11f, s => s.behavior.wVariant, 10f);
            yield return Case("wJump low", s => s.behavior.wJump = -1f, s => s.behavior.wJump, 0f);
            yield return Case("walkMinRatio low", s => s.behavior.walkMinRatio = -1f, s => s.behavior.walkMinRatio, 0f);
            yield return Case("walkMinRatio high", s => s.behavior.walkMinRatio = 20f, s => s.behavior.walkMinRatio, 10f);
            yield return Case("walkMaxRatio high", s => s.behavior.walkMaxRatio = 20f, s => s.behavior.walkMaxRatio, 10f);
            yield return Case("walkMaxRatio below walkMinRatio", s => { s.behavior.walkMinRatio = 2f; s.behavior.walkMaxRatio = 1f; }, s => s.behavior.walkMaxRatio, 2f);
            yield return Case("jumpMaxRiseRatio high", s => s.behavior.jumpMaxRiseRatio = 20f, s => s.behavior.jumpMaxRiseRatio, 10f);
            yield return Case("jumpMaxRunRatio low", s => s.behavior.jumpMaxRunRatio = -1f, s => s.behavior.jumpMaxRunRatio, 0f);
            yield return Case("focusWalkScale high", s => s.behavior.focusWalkScale = 2f, s => s.behavior.focusWalkScale, 1f);
            yield return Case("breakScale high", s => s.behavior.breakScale = 20f, s => s.behavior.breakScale, 10f);
            yield return Case("occludedFallSec low", s => s.behavior.occludedFallSec = -1f, s => s.behavior.occludedFallSec, 0f);
            yield return Case("occludedFallSec high", s => s.behavior.occludedFallSec = 10f, s => s.behavior.occludedFallSec, 5f);

            yield return Case("inertiaScale high", s => s.dynamics.inertiaScale = 3f, s => s.dynamics.inertiaScale, 2f);
            yield return Case("inertiaScale low", s => s.dynamics.inertiaScale = -1f, s => s.dynamics.inertiaScale, 0f);
            yield return Case("worldMoveSpeedLimit high", s => s.dynamics.worldMoveSpeedLimit = 500f, s => s.dynamics.worldMoveSpeedLimit, 100f);
            yield return Case("worldRotSpeedLimitDeg high", s => s.dynamics.worldRotSpeedLimitDeg = 10000f, s => s.dynamics.worldRotSpeedLimitDeg, 3600f);
            yield return Case("particleSpeedLimit low", s => s.dynamics.particleSpeedLimit = -1f, s => s.dynamics.particleSpeedLimit, 0f);
            yield return Case("teleportDistance low", s => s.dynamics.teleportDistance = 0f, s => s.dynamics.teleportDistance, 0.01f);
            yield return Case("teleportDistance high", s => s.dynamics.teleportDistance = 50f, s => s.dynamics.teleportDistance, 10f);
            yield return Case("teleportAngleDeg low", s => s.dynamics.teleportAngleDeg = 0f, s => s.dynamics.teleportAngleDeg, 1f);
            yield return Case("teleportAngleDeg high", s => s.dynamics.teleportAngleDeg = 270f, s => s.dynamics.teleportAngleDeg, 180f);
            yield return Case("gapTeleportSeconds low", s => s.dynamics.gapTeleportSeconds = 0f, s => s.dynamics.gapTeleportSeconds, 0.05f);
            yield return Case("gapTeleportSeconds high", s => s.dynamics.gapTeleportSeconds = 60f, s => s.dynamics.gapTeleportSeconds, 5f);
            yield return Case("rateHz low", s => s.dynamics.rateHz = 1f, s => s.dynamics.rateHz, 15f);
            yield return Case("rateHz high", s => s.dynamics.rateHz = 1000f, s => s.dynamics.rateHz, 120f);
            yield return Case("rateHz 30 kept", s => s.dynamics.rateHz = 30f, s => s.dynamics.rateHz, 30f);
            yield return Case("clothInertiaScale high", s => s.dynamics.clothInertiaScale = 3f, s => s.dynamics.clothInertiaScale, 2f);
            yield return Case("warmupSteps low", s => s.dynamics.warmupSteps = -1, s => s.dynamics.warmupSteps, 0);
            yield return Case("warmupSteps high", s => s.dynamics.warmupSteps = 1000, s => s.dynamics.warmupSteps, 100);
        }

        private static TestCaseData Case(string name, Action<AppSettings> set, Func<AppSettings, object> get, object expected)
        {
            return new TestCaseData(set, get, expected).SetName(name);
        }

        private static AppSettings WithOneOfEach()
        {
            var settings = new AppSettings();
            var placement = new AvatarPlacement();
            placement.savedParams.Add(new SavedParam());
            placement.pomodoroBindings.Add(new PomodoroParamBinding());
            settings.avatars.Add(placement);
            return settings;
        }

        private static IEnumerable<object> FloatHolders(AppSettings s)
        {
            yield return s;
            yield return s.pomodoro;
            yield return s.notify;
            yield return s.behavior;
            yield return s.dynamics;
            yield return s.avatars[0];
            yield return s.avatars[0].savedParams[0];
            yield return s.avatars[0].pomodoroBindings[0];
        }
    }
}
