using System;

namespace OpenPerch.App.Core
{
    /// <summary>
    /// The only way <c>OpenPerch.App.Core</c> touches JSON: JsonUtility in Unity, System.Text.Json in the dotnet tests.
    /// </summary>
    public interface ITextCodec
    {
        string Serialize<T>(T value) where T : class;

        /// <summary>
        /// Deserializes onto a fresh <typeparamref name="T"/>, so fields missing from <paramref name="text"/> keep their initializer values.
        /// </summary>
        /// <exception cref="FormatException">The text is not valid JSON for <typeparamref name="T"/>.</exception>
        T Deserialize<T>(string text) where T : class, new();
    }
}
