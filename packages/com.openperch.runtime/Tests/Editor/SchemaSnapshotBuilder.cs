using System;
using System.Globalization;
using System.Linq;
using System.Reflection;
using UnityEngine;

namespace OpenPerch.Runtime.Tests.Editor
{
    /// <summary>Describes the public types of an assembly by reflection, in the shape of <see cref="SchemaSnapshot"/>.</summary>
    public static class SchemaSnapshotBuilder
    {
        public static SchemaSnapshot Build(Assembly assembly, int schemaVersion)
        {
            return new SchemaSnapshot
            {
                schemaVersion = schemaVersion,
                assembly = assembly.GetName().Name,
                types = assembly.GetExportedTypes()
                    .OrderBy(t => t.FullName, StringComparer.Ordinal)
                    .Select(Describe)
                    .ToArray(),
            };
        }

        public static SchemaType Describe(Type type)
        {
            return new SchemaType
            {
                name = type.FullName,
                kind = KindOf(type),
                baseType = type.BaseType == null ? "" : TypeName(type.BaseType),
                interfaces = OwnInterfaces(type),
                fields = SerializedFields(type),
                enumValues = EnumValues(type),
            };
        }

        /// <summary>Full name without assembly qualifiers, so the snapshot does not change with assembly versions.</summary>
        public static string TypeName(Type type)
        {
            if (type.IsArray)
            {
                return TypeName(type.GetElementType()) + "[" + new string(',', type.GetArrayRank() - 1) + "]";
            }

            if (type.IsGenericType)
            {
                var definition = type.GetGenericTypeDefinition().FullName;
                var arity = definition.IndexOf('`');
                var arguments = type.GetGenericArguments().Select(TypeName);
                return definition.Substring(0, arity) + "<" + string.Join(", ", arguments) + ">";
            }

            return type.FullName;
        }

        private static string KindOf(Type type)
        {
            if (type.IsEnum)
            {
                return "enum";
            }

            if (type.IsInterface)
            {
                return "interface";
            }

            if (type.IsValueType)
            {
                return "struct";
            }

            return type.IsAbstract && type.IsSealed ? "static class" : "class";
        }

        private static string[] OwnInterfaces(Type type)
        {
            var inherited = type.BaseType == null ? Type.EmptyTypes : type.BaseType.GetInterfaces();
            return type.GetInterfaces()
                .Except(inherited)
                .Select(TypeName)
                .OrderBy(n => n, StringComparer.Ordinal)
                .ToArray();
        }

        private static SchemaField[] SerializedFields(Type type)
        {
            if (type.IsEnum || type.IsInterface)
            {
                return Array.Empty<SchemaField>();
            }

            const BindingFlags Declared = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.DeclaredOnly;
            return type.GetFields(Declared)
                .Where(IsSerialized)
                .Select(f => new SchemaField { name = f.Name, type = TypeName(f.FieldType) })
                .ToArray();
        }

        // Mirrors which instance fields Unity's serializer writes, so the snapshot covers exactly what bundles carry.
        private static bool IsSerialized(FieldInfo field)
        {
            return !field.IsInitOnly && !field.IsNotSerialized &&
                (field.IsPublic || field.IsDefined(typeof(SerializeField), false));
        }

        private static SchemaEnumValue[] EnumValues(Type type)
        {
            if (!type.IsEnum)
            {
                return Array.Empty<SchemaEnumValue>();
            }

            return type.GetFields(BindingFlags.Public | BindingFlags.Static)
                .Select(f => new SchemaEnumValue
                {
                    name = f.Name,
                    value = Convert.ToInt32(f.GetRawConstantValue(), CultureInfo.InvariantCulture),
                })
                .ToArray();
        }
    }
}
