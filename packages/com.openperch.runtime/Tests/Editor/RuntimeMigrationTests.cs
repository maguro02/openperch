using System;
using NUnit.Framework;
using UnityEngine;

namespace OpenPerch.Runtime.Tests.Editor
{
    public class RuntimeMigrationTests
    {
        [TestCase(typeof(PerchPhysBone))]
        [TestCase(typeof(PerchPhysBoneCollider))]
        [TestCase(typeof(PerchConstraint))]
        [TestCase(typeof(PerchContactData))]
        public void OnAfterDeserializeUpgradesUnversionedDataToVersion1(Type componentType)
        {
            Assert.That(DeserializeWithVersion(componentType, 0), Is.EqualTo(1));
        }

        [TestCase(typeof(PerchPhysBone))]
        [TestCase(typeof(PerchPhysBoneCollider))]
        [TestCase(typeof(PerchConstraint))]
        [TestCase(typeof(PerchContactData))]
        public void OnAfterDeserializeKeepsNewerVersions(Type componentType)
        {
            Assert.That(DeserializeWithVersion(componentType, RuntimeSchema.Current + 1), Is.EqualTo(RuntimeSchema.Current + 1));
        }

        private static int DeserializeWithVersion(Type componentType, int schemaVersion)
        {
            var host = new GameObject(nameof(RuntimeMigrationTests));
            try
            {
                var component = host.AddComponent(componentType);
                var field = componentType.GetField("schemaVersion");
                field.SetValue(component, schemaVersion);

                ((ISerializationCallbackReceiver)component).OnAfterDeserialize();

                return (int)field.GetValue(component);
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(host);
            }
        }
    }
}
