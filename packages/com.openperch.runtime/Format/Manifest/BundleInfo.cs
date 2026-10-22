using System;

namespace OpenPerch.Format
{
    /// <summary>Identifies and checks the AssetBundle section of a package.</summary>
    [Serializable]
    public sealed class BundleInfo
    {
        /// <summary>The AssetBundle name, "op-" followed by the exportId.</summary>
        public string name = "";

        /// <summary>
        /// The CRC Unity reports for the bundle. The first load passes it to <c>AssetBundle.LoadFromFileAsync</c> to check
        /// the bundle; later loads of the checked file pass 0.
        /// </summary>
        public uint crc;

        /// <summary>SHA-256 of the bundle section only, as 64 lowercase hex digits.</summary>
        public string sha256 = "";

        /// <summary>Length of the bundle section in bytes; equals the header's bundleLength.</summary>
        public long length;

        /// <summary>The asset to load first: "definition" for an avatar, "motionpack" for a motion pack.</summary>
        public string rootAsset = "";
    }
}
