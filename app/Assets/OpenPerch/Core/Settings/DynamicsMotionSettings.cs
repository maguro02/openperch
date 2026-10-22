using System;

namespace OpenPerch.App.Core
{
    [Serializable]
    public sealed class DynamicsMotionSettings
    {
        public float inertiaScale = 1f;
        public float worldMoveSpeedLimit = 3f;
        public float worldRotSpeedLimitDeg = 720f;
        public float particleSpeedLimit = 10f;
        public float teleportDistance = 0.5f;
        public float teleportAngleDeg = 60f;
        public float gapTeleportSeconds = 0.25f;
        public float rateHz = 60f;
        public float clothInertiaScale = 0.5f;
        public int warmupSteps = 10;
    }
}
