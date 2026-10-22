using System;
using System.IO;
using OpenPerch.App.Core;

namespace OpenPerch.Core.Tests.Settings
{
    /// <summary>Wraps a codec and throws from the first few <see cref="Serialize{T}"/> calls.</summary>
    public sealed class FailingCodec : ITextCodec
    {
        private readonly ITextCodec _inner;
        private readonly Func<Exception> _makeError;
        private int _failuresLeft;

        /// <param name="makeError">Creates the exception to throw; an <see cref="IOException"/> when null.</param>
        public FailingCodec(ITextCodec inner, int failures, Func<Exception> makeError = null)
        {
            _inner = inner;
            _failuresLeft = failures;
            _makeError = makeError ?? (() => new IOException("Simulated write failure."));
        }

        public int SerializeCalls { get; private set; }

        public string Serialize<T>(T value) where T : class
        {
            SerializeCalls++;
            if (_failuresLeft > 0)
            {
                _failuresLeft--;
                throw _makeError();
            }
            return _inner.Serialize(value);
        }

        public T Deserialize<T>(string text) where T : class, new() => _inner.Deserialize<T>(text);
    }
}
