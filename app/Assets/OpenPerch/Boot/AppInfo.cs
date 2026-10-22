using UnityEngine;

namespace OpenPerch.App.Boot
{
    /// <summary>Facts about the running build of the app.</summary>
    public static class AppInfo
    {
        /// <summary>
        /// The app's SemVer. The build script writes it to <c>PlayerSettings.bundleVersion</c>, which the player reports as
        /// <see cref="Application.version"/>.
        /// </summary>
        public static string Version => Application.version;
    }
}
