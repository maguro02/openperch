using System.Collections.Generic;
using UnityEditor;
using UnityEditor.Rendering;
using UnityEngine;
using UnityEngine.Rendering;

namespace OpenPerch.App.Editor
{
    /// <summary>Forces the Graphics settings the app depends on.</summary>
    internal static class GraphicsSettingsEnforcer
    {
        // Bundles leave out the shaders their project always includes and expect the player to provide them, so the app
        // always includes the same six as the VRChat project template. The app's own shaders follow; no scene references them.
        private static readonly string[] AlwaysIncludedShaders =
        {
            "Legacy Shaders/Diffuse",
            "Hidden/CubeBlur",
            "Hidden/CubeCopy",
            "Hidden/CubeBlend",
            "Sprites/Default",
            "UI/Default",
            "OpenPerch/Composite",
            "OpenPerch/UIPremul",
        };

        private static readonly GraphicsTier[] Tiers = { GraphicsTier.Tier1, GraphicsTier.Tier2, GraphicsTier.Tier3 };

        /// <returns>Problems that stopped the settings from being applied; empty when everything was applied.</returns>
        public static List<string> Apply()
        {
            var problems = new List<string>();
            var shaders = new Shader[AlwaysIncludedShaders.Length];
            for (var i = 0; i < shaders.Length; i++)
            {
                shaders[i] = Shader.Find(AlwaysIncludedShaders[i]);
                if (shaders[i] == null)
                {
                    problems.Add("Shader \"" + AlwaysIncludedShaders[i] + "\" was not found, so it cannot be always included.");
                }
            }

            // The always-included list has no public API; it is edited through the serialized GraphicsSettings object.
            var graphicsSettings = new SerializedObject(GraphicsSettings.GetGraphicsSettings());
            var alwaysIncluded = graphicsSettings.FindProperty("m_AlwaysIncludedShaders");
            if (alwaysIncluded == null || !alwaysIncluded.isArray)
            {
                problems.Add("GraphicsSettings has no m_AlwaysIncludedShaders list.");
            }

            if (problems.Count > 0)
            {
                return problems;
            }

            alwaysIncluded.arraySize = shaders.Length;
            for (var i = 0; i < shaders.Length; i++)
            {
                alwaysIncluded.GetArrayElementAtIndex(i).objectReferenceValue = shaders[i];
            }

            // Applied before the static setters below, which change the same object; applying this SerializedObject
            // afterwards could write its older copy of their values back.
            graphicsSettings.ApplyModifiedPropertiesWithoutUndo();

            GraphicsSettings.lightsUseLinearIntensity = true;
            GraphicsSettings.lightsUseColorTemperature = true;
            foreach (var tier in Tiers)
            {
                var settings = EditorGraphicsSettings.GetTierSettings(BuildTargetGroup.Standalone, tier);
                settings.hdrMode = CameraHDRMode.FP16;
                EditorGraphicsSettings.SetTierSettings(BuildTargetGroup.Standalone, tier, settings);
            }

            return problems;
        }
    }
}
