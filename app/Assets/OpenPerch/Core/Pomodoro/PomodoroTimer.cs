using System;
using System.Collections.Generic;

namespace OpenPerch.App.Core
{
    /// <summary>
    /// Runs the work/break cycle. Commands take effect immediately; their events, and those of a phase running out,
    /// are handed out by <see cref="Update"/>, which the owner calls every frame.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Time spent in a phase is the larger of the wall-clock and the monotonic time since the phase started or resumed.
    /// A 25-minute phase has to end once 25 minutes have passed in real time, even when the machine slept through them,
    /// and on some hosts the monotonic counter stops during sleep while the wall clock does not. The monotonic time is
    /// the floor, so setting the clock back cannot stretch a phase. Setting the clock forward cannot be told apart from
    /// sleep and ends the phase early, as a late transition.
    /// </para>
    /// <para>
    /// Phase lengths are read from the config when a phase starts, so edits apply from the next phase. The auto-start
    /// flags and <see cref="PomodoroConfig.longBreakEvery"/> are read when a phase ends.
    /// </para>
    /// </remarks>
    public sealed class PomodoroTimer
    {
        private static readonly TimeSpan LateThreshold = TimeSpan.FromSeconds(1);

        private readonly PomodoroConfig _config;
        private readonly ITimeSource _time;
        private readonly List<PomodoroEvent> _pending = new List<PomodoroEvent>();

        private PomodoroPhase _awaitedPhase;
        private TimeSpan _length;
        private TimeSpan _elapsedBeforeRun;
        private DateTime _runStartUtc;
        private double _runStartMonotonic;

        /// <param name="config">Kept by reference and read as described in the remarks.</param>
        /// <param name="time">Source of both the wall-clock and the monotonic time.</param>
        public PomodoroTimer(PomodoroConfig config, ITimeSource time)
        {
            _config = config ?? throw new ArgumentNullException(nameof(config));
            _time = time ?? throw new ArgumentNullException(nameof(time));
        }

        /// <summary>The current phase. A phase that runs out is replaced only inside <see cref="Update"/>.</summary>
        public PomodoroPhase Phase { get; private set; }

        /// <summary>True while a work or break phase is paused; always false in Idle and AwaitingNext.</summary>
        public bool Paused { get; private set; }

        /// <summary>Time left in the current work or break phase, frozen while paused; zero in Idle and AwaitingNext.</summary>
        public TimeSpan Remaining
        {
            get
            {
                if (!IsTimed(Phase)) return TimeSpan.Zero;
                var left = _length - Elapsed(_time.UtcNow, _time.MonotonicSeconds);
                return left > TimeSpan.Zero ? left : TimeSpan.Zero;
            }
        }

        /// <summary>
        /// Work phases that ran out or were skipped since the timer was created or last stopped. A long break follows
        /// each work phase that brings it to a multiple of <see cref="PomodoroConfig.longBreakEvery"/>; a value of zero
        /// or less there means no long breaks.
        /// </summary>
        public int CompletedWorkCount { get; private set; }

        /// <summary>
        /// Begins work from Idle, or from AwaitingNext the phase that is waiting: the break that is due after work, or
        /// work after a break. Does nothing in the other phases.
        /// </summary>
        public void Start()
        {
            PomodoroPhase next;
            if (Phase == PomodoroPhase.Idle) next = PomodoroPhase.Working;
            else if (Phase == PomodoroPhase.AwaitingNext) next = _awaitedPhase;
            else return;

            Enter(next, _time.UtcNow, _time.MonotonicSeconds, false);
        }

        /// <summary>Freezes the current work or break phase. Does nothing if there is none or it is already paused.</summary>
        public void Pause()
        {
            if (!IsTimed(Phase) || Paused) return;

            var nowUtc = _time.UtcNow;
            _elapsedBeforeRun = Elapsed(nowUtc, _time.MonotonicSeconds);
            Paused = true;
            _pending.Add(new PomodoroEvent(PomodoroEventKind.Paused, Phase, nowUtc, false));
        }

        /// <summary>Continues a paused phase from where it was frozen. Does nothing unless paused.</summary>
        public void Resume()
        {
            if (!IsTimed(Phase) || !Paused) return;

            _runStartUtc = _time.UtcNow;
            _runStartMonotonic = _time.MonotonicSeconds;
            Paused = false;
            _pending.Add(new PomodoroEvent(PomodoroEventKind.Resumed, Phase, _runStartUtc, false));
        }

        /// <summary>
        /// Ends the current work or break phase now, paused or not, as if its time had run out: skipped work counts
        /// toward the long break, and the auto-start flags decide whether the next phase starts or waits.
        /// Does nothing in Idle and AwaitingNext.
        /// </summary>
        public void Skip()
        {
            if (!IsTimed(Phase)) return;

            var nowUtc = _time.UtcNow;
            End(nowUtc, nowUtc, _time.MonotonicSeconds, false);
        }

        /// <summary>Returns to Idle from any other phase and resets <see cref="CompletedWorkCount"/>.</summary>
        public void Stop()
        {
            if (Phase == PomodoroPhase.Idle) return;

            _pending.Add(new PomodoroEvent(PomodoroEventKind.Stopped, Phase, _time.UtcNow, false));
            Phase = PomodoroPhase.Idle;
            Paused = false;
            CompletedWorkCount = 0;
        }

        /// <summary>
        /// Ends the current phase if its time has run out, then returns the events queued since the previous call,
        /// oldest first. Returns a shared empty list when there are none.
        /// </summary>
        /// <remarks>
        /// At most one phase ends per call, however long ago its deadline was. A transition confirmed more than a
        /// second after the deadline is <see cref="PomodoroEvent.Late"/>, and the next phase then starts from now
        /// instead of from the missed deadline; otherwise a long sleep would end every phase it slept through, one per
        /// frame. A transition on time starts the next phase at the deadline, so the cycle does not drift by a frame
        /// per phase.
        /// </remarks>
        public IReadOnlyList<PomodoroEvent> Update()
        {
            if (IsTimed(Phase) && !Paused)
            {
                var nowUtc = _time.UtcNow;
                var nowMonotonic = _time.MonotonicSeconds;
                var overshoot = Elapsed(nowUtc, nowMonotonic) - _length;
                if (overshoot >= TimeSpan.Zero)
                {
                    var endedUtc = nowUtc - overshoot;
                    if (overshoot > LateThreshold) End(endedUtc, nowUtc, nowMonotonic, true);
                    else End(endedUtc, endedUtc, nowMonotonic - overshoot.TotalSeconds, false);
                }
            }

            if (_pending.Count == 0) return Array.Empty<PomodoroEvent>();

            var events = _pending.ToArray();
            _pending.Clear();
            return events;
        }

        private static bool IsTimed(PomodoroPhase phase) =>
            phase == PomodoroPhase.Working || phase == PomodoroPhase.ShortBreak || phase == PomodoroPhase.LongBreak;

        private void End(DateTime endedUtc, DateTime nextStartUtc, double nextStartMonotonic, bool late)
        {
            _pending.Add(new PomodoroEvent(PomodoroEventKind.PhaseEnded, Phase, endedUtc, late));

            PomodoroPhase next;
            bool autoStart;
            if (Phase == PomodoroPhase.Working)
            {
                CompletedWorkCount++;
                var every = _config.longBreakEvery;
                next = every > 0 && CompletedWorkCount % every == 0 ? PomodoroPhase.LongBreak : PomodoroPhase.ShortBreak;
                autoStart = _config.autoStartBreak;
            }
            else
            {
                next = PomodoroPhase.Working;
                autoStart = _config.autoStartWork;
            }

            if (autoStart)
            {
                Enter(next, nextStartUtc, nextStartMonotonic, late);
            }
            else
            {
                _awaitedPhase = next;
                Enter(PomodoroPhase.AwaitingNext, nextStartUtc, nextStartMonotonic, late);
            }
        }

        private void Enter(PomodoroPhase phase, DateTime startUtc, double startMonotonic, bool late)
        {
            Phase = phase;
            Paused = false;
            _length = LengthOf(phase);
            _elapsedBeforeRun = TimeSpan.Zero;
            _runStartUtc = startUtc;
            _runStartMonotonic = startMonotonic;
            _pending.Add(new PomodoroEvent(PomodoroEventKind.PhaseStarted, phase, startUtc, late));
        }

        private TimeSpan LengthOf(PomodoroPhase phase)
        {
            int minutes = phase switch
            {
                PomodoroPhase.Working => _config.workMin,
                PomodoroPhase.ShortBreak => _config.shortBreakMin,
                PomodoroPhase.LongBreak => _config.longBreakMin,
                _ => 0,
            };
            return TimeSpan.FromMinutes(Math.Max(0, minutes));
        }

        private TimeSpan Elapsed(DateTime nowUtc, double nowMonotonic)
        {
            if (Paused) return _elapsedBeforeRun;

            var wall = nowUtc - _runStartUtc;
            // TimeSpan.FromSeconds rounds to whole milliseconds on .NET Framework-derived runtimes such as Unity's Mono,
            // so it would put the deadline at a different instant than in the dotnet tests.
            var monotonic = TimeSpan.FromTicks((long)Math.Round((nowMonotonic - _runStartMonotonic) * TimeSpan.TicksPerSecond));
            var run = wall > monotonic ? wall : monotonic;
            return run > TimeSpan.Zero ? _elapsedBeforeRun + run : _elapsedBeforeRun;
        }
    }
}
