using NUnit.Framework;
using OpenPerch.Format;

namespace OpenPerch.Core.Tests.Format
{
    [Category("T-VER")]
    public class SemVerTests
    {
        private static SemVer Parse(string s)
        {
            Assert.That(SemVer.TryParse(s, out SemVer v), Is.True, s);
            return v;
        }

        [TestCase("0.0.0")]
        [TestCase("1.0.0")]
        [TestCase("10.20.30")]
        [TestCase("1.2.3-alpha")]
        [TestCase("1.2.3-alpha.1")]
        [TestCase("1.2.3-0.3.7")]
        [TestCase("1.2.3-x.7.z.92")]
        [TestCase("1.2.3-x-y-z.--")]
        [TestCase("1.2.3+build.5")]
        [TestCase("1.2.3+001")]
        [TestCase("1.2.3-rc.1+exp.sha.5114f85")]
        public void ValidVersionsParseAndRoundTrip(string s)
        {
            Assert.That(Parse(s).ToString(), Is.EqualTo(s));
        }

        [Test]
        public void ExposesItsParts()
        {
            SemVer v = Parse("1.2.3-beta.4+build.9");

            Assert.That(v.Major, Is.EqualTo(1));
            Assert.That(v.Minor, Is.EqualTo(2));
            Assert.That(v.Patch, Is.EqualTo(3));
            Assert.That(v.PreRelease, Is.EqualTo("beta.4"));
            Assert.That(v.BuildMetadata, Is.EqualTo("build.9"));
        }

        [TestCase(null)]
        [TestCase("")]
        [TestCase("1")]
        [TestCase("1.0")]
        [TestCase("1.0.0.0")]
        [TestCase("v1.0.0")]
        [TestCase("01.0.0")]
        [TestCase("1.00.0")]
        [TestCase("1.0.0-")]
        [TestCase("1.0.0-01")]
        [TestCase("1.0.0-alpha..1")]
        [TestCase("1.0.0+")]
        [TestCase("1.0.0+a..b")]
        [TestCase("1.0.0-alpha_1")]
        [TestCase(" 1.0.0")]
        [TestCase("1.0.0 ")]
        [TestCase("1.0.0\n")]
        [TestCase("-1.0.0")]
        [TestCase("1.0.0-ä")]
        [TestCase("１.0.0")]
        [TestCase("99999999999.0.0")]
        public void InvalidVersionsAreRejected(string s)
        {
            Assert.That(SemVer.TryParse(s, out _), Is.False);
        }

        [Test]
        public void PrecedenceFollowsTheSpecification()
        {
            string[] ordered =
            {
                "1.0.0-alpha", "1.0.0-alpha.1", "1.0.0-alpha.beta", "1.0.0-beta", "1.0.0-beta.2", "1.0.0-beta.11",
                "1.0.0-rc.1", "1.0.0", "1.0.1", "1.1.0", "1.9.0", "1.10.0", "2.0.0",
            };
            for (int i = 0; i + 1 < ordered.Length; i++)
            {
                Assert.That(Parse(ordered[i]).CompareTo(Parse(ordered[i + 1])), Is.LessThan(0), ordered[i] + " < " + ordered[i + 1]);
                Assert.That(Parse(ordered[i + 1]).CompareTo(Parse(ordered[i])), Is.GreaterThan(0), ordered[i + 1] + " > " + ordered[i]);
            }
        }

        [Test]
        public void BuildMetadataIsIgnoredForPrecedence()
        {
            Assert.That(Parse("1.0.0+a").CompareTo(Parse("1.0.0+b")), Is.EqualTo(0));
            Assert.That(Parse("1.0.0-rc.1+a").CompareTo(Parse("1.0.0-rc.1")), Is.EqualTo(0));
        }

        [Test]
        public void HugeNumericPreReleaseIdentifiersCompareWithoutOverflow()
        {
            Assert.That(Parse("1.0.0-99999999999999999999").CompareTo(Parse("1.0.0-100000000000000000000")), Is.LessThan(0));
        }

        [Test]
        public void DefaultIsZeroWithEmptyParts()
        {
            SemVer v = default;

            Assert.That(v.PreRelease, Is.EqualTo(""));
            Assert.That(v.CompareTo(Parse("0.0.0")), Is.EqualTo(0));
        }
    }
}
