using System;
using System.Text.Json;
using OpenPerch.App.Core;

namespace OpenPerch.Core.Tests.Support
{
    /// <summary>
    /// System.Text.Json implementation of <see cref="ITextCodec"/> used where JsonUtility is unavailable.
    /// </summary>
    public sealed class StjCodec : ITextCodec
    {
        private static readonly JsonSerializerOptions Options = new JsonSerializerOptions
        {
            IncludeFields = true,
            WriteIndented = true,
        };

        public string Serialize<T>(T value) where T : class
        {
            if (value == null) throw new ArgumentNullException(nameof(value));
            return JsonSerializer.Serialize(value, Options);
        }

        public T Deserialize<T>(string text) where T : class, new()
        {
            if (text == null) throw new FormatException("JSON text is null.");
            try
            {
                var result = JsonSerializer.Deserialize<T>(text, Options);
                if (result == null) throw new FormatException("JSON value is null.");
                return result;
            }
            catch (JsonException e)
            {
                throw new FormatException(e.Message, e);
            }
        }
    }
}
