using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Reflection;

namespace OpenPerch.App.Tests.Shared
{
    /// <summary>
    /// Builds a persisted object in which every public field, nested ones included, differs from a default instance, so a
    /// round trip that silently drops a field shows up as a difference. The values depend only on the type, which lets the
    /// dotnet and the Unity tests feed the same input to their codecs.
    /// </summary>
    /// <remarks>
    /// Integers beyond the 32-bit signed range, floats that are exact in binary, and strings with quotes, backslashes and
    /// non-ASCII characters are used so that truncation, rounding and escaping bugs surface too. Collections get two elements.
    /// </remarks>
    public static class PersistFiller
    {
        private const int MaxDepth = 16;
        private const int CollectionLength = 2;

        /// <summary>Returns a new <typeparamref name="T"/> with every field filled.</summary>
        public static T Fill<T>() where T : class, new()
        {
            return (T)Fill(typeof(T));
        }

        /// <summary>Returns a new instance of <paramref name="type"/> (a class or struct) with every field filled.</summary>
        public static object Fill(Type type)
        {
            if (type == null) throw new ArgumentNullException(nameof(type));
            var seed = 0;
            return FillComposite(type, ref seed, 0);
        }

        private static object FillComposite(Type type, ref int seed, int depth)
        {
            if (depth > MaxDepth) throw new InvalidOperationException("The field graph of " + type.Name + " is too deep; is it recursive?");

            var instance = Activator.CreateInstance(type);
            var defaults = Activator.CreateInstance(type);
            foreach (var field in type.GetFields(BindingFlags.Instance | BindingFlags.Public).OrderBy(f => f.MetadataToken))
            {
                field.SetValue(instance, MakeValue(field.FieldType, field.GetValue(defaults), ref seed, depth + 1));
            }
            return instance;
        }

        private static object MakeValue(Type type, object defaultValue, ref int seed, int depth)
        {
            var n = ++seed;
            if (type == typeof(bool)) return !(bool)defaultValue;
            if (IsNumber(type))
            {
                var value = Number(type, n);
                return value.Equals(defaultValue) ? Number(type, n + 1) : value;
            }
            if (type == typeof(string)) return "v" + n.ToString(CultureInfo.InvariantCulture) + " \"q\" \\ 値";
            if (type.IsEnum) return OtherEnumValue(type, defaultValue, n);

            if (type.IsArray)
            {
                var elementType = type.GetElementType();
                var array = Array.CreateInstance(elementType, CollectionLength);
                for (var i = 0; i < CollectionLength; i++) array.SetValue(MakeElement(elementType, ref seed, depth), i);
                return array;
            }
            if (type.IsGenericType && type.GetGenericTypeDefinition() == typeof(List<>))
            {
                var elementType = type.GetGenericArguments()[0];
                var list = (IList)Activator.CreateInstance(type);
                for (var i = 0; i < CollectionLength; i++) list.Add(MakeElement(elementType, ref seed, depth));
                return list;
            }
            if (type.IsPrimitive || IsSystemType(type) || type.IsInterface || type.IsAbstract)
            {
                throw new NotSupportedException("Cannot fill a field of type " + type.FullName + ".");
            }
            return FillComposite(type, ref seed, depth);
        }

        private static bool IsSystemType(Type t) =>
            t.Namespace != null && (t.Namespace == "System" || t.Namespace.StartsWith("System.", StringComparison.Ordinal));

        private static object MakeElement(Type elementType, ref int seed, int depth)
        {
            var defaultValue = elementType.IsValueType ? Activator.CreateInstance(elementType) : null;
            return MakeValue(elementType, defaultValue, ref seed, depth + 1);
        }

        private static bool IsNumber(Type t) =>
            t == typeof(byte) || t == typeof(sbyte) || t == typeof(short) || t == typeof(ushort) || t == typeof(int) ||
            t == typeof(uint) || t == typeof(long) || t == typeof(ulong) || t == typeof(float) || t == typeof(double);

        private static object Number(Type type, int n)
        {
            if (type == typeof(byte)) return (byte)(100 + n % 100);
            if (type == typeof(sbyte)) return (sbyte)(-1 - n % 100);
            if (type == typeof(short)) return (short)(1000 + n % 1000);
            if (type == typeof(ushort)) return (ushort)(40000 + n % 1000);
            if (type == typeof(int)) return 1000 + n;
            if (type == typeof(uint)) return 3000000000u + (uint)n;
            if (type == typeof(long)) return 5000000000L + n;
            if (type == typeof(ulong)) return 10000000000000000000UL + (ulong)n;
            if (type == typeof(float)) return n + 0.5f;
            return n + 0.25;
        }

        private static object OtherEnumValue(Type type, object defaultValue, int n)
        {
            var values = Enum.GetValues(type);
            for (var i = 0; i < values.Length; i++)
            {
                var candidate = values.GetValue((n + i) % values.Length);
                if (!candidate.Equals(defaultValue)) return candidate;
            }
            throw new NotSupportedException(type.Name + " has no value other than its default.");
        }
    }
}
