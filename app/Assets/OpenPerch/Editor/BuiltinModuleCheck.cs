using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;

namespace OpenPerch.App.Editor
{
    /// <summary>
    /// Checks that the built-in modules the app needs are listed in Packages/manifest.json. A module missing there is left
    /// out of the player, and the components of its types in loaded bundles then go missing.
    /// </summary>
    internal static class BuiltinModuleCheck
    {
        private static readonly string[] RequiredModules =
        {
            "animation", "cloth", "particlesystem", "physics", "audio",
            "imageconversion", "jsonserialize", "ui", "director", "unitywebrequest",
        };

        // JsonUtility cannot read the dependencies object (a dictionary), so its module keys are matched directly.
        private static readonly Regex ModuleKey = new Regex("\"com\\.unity\\.modules\\.([a-z0-9]+)\"\\s*:", RegexOptions.CultureInvariant);

        /// <returns>One message per missing module, or why the manifest could not be read.</returns>
        public static List<string> FindProblems(string manifestPath)
        {
            if (!File.Exists(manifestPath))
            {
                return new List<string> { manifestPath + " does not exist." };
            }

            var listed = new HashSet<string>(ModuleKey.Matches(File.ReadAllText(manifestPath))
                .Cast<Match>()
                .Select(m => m.Groups[1].Value));
            return RequiredModules
                .Where(m => !listed.Contains(m))
                .Select(m => "Built-in module com.unity.modules." + m + " is missing from Packages/manifest.json.")
                .ToList();
        }
    }
}
