using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using UnityEngine;

namespace OpenPerch.App.Editor
{
    /// <summary>
    /// Checks that every file under a Resources folder (in Assets or in any package) or under Assets/StreamingAssets is
    /// declared in docs/asset-licenses.json. Those files ship with the player even when nothing references them, so this
    /// is the only point where an asset of unknown origin can be stopped before distribution.
    /// </summary>
    internal static class AssetLicenseAudit
    {
        /// <param name="assetPaths">Project paths of every asset that is not a folder.</param>
        /// <param name="declarationsPath">Absolute path of docs/asset-licenses.json.</param>
        /// <param name="projectFolder">Path of the Unity project from the repository root, such as <c>app</c>.</param>
        /// <returns>
        /// One message per undeclared file, or why the declarations could not be used. Files of the project are named by
        /// their path from the repository root, the form the declarations use for them.
        /// </returns>
        public static List<string> FindProblems(IEnumerable<string> assetPaths, string declarationsPath, string projectFolder)
        {
            if (!File.Exists(declarationsPath))
            {
                return new List<string> { declarationsPath + " does not exist." };
            }

            var problems = new List<string>();
            var entries = ReadEntries(declarationsPath, problems);
            if (problems.Count > 0)
            {
                return problems;
            }

            var declared = entries.Select(e => GlobToRegex(e.path)).ToList();
            var undeclared = new List<string>();
            foreach (var assetPath in assetPaths.Where(IsShippedRegardless))
            {
                var repositoryPath = RepositoryPath(assetPath, projectFolder);
                if (!declared.Any(r => r.IsMatch(assetPath) || (repositoryPath != null && r.IsMatch(repositoryPath))))
                {
                    undeclared.Add(repositoryPath ?? assetPath);
                }
            }

            undeclared.Sort(StringComparer.Ordinal);
            problems.AddRange(undeclared.Select(p => p + " ships with the player but is not declared in docs/asset-licenses.json."));
            return problems;
        }

        /// <summary>
        /// The path from the repository root of a file in the project's Assets folder, or null for a package file: the
        /// Package Manager keeps cached packages in the project's Library, so they have no path in the repository.
        /// </summary>
        private static string RepositoryPath(string assetPath, string projectFolder)
        {
            return assetPath.StartsWith("Assets/", StringComparison.Ordinal) ? projectFolder + "/" + assetPath : null;
        }

        private static bool IsShippedRegardless(string assetPath)
        {
            var segments = assetPath.Split('/');
            if (segments[0] == "Assets")
            {
                return (segments.Length > 2 && segments[1] == "StreamingAssets") || HasResourcesFolder(segments, 1);
            }

            // Packages/<name>/...: the package's own name is not a folder of the package.
            return segments[0] == "Packages" && HasResourcesFolder(segments, 2);
        }

        private static bool HasResourcesFolder(string[] segments, int first)
        {
            for (var i = first; i < segments.Length - 1; i++)
            {
                if (string.Equals(segments[i], "Resources", StringComparison.OrdinalIgnoreCase))
                {
                    return true;
                }
            }

            return false;
        }

        private static AssetLicenseEntry[] ReadEntries(string declarationsPath, List<string> problems)
        {
            var json = File.ReadAllText(declarationsPath).Trim();
            if (!json.StartsWith("[", StringComparison.Ordinal))
            {
                problems.Add(declarationsPath + " must be a JSON array of objects with path, license and source.");
                return Array.Empty<AssetLicenseEntry>();
            }

            AssetLicenseList list;
            try
            {
                list = JsonUtility.FromJson<AssetLicenseList>("{\"entries\":" + json + "}");
            }
            catch (ArgumentException e)
            {
                problems.Add(declarationsPath + " is not valid JSON: " + e.Message);
                return Array.Empty<AssetLicenseEntry>();
            }

            var entries = list?.entries ?? Array.Empty<AssetLicenseEntry>();
            for (var i = 0; i < entries.Length; i++)
            {
                var entry = entries[i];
                if (entry == null || string.IsNullOrWhiteSpace(entry.path) || string.IsNullOrWhiteSpace(entry.license) ||
                    string.IsNullOrWhiteSpace(entry.source))
                {
                    problems.Add(declarationsPath + ": entry " + i + " needs a non-empty path, license and source.");
                }
            }

            return entries;
        }

        private static Regex GlobToRegex(string glob)
        {
            var pattern = new StringBuilder("^");
            for (var i = 0; i < glob.Length; i++)
            {
                var c = glob[i];
                if (c == '*' && i + 1 < glob.Length && glob[i + 1] == '*')
                {
                    // "**/" may also match no folder at all, so "Assets/**/Resources/x" covers "Assets/Resources/x".
                    var slashFollows = i + 2 < glob.Length && glob[i + 2] == '/';
                    pattern.Append(slashFollows ? "(?:.*/)?" : ".*");
                    i += slashFollows ? 2 : 1;
                }
                else if (c == '*')
                {
                    pattern.Append("[^/]*");
                }
                else if (c == '?')
                {
                    pattern.Append("[^/]");
                }
                else
                {
                    pattern.Append(Regex.Escape(c.ToString()));
                }
            }

            return new Regex(pattern.Append('$').ToString(), RegexOptions.CultureInvariant);
        }
    }
}
