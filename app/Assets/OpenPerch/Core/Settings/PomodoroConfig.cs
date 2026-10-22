using System;

namespace OpenPerch.App.Core
{
    [Serializable]
    public sealed class PomodoroConfig
    {
        public int workMin = 25;
        public int shortBreakMin = 5;
        public int longBreakMin = 15;
        public int longBreakEvery = 4;
        public bool autoStartBreak = true;
        public bool autoStartWork = false;
    }
}
