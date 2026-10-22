using System;

namespace OpenPerch.App.Editor
{
    /// <summary>One element of the array in docs/asset-licenses.json.</summary>
    [Serializable]
    public sealed class AssetLicenseEntry
    {
        /// <summary>
        /// The declared files. Files of the app project are written as paths from the repository root, such as
        /// <c>app/Assets/StreamingAssets/i18n/ja.json</c>; their Unity project path (<c>Assets/…</c>) matches too.
        /// Files of packages are written as Unity project paths, <c>Packages/&lt;package&gt;/…</c>, because packages the
        /// Package Manager caches have no path in the repository.
        /// <c>*</c> matches within one folder name, <c>**</c> across folders and <c>?</c> one character.
        /// </summary>
        public string path = "";

        public string license = "";

        /// <summary>Where the files come from.</summary>
        public string source = "";
    }
}
