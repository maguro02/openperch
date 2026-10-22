using System;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using OpenPerch.App.Core;
using OpenPerch.App.Tests.Shared;

namespace OpenPerch.Core.Tests.Pomodoro
{
    public abstract class PomodoroTimerTestBase
    {
        protected static readonly DateTime T0 = new DateTime(2026, 1, 1, 9, 0, 0, DateTimeKind.Utc);

        protected PomodoroConfig Config { get; private set; }

        protected FakeTimeSource Clock { get; private set; }

        protected PomodoroTimer Timer { get; private set; }

        [SetUp]
        public void CreateTimer()
        {
            Config = new PomodoroConfig();
            // A monotonic origin far from zero catches code that mistakes it for time since start.
            Clock = new FakeTimeSource(T0, 5000);
            Timer = new PomodoroTimer(Config, Clock);
        }

        protected static (PomodoroEventKind Kind, PomodoroPhase Phase)[] Kinds(IReadOnlyList<PomodoroEvent> events) =>
            events.Select(e => (e.Kind, e.Phase)).ToArray();

        protected void AdvanceMinutes(double minutes) => Clock.Advance(minutes * 60);

        protected void StartWork()
        {
            Timer.Start();
            Timer.Update();
        }

        /// <summary>Advances the clock by exactly the remaining time and returns what the next update reports.</summary>
        protected IReadOnlyList<PomodoroEvent> RunOut()
        {
            Clock.Advance(Timer.Remaining.TotalSeconds);
            return Timer.Update();
        }

        protected void DriveTo(PomodoroPhase phase)
        {
            switch (phase)
            {
                case PomodoroPhase.Working:
                    StartWork();
                    AdvanceMinutes(10);
                    break;
                case PomodoroPhase.ShortBreak:
                    StartWork();
                    RunOut();
                    AdvanceMinutes(1);
                    break;
                case PomodoroPhase.LongBreak:
                    Config.longBreakEvery = 1;
                    StartWork();
                    RunOut();
                    AdvanceMinutes(1);
                    break;
                case PomodoroPhase.AwaitingNext:
                    Config.autoStartBreak = false;
                    StartWork();
                    RunOut();
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(phase), phase, null);
            }
            Assert.That(Timer.Phase, Is.EqualTo(phase), "precondition");
        }
    }
}
