using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using OpenPerch.App.Core;
using OpenPerch.App.Tests.Shared;

namespace OpenPerch.Core.Tests.Settings
{
    [Category("T-SET")]
    public class PersistTypeRulesTests
    {
        [Test]
        public void PersistedTypesFollowTheRules()
        {
            var violations = PersistTypeRules.Check(typeof(AppSettings), typeof(LibraryIndex), typeof(LocaleFile));

            Assert.That(violations, Is.Empty);
        }

        [Test]
        public void WalkReachesEveryNestedType()
        {
            var types = PersistTypeRules.ReachableTypes(typeof(AppSettings), typeof(LibraryIndex), typeof(LocaleFile));

            var expected = new[]
            {
                typeof(AppSettings), typeof(AvatarPlacement), typeof(SavedParam), typeof(PomodoroParamBinding),
                typeof(PomodoroConfig), typeof(NotificationSettings), typeof(BehaviorConfig), typeof(DynamicsMotionSettings),
                typeof(Vec2I), typeof(LibraryIndex), typeof(LibraryEntry), typeof(LocaleFile), typeof(LocaleEntry),
            };
            Assert.That(expected.Except(types), Is.Empty);
        }

        [TestCase("RuleBreakingSample.frozen: readonly field")]
        [TestCase("RuleBreakingSample.lookup: unsupported generic type Dictionary`2")]
        [TestCase("RuleBreakingSample.abstractList: interface type IList`1")]
        [TestCase("OpenSample: class is not sealed")]
        [TestCase("UnserializableSample: not [Serializable]")]
        [TestCase("FrozenPoint.x: readonly field")]
        [TestCase("RuleBreakingSample.when: unsupported type System.DateTime")]
        [TestCase("RuleBreakingSample.anything: object type")]
        [TestCase("RuleBreakingSample.nested: nested collection")]
        [TestCase("RuleBreakingSample.missing: null in a default instance")]
        [TestCase("RuleBreakingSample._hidden: non-public field")]
        [TestCase("RuleBreakingSample.Count: property")]
        public void EachRuleIsEnforced(string violation)
        {
            var violations = PersistTypeRules.Check(typeof(RuleBreakingSample));

            Assert.That(violations, Has.Member(violation), string.Join("\n", violations));
        }

        [Test]
        public void StaticMembersAreIgnored()
        {
            Assert.That(PersistTypeRules.Check(typeof(StaticMembersSample)), Is.Empty);
        }

        [Test]
        public void ReportsEachViolationOnce()
        {
            var violations = PersistTypeRules.Check(typeof(RuleBreakingSample), typeof(RuleBreakingSample));

            var repeated = violations.GroupBy(v => v).Where(g => g.Count() > 1).Select(g => g.Key);
            Assert.That(repeated, Is.Empty);
            Assert.That(violations, Is.Not.Empty);
        }

        [Test]
        public void NonCompositeRootIsReported()
        {
            var violations = PersistTypeRules.Check(typeof(PomodoroMode));

            Assert.That(violations, Is.EqualTo(new List<string> { "PomodoroMode: not a class or struct" }));
        }
    }
}
