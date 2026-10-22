using System;

namespace OpenPerch.App.Core
{
    [Serializable]
    public sealed class BehaviorConfig
    {
        public float idleMinSec = 4f;
        public float idleMaxSec = 12f;
        public float wWalk = 1f;
        public float wSit = 0.6f;
        public float wIdle = 1f;
        public float wVariant = 0.3f;
        public float wJump = 0.4f;
        public float walkMinRatio = 0.3f;
        public float walkMaxRatio = 1.6f;
        public float jumpMaxRiseRatio = 1.5f;
        public float jumpMaxRunRatio = 2.5f;
        public float focusWalkScale = 0.2f;
        public float breakScale = 1.5f;
        public float occludedFallSec = 0.5f;
    }
}
