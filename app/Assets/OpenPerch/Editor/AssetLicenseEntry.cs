using System;

namespace OpenPerch.App.Editor
{
    /// <summary>One element of the array in docs/asset-licenses.json.</summary>
    [Serializable]
    public sealed class AssetLicenseEntry
    {
        /// <summary>
        /// Unity project path of the declared files, such as <c>Assets/…</c> or <c>Packages/&lt;package&gt;/…</c>.
        /// <c>*</c> matches within one folder name, <c>**</c> across folders and <c>?</c> one character.
        /// </summary>
        public string path = "";

        public string license = "";

        /// <summary>Where the files come from.</summary>
        public string source = "";
    }
}
