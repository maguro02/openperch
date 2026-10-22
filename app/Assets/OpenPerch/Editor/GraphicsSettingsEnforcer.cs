using System.Collections.Generic;
using System.Linq;
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

        /// <summary>Checks, without changing anything, what would stop <see cref="Apply"/> from forcing the settings.</summary>
        /// <returns>One message per problem; empty when <see cref="Apply"/> can run.</returns>
        public static List<string> FindProblems()
        {
            var problems = AlwaysIncludedShaders
                .Where(name => Shader.Find(name) == null)
                .Select(name => "Shader \"" + name + "\" was not found, so it cannot be always included.")
                .ToList();
            if (AlwaysIncludedList(new SerializedObject(GraphicsSettings.GetGraphicsSettings())) == null)
            {
                problems.Add("GraphicsSettings has no m_AlwaysIncludedShaders list.");
            }

            return problems;
        }

        /// <summary>Forces the settings. Call only when <see cref="FindProblems"/> found nothing.</summary>
        public static void Apply()
        {
            var graphicsSettings = new SerializedObject(GraphicsSettings.GetGraphicsSettings());
            var alwaysIncluded = AlwaysIncludedList(graphicsSettings);
            alwaysIncluded.arraySize = AlwaysIncludedShaders.Length;
            for (var i = 0; i < AlwaysIncludedShaders.Length; i++)
            {
                alwaysIncluded.GetArrayElementAtIndex(i).objectReferenceValue = Shader.Find(AlwaysIncludedShaders[i]);
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
        }

        // The always-included list has no public API; it is edited through the serialized GraphicsSettings object.
        private static SerializedProperty AlwaysIncludedList(SerializedObject graphicsSettings)
        {
            var list = graphicsSettings.FindProperty("m_AlwaysIncludedShaders");
            return list != null && list.isArray ? list : null;
        }
    }
}
