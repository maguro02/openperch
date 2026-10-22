using System;

namespace OpenPerch.Runtime.Tests.Editor
{
    /// <summary>One member of an enum in a <see cref="SchemaSnapshot"/>.</summary>
    [Serializable]
    public sealed class SchemaEnumValue
    {
        public string name = "";
        public int value;
    }
}
