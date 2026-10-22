using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Reflection;

namespace OpenPerch.App.Tests.Shared
{
    /// <summary>Compares two persisted object graphs field by field through reflection.</summary>
    public static class PersistComparer
    {
        private const int MaxDepth = 32;

        /// <summary>
        /// Returns one message per differing value, such as <c>AppSettings.avatars[1].xNorm: expected 3.5 but was 0</c>;
        /// empty when the graphs are equal. Public instance fields are compared recursively, and arrays and lists element by
        /// element. Floats are compared exactly.
        /// </summary>
        public static IReadOnlyList<string> Differences(object expected, object actual)
        {
            var differences = new List<string>();
            var root = (expected ?? actual)?.GetType().Name ?? "value";
            Compare(expected, actual, root, differences, 0);
            return differences;
        }

        private static void Compare(object expected, object actual, string path, List<string> differences, int depth)
        {
            if (depth > MaxDepth) throw new InvalidOperationException("The object graph is too deep at " + path + "; is it cyclic?");

            if (expected == null || actual == null)
            {
                if (expected != null || actual != null) differences.Add(Mismatch(path, expected, actual));
                return;
            }

            var type = expected.GetType();
            if (type != actual.GetType())
            {
                differences.Add(path + ": expected type " + type.Name + " but was " + actual.GetType().Name);
                return;
            }

            if (type.IsPrimitive || type.IsEnum || type == typeof(string) || type == typeof(decimal))
            {
                if (!expected.Equals(actual)) differences.Add(Mismatch(path, expected, actual));
                return;
            }

            if (expected is IList expectedList)
            {
                var actualList = (IList)actual;
                if (expectedList.Count != actualList.Count)
                {
                    differences.Add(path + ": expected " + Count(expectedList) + " items but was " + Count(actualList));
                    return;
                }
                for (var i = 0; i < expectedList.Count; i++)
                {
                    var itemPath = path + "[" + i.ToString(CultureInfo.InvariantCulture) + "]";
                    Compare(expectedList[i], actualList[i], itemPath, differences, depth + 1);
                }
                return;
            }

            foreach (var field in type.GetFields(BindingFlags.Instance | BindingFlags.Public).OrderBy(f => f.MetadataToken))
            {
                Compare(field.GetValue(expected), field.GetValue(actual), path + "." + field.Name, differences, depth + 1);
            }
        }

        private static string Mismatch(string path, object expected, object actual) =>
            path + ": expected " + Describe(expected) + " but was " + Describe(actual);

        private static string Count(IList list) => list.Count.ToString(CultureInfo.InvariantCulture);

        private static string Describe(object value)
        {
            if (value == null) return "null";
            if (value is string s) return "\"" + s + "\"";
            if (value is IList list) return "[" + Count(list) + " items]";
            return Convert.ToString(value, CultureInfo.InvariantCulture);
        }
    }
}
