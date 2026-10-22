using UnityEngine;

namespace OpenPerch.Runtime
{
    /// <summary>
    /// Collider for <see cref="PerchPhysBone"/> chains, converted from VRChat's PhysBone Collider.
    /// A GameObject may carry several of them.
    /// </summary>
    public sealed class PerchPhysBoneCollider : MonoBehaviour, ISerializationCallbackReceiver
    {
        public int schemaVersion = 1;

        /// <summary>Transform the collider follows. Null means the Transform this component is on.</summary>
        public Transform rootTransform;

        public PbColliderShape shapeType;
        public float radius;
        public float height;
        public Vector3 position;
        public Quaternion rotation = Quaternion.identity;
        public bool insideBounds;
        public bool bonesAsSphere;

        /// <summary>
        /// True for colliders marked global in the SDK and for those converted from the avatar descriptor's hand and finger colliders.
        /// </summary>
        public bool isGlobal;

        public void OnBeforeSerialize()
        {
        }

        /// <summary>Delegates to <see cref="RuntimeMigration"/>; see there for what this may do.</summary>
        public void OnAfterDeserialize() => RuntimeMigration.Migrate(this);
    }
}
