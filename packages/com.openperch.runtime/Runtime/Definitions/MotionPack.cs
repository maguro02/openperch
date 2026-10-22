using System;
using UnityEngine;

namespace OpenPerch.Runtime
{
    /// <summary>Root asset of a motion bundle.</summary>
    public sealed class MotionPack : ScriptableObject
    {
        public int schemaVersion = 1;
        public string exportId;
        public string displayName;
        public MotionEntry[] motions = Array.Empty<MotionEntry>();
    }
}
