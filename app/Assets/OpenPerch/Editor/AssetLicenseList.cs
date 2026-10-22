using System;

namespace OpenPerch.App.Editor
{
    /// <summary>Object wrapped around the array of docs/asset-licenses.json, because JsonUtility cannot read a top-level array.</summary>
    [Serializable]
    public sealed class AssetLicenseList
    {
        public AssetLicenseEntry[] entries = Array.Empty<AssetLicenseEntry>();
    }
}
