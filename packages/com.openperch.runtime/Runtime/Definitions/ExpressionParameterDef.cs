using System;

namespace OpenPerch.Runtime
{
    /// <summary>One entry of the avatar's Expression Parameters.</summary>
    [Serializable]
    public sealed class ExpressionParameterDef
    {
        public string name;
        public ExprParamType type;
        public float defaultValue;
        public bool saved;
        public bool synced;
    }
}
