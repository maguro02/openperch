using System;

namespace OpenPerch.App.Core
{
    /// <summary>An expression-menu control the user picked to drive while a pomodoro mode is active.</summary>
    [Serializable]
    public sealed class PomodoroParamBinding
    {
        public PomodoroMode mode;
        public string menuPath = "";
        public string controlName = "";
        public string parameter = "";
        public float value;
    }
}
