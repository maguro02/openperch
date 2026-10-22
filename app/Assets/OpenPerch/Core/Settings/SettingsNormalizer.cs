using System;
using System.Collections.Generic;

namespace OpenPerch.App.Core
{
    /// <summary>
    /// Repairs settings read from disk: null fields become their defaults, numbers are clamped into their valid ranges,
    /// codes outside their set fall back to the default, and <see cref="AppSettings.schemaVersion"/> is migrated to
    /// <see cref="CurrentSchemaVersion"/>. Non-finite floats also fall back to the default.
    /// </summary>
    public static class SettingsNormalizer
    {
        /// <summary>The <see cref="AppSettings.schemaVersion"/> this build reads and writes.</summary>
        public const int CurrentSchemaVersion = 1;

        /// <summary>
        /// Normalizes <paramref name="s"/> in place and returns it, or returns new defaults when it is null. A
        /// <see cref="AppSettings.schemaVersion"/> above <see cref="CurrentSchemaVersion"/>, written by a newer build, is
        /// lowered to <see cref="CurrentSchemaVersion"/> as well: the fields only that build knows are already gone, so once
        /// these settings are saved the newer build's values are lost. <see cref="SettingsStore.Load"/> copies such a file
        /// aside before it calls this method.
        /// </summary>
        public static AppSettings Normalize(AppSettings s)
        {
            if (s == null) return new AppSettings();

            Migrate(s);

            var d = new AppSettings();
            s.language = s.language ?? d.language;
            s.monitorDeviceName = s.monitorDeviceName ?? d.monitorDeviceName;
            s.targetFpsCap = Clamp(s.targetFpsCap, 10, 240);
            s.checkUpdates = s.checkUpdates >= -1 && s.checkUpdates <= 1 ? s.checkUpdates : d.checkUpdates;
            s.ambientIntensity = Clamp(s.ambientIntensity, 0f, 2f, d.ambientIntensity);
            s.msaa = NormalizeMsaa(s.msaa);

            if (s.avatars == null) s.avatars = new List<AvatarPlacement>();
            s.avatars.RemoveAll(p => p == null);
            foreach (var placement in s.avatars) NormalizePlacement(placement);

            s.pomodoro = NormalizePomodoro(s.pomodoro);
            s.notify = NormalizeNotify(s.notify);
            s.behavior = NormalizeBehavior(s.behavior);
            s.dynamics = NormalizeDynamics(s.dynamics);
            return s;
        }

        private static void Migrate(AppSettings s)
        {
            // Version 0 has the same fields as version 1, so migrating it only changes the number. A newer version is
            // lowered too, so that when the newer build reads the file again it migrates it instead of trusting a version
            // whose fields are no longer all there.
            s.schemaVersion = CurrentSchemaVersion;
        }

        private static void NormalizePlacement(AvatarPlacement p)
        {
            var d = new AvatarPlacement();
            p.instanceId = p.instanceId ?? d.instanceId;
            p.exportId = p.exportId ?? d.exportId;
            p.lineageId = p.lineageId ?? d.lineageId;
            p.motionPackId = p.motionPackId ?? d.motionPackId;
            p.displayHeightDip = Clamp(p.displayHeightDip, 100, 1080);
            p.xNorm = Clamp(p.xNorm, 0f, 1f, d.xNorm);
            p.compositingOverride = p.compositingOverride >= -1 && p.compositingOverride <= 1 ? p.compositingOverride : d.compositingOverride;

            if (p.savedParams == null) p.savedParams = new List<SavedParam>();
            p.savedParams.RemoveAll(x => x == null);
            foreach (var saved in p.savedParams)
            {
                saved.name = saved.name ?? "";
                saved.value = ClampParamValue(saved.value);
            }

            if (p.pomodoroBindings == null) p.pomodoroBindings = new List<PomodoroParamBinding>();
            p.pomodoroBindings.RemoveAll(x => x == null);
            foreach (var binding in p.pomodoroBindings)
            {
                if (!Enum.IsDefined(typeof(PomodoroMode), binding.mode)) binding.mode = PomodoroMode.None;
                binding.menuPath = binding.menuPath ?? "";
                binding.controlName = binding.controlName ?? "";
                binding.parameter = binding.parameter ?? "";
                binding.value = ClampParamValue(binding.value);
            }
        }

        private static PomodoroConfig NormalizePomodoro(PomodoroConfig c)
        {
            if (c == null) return new PomodoroConfig();
            c.workMin = Clamp(c.workMin, 1, 180);
            c.shortBreakMin = Clamp(c.shortBreakMin, 1, 60);
            c.longBreakMin = Clamp(c.longBreakMin, 1, 120);
            c.longBreakEvery = Clamp(c.longBreakEvery, 1, 12);
            return c;
        }

        private static NotificationSettings NormalizeNotify(NotificationSettings n)
        {
            if (n == null) return new NotificationSettings();
            var d = new NotificationSettings();
            n.soundKind = n.soundKind >= 0 && n.soundKind <= 3 ? n.soundKind : d.soundKind;
            n.soundFile = n.soundFile ?? d.soundFile;
            n.volume = Clamp(n.volume, 0f, 1f, d.volume);
            return n;
        }

        private static BehaviorConfig NormalizeBehavior(BehaviorConfig b)
        {
            if (b == null) return new BehaviorConfig();
            var d = new BehaviorConfig();
            b.idleMinSec = Clamp(b.idleMinSec, 1f, 300f, d.idleMinSec);
            b.idleMaxSec = Clamp(b.idleMaxSec, b.idleMinSec, 300f, Math.Max(d.idleMaxSec, b.idleMinSec));
            b.wWalk = Clamp(b.wWalk, 0f, 10f, d.wWalk);
            b.wSit = Clamp(b.wSit, 0f, 10f, d.wSit);
            b.wIdle = Clamp(b.wIdle, 0f, 10f, d.wIdle);
            b.wVariant = Clamp(b.wVariant, 0f, 10f, d.wVariant);
            b.wJump = Clamp(b.wJump, 0f, 10f, d.wJump);
            b.walkMinRatio = Clamp(b.walkMinRatio, 0f, 10f, d.walkMinRatio);
            b.walkMaxRatio = Clamp(b.walkMaxRatio, b.walkMinRatio, 10f, Math.Max(d.walkMaxRatio, b.walkMinRatio));
            b.jumpMaxRiseRatio = Clamp(b.jumpMaxRiseRatio, 0f, 10f, d.jumpMaxRiseRatio);
            b.jumpMaxRunRatio = Clamp(b.jumpMaxRunRatio, 0f, 10f, d.jumpMaxRunRatio);
            b.focusWalkScale = Clamp(b.focusWalkScale, 0f, 1f, d.focusWalkScale);
            b.breakScale = Clamp(b.breakScale, 0f, 10f, d.breakScale);
            b.occludedFallSec = Clamp(b.occludedFallSec, 0f, 5f, d.occludedFallSec);
            return b;
        }

        private static DynamicsMotionSettings NormalizeDynamics(DynamicsMotionSettings m)
        {
            if (m == null) return new DynamicsMotionSettings();
            var d = new DynamicsMotionSettings();
            m.inertiaScale = Clamp(m.inertiaScale, 0f, 2f, d.inertiaScale);
            m.worldMoveSpeedLimit = Clamp(m.worldMoveSpeedLimit, 0f, 100f, d.worldMoveSpeedLimit);
            m.worldRotSpeedLimitDeg = Clamp(m.worldRotSpeedLimitDeg, 0f, 3600f, d.worldRotSpeedLimitDeg);
            m.particleSpeedLimit = Clamp(m.particleSpeedLimit, 0f, 100f, d.particleSpeedLimit);
            m.teleportDistance = Clamp(m.teleportDistance, 0.01f, 10f, d.teleportDistance);
            m.teleportAngleDeg = Clamp(m.teleportAngleDeg, 1f, 180f, d.teleportAngleDeg);
            m.gapTeleportSeconds = Clamp(m.gapTeleportSeconds, 0.05f, 5f, d.gapTeleportSeconds);
            m.rateHz = Clamp(m.rateHz, 15f, 120f, d.rateHz);
            m.clothInertiaScale = Clamp(m.clothInertiaScale, 0f, 2f, d.clothInertiaScale);
            m.warmupSteps = Clamp(m.warmupSteps, 0, 100);
            return m;
        }

        /// <summary>Snaps to the sample counts the render textures accept: 1, 2, 4 or 8.</summary>
        private static int NormalizeMsaa(int samples)
        {
            if (samples >= 8) return 8;
            if (samples >= 4) return 4;
            if (samples >= 2) return 2;
            return 1;
        }

        // The parameter type is unknown here, so the bound covers every expression type: Float -1..1, Int 0..255, Bool 0/1.
        private static float ClampParamValue(float v) => Clamp(v, -1f, 255f, 0f);

        private static int Clamp(int v, int min, int max) => v < min ? min : v > max ? max : v;

        private static float Clamp(float v, float min, float max, float fallback)
        {
            if (float.IsNaN(v) || float.IsInfinity(v)) return fallback;
            return v < min ? min : v > max ? max : v;
        }
    }
}
