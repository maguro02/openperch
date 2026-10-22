using System;

namespace OpenPerch.Core.Tests.Settings
{
    /// <summary>Sealed, but derived from another persisted class, which the rules forbid along with all polymorphism.</summary>
    [Serializable]
    public sealed class DerivedSample : OpenSample
    {
        public int extra;
    }
}
