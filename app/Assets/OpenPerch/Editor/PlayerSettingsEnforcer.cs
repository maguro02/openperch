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

        /// <returns>Problems that stopped the settings from being applied; empty when everything was applied.</returns>
        public static List<string> Apply(BuildProfile profile)
        {
            var problems = new List<string>();
            if (!string.IsNullOrEmpty(CloudProjectSettings.projectId))
            {
                problems.Add("The project is linked to Unity Cloud project " + CloudProjectSettings.projectId +
                    "; unlink it in Project Settings > Services.");
            }

            var inputHandler = FindPlayerSettingsProperty("activeInputHandler");
            if (inputHandler == null)
            {
                problems.Add("PlayerSettings has no activeInputHandler property, so Input Manager (Old) cannot be forced.");
            }

            if (problems.Count > 0)
            {
                return problems;
            }

            // Applied before the static setters below, which change the same object; applying this SerializedObject
            // afterwards could write its older copy of their values back.
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

            return problems;
        }

        private static SerializedProperty FindPlayerSettingsProperty(string name)
        {
            var settings = Resources.FindObjectsOfTypeAll<PlayerSettings>().FirstOrDefault();
            return settings == null ? null : new SerializedObject(settings).FindProperty(name);
        }
    }
}
