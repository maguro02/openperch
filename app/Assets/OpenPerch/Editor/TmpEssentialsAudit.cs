using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;

namespace OpenPerch.App.Editor
{
    /// <summary>
    /// Checks that the parts of TextMesh Pro's Essential Resources the app does not ship are gone: the EmojiOne sprite
    /// asset and texture, the LiberationSans SDF font assets, and TMP Settings' reference to a default sprite asset.
    /// </summary>
    internal static class TmpEssentialsAudit
    {
        // Private field of TMP_Settings behind TMP_Settings.defaultSpriteAsset. Read through SerializedObject because the
        // editor assembly does not reference TextMesh Pro.
        private const string DefaultSpriteAssetField = "m_defaultSpriteAsset";

        /// <param name="assetPaths">Project paths of every asset that is not a folder.</param>
        /// <returns>One message per leftover.</returns>
        public static List<string> FindProblems(IEnumerable<string> assetPaths)
        {
            var problems = new List<string>();
            foreach (var path in assetPaths.Where(p => p.StartsWith("Assets/", StringComparison.Ordinal)))
            {
                var fileName = path.Substring(path.LastIndexOf('/') + 1);
                if (fileName == "EmojiOne.asset" || fileName == "EmojiOne.png")
                {
                    problems.Add(path + ": delete TextMesh Pro's EmojiOne sprite asset and texture.");
                }
                else if (fileName.StartsWith("LiberationSans SDF", StringComparison.Ordinal))
                {
                    problems.Add(path + ": delete TextMesh Pro's LiberationSans SDF font assets.");
                }
                else if (fileName == "TMP Settings.asset")
                {
                    var problem = CheckDefaultSpriteAsset(path);
                    if (problem != null)
                    {
                        problems.Add(problem);
                    }
                }
            }

            return problems;
        }

        private static string CheckDefaultSpriteAsset(string path)
        {
            var settings = AssetDatabase.LoadMainAssetAtPath(path);
            if (settings == null)
            {
                return path + " could not be loaded.";
            }

            var defaultSprite = new SerializedObject(settings).FindProperty(DefaultSpriteAssetField);
            if (defaultSprite == null)
            {
                return path + " has no " + DefaultSpriteAssetField + " field, so its default sprite asset cannot be checked.";
            }

            return defaultSprite.objectReferenceValue == null
                ? null
                : path + ": clear Default Sprite Asset in the TMP Settings.";
        }
    }
}
