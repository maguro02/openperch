using System;

namespace OpenPerch.App.Core
{
    [Serializable]
    public sealed class NotificationSettings
    {
        /// <summary>0: none, 1: bundled sound, 2: user file, 3: OS system sound.</summary>
        public int soundKind = 1;

        public string soundFile = "";
        public bool balloon;
        public float volume = 0.7f;
    }
}
