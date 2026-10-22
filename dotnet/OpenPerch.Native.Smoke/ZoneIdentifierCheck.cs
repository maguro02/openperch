using System.IO;
using System.Text;
using OpenPerch.App.Platform.Windows.Native;

namespace OpenPerch.Native.Smoke
{
    /// <summary>Writes a Mark-of-the-Web the way a browser does and reads it back through CreateFileW.</summary>
    internal static class ZoneIdentifierCheck
    {
        private const string Mark = "[ZoneTransfer]\r\nZoneId=3\r\nHostUrl=https://example.invalid/avatar.opk\r\n";

        public static void Run(SmokeReport report)
        {
            string marked = Path.GetTempFileName();
            string unmarked = Path.GetTempFileName();
            try
            {
                File.WriteAllText(marked + ZoneIdentifier.StreamSuffix, Mark, Encoding.ASCII);

                int error = ZoneIdentifier.ReadStream(marked, out byte[] content);
                report.Require(
                    error == Kernel32.ERROR_SUCCESS && content.Length == Mark.Length,
                    $"CreateFileW and ReadFile read the Zone.Identifier stream (error {error}, {content.Length} bytes)");
                report.Require(
                    ZoneIdentifier.TryParseZoneId(content, out int zoneId) && zoneId == ZoneIdentifier.UrlZoneInternet,
                    $"the stream parses as ZoneId 3 (got {zoneId})");

                error = ZoneIdentifier.ReadStream(unmarked, out content);
                report.Require(
                    error == Kernel32.ERROR_FILE_NOT_FOUND && content.Length == 0 && ZoneIdentifier.MeansUnmarked(error),
                    $"a file without the stream reports ERROR_FILE_NOT_FOUND, which counts as unmarked (error {error})");
            }
            finally
            {
                File.Delete(marked);
                File.Delete(unmarked);
            }
        }
    }
}
