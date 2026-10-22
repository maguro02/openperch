using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace OpenPerch.App.Editor
{
    /// <summary>Forces the single quality level the app ships with.</summary>
    internal static class QualitySettingsEnforcer
    {
        // Unity has no public API to delete quality levels or to set the per-platform defaults, so both are edited through
        // the serialized QualitySettings object, as the Quality page of Project Settings does.
        private const string AssetPath = "ProjectSettings/QualitySettings.asset";
        private const string LevelsProperty = "m_QualitySettings";
        private const string CurrentLevelProperty = "m_CurrentQuality";
        private const string PlatformDefaultsProperty = "m_PerPlatformDefaultQuality";
        private const string StandalonePlatform = "Standalone";

        /// <summary>Checks, without changing anything, what would stop <see cref="Apply"/> from forcing the settings.</summary>
        /// <returns>One message per problem; empty when <see cref="Apply"/> can run.</returns>
        public static List<string> FindProblems()
        {
            var problems = new List<string>();
            var settings = Load();
            if (settings == null)
            {
                problems.Add(AssetPath + " could not be loaded.");
                return problems;
            }

            var levels = settings.FindProperty(LevelsProperty);
            if (levels == null || !levels.isArray || levels.arraySize == 0)
            {
                problems.Add(AssetPath + " has no " + LevelsProperty + " list with a quality level in it.");
            }

            var current = settings.FindProperty(CurrentLevelProperty);
            if (current == null || current.propertyType != SerializedPropertyType.Integer)
            {
                problems.Add(AssetPath + " has no integer " + CurrentLevelProperty + ".");
            }

            var defaults = settings.FindProperty(PlatformDefaultsProperty);
            if (defaults == null || !defaults.isArray)
            {
                problems.Add(AssetPath + " has no " + PlatformDefaultsProperty + " list.");
                return problems;
            }

            for (var i = 0; i < defaults.arraySize; i++)
            {
                var entry = defaults.GetArrayElementAtIndex(i);
                if (entry.FindPropertyRelative("first") == null || entry.FindPropertyRelative("second") == null)
                {
                    problems.Add(AssetPath + ": entry " + i + " of " + PlatformDefaultsProperty + " has no platform and level.");
                }
            }

            return problems;
        }

        /// <summary>
        /// Removes every quality level except the one the Windows player starts with, makes it the default on every
        /// platform and sets its values. Call only when <see cref="FindProblems"/> found nothing.
        /// </summary>
        public static void Apply()
        {
            var settings = Load();
            var levels = settings.FindProperty(LevelsProperty);
            var current = settings.FindProperty(CurrentLevelProperty);
            var defaults = settings.FindProperty(PlatformDefaultsProperty);

            // Keeping the level the player already started with leaves the settings this class does not force (shadow
            // type and resolution, LOD bias and so on) as they were.
            var kept = StandaloneLevel(defaults, current.intValue, levels.arraySize);
            if (kept != 0)
            {
                levels.MoveArrayElement(kept, 0);
            }

            levels.arraySize = 1;
            var excluded = levels.GetArrayElementAtIndex(0).FindPropertyRelative("excludedTargetPlatforms");
            if (excluded != null && excluded.isArray)
            {
                excluded.arraySize = 0;
            }

            current.intValue = 0;
            for (var i = 0; i < defaults.arraySize; i++)
            {
                defaults.GetArrayElementAtIndex(i).FindPropertyRelative("second").intValue = 0;
            }

            // Applied before the static setters below, which change the same object; applying this SerializedObject
            // afterwards could write its older copy of their values back.
            settings.ApplyModifiedPropertiesWithoutUndo();
            if (QualitySettings.names.Length != 1)
            {
                throw new InvalidOperationException(
                    "Quality settings still have " + QualitySettings.names.Length + " levels after all but one were removed.");
            }

            QualitySettings.SetQualityLevel(0, true);
            QualitySettings.skinWeights = SkinWeights.FourBones;
            QualitySettings.antiAliasing = 0;
            QualitySettings.globalTextureMipmapLimit = 0;
            QualitySettings.softParticles = false;
            QualitySettings.pixelLightCount = 2;
            QualitySettings.shadowDistance = 10f;
            QualitySettings.vSyncCount = 0;
        }

        private static SerializedObject Load()
        {
            var assets = AssetDatabase.LoadAllAssetsAtPath(AssetPath);
            return assets.Length > 0 && assets[0] != null ? new SerializedObject(assets[0]) : null;
        }

        /// <summary>The Standalone default level, or the current level when no default is set for Standalone.</summary>
        private static int StandaloneLevel(SerializedProperty defaults, int current, int levelCount)
        {
            var level = current;
            for (var i = 0; i < defaults.arraySize; i++)
            {
                var entry = defaults.GetArrayElementAtIndex(i);
                if (entry.FindPropertyRelative("first").stringValue == StandalonePlatform)
                {
                    level = entry.FindPropertyRelative("second").intValue;
                    break;
                }
            }

            return level >= 0 && level < levelCount ? level : 0;
        }
    }
}
