using System.Collections.Generic;
using UnityEngine;

namespace OpenPerch.Runtime
{
    /// <summary>
    /// Contact sender or receiver converted from VRChat's Contact components. The app keeps the data but does not simulate contacts.
    /// </summary>
    public sealed class PerchContactData : MonoBehaviour, ISerializationCallbackReceiver
    {
        public int schemaVersion = 1;
        public bool isReceiver;
        public PerchContactShape shape;

        /// <summary>Transform the contact follows. Null means the Transform this component is on.</summary>
        public Transform rootTransform;

        public float radius;
        public float height;
        public Vector3 position;
        public Vector3 size;
        public Quaternion rotation = Quaternion.identity;
        public List<string> collisionTags = new();
        public int receiverType;
        public string parameter = "";
        public bool allowSelf;
        public bool allowOthers;
        public bool localOnly;

        public void OnBeforeSerialize()
        {
        }

        /// <summary>Delegates to <see cref="RuntimeMigration"/>; see there for what this may do.</summary>
        public void OnAfterDeserialize() => RuntimeMigration.Migrate(this);
    }
}
