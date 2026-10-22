using System;
using UnityEngine;

namespace OpenPerch.Runtime
{
    /// <summary>One source of a <see cref="PerchConstraint"/>.</summary>
    [Serializable]
    public struct PerchConstraintSource
    {
        public Transform source;
        public float weight;
        public Vector3 parentPositionOffset;
        public Vector3 parentRotationOffset;
    }
}
