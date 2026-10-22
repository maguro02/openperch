using System;

namespace OpenPerch.Core.Tests.Settings
{
    /// <summary>A readonly struct, whose fields JsonUtility skips without an error.</summary>
    [Serializable]
    public readonly struct FrozenPoint
    {
        public readonly int x;

        public FrozenPoint(int x)
        {
            this.x = x;
        }
    }
}
