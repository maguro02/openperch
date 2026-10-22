using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using NUnit.Framework;
using OpenPerch.App.Core;
using OpenPerch.App.Tests.Shared;

namespace OpenPerch.Core.Tests.Settings
{
    [Category("T-SET")]
    public class PersistFillerTests
    {
        [TestCase(typeof(AppSettings))]
        [TestCase(typeof(LibraryIndex))]
        [TestCase(typeof(LocaleFile))]
        public void EveryFieldDiffersFromTheDefault(Type type)
        {
            var unchanged = new List<string>();

            CollectUnchanged(PersistFiller.Fill(type), Activator.CreateInstance(type), type.Name, unchanged);

            Assert.That(unchanged, Is.Empty);
        }

        [Test]
        public void SameTypeGivesTheSameValues()
        {
            Assert.That(PersistComparer.Differences(PersistFiller.Fill<AppSettings>(), PersistFiller.Fill<AppSettings>()), Is.Empty);
        }

        [Test]
        public void CollectionsGetTwoFilledElements()
        {
            var settings = PersistFiller.Fill<AppSettings>();

            Assert.That(settings.avatars, Has.Count.EqualTo(2));
            Assert.That(settings.avatars[0].exportId, Is.Not.EqualTo(settings.avatars[1].exportId));
            Assert.That(settings.avatars[1].savedParams, Has.Count.EqualTo(2));
            Assert.That(PersistFiller.Fill<LibraryIndex>().entries[0].warningCodes, Has.Length.EqualTo(2));
        }

        private static void CollectUnchanged(object filled, object defaults, string path, List<string> unchanged)
        {
            foreach (var field in filled.GetType().GetFields(BindingFlags.Instance | BindingFlags.Public))
            {
                var fieldPath = path + "." + field.Name;
                var filledValue = field.GetValue(filled);
                var defaultValue = field.GetValue(defaults);
                var type = field.FieldType;

                if (filledValue is IList list)
                {
                    if (list.Count == 0)
                    {
                        unchanged.Add(fieldPath);
                        continue;
                    }
                    var elementType = type.IsArray ? type.GetElementType() : type.GetGenericArguments()[0];
                    if (IsComposite(elementType))
                    {
                        CollectUnchanged(list[0], Activator.CreateInstance(elementType), fieldPath + "[0]", unchanged);
                    }
                }
                else if (IsComposite(type))
                {
                    CollectUnchanged(filledValue, defaultValue, fieldPath, unchanged);
                }
                else if (Equals(filledValue, defaultValue))
                {
                    unchanged.Add(fieldPath);
                }
            }
        }

        private static bool IsComposite(Type t) => !t.IsPrimitive && !t.IsEnum && t != typeof(string);
    }
}
