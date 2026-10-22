using System;
using System.IO;
using NUnit.Framework;
using OpenPerch.App.Tests.Shared;

namespace OpenPerch.Core.Tests.Support
{
    public class TestSupportTests
    {
        [Test]
        public void RepoRootContainsLicense()
        {
            Assert.That(File.Exists(RepoPaths.Combine("LICENSE")), Is.True);
        }

        [Test]
        public void FakeRandomCyclesThroughTheSequence()
        {
            var rng = new FakeRandom(0.1f, 0.9f);

            Assert.That(rng.Next01(), Is.EqualTo(0.1f));
            Assert.That(rng.Range(0, 10), Is.EqualTo(9));
            Assert.That(rng.Next01(), Is.EqualTo(0.1f));
        }

        [Test]
        public void WallClockOnlyAdvanceLeavesMonotonicTimeUnchanged()
        {
            var time = new FakeTimeSource(new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc), 100);

            time.AdvanceWallClockOnly(60);

            Assert.That(time.MonotonicSeconds, Is.EqualTo(100));
            Assert.That(time.UtcNow, Is.EqualTo(new DateTime(2026, 1, 1, 0, 1, 0, DateTimeKind.Utc)));
        }
    }
}
