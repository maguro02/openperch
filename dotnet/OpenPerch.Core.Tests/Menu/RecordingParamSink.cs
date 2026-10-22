using System.Collections.Generic;
using System.Globalization;
using OpenPerch.App.Core;

namespace OpenPerch.Core.Tests.Menu
{
    /// <summary>Accepts every parameter name, keeps the latest value of each and logs every write as "name=value".</summary>
    public sealed class RecordingParamSink : IParamSink
    {
        private readonly Dictionary<string, float> _values = new Dictionary<string, float>();

        public List<string> Writes { get; } = new List<string>();

        public void Set(string name, float value)
        {
            _values[name] = value;
            Writes.Add(name + "=" + value.ToString(CultureInfo.InvariantCulture));
        }

        public bool TryGet(string name, out float value) => _values.TryGetValue(name, out value);

        /// <exception cref="KeyNotFoundException">The parameter was never written.</exception>
        public float Get(string name) => _values[name];
    }
}
