using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;

namespace OpenPerch.App.Editor
{
    /// <summary>Options of <see cref="BuildScript.BuildWindowsCli"/>, read from the editor's command line.</summary>
    public sealed class BuildArguments
    {
        // The pattern published with SemVer 2.0.0, with [0-9] instead of \d so that non-ASCII digits are rejected.
        private static readonly Regex SemVerPattern = new Regex(
            @"^(0|[1-9][0-9]*)\.(0|[1-9][0-9]*)\.(0|[1-9][0-9]*)" +
            @"(?:-((?:0|[1-9][0-9]*|[0-9]*[a-zA-Z-][0-9a-zA-Z-]*)(?:\.(?:0|[1-9][0-9]*|[0-9]*[a-zA-Z-][0-9a-zA-Z-]*))*))?" +
            @"(?:\+([0-9a-zA-Z-]+(?:\.[0-9a-zA-Z-]+)*))?$",
            RegexOptions.CultureInvariant);

        private BuildArguments(string outputDirectory, string version, bool development, BuildProfile profile)
        {
            OutputDirectory = outputDirectory;
            Version = version;
            Development = development;
            Profile = profile;
        }

        /// <summary>
        /// <c>-opOut</c>: directory the player is written to, as given. The build resolves a relative path from the
        /// repository root, so <c>out/…</c> lands in the git-ignored out folder, and refuses a folder inside the project's
        /// Assets or Packages.
        /// </summary>
        public string OutputDirectory { get; }

        /// <summary><c>-opVersion</c>: the app's SemVer.</summary>
        public string Version { get; }

        /// <summary><c>-opDev</c>: development player with the test-support assemblies compiled in.</summary>
        public bool Development { get; }

        /// <summary><c>-opProfile</c>: <see cref="BuildProfile.U2022"/> unless given.</summary>
        public BuildProfile Profile { get; }

        /// <summary>
        /// Reads the <c>-op*</c> options from <paramref name="commandLine"/> and ignores Unity's own arguments.
        /// </summary>
        /// <returns>False with a message in <paramref name="error"/> when an option is missing, unknown, repeated or invalid.</returns>
        public static bool TryParse(IReadOnlyList<string> commandLine, out BuildArguments arguments, out string error)
        {
            arguments = null;
            error = null;
            string outputDirectory = null;
            string version = null;
            string profileName = null;
            var development = false;

            for (var i = 0; i < commandLine.Count; i++)
            {
                var option = commandLine[i];
                switch (option)
                {
                    case "-opOut":
                        if (!TryTakeValue(commandLine, ref i, ref outputDirectory, out error))
                        {
                            return false;
                        }

                        break;
                    case "-opVersion":
                        if (!TryTakeValue(commandLine, ref i, ref version, out error))
                        {
                            return false;
                        }

                        break;
                    case "-opProfile":
                        if (!TryTakeValue(commandLine, ref i, ref profileName, out error))
                        {
                            return false;
                        }

                        break;
                    case "-opDev":
                        development = true;
                        break;
                    default:
                        // A misspelt option must fail instead of silently building with defaults, but Unity's own arguments
                        // pass through untouched; ours alone are "-op" followed by a capital letter.
                        if (option.Length > 3 && option.StartsWith("-op", StringComparison.Ordinal) && char.IsUpper(option[3]))
                        {
                            error = "Unknown option " + option + ".";
                            return false;
                        }

                        break;
                }
            }

            if (string.IsNullOrWhiteSpace(outputDirectory))
            {
                error = "-opOut <dir> is required.";
                return false;
            }

            if (version == null || !SemVerPattern.IsMatch(version))
            {
                error = "-opVersion needs a SemVer such as 1.2.3 or 1.2.3-beta.1, got '" + version + "'.";
                return false;
            }

            var profile = BuildProfile.U2022;
            if (profileName != null && !TryParseProfile(profileName, out profile))
            {
                error = "-opProfile must be U2022 or U6000, got '" + profileName + "'.";
                return false;
            }

            arguments = new BuildArguments(outputDirectory, version, development, profile);
            return true;
        }

        private static bool TryTakeValue(IReadOnlyList<string> commandLine, ref int index, ref string value, out string error)
        {
            var option = commandLine[index];
            if (value != null)
            {
                error = option + " is given more than once.";
                return false;
            }

            if (index + 1 >= commandLine.Count || commandLine[index + 1].StartsWith("-", StringComparison.Ordinal))
            {
                error = option + " needs a value.";
                return false;
            }

            index++;
            value = commandLine[index];
            error = null;
            return true;
        }

        private static bool TryParseProfile(string name, out BuildProfile profile)
        {
            switch (name)
            {
                case "U2022":
                    profile = BuildProfile.U2022;
                    return true;
                case "U6000":
                    profile = BuildProfile.U6000;
                    return true;
                default:
                    profile = BuildProfile.U2022;
                    return false;
            }
        }
    }
}
