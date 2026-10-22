using System.IO;
using OpenPerch.App.Core;

namespace OpenPerch.Core.Tests.Settings
{
    /// <summary>Wraps a codec and throws <see cref="IOException"/> from the first few <see cref="Serialize{T}"/> calls.</summary>
    public sealed class FailingCodec : ITextCodec
    {
        private readonly ITextCodec _inner;
        private int _failuresLeft;

        public FailingCodec(ITextCodec inner, int failures)
        {
            _inner = inner;
            _failuresLeft = failures;
        }

        public int SerializeCalls { get; private set; }

        public string Serialize<T>(T value) where T : class
        {
            SerializeCalls++;
            if (_failuresLeft > 0)
            {
                _failuresLeft--;
                throw new IOException("Simulated write failure.");
            }
            return _inner.Serialize(value);
        }

        public T Deserialize<T>(string text) where T : class, new() => _inner.Deserialize<T>(text);
    }
}
