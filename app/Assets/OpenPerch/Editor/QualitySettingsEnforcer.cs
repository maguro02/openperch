using System.Collections.Generic;
using UnityEngine;

namespace OpenPerch.App.Editor
{
    /// <summary>Forces the single quality level the app ships with.</summary>
    internal static class QualitySettingsEnforcer
    {
        /// <returns>Problems that stopped the settings from being applied; empty when everything was applied.</returns>
        public static List<string> Apply()
        {
            var problems = new List<string>();

            // Unity has no public API to delete quality levels, so extra ones are reported instead of removed.
            var levels = QualitySettings.names;
            if (levels.Length != 1)
            {
                problems.Add("Exactly one quality level is required, found " + levels.Length + " (" + string.Join(", ", levels) +
                    "). Delete the others in Project Settings > Quality.");
                return problems;
            }

            QualitySettings.SetQualityLevel(0, true);
            QualitySettings.skinWeights = SkinWeights.FourBones;
            QualitySettings.antiAliasing = 0;
            QualitySettings.globalTextureMipmapLimit = 0;
            QualitySettings.softParticles = false;
            QualitySettings.pixelLightCount = 2;
            QualitySettings.shadowDistance = 10f;
            QualitySettings.vSyncCount = 0;
            return problems;
        }
    }
}
