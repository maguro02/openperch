using System;

namespace OpenPerch.Core.Tests.Settings
{
    /// <summary>Has no public parameterless constructor, so a codec cannot create it with its initializer values.</summary>
    [Serializable]
    public sealed class NoDefaultConstructorSample
    {
        public int value;

        public NoDefaultConstructorSample(int value)
        {
            this.value = value;
        }
    }
}
