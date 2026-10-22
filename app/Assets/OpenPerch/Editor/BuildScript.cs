using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEngine;

namespace OpenPerch.App.Editor
{
    /// <summary>Builds the Windows player from the command line.</summary>
    public static class BuildScript
    {
        private const string LogPrefix = "[OpenPerch build] ";
        private const string TestSupportDefine = "OPENPERCH_TEST_SUPPORT";
        private const string PlayerFileName = "OpenPerch.exe";

        // Batch mode opens the project in whichever patch it is started with, so the patch is checked here. The app's
        // compatibility checks assume this exact player version, and older 2022.3 patches lack the fix for CVE-2025-59489.
        private const string U2022Editor = "2022.3.62f3";

        // No 6000.3 patch has been chosen for the app yet, so the U6000 profile refuses to build until one is set here.
        private const string U6000Editor = null;

        /// <summary>
        /// Entry point of
        /// <c>-executeMethod OpenPerch.App.Editor.BuildScript.BuildWindowsCli -opOut &lt;dir&gt; -opVersion &lt;semver&gt; [-opDev] [-opProfile U2022|U6000]</c>.
        /// Audits what the player would ship, forces the Player, Quality and Graphics settings, builds the Windows x64
        /// player into the output directory and puts a Licenses folder next to it.
        /// In batch mode the editor then exits with 0 on success and 1 on any failure.
        /// </summary>
        public static void BuildWindowsCli()
        {
            var succeeded = false;
            try
            {
                succeeded = Build(Environment.GetCommandLineArgs());
            }
            catch (Exception e)
            {
                Debug.LogException(e);
            }

            if (succeeded)
            {
                Debug.Log(LogPrefix + "Succeeded.");
            }
            else
            {
                Debug.LogError(LogPrefix + "Failed.");
            }

            if (Application.isBatchMode)
            {
                EditorApplication.Exit(succeeded ? 0 : 1);
            }
        }

        private static bool Build(string[] commandLine)
        {
            if (!BuildArguments.TryParse(commandLine, out var arguments, out var argumentError))
            {
                return Fail(new[] { argumentError });
            }

            Debug.Log(LogPrefix + "Building " + arguments.Version + " with profile " + arguments.Profile +
                (arguments.Development ? " (development)." : "."));

            var projectRoot = Directory.GetParent(Application.dataPath).FullName;
            var repositoryRoot = Directory.GetParent(projectRoot).FullName;
            var assetPaths = AssetDatabase.GetAllAssetPaths().Where(p => !AssetDatabase.IsValidFolder(p)).ToList();
            var scenes = EditorBuildSettings.scenes.Where(s => s.enabled).Select(s => s.path).ToArray();

            // Every check runs before the first setting is changed, so a build refused by a check leaves the project as it was.
            var problems = new List<string>();
            problems.AddRange(CheckEditor(arguments.Profile));
            var outputDirectory = ResolveOutputDirectory(arguments.OutputDirectory, repositoryRoot, projectRoot, problems);
            problems.AddRange(BuiltinModuleCheck.FindProblems(Path.Combine(projectRoot, "Packages", "manifest.json")));
            problems.AddRange(AssetLicenseAudit.FindProblems(
                assetPaths, Path.Combine(repositoryRoot, "docs", "asset-licenses.json"), Path.GetFileName(projectRoot)));
            problems.AddRange(TmpEssentialsAudit.FindProblems(assetPaths));
            problems.AddRange(MissingLicenseFiles(repositoryRoot));
            problems.AddRange(PlayerSettingsEnforcer.FindProblems());
            problems.AddRange(QualitySettingsEnforcer.FindProblems());
            problems.AddRange(GraphicsSettingsEnforcer.FindProblems());
            if (scenes.Length == 0)
            {
                problems.Add("No scene is enabled in File > Build Settings.");
            }

            if (problems.Count > 0)
            {
                return Fail(problems);
            }

            PlayerSettingsEnforcer.Apply(arguments.Profile);
            QualitySettingsEnforcer.Apply();
            GraphicsSettingsEnforcer.Apply();
            PlayerSettings.bundleVersion = arguments.Version;

            Directory.CreateDirectory(outputDirectory);
            if (!BuildPlayer(arguments, scenes, Path.Combine(outputDirectory, PlayerFileName)))
            {
                return false;
            }

            CopyLicenses(repositoryRoot, outputDirectory);
            Debug.Log(LogPrefix + "Player and licenses written to " + outputDirectory);
            return true;
        }

        private static IEnumerable<string> CheckEditor(BuildProfile profile)
        {
            var required = profile == BuildProfile.U6000 ? U6000Editor : U2022Editor;
            if (required == null)
            {
                yield return "Profile " + profile + " has no Unity patch pinned in BuildScript yet, so it cannot build.";
            }
            else if (Application.unityVersion != required)
            {
                yield return "Profile " + profile + " builds with Unity " + required + " only, but this editor is " +
                    Application.unityVersion + ".";
            }

            if (!BuildPipeline.IsBuildTargetSupported(BuildTargetGroup.Standalone, BuildTarget.StandaloneWindows64))
            {
                yield return "Windows Build Support (Mono) is not installed for this editor.";
            }
        }

        /// <summary>
        /// Resolves <paramref name="requested"/> from the repository root when it is relative, and reports a folder Unity
        /// would import. The editor's working folder is the project, where a relative <c>out/…</c> would leave the player
        /// untracked next to the sources instead of in the git-ignored out folder.
        /// </summary>
        private static string ResolveOutputDirectory(string requested, string repositoryRoot, string projectRoot, List<string> problems)
        {
            var resolved = Path.GetFullPath(Path.Combine(repositoryRoot, requested));
            foreach (var imported in new[] { "Assets", "Packages" })
            {
                if (IsSameOrInside(resolved, Path.Combine(projectRoot, imported)))
                {
                    problems.Add("-opOut " + requested + " is inside the project's " + imported +
                        " folder, where Unity would import the player.");
                }
            }

            return resolved;
        }

        private static bool IsSameOrInside(string path, string folder)
        {
            // Case-insensitive, as the file systems of macOS and Windows are by default; refusing too much is harmless here.
            return WithTrailingSeparator(path).StartsWith(WithTrailingSeparator(folder), StringComparison.OrdinalIgnoreCase);
        }

        private static string WithTrailingSeparator(string path)
        {
            return Path.GetFullPath(path).TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar) + Path.DirectorySeparatorChar;
        }

        private static bool BuildPlayer(BuildArguments arguments, string[] scenes, string playerPath)
        {
            var standalone = NamedBuildTarget.Standalone;
            var originalDefines = PlayerSettings.GetScriptingDefineSymbols(standalone);
            try
            {
                PlayerSettings.SetScriptingDefineSymbols(standalone, DefinesFor(originalDefines, arguments.Development));
                AssetDatabase.SaveAssets();

                var report = BuildPipeline.BuildPlayer(new BuildPlayerOptions
                {
                    scenes = scenes,
                    locationPathName = playerPath,
                    target = BuildTarget.StandaloneWindows64,
                    targetGroup = BuildTargetGroup.Standalone,
                    options = arguments.Development ? BuildOptions.Development : BuildOptions.None,
                });

                if (report.summary.result != BuildResult.Succeeded)
                {
                    return Fail(new[] { "BuildPlayer ended as " + report.summary.result + " with " + report.summary.totalErrors + " error(s)." });
                }

                return true;
            }
            finally
            {
                // The define switches on test-only assemblies; restoring it keeps a development build from leaving them
                // compiled into the editor afterwards.
                PlayerSettings.SetScriptingDefineSymbols(standalone, originalDefines);
                AssetDatabase.SaveAssets();
            }
        }

        private static string DefinesFor(string defines, bool development)
        {
            var symbols = defines.Split(new[] { ';' }, StringSplitOptions.RemoveEmptyEntries)
                .Select(s => s.Trim())
                .Where(s => s.Length > 0 && s != TestSupportDefine)
                .ToList();
            if (development)
            {
                symbols.Add(TestSupportDefine);
            }

            return string.Join(";", symbols);
        }

        private static IEnumerable<string> MissingLicenseFiles(string repositoryRoot)
        {
            return new[] { "LICENSE", "THIRD_PARTY_NOTICES.md" }
                .Where(name => !File.Exists(Path.Combine(repositoryRoot, name)))
                .Select(name => name + " is missing from the repository root, so the Licenses folder cannot be made.");
        }

        private static void CopyLicenses(string repositoryRoot, string outputDirectory)
        {
            var licenses = Directory.CreateDirectory(Path.Combine(outputDirectory, "Licenses")).FullName;
            File.Copy(Path.Combine(repositoryRoot, "LICENSE"), Path.Combine(licenses, "LICENSE.txt"), true);
            File.Copy(Path.Combine(repositoryRoot, "THIRD_PARTY_NOTICES.md"), Path.Combine(licenses, "THIRD_PARTY_NOTICES.md"), true);
        }

        private static bool Fail(IEnumerable<string> problems)
        {
            foreach (var problem in problems)
            {
                Debug.LogError(LogPrefix + problem);
            }

            return false;
        }
    }
}
