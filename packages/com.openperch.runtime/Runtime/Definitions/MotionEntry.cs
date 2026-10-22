using System;
using UnityEngine;

namespace OpenPerch.Runtime
{
    /// <summary>One clip of a <see cref="MotionPack"/> and the role the app plays it in.</summary>
    [Serializable]
    public sealed class MotionEntry
    {
        public string id;
        public string sourceName;
        public MotionRole role;
        public AnimationClip clip;
        public float length;
        public bool loop;
        public bool hasRootMotion;
        public Vector3 averageVelocity;
        public float weight = 1f;

        /// <summary>Speed in m/s at which the app moves a <see cref="MotionRole.Walk"/> clip that has no root motion.</summary>
        public float walkSpeed = 0.6f;

        /// <summary>For <see cref="MotionRole.SitOnEdge"/>: height in metres from the edge sat on to the root origin.</summary>
        public float seatHeight;
    }
}
