using System;

namespace OpenPerch.Runtime
{
    /// <summary>One operation of a <see cref="PerchParameterDriver"/>.</summary>
    [Serializable]
    public sealed class DriverEntry
    {
        public DriverOp type;
        public string name = "";
        public string source = "";
        public float value;
        public float valueMin;
        public float valueMax;
        public float chance;
        public bool convertRange;
        public float sourceMin;
        public float sourceMax;
        public float destMin;
        public float destMax;
        public bool preventRepeats;
    }
}
