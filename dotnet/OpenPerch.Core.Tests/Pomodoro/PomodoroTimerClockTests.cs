using System;
using System.Linq;
using NUnit.Framework;
using OpenPerch.App.Core;
using static OpenPerch.App.Core.PomodoroEventKind;
using static OpenPerch.App.Core.PomodoroPhase;

namespace OpenPerch.Core.Tests.Pomodoro
{
    [Category("T-POM")]
    public class PomodoroTimerClockTests : PomodoroTimerTestBase
    {
        private const double ThreeHours = 3 * 60 * 60;

        [TestCase(false)]
        [TestCase(true)]
        public void SleepingThroughSeveralDeadlinesConfirmsOneLateTransition(bool monotonicStops)
        {
            Config.autoStartBreak = true;
            Config.autoStartWork = true;
            StartWork();
            AdvanceMinutes(10);

            Sleep(ThreeHours, monotonicStops);
            var events = Timer.Update();

            Assert.That(Kinds(events), Is.EqualTo(new[] { (PhaseEnded, Working), (PhaseStarted, ShortBreak) }));
            Assert.That(events.Select(e => e.Late), Is.All.True);
            Assert.That(events[0].AtUtc, Is.EqualTo(T0.AddMinutes(25)));
            Assert.That(events[1].AtUtc, Is.EqualTo(T0.AddMinutes(10).AddHours(3)));
            Assert.That(Timer.CompletedWorkCount, Is.EqualTo(1));
            Assert.That(Timer.Remaining, Is.EqualTo(TimeSpan.FromMinutes(5)));
            Assert.That(Timer.Update(), Is.Empty);
        }

        [TestCase(false)]
        [TestCase(true)]
        public void PhaseStartedLateRunsItsFullLengthFromWaking(bool monotonicStops)
        {
            Config.autoStartWork = true;
            StartWork();
            Sleep(ThreeHours, monotonicStops);
            Timer.Update();

            AdvanceMinutes(5);
            var events = Timer.Update();

            Assert.That(Kinds(events), Is.EqualTo(new[] { (PhaseEnded, ShortBreak), (PhaseStarted, Working) }));
            Assert.That(events.Select(e => e.Late), Is.All.False);
            Assert.That(events[0].AtUtc, Is.EqualTo(T0.AddHours(3).AddMinutes(5)));
        }

        [Test]
        public void SleepingPastAWorkDeadlineWithoutAutoStartWaitsLate()
        {
            Config.autoStartBreak = false;
            StartWork();

            Sleep(ThreeHours, true);
            var events = Timer.Update();

            Assert.That(Kinds(events), Is.EqualTo(new[] { (PhaseEnded, Working), (PhaseStarted, AwaitingNext) }));
            Assert.That(events.Select(e => e.Late), Is.All.True);
            Assert.That(events[0].AtUtc, Is.EqualTo(T0.AddMinutes(25)));
            Assert.That(Timer.Phase, Is.EqualTo(AwaitingNext));
        }

        [Test]
        public void SleepingWhileAwaitingChangesNothing()
        {
            DriveTo(AwaitingNext);

            Sleep(ThreeHours, true);

            Assert.That(Timer.Update(), Is.Empty);
            Assert.That(Timer.Phase, Is.EqualTo(AwaitingNext));
            Assert.That(Timer.Remaining, Is.EqualTo(TimeSpan.Zero));
        }

        [TestCase(1.0, false)]
        [TestCase(1.5, true)]
        public void TransitionIsLateWhenConfirmedMoreThanASecondAfterTheDeadline(double overshootSeconds, bool late)
        {
            StartWork();

            Clock.Advance(25 * 60 + overshootSeconds);
            var events = Timer.Update();

            Assert.That(Kinds(events), Is.EqualTo(new[] { (PhaseEnded, Working), (PhaseStarted, ShortBreak) }));
            Assert.That(events.Select(e => e.Late), Is.All.EqualTo(late));
            Assert.That(events[0].AtUtc, Is.EqualTo(T0.AddMinutes(25)));
        }

        [Test]
        public void SettingTheClockBackDoesNotStretchAPhase()
        {
            StartWork();
            AdvanceMinutes(10);

            Clock.AdvanceWallClockOnly(-3600);
            Assert.That(Timer.Remaining, Is.EqualTo(TimeSpan.FromMinutes(15)));

            AdvanceMinutes(15);
            var events = Timer.Update();

            Assert.That(Kinds(events), Is.EqualTo(new[] { (PhaseEnded, Working), (PhaseStarted, ShortBreak) }));
            Assert.That(events.Select(e => e.Late), Is.All.False);
            Assert.That(events[0].AtUtc, Is.EqualTo(Clock.UtcNow));
            Assert.That(Timer.Remaining, Is.EqualTo(TimeSpan.FromMinutes(5)));
        }

        [Test]
        public void SettingTheClockForwardEndsThePhaseLate()
        {
            StartWork();
            AdvanceMinutes(10);

            Clock.AdvanceWallClockOnly(3600);
            var events = Timer.Update();

            Assert.That(Kinds(events), Is.EqualTo(new[] { (PhaseEnded, Working), (PhaseStarted, ShortBreak) }));
            Assert.That(events.Select(e => e.Late), Is.All.True);
            Assert.That(Timer.Remaining, Is.EqualTo(TimeSpan.FromMinutes(5)));
        }

        [Test]
        public void ResumingAfterSleepWhilePausedIsNotLate()
        {
            StartWork();
            AdvanceMinutes(10);
            Timer.Pause();
            Sleep(ThreeHours, true);

            Timer.Resume();
            Assert.That(Timer.Remaining, Is.EqualTo(TimeSpan.FromMinutes(15)));
            AdvanceMinutes(15);
            var events = Timer.Update();

            Assert.That(Kinds(events), Is.EqualTo(new[]
            {
                (Paused, Working), (Resumed, Working), (PhaseEnded, Working), (PhaseStarted, ShortBreak),
            }));
            Assert.That(events.Select(e => e.Late), Is.All.False);
        }

        private void Sleep(double seconds, bool monotonicStops)
        {
            if (monotonicStops) Clock.AdvanceWallClockOnly(seconds);
            else Clock.Advance(seconds);
        }
    }
}
