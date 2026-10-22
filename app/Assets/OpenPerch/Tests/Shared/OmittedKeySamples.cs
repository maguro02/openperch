using OpenPerch.App.Core;

namespace OpenPerch.App.Tests.Shared
{
    /// <summary>
    /// JSON texts that leave keys out of list elements, with the objects they must read as. The dotnet and the Unity tests
    /// feed the same texts to their codecs, so a codec that creates list elements without running their field initializers
    /// shows up as a difference.
    /// </summary>
    public static class OmittedKeySamples
    {
        private const string ExportId = "3f2c9a0d6b7e4c1fa2b3c4d5e6f70812";

        /// <summary>
        /// Settings with two avatars: the first sets only <c>exportId</c> and <c>displayHeightDip</c>, the second sets nothing.
        /// </summary>
        public const string SettingsJson =
            "{\"avatars\": [{\"exportId\": \"" + ExportId + "\", \"displayHeightDip\": 420}, {}]}";

        /// <summary>A library index whose only entry sets just <c>exportId</c>.</summary>
        public const string IndexJson = "{\"entries\": [{\"exportId\": \"" + ExportId + "\"}]}";

        /// <summary>Returns what <see cref="SettingsJson"/> must read as.</summary>
        public static AppSettings ExpectedSettings()
        {
            var settings = new AppSettings();
            settings.avatars.Add(new AvatarPlacement { exportId = ExportId, displayHeightDip = 420 });
            settings.avatars.Add(new AvatarPlacement());
            return settings;
        }

        /// <summary>Returns what <see cref="IndexJson"/> must read as.</summary>
        public static LibraryIndex ExpectedIndex()
        {
            var index = new LibraryIndex();
            index.entries.Add(new LibraryEntry { exportId = ExportId });
            return index;
        }
    }
}
