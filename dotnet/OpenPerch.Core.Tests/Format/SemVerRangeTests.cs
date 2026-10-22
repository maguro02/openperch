using System;
using NUnit.Framework;
using OpenPerch.Format;

namespace OpenPerch.Core.Tests.Format
{
    [Category("T-VER")]
    public class SemVerRangeTests
    {
        private static SemVerRange Range(string s)
        {
            Assert.That(SemVerRange.TryParse(s, out SemVerRange r), Is.True, s);
            return r;
        }

        private static SemVer V(string s)
        {
            Assert.That(SemVer.TryParse(s, out SemVer v), Is.True, s);
            return v;
        }

        [TestCase("1.0.0", true)]
        [TestCase("1.0.2", true)]
        [TestCase("1.0.3-beta", true)]
        [TestCase("1.0.3", false)]
        [TestCase("0.9.9", false)]
        [TestCase("1.0.0-rc.1", false)]
        [TestCase("2.0.0", false)]
        public void HalfOpenRange(string version, bool expected)
        {
            Assert.That(Range(">=1.0.0 <1.0.3").Contains(V(version)), Is.EqualTo(expected));
        }

        [Test]
        public void EveryOperator()
        {
            Assert.That(Range(">1.0.0").Contains(V("1.0.0")), Is.False);
            Assert.That(Range(">1.0.0").Contains(V("1.0.1")), Is.True);
            Assert.That(Range("<=2.0.0").Contains(V("2.0.0")), Is.True);
            Assert.That(Range("<=2.0.0").Contains(V("2.0.1")), Is.False);
            Assert.That(Range("=1.2.3").Contains(V("1.2.3")), Is.True);
            Assert.That(Range("=1.2.3").Contains(V("1.2.4")), Is.False);
            Assert.That(Range("1.2.3").Contains(V("1.2.3+build")), Is.True);
            Assert.That(Range("1.2.3").Contains(V("1.2.2")), Is.False);
        }

        [Test]
        public void ExtraSpacesAreAllowedBetweenComparators()
        {
            Assert.That(Range("  >=1.0.0   <2.0.0 ").Contains(V("1.5.0")), Is.True);
        }

        [TestCase(null)]
        [TestCase("")]
        [TestCase("   ")]
        [TestCase(">= 1.0.0")]
        [TestCase("~1.0.0")]
        [TestCase("^1.0.0")]
        [TestCase("1.0")]
        [TestCase(">=1.0.0 || <2.0.0")]
        [TestCase("=>1.0.0")]
        [TestCase(">=v1.0.0")]
        public void InvalidRangesAreRejected(string s)
        {
            Assert.That(SemVerRange.TryParse(s, out _), Is.False);
        }

        [Test]
        public void DefaultRangeContainsNothing()
        {
            Assert.That(default(SemVerRange).Contains(V("1.0.0")), Is.False);
        }

        [Test]
        public void KnownBadValidatesItsRange()
        {
            var bad = new KnownBad(">=1.0.0 <1.0.3", "E_COMPAT_KNOWN_BAD_EXPORTER", true);

            Assert.That(bad.Range, Is.EqualTo(">=1.0.0 <1.0.3"));
            Assert.That(bad.MessageCode, Is.EqualTo("E_COMPAT_KNOWN_BAD_EXPORTER"));
            Assert.That(bad.Reject, Is.True);
            Assert.Throws<ArgumentException>(() => new KnownBad("~1.0.0", "E_COMPAT_KNOWN_BAD_EXPORTER", true));
            Assert.Throws<ArgumentException>(() => new KnownBad(">=1.0.0", "", false));
        }
    }
}
