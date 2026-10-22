using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using NUnit.Framework;
using UnityEngine;

namespace OpenPerch.Runtime.Tests.Editor
{
    /// <summary>
    /// Checks the IL of the only code a loaded bundle makes the runtime types run: the serialization callbacks, the
    /// migrations they hand the object to, and the state machine behaviours' enter and exit.
    /// </summary>
    public class RuntimeCallbackTests
    {
        private const BindingFlags DeclaredMethods = BindingFlags.Instance | BindingFlags.Static |
            BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.DeclaredOnly;

        private static Assembly RuntimeAssembly => typeof(RuntimeSchema).Assembly;

        [Test]
        [Category("T-SCHEMA")]
        public void OnBeforeSerializeCallsNothing()
        {
            var receivers = SerializationReceivers();
            Assert.That(receivers, Is.Not.Empty, "No runtime type implements ISerializationCallbackReceiver.");

            var problems = receivers
                .Select(t => Implementation(t, nameof(ISerializationCallbackReceiver.OnBeforeSerialize)))
                .SelectMany(m => Calls(m).Select(c => Describe(m) + " calls " + Describe(c)))
                .ToList();

            Assert.That(problems, Is.Empty, "OnBeforeSerialize must do nothing:\n" + string.Join("\n", problems));
        }

        [Test]
        [Category("T-SCHEMA")]
        public void OnAfterDeserializeOnlyCallsTheMigrationOfItsType()
        {
            var receivers = SerializationReceivers();
            Assert.That(receivers, Is.Not.Empty, "No runtime type implements ISerializationCallbackReceiver.");

            var problems = new List<string>();
            foreach (var type in receivers)
            {
                var method = Implementation(type, nameof(ISerializationCallbackReceiver.OnAfterDeserialize));
                var calls = Calls(method).ToList();
                problems.AddRange(calls.Where(c => !IsMigrationOf(c, type)).Select(c => Describe(method) + " calls " + Describe(c)));
                if (!calls.Any(c => IsMigrationOf(c, type)))
                {
                    problems.Add(Describe(method) + " does not call RuntimeMigration.Migrate(" + type.Name + ")");
                }
            }

            Assert.That(problems, Is.Empty,
                "OnAfterDeserialize must only hand the object to its RuntimeMigration.Migrate overload:\n" + string.Join("\n", problems));
        }

        [Test]
        [Category("T-SCHEMA")]
        public void MigrationsCallNothingLoopNowhereAndUseNoStaticField()
        {
            var methods = typeof(RuntimeMigration).GetMethods(DeclaredMethods);
            Assert.That(methods, Is.Not.Empty, "RuntimeMigration declares no method.");

            var problems = new List<string>();
            foreach (var method in methods)
            {
                foreach (var instruction in IlReader.Read(method))
                {
                    if (IsCall(instruction))
                    {
                        problems.Add(Describe(method) + " calls " + Describe(instruction.Method));
                    }

                    if (instruction.BranchTargets.Any(target => target <= instruction.Offset))
                    {
                        problems.Add(Describe(method) + " jumps backwards at IL_" +
                            instruction.Offset.ToString("X4", CultureInfo.InvariantCulture));
                    }

                    if (instruction.Field != null && instruction.Field.IsStatic)
                    {
                        problems.Add(Describe(method) + " uses the static field " + instruction.Field.DeclaringType.FullName + "." +
                            instruction.Field.Name);
                    }
                }
            }

            // Unity may deserialize on a loading thread while the rest of the bundle is still being read, so a migration
            // has to finish in bounded time and must neither call into Unity nor share state with anything else.
            Assert.That(problems, Is.Empty,
                "Migrations must not call anything, loop or use static fields:\n" + string.Join("\n", problems));
        }

        [Test]
        [Category("T-SCHEMA")]
        public void StateMachineBehavioursOnlyForwardToTheHandler()
        {
            var methods = RuntimeAssembly.GetTypes()
                .Where(t => typeof(StateMachineBehaviour).IsAssignableFrom(t))
                .SelectMany(t => t.GetMethods(DeclaredMethods))
                .Where(m => m.Name == "OnStateEnter" || m.Name == "OnStateExit")
                .ToList();
            Assert.That(methods, Is.Not.Empty, "No runtime state machine behaviour overrides OnStateEnter or OnStateExit.");

            var problems = methods
                .SelectMany(m => Calls(m).Where(c => !IsForwarding(c)).Select(c => Describe(m) + " calls " + Describe(c)))
                .ToList();

            Assert.That(problems, Is.Empty,
                "State machine behaviours may only look up IPerchSmbHandler on the Animator and call it:\n" + string.Join("\n", problems));
        }

        private static List<Type> SerializationReceivers()
        {
            return RuntimeAssembly.GetTypes()
                .Where(t => t.IsClass && !t.IsAbstract && typeof(ISerializationCallbackReceiver).IsAssignableFrom(t))
                .ToList();
        }

        /// <summary>The method that implements <paramref name="callback"/> for <paramref name="type"/>, however it is declared.</summary>
        private static MethodInfo Implementation(Type type, string callback)
        {
            var map = type.GetInterfaceMap(typeof(ISerializationCallbackReceiver));
            return map.TargetMethods[Array.FindIndex(map.InterfaceMethods, m => m.Name == callback)];
        }

        /// <summary>Every method <paramref name="method"/> calls, constructs with or takes a pointer to; null for a calli.</summary>
        private static IEnumerable<MethodBase> Calls(MethodBase method)
        {
            return IlReader.Read(method).Where(IsCall).Select(i => i.Method);
        }

        private static bool IsCall(IlInstruction instruction)
        {
            return instruction.Method != null || instruction.OpCode == OpCodes.Calli;
        }

        private static bool IsMigrationOf(MethodBase callee, Type type)
        {
            if (callee == null || callee.DeclaringType != typeof(RuntimeMigration) || callee.Name != nameof(RuntimeMigration.Migrate))
            {
                return false;
            }

            var parameters = callee.GetParameters();
            return parameters.Length == 1 && parameters[0].ParameterType == type;
        }

        private static bool IsForwarding(MethodBase callee)
        {
            if (callee == null)
            {
                return false;
            }

            if (callee.DeclaringType == typeof(IPerchSmbHandler))
            {
                return true;
            }

            return callee.DeclaringType == typeof(Component) && callee.Name == nameof(Component.TryGetComponent) &&
                callee.IsGenericMethod && callee.GetGenericArguments().SequenceEqual(new[] { typeof(IPerchSmbHandler) });
        }

        private static string Describe(MethodBase method)
        {
            if (method == null)
            {
                return "a function pointer (calli)";
            }

            var name = method.DeclaringType == null ? method.Name : method.DeclaringType.FullName + "." + method.Name;
            if (method.IsGenericMethod)
            {
                name += "<" + string.Join(", ", method.GetGenericArguments().Select(a => a.Name)) + ">";
            }

            return name + "(" + string.Join(", ", method.GetParameters().Select(p => p.ParameterType.Name)) + ")";
        }
    }
}
