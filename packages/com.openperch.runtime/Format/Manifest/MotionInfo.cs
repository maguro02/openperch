using System;

namespace OpenPerch.Format
{
    /// <summary>One clip of a motion pack, listed so the app can show which roles a pack covers before loading it.</summary>
    [Serializable]
    public sealed class MotionInfo
    {
        public string id = "";

        /// <summary>The <c>MotionRole</c> name, for example "Walk".</summary>
        public string role = "";

        /// <summary>The clip or FBX take the motion was exported from.</summary>
        public string sourceName = "";

        /// <summary>Clip length in seconds.</summary>
        public float length;

        public bool loop;
        public bool hasRootMotion;
    }
}
