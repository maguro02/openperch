using System;

namespace OpenPerch.Runtime.Tests.Editor
{
    /// <summary>One public type of a <see cref="SchemaSnapshot"/>.</summary>
    [Serializable]
    public sealed class SchemaType
    {
        /// <summary>Full name of the type.</summary>
        public string name = "";

        /// <summary>One of "class", "static class", "struct", "enum" or "interface".</summary>
        public string kind = "";

        /// <summary>Full name of the base type; empty for interfaces.</summary>
        public string baseType = "";

        /// <summary>Interfaces the type itself adds on top of those of its base type.</summary>
        public string[] interfaces = Array.Empty<string>();

        /// <summary>
        /// Whether the type is marked <c>[Serializable]</c>. Recorded for classes and structs that do not derive from
        /// <c>UnityEngine.Object</c>, because Unity silently skips fields of such a type without the attribute; false for
        /// every other type.
        /// </summary>
        public bool serializable;

        /// <summary>Fields Unity serializes, in declaration order.</summary>
        public SchemaField[] fields = Array.Empty<SchemaField>();

        public SchemaEnumValue[] enumValues = Array.Empty<SchemaEnumValue>();
    }
}
