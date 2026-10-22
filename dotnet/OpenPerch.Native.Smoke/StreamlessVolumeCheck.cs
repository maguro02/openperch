using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using OpenPerch.App.Platform.Windows.Native;

namespace OpenPerch.Native.Smoke
{
    /// <summary>
    /// Formats small FAT32 and exFAT virtual disks, which cannot hold named streams, and reads the Mark-of-the-Web of a
    /// file on each through <see cref="ZoneIdentifier.ReadStream"/>.
    /// </summary>
    internal static class StreamlessVolumeCheck
    {
        // FAT32 needs at least 65,525 clusters, which a much smaller disk at the default cluster size falls short of.
        private const int DiskMegabytes = 96;
        private const int DiskpartTimeoutMilliseconds = 120_000;

        private static readonly string[] FileSystems = { "FAT32", "exFAT" };

        public static void Run(SmokeReport report, bool elevated)
        {
            if (!elevated)
            {
                report.Info("skipped: diskpart attaches virtual disks only from an elevated process");
                return;
            }

            foreach (string fileSystem in FileSystems)
            {
                ReadOnVolume(report, fileSystem);
            }
        }

        private static void ReadOnVolume(SmokeReport report, string fileSystem)
        {
            string directory = Path.Combine(Path.GetTempPath(), "openperch-smoke-" + Guid.NewGuid().ToString("N"));
            string disk = Path.Combine(directory, fileSystem + ".vhd");
            char letter = FreeDriveLetter();

            // diskpart does not document its script encoding, so the script stays ASCII.
            if (letter == '\0' || disk.Any(c => c > 0x7F))
            {
                report.Info($"skipped {fileSystem}: no free drive letter, or a non-ASCII temporary path ({disk})");
                return;
            }

            Directory.CreateDirectory(directory);
            try
            {
                string script =
                    $"create vdisk file=\"{disk}\" maximum={DiskMegabytes} type=expandable\r\n" +
                    $"select vdisk file=\"{disk}\"\r\n" +
                    "attach vdisk\r\n" +
                    "create partition primary\r\n" +
                    $"format fs={fileSystem} quick label=SMOKE\r\n" +
                    $"assign letter={letter}\r\n";
                bool mounted = Diskpart(directory, "create", script, out string output);
                report.Require(
                    mounted,
                    mounted ? $"diskpart formats a {fileSystem} virtual disk and mounts it at {letter}:" : $"diskpart formats a {fileSystem} virtual disk ({output})");
                if (!mounted)
                {
                    return;
                }

                string root = letter + @":\";
                string format = new DriveInfo(root).DriveFormat;
                report.Require(string.Equals(format, fileSystem, StringComparison.OrdinalIgnoreCase), $"the volume at {root} reports {format}");

                string file = Path.Combine(root, "avatar.opk");
                File.WriteAllBytes(file, new byte[] { 0x4F, 0x50, 0x4B });
                int error = ZoneIdentifier.ReadStream(file, out byte[] content);
                report.Require(
                    error == Kernel32.ERROR_INVALID_NAME && content.Length == 0 && ZoneIdentifier.MeansUnmarked(error),
                    $"a file on {fileSystem} reports ERROR_INVALID_NAME, which counts as unmarked (error {error})");
            }
            finally
            {
                if (File.Exists(disk) && !Diskpart(directory, "detach", $"select vdisk file=\"{disk}\"\r\ndetach vdisk\r\n", out string output))
                {
                    report.Info($"diskpart could not detach the {fileSystem} virtual disk ({output})");
                }
                try
                {
                    Directory.Delete(directory, true);
                }
                catch (IOException e)
                {
                    report.Info($"could not delete {directory}: {e.Message}");
                }
            }
        }

        private static char FreeDriveLetter()
        {
            var used = new HashSet<char>(DriveInfo.GetDrives().Select(drive => char.ToUpperInvariant(drive.Name[0])));
            for (char letter = 'Z'; letter >= 'D'; letter--)
            {
                if (!used.Contains(letter))
                {
                    return letter;
                }
            }
            return '\0';
        }

        private static bool Diskpart(string directory, string name, string script, out string output)
        {
            string scriptPath = Path.Combine(directory, name + ".txt");
            File.WriteAllText(scriptPath, script, Encoding.ASCII);

            var start = new ProcessStartInfo(Path.Combine(Environment.SystemDirectory, "diskpart.exe"), "/s \"" + scriptPath + "\"")
            {
                UseShellExecute = false,
                CreateNoWindow = true,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
            };
            using (Process process = Process.Start(start))
            {
                Task<string> stdout = process.StandardOutput.ReadToEndAsync();
                Task<string> stderr = process.StandardError.ReadToEndAsync();
                if (!process.WaitForExit(DiskpartTimeoutMilliseconds))
                {
                    process.Kill();
                    output = $"no exit within {DiskpartTimeoutMilliseconds / 1000} s";
                    return false;
                }

                output = $"exit {process.ExitCode}: {(stdout.Result + stderr.Result).Trim()}";
                return process.ExitCode == 0;
            }
        }
    }
}
