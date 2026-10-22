using System;
using UnityEngine;

namespace OpenPerch.Runtime
{
    /// <summary>One control of an expressions menu page.</summary>
    [Serializable]
    public sealed class MenuControlDef
    {
        public string name;
        public Texture2D icon;
        public MenuControlType type;
        public string parameter = "";
        public float value;
        public string[] subParameters = Array.Empty<string>();

        /// <summary>Index into <see cref="AvatarDefinition.menuPages"/> opened by a SubMenu control, or -1.</summary>
        public int subMenuIndex = -1;

        public string[] labelNames = Array.Empty<string>();
        public Texture2D[] labelIcons = Array.Empty<Texture2D>();
    }
}
