using System;
using System.Collections.Generic;

namespace OpenPerch.App.Core
{
    [Serializable]
    public sealed class AppSettings
    {
        public int schemaVersion = 1;
        public string language = "";
        public string monitorDeviceName = "";
        public int targetFpsCap = 60;
        public List<AvatarPlacement> avatars = new();
        public PomodoroConfig pomodoro = new();
        public NotificationSettings notify = new();
        public BehaviorConfig behavior = new();
        public DynamicsMotionSettings dynamics = new();
        public bool autostart;

        /// <summary>-1 until the user answers the first-run question, then 0 (off) or 1 (on).</summary>
        public int checkUpdates = -1;

        public float ambientIntensity = 0.5f;
        public int msaa = 4;
        public bool halfTextures;
        public Vec2I widgetPosDip;
        public bool firstRunDone;
    }
}
