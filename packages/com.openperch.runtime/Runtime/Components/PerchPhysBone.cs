using System.Collections.Generic;
using UnityEngine;

namespace OpenPerch.Runtime
{
    /// <summary>
    /// PhysBone settings converted from VRChat's PhysBone component, read by the app's own solver.
    /// A GameObject may carry several of them.
    /// </summary>
    public sealed class PerchPhysBone : MonoBehaviour, ISerializationCallbackReceiver
    {
        public int schemaVersion = 1;
        public PbVersion version;
        public PbIntegration integrationType;

        /// <summary>
        /// First bone of the chain. Null means the Transform this component is on, as when VRChat's Root Transform is left empty.
        /// </summary>
        public Transform rootTransform;

        public List<Transform> ignoreTransforms = new();
        public bool ignoreOtherPhysBones = true;
        public Vector3 endpointPosition;
        public PbMultiChild multiChildType;
        public float pull;
        public AnimationCurve pullCurve;

        /// <summary>Spring with <see cref="PbIntegration.Simplified"/>, Momentum with <see cref="PbIntegration.Advanced"/>.</summary>
        public float spring;

        public AnimationCurve springCurve;
        public float stiffness;
        public AnimationCurve stiffnessCurve;
        public float gravity;
        public AnimationCurve gravityCurve;
        public float gravityFalloff;
        public AnimationCurve gravityFalloffCurve;
        public PbImmobileType immobileType;
        public float immobile;
        public AnimationCurve immobileCurve;
        public PbLimitType limitType;
        public float maxAngleX;
        public AnimationCurve maxAngleXCurve;
        public float maxAngleZ;
        public AnimationCurve maxAngleZCurve;
        public Vector3 limitRotation;
        public AnimationCurve limitRotationXCurve;
        public AnimationCurve limitRotationYCurve;
        public AnimationCurve limitRotationZCurve;
        public float radius;
        public AnimationCurve radiusCurve;
        public PbAllow allowCollision;
        public bool collisionAllowSelf = true;
        public bool collisionAllowOthers = true;
        public List<PerchPhysBoneCollider> colliders = new();
        public float stretchMotion;
        public AnimationCurve stretchMotionCurve;
        public float maxStretch;
        public AnimationCurve maxStretchCurve;
        public float maxSquish;
        public AnimationCurve maxSquishCurve;
        public PbAllow allowGrabbing;
        public bool grabAllowSelf = true;
        public bool grabAllowOthers = true;
        public PbAllow allowPosing;
        public bool poseAllowSelf = true;
        public bool poseAllowOthers = true;

        /// <summary>Carried over from the source avatar; the app does not grab bones, so it never reads this.</summary>
        public float grabMovement;

        /// <summary>Carried over from the source avatar; the app does not grab bones, so it never reads this.</summary>
        public bool snapToHand;

        public string parameter = "";
        public bool isAnimated;
        public bool resetWhenDisabled;

        public void OnBeforeSerialize()
        {
        }

        /// <summary>Delegates to <see cref="RuntimeMigration"/>; see there for what this may do.</summary>
        public void OnAfterDeserialize() => RuntimeMigration.Migrate(this);
    }
}
