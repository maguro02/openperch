using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UnityEngine.Animations;

namespace OpenPerch.Runtime.Tests.Editor
{
    public class RuntimeSchemaTests
    {
        private const string SnapshotPath = "Packages/com.openperch.runtime/Tests/Editor/schema-v1.json";

        private const BindingFlags DeclaredMethods = BindingFlags.Instance | BindingFlags.Static |
            BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.DeclaredOnly;

        // Messages Unity sends to MonoBehaviours and ScriptableObjects. Any of them on a runtime type would run code
        // from an untrusted bundle as soon as it is loaded or instantiated.
        private static readonly HashSet<string> UnityMessages = new HashSet<string>(StringComparer.Ordinal)
        {
            "Awake", "Start", "Update", "FixedUpdate", "LateUpdate", "OnEnable", "OnDisable", "OnDestroy", "OnValidate", "Reset",
            "OnGUI", "OnApplicationFocus", "OnApplicationPause", "OnApplicationQuit", "OnLevelWasLoaded",
            "OnAnimatorIK", "OnAnimatorMove", "OnAudioFilterRead", "OnDidApplyAnimationProperties",
            "OnBecameInvisible", "OnBecameVisible", "OnBeforeTransformParentChanged", "OnTransformChildrenChanged",
            "OnTransformParentChanged", "OnCanvasGroupChanged", "OnCanvasHierarchyChanged", "OnRectTransformDimensionsChange",
            "OnRectTransformRemoved",
            "OnCollisionEnter", "OnCollisionEnter2D", "OnCollisionExit", "OnCollisionExit2D", "OnCollisionStay", "OnCollisionStay2D",
            "OnTriggerEnter", "OnTriggerEnter2D", "OnTriggerExit", "OnTriggerExit2D", "OnTriggerStay", "OnTriggerStay2D",
            "OnControllerColliderHit", "OnJointBreak", "OnJointBreak2D",
            "OnMouseDown", "OnMouseDrag", "OnMouseEnter", "OnMouseExit", "OnMouseOver", "OnMouseUp", "OnMouseUpAsButton",
            "OnParticleCollision", "OnParticleSystemStopped", "OnParticleTrigger", "OnParticleUpdateJobScheduled",
            "OnPreCull", "OnPreRender", "OnPostRender", "OnRenderImage", "OnRenderObject", "OnWillRenderObject",
            "OnDrawGizmos", "OnDrawGizmosSelected",
            "OnServerInitialized", "OnConnectedToServer", "OnDisconnectedFromServer", "OnFailedToConnect",
            "OnFailedToConnectToMasterServer", "OnMasterServerEvent", "OnNetworkInstantiate", "OnPlayerConnected",
            "OnPlayerDisconnected", "OnSerializeNetworkView",
        };

        private static readonly HashSet<string> StateMachineMessages = new HashSet<string>(StringComparer.Ordinal)
        {
            "OnStateEnter", "OnStateExit", "OnStateUpdate", "OnStateMove", "OnStateIK", "OnStateMachineEnter", "OnStateMachineExit",
        };

        private static Assembly RuntimeAssembly => typeof(RuntimeSchema).Assembly;

        [Test]
        [Category("T-SCHEMA")]
        public void SnapshotDescribesTheCurrentSchema()
        {
            var snapshot = LoadSnapshot();

            Assert.That(snapshot.assembly, Is.EqualTo(RuntimeAssembly.GetName().Name));
            Assert.That(snapshot.schemaVersion, Is.EqualTo(RuntimeSchema.Current),
                "RuntimeSchema.Current changed: record the new schema in its own snapshot and keep schema-v1.json frozen.");
        }

        [Test]
        [Category("T-SCHEMA")]
        public void FrozenTypesFieldsAndEnumValuesAreUnchanged()
        {
            var expected = LoadSnapshot();
            var actual = ActualTypes();
            var problems = new List<string>();

            foreach (var frozen in expected.types)
            {
                if (!actual.TryGetValue(frozen.name, out var current))
                {
                    problems.Add(frozen.name + ": type removed or renamed");
                    continue;
                }

                CompareShape(frozen, current, problems);

                var currentFields = current.fields.ToDictionary(f => f.name, f => f.type, StringComparer.Ordinal);
                foreach (var field in frozen.fields)
                {
                    if (!currentFields.TryGetValue(field.name, out var type))
                    {
                        problems.Add(frozen.name + "." + field.name + ": field removed or renamed");
                    }
                    else if (type != field.type)
                    {
                        problems.Add(frozen.name + "." + field.name + ": type changed from " + field.type + " to " + type);
                    }
                }

                var currentValues = current.enumValues.ToDictionary(v => v.name, v => v.value, StringComparer.Ordinal);
                foreach (var member in frozen.enumValues)
                {
                    if (!currentValues.TryGetValue(member.name, out var value))
                    {
                        problems.Add(frozen.name + "." + member.name + ": enum member removed or renamed");
                    }
                    else if (value != member.value)
                    {
                        problems.Add(frozen.name + "." + member.name + ": value changed from " + member.value + " to " + value);
                    }
                }
            }

            Assert.That(problems, Is.Empty, "The frozen serialization contract changed:\n" + string.Join("\n", problems));
        }

        [Test]
        [Category("T-SCHEMA")]
        public void EveryPublicTypeFieldAndEnumValueIsInTheSnapshot()
        {
            var expected = LoadSnapshot().types.ToDictionary(t => t.name, StringComparer.Ordinal);
            var problems = new List<string>();

            foreach (var current in ActualTypes().Values)
            {
                if (!expected.TryGetValue(current.name, out var frozen))
                {
                    problems.Add(current.name + ": type missing from the snapshot");
                    continue;
                }

                var frozenFields = new HashSet<string>(frozen.fields.Select(f => f.name), StringComparer.Ordinal);
                problems.AddRange(current.fields
                    .Where(f => !frozenFields.Contains(f.name))
                    .Select(f => current.name + "." + f.name + ": field missing from the snapshot"));

                var frozenMembers = new HashSet<string>(frozen.enumValues.Select(v => v.name), StringComparer.Ordinal);
                problems.AddRange(current.enumValues
                    .Where(v => !frozenMembers.Contains(v.name))
                    .Select(v => current.name + "." + v.name + ": enum member missing from the snapshot"));
            }

            Assert.That(problems, Is.Empty, "Additions must be recorded in the snapshot:\n" + string.Join("\n", problems));
        }

        [Test]
        [Category("T-SCHEMA")]
        public void RuntimeObjectsDeclareNoLifecycleMethods()
        {
            var problems = RuntimeAssembly.GetTypes()
                .Where(t => typeof(UnityEngine.Object).IsAssignableFrom(t))
                .SelectMany(t => t.GetMethods(DeclaredMethods)
                    .Where(m => UnityMessages.Contains(m.Name))
                    .Select(m => t.FullName + "." + m.Name))
                .ToList();

            Assert.That(problems, Is.Empty, "Runtime types must not have Unity lifecycle methods:\n" + string.Join("\n", problems));
        }

        [Test]
        [Category("T-SCHEMA")]
        public void StateMachineBehavioursOnlyForwardEnterAndExit()
        {
            var problems = RuntimeAssembly.GetTypes()
                .Where(t => typeof(StateMachineBehaviour).IsAssignableFrom(t))
                .SelectMany(t => t.GetMethods(DeclaredMethods)
                    .Where(m => StateMachineMessages.Contains(m.Name) && !IsPlayableEnterOrExit(m))
                    .Select(m => t.FullName + "." + m.Name + "(" + m.GetParameters().Length + " parameters)"))
                .ToList();

            Assert.That(problems, Is.Empty,
                "State machine behaviours may only override the AnimatorControllerPlayable overloads of OnStateEnter and OnStateExit:\n" +
                string.Join("\n", problems));
        }

        private static bool IsPlayableEnterOrExit(MethodInfo method)
        {
            if (method.Name != "OnStateEnter" && method.Name != "OnStateExit")
            {
                return false;
            }

            var parameters = method.GetParameters();
            return parameters.Length == 4 && parameters[3].ParameterType == typeof(AnimatorControllerPlayable);
        }

        private static void CompareShape(SchemaType frozen, SchemaType current, List<string> problems)
        {
            if (current.kind != frozen.kind)
            {
                problems.Add(frozen.name + ": kind changed from " + frozen.kind + " to " + current.kind);
            }

            if (current.baseType != frozen.baseType)
            {
                problems.Add(frozen.name + ": base type changed from " + frozen.baseType + " to " + current.baseType);
            }

            var frozenInterfaces = string.Join(", ", frozen.interfaces.OrderBy(i => i, StringComparer.Ordinal));
            var currentInterfaces = string.Join(", ", current.interfaces.OrderBy(i => i, StringComparer.Ordinal));
            if (currentInterfaces != frozenInterfaces)
            {
                problems.Add(frozen.name + ": interfaces changed from [" + frozenInterfaces + "] to [" + currentInterfaces + "]");
            }

            if (current.serializable != frozen.serializable)
            {
                problems.Add(frozen.name + ": [Serializable] " + (current.serializable ? "added" : "removed"));
            }
        }

        private static Dictionary<string, SchemaType> ActualTypes()
        {
            return SchemaSnapshotBuilder.Build(RuntimeAssembly, RuntimeSchema.Current).types
                .ToDictionary(t => t.name, StringComparer.Ordinal);
        }

        private static SchemaSnapshot LoadSnapshot()
        {
            var asset = AssetDatabase.LoadAssetAtPath<TextAsset>(SnapshotPath);
            Assert.That(asset, Is.Not.Null, SnapshotPath + " could not be loaded.");
            var snapshot = JsonUtility.FromJson<SchemaSnapshot>(asset.text);
            Assert.That(snapshot, Is.Not.Null, SnapshotPath + " is not a schema snapshot.");
            return snapshot;
        }
    }
}
