using System;

namespace OpenPerch.Runtime.Tests.Editor
{
    /// <summary>Serialized shape of the runtime assembly, as stored in <c>schema-v1.json</c>.</summary>
    [Serializable]
    public sealed class SchemaSnapshot
    {
        public int schemaVersion;
        public string assembly = "";
        public SchemaType[] types = Array.Empty<SchemaType>();
    }
}
