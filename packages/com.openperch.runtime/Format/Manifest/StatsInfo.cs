using System;

namespace OpenPerch.Format
{
    /// <summary>Size estimates recorded at export. Shown to the user only; never used to size allocations.</summary>
    [Serializable]
    public sealed class StatsInfo
    {
        public int triangles;
        public int materials;
        public int skinnedMeshes;
        public int physBoneTransforms;
        public long estimatedTextureBytes;
        public long estimatedMeshBytes;
    }
}
