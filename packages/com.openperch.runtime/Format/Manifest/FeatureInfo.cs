using System;

namespace OpenPerch.Format
{
    /// <summary>What the avatar uses. Shown to the user and used for defaults, never for limits or accept decisions.</summary>
    [Serializable]
    public sealed class FeatureInfo
    {
        public bool fx;
        public int menuPages;
        public int physBones;
        public int constraints;
        public int contacts;
        public bool eyeLook;
        public bool blink;
    }
}
