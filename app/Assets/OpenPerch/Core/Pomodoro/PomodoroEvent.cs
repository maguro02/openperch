using System;

namespace OpenPerch.App.Core
{
    /// <summary>A change of the <see cref="PomodoroTimer"/>, handed out by <see cref="PomodoroTimer.Update"/>.</summary>
    public readonly struct PomodoroEvent
    {
        public readonly PomodoroEventKind Kind;

        /// <summary>
        /// The phase that started, ended, was paused or resumed; for <see cref="PomodoroEventKind.Stopped"/>, the phase that was stopped.
        /// </summary>
        public readonly PomodoroPhase Phase;

        /// <summary>
        /// When the change took effect. A phase that ran out reports the moment it ran out, which can be well before
        /// the <see cref="PomodoroTimer.Update"/> that confirmed it.
        /// </summary>
        public readonly DateTime AtUtc;

        /// <summary>
        /// True on the <see cref="PomodoroEventKind.PhaseEnded"/> and <see cref="PomodoroEventKind.PhaseStarted"/> of a transition
        /// confirmed more than a second after its deadline, typically after system sleep.
        /// </summary>
        public readonly bool Late;

        public PomodoroEvent(PomodoroEventKind kind, PomodoroPhase phase, DateTime atUtc, bool late)
        {
            Kind = kind;
            Phase = phase;
            AtUtc = atUtc;
            Late = late;
        }
    }
}
