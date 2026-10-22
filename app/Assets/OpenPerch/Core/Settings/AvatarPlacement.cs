using System;
using System.Collections.Generic;

namespace OpenPerch.App.Core
{
    [Serializable]
    public sealed class AvatarPlacement
    {
        public string instanceId = "";
        public string exportId = "";
        public string lineageId = "";
        public string motionPackId = "";
        public int displayHeightDip = 360;
        public float xNorm;
        public bool hidden;

        /// <summary>-1 keeps the compositing mode recommended by the package manifest.</summary>
        public int compositingOverride = -1;

        public List<SavedParam> savedParams = new();
        public List<PomodoroParamBinding> pomodoroBindings = new();
    }
}
