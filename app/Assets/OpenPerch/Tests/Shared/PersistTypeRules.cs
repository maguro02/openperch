using System;
using System.Collections.Generic;
using System.Reflection;

namespace OpenPerch.App.Tests.Shared
{
    /// <summary>
    /// Reflection check of the rules for persisted types. Every class or struct reachable from the roots through fields must
    /// be <c>[Serializable]</c>, keep its data in public, non-readonly fields of types both JsonUtility and System.Text.Json
    /// handle the same way (no dictionaries, sets, interfaces, <c>object</c> or nested collections), be a sealed class or a
    /// struct, and have no null reference fields in a default instance. Only instance members are inspected.
    /// </summary>
    public static class PersistTypeRules
    {
        private const BindingFlags DeclaredInstance =
            BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.DeclaredOnly;

        /// <summary>Returns one message per violation, in the form <c>Type.member: rule</c>; empty when every type complies.</summary>
        public static IReadOnlyList<string> Check(params Type[] roots)
        {
            return Analyze(roots, out _);
        }

        /// <summary>Returns the roots and every class or struct reachable from them through fields and collection elements.</summary>
        public static IReadOnlyList<Type> ReachableTypes(params Type[] roots)
        {
            Analyze(roots, out var types);
            return types;
        }

        private static List<string> Analyze(Type[] roots, out List<Type> types)
        {
            if (roots == null) throw new ArgumentNullException(nameof(roots));

            var violations = new List<string>();
            types = new List<Type>();
            var seen = new HashSet<Type>();
            var queue = new Queue<Type>(roots);
            while (queue.Count > 0)
            {
                var type = queue.Dequeue();
                if (!seen.Add(type)) continue;
                types.Add(type);
                CheckComposite(type, queue, violations);
            }
            return violations;
        }

        private static void CheckComposite(Type type, Queue<Type> queue, List<string> violations)
        {
            if (!type.IsClass && !(type.IsValueType && !type.IsPrimitive && !type.IsEnum))
            {
                violations.Add(type.Name + ": not a class or struct");
                return;
            }

            if (!type.IsDefined(typeof(SerializableAttribute), false)) violations.Add(type.Name + ": not [Serializable]");
            if (type.IsClass)
            {
                if (!type.IsSealed) violations.Add(type.Name + ": class is not sealed");
                if (type.BaseType != typeof(object)) violations.Add(type.Name + ": derives from " + type.BaseType?.Name);
            }

            foreach (var property in type.GetProperties(DeclaredInstance))
            {
                violations.Add(type.Name + "." + property.Name + ": property");
            }

            foreach (var field in type.GetFields(DeclaredInstance))
            {
                var prefix = type.Name + "." + field.Name + ": ";
                if (!field.IsPublic) violations.Add(prefix + "non-public field");
                if (field.IsInitOnly) violations.Add(prefix + "readonly field");
                var problem = CheckFieldType(field.FieldType, queue);
                if (problem != null) violations.Add(prefix + problem);
            }

            CheckDefaultInstance(type, violations);
        }

        private static string CheckFieldType(Type t, Queue<Type> queue)
        {
            if (IsScalar(t)) return null;
            if (t.IsArray)
            {
                return t.GetArrayRank() == 1 ? CheckElementType(t.GetElementType(), queue) : "multi-dimensional array";
            }
            if (t.IsGenericType)
            {
                return t.GetGenericTypeDefinition() == typeof(List<>)
                    ? CheckElementType(t.GetGenericArguments()[0], queue)
                    : "unsupported generic type " + t.Name;
            }
            if (t.IsInterface) return "interface type " + t.Name;
            if (t == typeof(object)) return "object type";
            if (t.IsPointer || t.IsByRef || IsSystemType(t)) return "unsupported type " + t.FullName;

            queue.Enqueue(t);
            return null;
        }

        private static string CheckElementType(Type element, Queue<Type> queue)
        {
            if (element.IsArray || (element.IsGenericType && element.GetGenericTypeDefinition() == typeof(List<>)))
            {
                return "nested collection";
            }
            return CheckFieldType(element, queue);
        }

        private static void CheckDefaultInstance(Type type, List<string> violations)
        {
            object instance;
            if (type.IsValueType)
            {
                instance = Activator.CreateInstance(type);
            }
            else
            {
                if (type.IsAbstract) return;
                if (type.GetConstructor(Type.EmptyTypes) == null)
                {
                    violations.Add(type.Name + ": no public parameterless constructor");
                    return;
                }
                try
                {
                    instance = Activator.CreateInstance(type);
                }
                catch (TargetInvocationException e)
                {
                    violations.Add(type.Name + ": constructor threw " + e.InnerException?.GetType().Name);
                    return;
                }
            }

            foreach (var field in type.GetFields(BindingFlags.Instance | BindingFlags.Public))
            {
                if (!field.FieldType.IsValueType && field.GetValue(instance) == null)
                {
                    violations.Add(type.Name + "." + field.Name + ": null in a default instance");
                }
            }
        }

        private static bool IsScalar(Type t) =>
            t == typeof(bool) || t == typeof(byte) || t == typeof(sbyte) || t == typeof(short) || t == typeof(ushort) ||
            t == typeof(int) || t == typeof(uint) || t == typeof(long) || t == typeof(ulong) ||
            t == typeof(float) || t == typeof(double) || t == typeof(string) || t.IsEnum;

        // char, decimal, DateTime, Guid and the like are written differently by the two codecs, or not at all by JsonUtility.
        private static bool IsSystemType(Type t) =>
            t.Namespace != null && (t.Namespace == "System" || t.Namespace.StartsWith("System.", StringComparison.Ordinal));
    }
}
