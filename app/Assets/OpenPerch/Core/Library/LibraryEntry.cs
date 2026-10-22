using System;

namespace OpenPerch.App.Core
{
    [Serializable]
    public sealed class LibraryEntry
    {
        public string exportId = "";
        public string lineageId = "";
        public string kind = "";
        public string displayName = "";
        public string sha256 = "";
        public string unityVersion = "";
        public string exporterVersion = "";
        public string importedAtUtc = "";
        public uint crc;
        public bool crcVerified;
        public long sizeBytes;
        public long estimatedMemoryBytes;

        /// <summary>One of "ok", "needsReimport" or "incompatible".</summary>
        public string status = "ok";

        public string[] warningCodes = Array.Empty<string>();
    }
}
