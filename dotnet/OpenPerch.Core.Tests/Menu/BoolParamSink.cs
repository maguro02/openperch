using System.Collections.Generic;
using OpenPerch.App.Core;

namespace OpenPerch.Core.Tests.Menu
{
    /// <summary>Stores every parameter the way an animator Bool holds it: any value other than 0 reads back as 1.</summary>
    public sealed class BoolParamSink : IParamSink
    {
        private readonly Dictionary<string, float> _values = new Dictionary<string, float>();

        public void Set(string name, float value) => _values[name] = value != 0f ? 1f : 0f;

        public bool TryGet(string name, out float value) => _values.TryGetValue(name, out value);
    }
}
