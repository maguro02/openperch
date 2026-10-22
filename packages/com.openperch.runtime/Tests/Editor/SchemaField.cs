using System;

namespace OpenPerch.Runtime.Tests.Editor
{
    /// <summary>One serialized field of a <see cref="SchemaType"/>.</summary>
    [Serializable]
    public sealed class SchemaField
    {
        public string name = "";

        /// <summary>Full name of the field type, with generic arguments written as <c>List&lt;T&gt;</c>.</summary>
        public string type = "";
    }
}
