using System;

namespace OpenPerch.Core.Tests.Settings
{
    /// <summary>Not sealed, so a field of this type could hold a subclass that the codecs would cut down to this type.</summary>
    [Serializable]
    public class OpenSample
    {
        public int value;
    }
}
