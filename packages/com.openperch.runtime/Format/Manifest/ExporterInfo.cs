using System;

namespace OpenPerch.Format
{
    /// <summary>Describes the exporter and the Unity project a package came from.</summary>
    [Serializable]
    public sealed class ExporterInfo
    {
        /// <summary>SemVer of the exporter package.</summary>
        public string version = "";

        /// <summary>The editor version, for example "2022.3.22f1".</summary>
        public string unityVersion = "";

        /// <summary>The <c>BuildTarget</c> name the bundle was built for, for example "StandaloneWindows64".</summary>
        public string buildTarget = "";

        /// <summary>"Linear" or "Gamma".</summary>
        public string colorSpace = "";

        /// <summary>Empty when the export did not go through the VRChat SDK.</summary>
        public string vrcSdkVersion = "";

        /// <summary>Other packages that took part in the export, as "package.id@version".</summary>
        public string[] tools = Array.Empty<string>();
    }
}
