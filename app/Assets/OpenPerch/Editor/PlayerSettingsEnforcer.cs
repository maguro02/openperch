using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditor.Analytics;
using UnityEditor.Build;
using UnityEditor.CrashReporting;
using UnityEngine;
using UnityEngine.Rendering;

namespace OpenPerch.App.Editor
{
    /// <summary>Forces the Player settings the app depends on, so that no build picks up a stray local change.</summary>
    internal static class PlayerSettingsEnforcer
    {
        // Value of "activeInputHandler" in ProjectSettings.asset for Input Manager (Old). Unity has no public API for this
        // setting, so it is written through the serialized PlayerSettings object.
        private const int InputManagerOld = 0;

        /// <summary>Checks, without changing anything, what would stop <see cref="Apply"/> from forcing the settings.</summary>
        /// <returns>One message per problem; empty when <see cref="Apply"/> can run.</returns>
        public static List<string> FindProblems()
        {
            var problems = new List<string>();
            if (!string.IsNullOrEmpty(CloudProjectSettings.projectId))
            {
                problems.Add("The project is linked to Unity Cloud project " + CloudProjectSettings.projectId +
                    "; unlink it in Project Settings > Services.");
            }

            if (FindPlayerSettingsProperty("activeInputHandler") == null)
            {
                problems.Add("PlayerSettings has no activeInputHandler property, so Input Manager (Old) cannot be forced.");
            }

            return problems;
        }

        /// <summary>Forces the settings. Call only when <see cref="FindProblems"/> found nothing.</summary>
        public static void Apply(BuildProfile profile)
        {
            // Applied before the static setters below, which change the same object; applying this SerializedObject
            // afterwards could write its older copy of their values back.
            var inputHandler = FindPlayerSettingsProperty("activeInputHandler");
            inputHandler.intValue = InputManagerOld;
            inputHandler.serializedObject.ApplyModifiedPropertiesWithoutUndo();

            var standalone = NamedBuildTarget.Standalone;
            PlayerSettings.SetScriptingBackend(standalone, ScriptingImplementation.Mono2x);
            PlayerSettings.SetManagedStrippingLevel(standalone, ManagedStrippingLevel.Disabled);
            PlayerSettings.SetApiCompatibilityLevel(standalone, ApiCompatibilityLevel.NET_Standard);

            PlayerSettings.colorSpace = ColorSpace.Linear;
            PlayerSettings.SetUseDefaultGraphicsAPIs(BuildTarget.StandaloneWindows64, false);
            PlayerSettings.SetGraphicsAPIs(BuildTarget.StandaloneWindows64, new[] { GraphicsDeviceType.Direct3D11 });
            PlayerSettings.useFlipModelSwapchain = false;

            PlayerSettings.fullScreenMode = FullScreenMode.Windowed;
            PlayerSettings.defaultScreenWidth = 480;
            PlayerSettings.defaultScreenHeight = 270;
            PlayerSettings.resizableWindow = true;
            PlayerSettings.allowFullscreenSwitch = false;
            PlayerSettings.runInBackground = true;
            PlayerSettings.forceSingleInstance = false;

            PlayerSettings.gpuSkinning = true;
            PlayerSettings.enableFrameTimingStats = true;
            AnalyticsSettings.enabled = false;
            PerformanceReportingSettings.enabled = false;
            CrashReportingSettings.enabled = false;

            if (profile == BuildProfile.U6000)
            {
                PlayerSettings.SplashScreen.show = false;
            }
        }

        private static SerializedProperty FindPlayerSettingsProperty(string name)
        {
            var settings = Resources.FindObjectsOfTypeAll<PlayerSettings>().FirstOrDefault();
            return settings == null ? null : new SerializedObject(settings).FindProperty(name);
        }
    }
}
