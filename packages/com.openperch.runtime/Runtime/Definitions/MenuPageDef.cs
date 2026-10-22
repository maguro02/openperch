using System;

namespace OpenPerch.Runtime
{
    /// <summary>One page of the avatar's expressions menu.</summary>
    [Serializable]
    public sealed class MenuPageDef
    {
        public string name;

        /// <summary>At most 8 controls, as in VRChat's expressions menu.</summary>
        public MenuControlDef[] controls;
    }
}
