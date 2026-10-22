using UnityEngine;

namespace OpenPerch.Runtime
{
    /// <summary>
    /// Constraint converted from VRChat's constraints, solved by the app. A GameObject may carry several of them.
    /// </summary>
    public sealed class PerchConstraint : MonoBehaviour, ISerializationCallbackReceiver
    {
        public int schemaVersion = 1;
        public PerchConstraintKind kind;
        public bool isActive = true;
        public float globalWeight = 1f;

        /// <summary>
        /// Transform the constraint drives. Null means the Transform this component is on, as when VRChat's Target Transform is left empty.
        /// </summary>
        public Transform targetTransform;

        public bool solveInLocalSpace;
        public bool freezeToWorld;
        public bool locked;
        public bool affectsPositionX = true;
        public bool affectsPositionY = true;
        public bool affectsPositionZ = true;
        public bool affectsRotationX = true;
        public bool affectsRotationY = true;
        public bool affectsRotationZ = true;
        public bool affectsScaleX = true;
        public bool affectsScaleY = true;
        public bool affectsScaleZ = true;
        public Vector3 positionAtRest;
        public Vector3 positionOffset;
        public Vector3 rotationAtRest;
        public Vector3 rotationOffset;
        public Vector3 scaleAtRest = Vector3.one;
        public Vector3 scaleOffset = Vector3.one;
        public Vector3 aimAxis = Vector3.forward;
        public Vector3 upAxis = Vector3.up;
        public Vector3 worldUpVector = Vector3.up;

        /// <summary>
        /// Same values as Unity's <c>AimConstraint.WorldUpType</c>: 0 SceneUp, 1 ObjectUp, 2 ObjectRotationUp, 3 Vector, 4 None.
        /// ObjectUp and ObjectRotationUp without a <see cref="worldUpTransform"/> are solved as SceneUp.
        /// </summary>
        public int worldUpType;

        public Transform worldUpTransform;
        public float roll;
        public bool useUpTransform;

        /// <summary>Number of sources in use, 0 to 16, filled from <see cref="source0"/> upwards.</summary>
        public int sourceCount;

        // Animation curves cannot bind to array or list elements, so each source is a field of its own and FX clips
        // can address it as "source3.weight".
        public PerchConstraintSource source0;
        public PerchConstraintSource source1;
        public PerchConstraintSource source2;
        public PerchConstraintSource source3;
        public PerchConstraintSource source4;
        public PerchConstraintSource source5;
        public PerchConstraintSource source6;
        public PerchConstraintSource source7;
        public PerchConstraintSource source8;
        public PerchConstraintSource source9;
        public PerchConstraintSource source10;
        public PerchConstraintSource source11;
        public PerchConstraintSource source12;
        public PerchConstraintSource source13;
        public PerchConstraintSource source14;
        public PerchConstraintSource source15;

        public void OnBeforeSerialize()
        {
        }

        /// <summary>Delegates to <see cref="RuntimeMigration"/>; see there for what this may do.</summary>
        public void OnAfterDeserialize() => RuntimeMigration.Migrate(this);
    }
}
