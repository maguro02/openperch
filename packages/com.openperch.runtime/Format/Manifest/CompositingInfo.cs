using System;

namespace OpenPerch.Format
{
    /// <summary>The exporter's recommendation for the compositing mode, used as the default.</summary>
    [Serializable]
    public sealed class CompositingInfo
    {
        public bool requiresPrecise;

        /// <summary>Why precise compositing is needed, as "reason:count", for example "additive:1".</summary>
        public string[] reasons = Array.Empty<string>();
    }
}
