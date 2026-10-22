using System;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using OpenPerch.App.Core;
using static OpenPerch.App.Core.PomodoroEventKind;
using static OpenPerch.App.Core.PomodoroPhase;

namespace OpenPerch.Core.Tests.Pomodoro
{
    [Category("T-POM")]
    public class PomodoroTimerTransitionTests : PomodoroTimerTestBase
    {
        [Test]
        public void ConstructorRejectsMissingDependencies()
        {
            Assert.Throws<ArgumentNullException>(() => new PomodoroTimer(null, Clock));
            Assert.Throws<ArgumentNullException>(() => new PomodoroTimer(Config, null));
        }

        [Test]
        public void NewTimerIsIdleWithNothingToReport()
        {
            Assert.That(Timer.Phase, Is.EqualTo(Idle));
            Assert.That(Timer.Paused, Is.False);
            Assert.That(Timer.Remaining, Is.EqualTo(TimeSpan.Zero));
            Assert.That(Timer.CompletedWorkCount, Is.EqualTo(0));
            Assert.That(Timer.Update(), Is.SameAs(Array.Empty<PomodoroEvent>()));
        }

        [Test]
        public void FrameWithNothingToReportReturnsTheSharedEmptyList()
        {
            StartWork();
            AdvanceMinutes(1);

            Assert.That(Timer.Update(), Is.SameAs(Array.Empty<PomodoroEvent>()));
        }

        [Test]
        public void StartFromIdleBeginsWork()
        {
            Timer.Start();
            AdvanceMinutes(1);
            var events = Timer.Update();

            Assert.That(Kinds(events), Is.EqualTo(new[] { (PhaseStarted, Working) }));
            Assert.That(events[0].AtUtc, Is.EqualTo(T0));
            Assert.That(events[0].Late, Is.False);
            Assert.That(Timer.Phase, Is.EqualTo(Working));
            Assert.That(Timer.Remaining, Is.EqualTo(TimeSpan.FromMinutes(24)));
        }

        [Test]
        public void WorkRunningOutStartsAShortBreakWhenAutoStartBreakIsOn()
        {
            Config.autoStartBreak = true;
            StartWork();

            AdvanceMinutes(25);
            var events = Timer.Update();

            Assert.That(Kinds(events), Is.EqualTo(new[] { (PhaseEnded, Working), (PhaseStarted, ShortBreak) }));
            Assert.That(events.Select(e => e.AtUtc), Is.All.EqualTo(T0.AddMinutes(25)));
            Assert.That(events.Select(e => e.Late), Is.All.False);
            Assert.That(Timer.Phase, Is.EqualTo(ShortBreak));
            Assert.That(Timer.CompletedWorkCount, Is.EqualTo(1));
            Assert.That(Timer.Remaining, Is.EqualTo(TimeSpan.FromMinutes(5)));
        }

        [Test]
        public void WorkRunningOutStartsALongBreakWhenTheCountReachesAMultipleOfLongBreakEvery()
        {
            Config.longBreakEvery = 1;
            StartWork();

            var events = RunOut();

            Assert.That(Kinds(events), Is.EqualTo(new[] { (PhaseEnded, Working), (PhaseStarted, LongBreak) }));
            Assert.That(Timer.Remaining, Is.EqualTo(TimeSpan.FromMinutes(15)));
        }

        [Test]
        public void WorkRunningOutWaitsWhenAutoStartBreakIsOff()
        {
            Config.autoStartBreak = false;
            StartWork();

            var events = RunOut();

            Assert.That(Kinds(events), Is.EqualTo(new[] { (PhaseEnded, Working), (PhaseStarted, AwaitingNext) }));
            Assert.That(events.Select(e => e.AtUtc), Is.All.EqualTo(T0.AddMinutes(25)));
            Assert.That(Timer.Phase, Is.EqualTo(AwaitingNext));
            Assert.That(Timer.CompletedWorkCount, Is.EqualTo(1));
            Assert.That(Timer.Remaining, Is.EqualTo(TimeSpan.Zero));
        }

        [TestCase(4, ShortBreak)]
        [TestCase(1, LongBreak)]
        public void BreakRunningOutStartsWorkWhenAutoStartWorkIsOn(int longBreakEvery, PomodoroPhase breakPhase)
        {
            Config.longBreakEvery = longBreakEvery;
            Config.autoStartWork = true;
            StartWork();
            RunOut();

            var events = RunOut();

            Assert.That(Kinds(events), Is.EqualTo(new[] { (PhaseEnded, breakPhase), (PhaseStarted, Working) }));
            Assert.That(Timer.Phase, Is.EqualTo(Working));
            Assert.That(Timer.Remaining, Is.EqualTo(TimeSpan.FromMinutes(25)));
        }

        [TestCase(4, ShortBreak)]
        [TestCase(1, LongBreak)]
        public void BreakRunningOutWaitsWhenAutoStartWorkIsOff(int longBreakEvery, PomodoroPhase breakPhase)
        {
            Config.longBreakEvery = longBreakEvery;
            Config.autoStartWork = false;
            StartWork();
            RunOut();

            var events = RunOut();

            Assert.That(Kinds(events), Is.EqualTo(new[] { (PhaseEnded, breakPhase), (PhaseStarted, AwaitingNext) }));
            Assert.That(Timer.Phase, Is.EqualTo(AwaitingNext));
            Assert.That(Timer.Remaining, Is.EqualTo(TimeSpan.Zero));
        }

        [TestCase(4, ShortBreak, 5)]
        [TestCase(1, LongBreak, 15)]
        public void StartWhileAwaitingAfterWorkBeginsTheBreakThatIsDue(int longBreakEvery, PomodoroPhase breakPhase, int breakMinutes)
        {
            Config.longBreakEvery = longBreakEvery;
            Config.autoStartBreak = false;
            StartWork();
            RunOut();
            AdvanceMinutes(3);

            Timer.Start();
            var events = Timer.Update();

            Assert.That(Kinds(events), Is.EqualTo(new[] { (PhaseStarted, breakPhase) }));
            Assert.That(events[0].AtUtc, Is.EqualTo(T0.AddMinutes(28)));
            Assert.That(Timer.Phase, Is.EqualTo(breakPhase));
            Assert.That(Timer.Remaining, Is.EqualTo(TimeSpan.FromMinutes(breakMinutes)));
        }

        [Test]
        public void StartWhileAwaitingAfterABreakBeginsWork()
        {
            Config.autoStartWork = false;
            StartWork();
            RunOut();
            RunOut();

            Timer.Start();
            var events = Timer.Update();

            Assert.That(Kinds(events), Is.EqualTo(new[] { (PhaseStarted, Working) }));
            Assert.That(Timer.Remaining, Is.EqualTo(TimeSpan.FromMinutes(25)));
        }

        [Test]
        public void StartDoesNothingWhileAPhaseIsRunningOrPaused()
        {
            StartWork();
            AdvanceMinutes(10);

            Timer.Start();
            Assert.That(Timer.Update(), Is.Empty);
            Assert.That(Timer.Remaining, Is.EqualTo(TimeSpan.FromMinutes(15)));

            Timer.Pause();
            Timer.Update();
            Timer.Start();
            Assert.That(Timer.Update(), Is.Empty);
            Assert.That(Timer.Paused, Is.True);
        }

        [Test]
        public void PhaseEndsWhenRemainingReachesExactlyZero()
        {
            StartWork();

            Clock.Advance(25 * 60 - 1);
            Assert.That(Timer.Update(), Is.Empty);
            Assert.That(Timer.Remaining, Is.EqualTo(TimeSpan.FromSeconds(1)));

            Clock.Advance(1);
            Assert.That(Timer.Remaining, Is.EqualTo(TimeSpan.Zero));
            var events = Timer.Update();

            Assert.That(Kinds(events), Is.EqualTo(new[] { (PhaseEnded, Working), (PhaseStarted, ShortBreak) }));
            Assert.That(events.Select(e => e.AtUtc), Is.All.EqualTo(T0.AddMinutes(25)));
            Assert.That(events.Select(e => e.Late), Is.All.False);
            Assert.That(Timer.Remaining, Is.EqualTo(TimeSpan.FromMinutes(5)));
        }

        [Test]
        public void OnTimeTransitionStartsTheNextPhaseAtTheDeadline()
        {
            StartWork();

            Clock.Advance(25 * 60 + 0.5);
            var events = Timer.Update();

            Assert.That(events.Select(e => e.Late), Is.All.False);
            Assert.That(events.Select(e => e.AtUtc), Is.All.EqualTo(T0.AddMinutes(25)));
            Assert.That(Timer.Remaining, Is.EqualTo(TimeSpan.FromMinutes(5) - TimeSpan.FromSeconds(0.5)));
        }

        [Test]
        public void EveryFourthWorkPhaseIsFollowedByALongBreakByDefault()
        {
            Config.autoStartWork = true;
            StartWork();
            var breaks = new List<PomodoroPhase>();

            for (int i = 1; i <= 8; i++)
            {
                RunOut();
                breaks.Add(Timer.Phase);
                Assert.That(Timer.CompletedWorkCount, Is.EqualTo(i));
                RunOut();
            }

            Assert.That(breaks, Is.EqualTo(new[]
            {
                ShortBreak, ShortBreak, ShortBreak, LongBreak,
                ShortBreak, ShortBreak, ShortBreak, LongBreak,
            }));
        }

        [Test]
        public void LongBreakEveryChangesTheCycle()
        {
            Config.longBreakEvery = 2;
            Config.autoStartWork = true;
            StartWork();
            var breaks = new List<PomodoroPhase>();

            for (int i = 0; i < 4; i++)
            {
                RunOut();
                breaks.Add(Timer.Phase);
                RunOut();
            }

            Assert.That(breaks, Is.EqualTo(new[] { ShortBreak, LongBreak, ShortBreak, LongBreak }));
        }

        [TestCase(0)]
        [TestCase(-1)]
        public void NonPositiveLongBreakEveryMeansNoLongBreaks(int longBreakEvery)
        {
            Config.longBreakEvery = longBreakEvery;
            Config.autoStartWork = true;
            StartWork();

            for (int i = 0; i < 4; i++)
            {
                RunOut();
                Assert.That(Timer.Phase, Is.EqualTo(ShortBreak));
                RunOut();
            }
        }

        [Test]
        public void LengthEditsApplyFromTheNextPhase()
        {
            StartWork();

            Config.workMin = 50;
            Config.shortBreakMin = 10;
            Assert.That(Timer.Remaining, Is.EqualTo(TimeSpan.FromMinutes(25)));

            AdvanceMinutes(25);
            Timer.Update();
            Assert.That(Timer.Phase, Is.EqualTo(ShortBreak));
            Assert.That(Timer.Remaining, Is.EqualTo(TimeSpan.FromMinutes(10)));
        }

        [TestCase(0)]
        [TestCase(-5)]
        public void ZeroOrNegativeLengthEndsOnTheNextUpdateOnTime(int workMin)
        {
            Config.workMin = workMin;

            Timer.Start();
            var events = Timer.Update();

            Assert.That(Kinds(events), Is.EqualTo(new[] { (PhaseStarted, Working), (PhaseEnded, Working), (PhaseStarted, ShortBreak) }));
            Assert.That(events.Select(e => e.Late), Is.All.False);
        }

        [Test]
        public void ZeroLengthPhasesStillEndOnePerUpdate()
        {
            Config.workMin = 0;
            Config.shortBreakMin = 0;
            Config.autoStartWork = true;
            StartWork();

            Assert.That(Kinds(Timer.Update()), Is.EqualTo(new[] { (PhaseEnded, ShortBreak), (PhaseStarted, Working) }));
            Assert.That(Kinds(Timer.Update()), Is.EqualTo(new[] { (PhaseEnded, Working), (PhaseStarted, ShortBreak) }));
        }
    }
}
