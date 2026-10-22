using System;
using OpenPerch.App.Core;

namespace OpenPerch.App.Tests.Shared
{
    public sealed class FakeTimeSource : ITimeSource
    {
        public FakeTimeSource(DateTime startUtc, double startMonotonicSeconds = 0)
        {
            UtcNow = DateTime.SpecifyKind(startUtc, DateTimeKind.Utc);
            MonotonicSeconds = startMonotonicSeconds;
        }

        public DateTime UtcNow { get; private set; }

        public double MonotonicSeconds { get; private set; }

        /// <summary>Advances both clocks together, as while the machine is awake.</summary>
        public void Advance(double seconds)
        {
            UtcNow = UtcNow.AddSeconds(seconds);
            MonotonicSeconds += seconds;
        }

        /// <summary>
        /// Advances only the wall clock. Models a system sleep on hosts whose monotonic counter stops while asleep,
        /// or a manual clock change.
        /// </summary>
        public void AdvanceWallClockOnly(double seconds)
        {
            UtcNow = UtcNow.AddSeconds(seconds);
        }
    }
}
