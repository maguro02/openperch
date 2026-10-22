using System;
using NUnit.Framework;
using OpenPerch.Format;

namespace OpenPerch.Core.Tests.Format
{
    [Category("T-VER")]
    public class UnityVersionTests
    {
        private static UnityVersion Parse(string s)
        {
            Assert.That(UnityVersion.TryParse(s, out UnityVersion v), Is.True, s);
            return v;
        }

        [Test]
        public void ParsesEveryPart()
        {
            UnityVersion v = Parse("2022.3.22f1");

            Assert.That(v.Major, Is.EqualTo(2022));
            Assert.That(v.Minor, Is.EqualTo(3));
            Assert.That(v.Patch, Is.EqualTo(22));
            Assert.That(v.Kind, Is.EqualTo('f'));
            Assert.That(v.Build, Is.EqualTo(1));
            Assert.That(v.ToString(), Is.EqualTo("2022.3.22f1"));
        }

        [Test]
        public void PatchOrdersNumerically()
        {
            Assert.That(Parse("2022.3.22f1").CompareTo(Parse("2022.3.62f3")), Is.LessThan(0));
            Assert.That(Parse("2022.3.62f3").CompareTo(Parse("2022.3.22f1")), Is.GreaterThan(0));
            Assert.That(Parse("2022.3.9f1").CompareTo(Parse("2022.3.10f1")), Is.LessThan(0));
            Assert.That(Parse("2022.3.62f2").CompareTo(Parse("2022.3.62f3")), Is.LessThan(0));
        }

        [Test]
        public void ChinaSuffixParsesAndIsIgnoredForOrdering()
        {
            UnityVersion china = Parse("2022.3.22f1c1");

            Assert.That(china.CompareTo(Parse("2022.3.22f1")), Is.EqualTo(0));
            Assert.That(china.CompareTo(Parse("2022.3.62f3")), Is.LessThan(0));
            Assert.That(china.ToString(), Is.EqualTo("2022.3.22f1"));
        }

        [Test]
        public void KindsOrderAlphaBetaFinalPatch()
        {
            UnityVersion a = Parse("2022.3.22a5");
            UnityVersion b = Parse("2022.3.22b1");
            UnityVersion f = Parse("2022.3.22f1");
            UnityVersion p = Parse("2022.3.22p1");

            Assert.That(a.CompareTo(b), Is.LessThan(0));
            Assert.That(b.CompareTo(f), Is.LessThan(0));
            Assert.That(f.CompareTo(p), Is.LessThan(0));
            Assert.That(p.CompareTo(a), Is.GreaterThan(0));
        }

        [Test]
        public void BuildOrdersWithinAKind()
        {
            Assert.That(Parse("2022.3.22f1").CompareTo(Parse("2022.3.22f2")), Is.LessThan(0));
            Assert.That(Parse("2022.3.22f10").CompareTo(Parse("2022.3.22f9")), Is.GreaterThan(0));
        }

        [Test]
        public void Unity6MinorVersionsShareAStream()
        {
            Assert.That(Parse("6000.0.23f1").SameStream(Parse("6000.3.0f1")), Is.True);
            Assert.That(Parse("6000.3.0f1").SameStream(Parse("6000.0.23f1")), Is.True);
        }

        [Test]
        public void BeforeUnity6TheMinorVersionIsPartOfTheStream()
        {
            Assert.That(Parse("2022.3.22f1").SameStream(Parse("2022.3.62f3")), Is.True);
            Assert.That(Parse("2022.2.0f1").SameStream(Parse("2022.3.0f1")), Is.False);
            Assert.That(Parse("2021.3.45f1").SameStream(Parse("2022.3.22f1")), Is.False);
            Assert.That(Parse("2022.3.22f1").SameStream(Parse("6000.0.23f1")), Is.False);
        }

        [TestCase(null)]
        [TestCase("")]
        [TestCase("abc")]
        [TestCase("2022.3")]
        [TestCase("2022.3.22")]
        [TestCase("2022.3.22f")]
        [TestCase("2022.3.22x1")]
        [TestCase("2022.3.22F1")]
        [TestCase("2022.3.22f1c")]
        [TestCase("2022.3.22f1-dev")]
        [TestCase(" 2022.3.22f1")]
        [TestCase("2022.3.22f1 ")]
        [TestCase("2022.3.22f1\n")]
        [TestCase("2022.-3.22f1")]
        [TestCase("+2022.3.22f1")]
        [TestCase("２０２２.3.22f1")]
        [TestCase("99999999999.3.22f1")]
        [TestCase("2022.3.22f99999999999")]
        public void UnparsableStringsAreRejected(string s)
        {
            Assert.That(UnityVersion.TryParse(s, out UnityVersion v), Is.False);
            Assert.That(v.Major, Is.EqualTo(0));
        }

        [Test]
        public void ConstructorRejectsUnknownKinds()
        {
            Assert.Throws<ArgumentOutOfRangeException>(() => new UnityVersion(2022, 3, 22, 'x', 1));
            Assert.Throws<ArgumentOutOfRangeException>(() => new UnityVersion(-1, 3, 22, 'f', 1));
            Assert.That(new UnityVersion(2022, 3, 62, 'f', 3).CompareTo(Parse("2022.3.62f3")), Is.EqualTo(0));
        }
    }
}
