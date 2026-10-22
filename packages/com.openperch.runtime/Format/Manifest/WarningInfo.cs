using System;

namespace OpenPerch.Format
{
    /// <summary>A warning the exporter raised, kept so the app can show it again.</summary>
    [Serializable]
    public sealed class WarningInfo
    {
        /// <summary>A code from <see cref="MessageCatalog"/>.</summary>
        public string code = "";

        public string detail = "";
    }
}
