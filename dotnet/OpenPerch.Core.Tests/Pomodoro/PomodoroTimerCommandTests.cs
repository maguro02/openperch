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
    public class PomodoroTimerCommandTests : PomodoroTimerTestBase
    {
        [TestCase(false)]
        [TestCase(true)]
        public void PauseFreezesRemainingWhileTimePasses(bool monotonicStops)
        {
            StartWork();
            AdvanceMinutes(10);

            Timer.Pause();
            var events = Timer.Update();
            if (monotonicStops) Clock.AdvanceWallClockOnly(3600);
            else Clock.Advance(3600);

            Assert.That(Kinds(events), Is.EqualTo(new[] { (Paused, Working) }));
            Assert.That(events[0].AtUtc, Is.EqualTo(T0.AddMinutes(10)));
            Assert.That(Timer.Paused, Is.True);
            Assert.That(Timer.Remaining, Is.EqualTo(TimeSpan.FromMinutes(15)));
            Assert.That(Timer.Update(), Is.Empty);
            Assert.That(Timer.Phase, Is.EqualTo(Working));
        }

        [Test]
        public void ResumeContinuesFromWherePauseFroze()
        {
            StartWork();
            AdvanceMinutes(10);
            Timer.Pause();
            AdvanceMinutes(30);

            Timer.Resume();
            var events = Timer.Update();

            Assert.That(Kinds(events), Is.EqualTo(new[] { (Paused, Working), (Resumed, Working) }));
            Assert.That(events[1].AtUtc, Is.EqualTo(T0.AddMinutes(40)));
            Assert.That(Timer.Paused, Is.False);
            Assert.That(Timer.Remaining, Is.EqualTo(TimeSpan.FromMinutes(15)));

            AdvanceMinutes(5);
            Assert.That(Timer.Remaining, Is.EqualTo(TimeSpan.FromMinutes(10)));

            AdvanceMinutes(10);
            var ended = Timer.Update();
            Assert.That(Kinds(ended), Is.EqualTo(new[] { (PhaseEnded, Working), (PhaseStarted, ShortBreak) }));
            Assert.That(ended[0].AtUtc, Is.EqualTo(T0.AddMinutes(55)));
            Assert.That(ended[0].Late, Is.False);
        }

        [Test]
        public void PauseAndResumeDoNothingWithoutAWorkOrBreakPhase()
        {
            Timer.Pause();
            Timer.Resume();
            Assert.That(Timer.Update(), Is.Empty);

            DriveTo(AwaitingNext);
            Timer.Pause();
            Timer.Resume();
            Assert.That(Timer.Update(), Is.Empty);
            Assert.That(Timer.Paused, Is.False);
        }

        [Test]
        public void RepeatedPauseOrResumeIsReportedOnce()
        {
            StartWork();

            Timer.Resume();
            Timer.Pause();
            Timer.Pause();
            Timer.Resume();
            Timer.Resume();

            Assert.That(Kinds(Timer.Update()), Is.EqualTo(new[] { (Paused, Working), (Resumed, Working) }));
        }

        [Test]
        public void PauseWorksDuringABreak()
        {
            DriveTo(ShortBreak);

            Timer.Pause();
            AdvanceMinutes(30);

            Assert.That(Kinds(Timer.Update()), Is.EqualTo(new[] { (Paused, ShortBreak) }));
            Assert.That(Timer.Remaining, Is.EqualTo(TimeSpan.FromMinutes(4)));
        }

        [Test]
        public void SkipEndsWorkNowAndCountsIt()
        {
            StartWork();
            AdvanceMinutes(10);

            Timer.Skip();
            AdvanceMinutes(1);
            var events = Timer.Update();

            Assert.That(Kinds(events), Is.EqualTo(new[] { (PhaseEnded, Working), (PhaseStarted, ShortBreak) }));
            Assert.That(events.Select(e => e.AtUtc), Is.All.EqualTo(T0.AddMinutes(10)));
            Assert.That(events.Select(e => e.Late), Is.All.False);
            Assert.That(Timer.CompletedWorkCount, Is.EqualTo(1));
            Assert.That(Timer.Remaining, Is.EqualTo(TimeSpan.FromMinutes(4)));
        }

        [Test]
        public void SkipWaitsWhenTheAutoStartFlagIsOff()
        {
            Config.autoStartBreak = false;
            Config.autoStartWork = false;
            StartWork();

            Timer.Skip();
            Assert.That(Kinds(Timer.Update()), Is.EqualTo(new[] { (PhaseEnded, Working), (PhaseStarted, AwaitingNext) }));

            Timer.Start();
            Timer.Skip();
            Assert.That(Kinds(Timer.Update()), Is.EqualTo(new[]
            {
                (PhaseStarted, ShortBreak), (PhaseEnded, ShortBreak), (PhaseStarted, AwaitingNext),
            }));
        }

        [Test]
        public void SkipEndsABreakAndStartsWorkWhenAutoStartWorkIsOn()
        {
            Config.autoStartWork = true;
            DriveTo(ShortBreak);

            Timer.Skip();

            Assert.That(Kinds(Timer.Update()), Is.EqualTo(new[] { (PhaseEnded, ShortBreak), (PhaseStarted, Working) }));
            Assert.That(Timer.Remaining, Is.EqualTo(TimeSpan.FromMinutes(25)));
            Assert.That(Timer.CompletedWorkCount, Is.EqualTo(1));
        }

        [Test]
        public void SkipWhilePausedStartsTheNextPhaseRunning()
        {
            StartWork();
            Timer.Pause();

            Timer.Skip();
            Timer.Update();
            AdvanceMinutes(1);

            Assert.That(Timer.Phase, Is.EqualTo(ShortBreak));
            Assert.That(Timer.Paused, Is.False);
            Assert.That(Timer.Remaining, Is.EqualTo(TimeSpan.FromMinutes(4)));
        }

        [Test]
        public void SkipDoesNothingWithoutAWorkOrBreakPhase()
        {
            Timer.Skip();
            Assert.That(Timer.Update(), Is.Empty);
            Assert.That(Timer.Phase, Is.EqualTo(Idle));

            DriveTo(AwaitingNext);
            Timer.Skip();
            Assert.That(Timer.Update(), Is.Empty);
            Assert.That(Timer.Phase, Is.EqualTo(AwaitingNext));
        }

        [Test]
        public void SkippedWorkCountsTowardTheLongBreak()
        {
            Config.autoStartWork = true;
            StartWork();
            var breaks = new List<PomodoroPhase>();

            for (int i = 0; i < 4; i++)
            {
                Timer.Skip();
                breaks.Add(Timer.Phase);
                Timer.Skip();
            }

            Assert.That(breaks, Is.EqualTo(new[] { ShortBreak, ShortBreak, ShortBreak, LongBreak }));
            Assert.That(Timer.CompletedWorkCount, Is.EqualTo(4));
        }

        [TestCase(Working)]
        [TestCase(ShortBreak)]
        [TestCase(LongBreak)]
        [TestCase(AwaitingNext)]
        public void StopReturnsToIdleFromAnyPhase(PomodoroPhase phase)
        {
            DriveTo(phase);

            Timer.Stop();
            var events = Timer.Update();

            Assert.That(Kinds(events), Is.EqualTo(new[] { (Stopped, phase) }));
            Assert.That(events[0].AtUtc, Is.EqualTo(Clock.UtcNow));
            Assert.That(events[0].Late, Is.False);
            Assert.That(Timer.Phase, Is.EqualTo(Idle));
            Assert.That(Timer.Paused, Is.False);
            Assert.That(Timer.Remaining, Is.EqualTo(TimeSpan.Zero));
            Assert.That(Timer.CompletedWorkCount, Is.EqualTo(0));
        }

        [Test]
        public void StopWhilePausedClearsThePause()
        {
            StartWork();
            Timer.Pause();

            Timer.Stop();

            Assert.That(Kinds(Timer.Update()), Is.EqualTo(new[] { (Paused, Working), (Stopped, Working) }));
            Assert.That(Timer.Phase, Is.EqualTo(Idle));
            Assert.That(Timer.Paused, Is.False);
        }

        [Test]
        public void StopWhenIdleReportsNothing()
        {
            Timer.Stop();

            Assert.That(Timer.Update(), Is.Empty);
            Assert.That(Timer.Phase, Is.EqualTo(Idle));
        }

        [Test]
        public void StartAfterStopBeginsANewCycle()
        {
            Config.longBreakEvery = 2;
            StartWork();
            RunOut();
            Timer.Stop();
            AdvanceMinutes(60);

            Timer.Start();
            var started = Timer.Update();
            var ended = RunOut();

            Assert.That(Kinds(started), Is.EqualTo(new[] { (Stopped, ShortBreak), (PhaseStarted, Working) }));
            Assert.That(started[1].AtUtc, Is.EqualTo(T0.AddMinutes(85)));
            Assert.That(Kinds(ended), Is.EqualTo(new[] { (PhaseEnded, Working), (PhaseStarted, ShortBreak) }));
            Assert.That(Timer.CompletedWorkCount, Is.EqualTo(1));
        }

        [Test]
        public void CommandEventsKeepTheirOrderAndTimes()
        {
            Timer.Start();
            AdvanceMinutes(1);
            Timer.Pause();
            AdvanceMinutes(1);
            Timer.Resume();
            AdvanceMinutes(1);
            Timer.Skip();
            AdvanceMinutes(1);
            Timer.Stop();
            AdvanceMinutes(1);

            var events = Timer.Update();

            Assert.That(Kinds(events), Is.EqualTo(new[]
            {
                (PhaseStarted, Working), (Paused, Working), (Resumed, Working),
                (PhaseEnded, Working), (PhaseStarted, ShortBreak), (Stopped, ShortBreak),
            }));
            Assert.That(events.Select(e => e.AtUtc), Is.EqualTo(new[]
            {
                T0, T0.AddMinutes(1), T0.AddMinutes(2), T0.AddMinutes(3), T0.AddMinutes(3), T0.AddMinutes(4),
            }));
            Assert.That(events.Select(e => e.Late), Is.All.False);
        }

        [Test]
        public void EventsAreHandedOutOnceAndNotReused()
        {
            Timer.Start();
            var first = Timer.Update();
            Timer.Pause();
            var second = Timer.Update();

            Assert.That(Kinds(first), Is.EqualTo(new[] { (PhaseStarted, Working) }));
            Assert.That(Kinds(second), Is.EqualTo(new[] { (Paused, Working) }));
            Assert.That(Timer.Update(), Is.Empty);
        }
    }
}
