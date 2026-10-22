using System;
using OpenPerch.App.Core;
using UnityEngine;

namespace OpenPerch.App.Boot
{
    /// <summary><see cref="ITextCodec"/> for the player, backed by <see cref="JsonUtility"/>.</summary>
    public sealed class JsonUtilityCodec : ITextCodec
    {
        /// <summary>Writes <paramref name="value"/> as indented JSON.</summary>
        public string Serialize<T>(T value) where T : class
        {
            if (value == null) throw new ArgumentNullException(nameof(value));
            return JsonUtility.ToJson(value, true);
        }

        /// <summary>
        /// Overwrites a new <typeparamref name="T"/> with <paramref name="text"/>, so fields the text omits keep their
        /// initializer values.
        /// </summary>
        /// <exception cref="FormatException">The text is not a JSON object, or JsonUtility rejects it.</exception>
        public T Deserialize<T>(string text) where T : class, new()
        {
            if (text == null) throw new FormatException("JSON text is null.");

            var json = text.TrimStart('﻿', ' ', '\t', '\r', '\n');
            // Rejecting non-objects here makes empty and non-object files count as corrupt, as they do with System.Text.Json.
            if (json.Length == 0 || json[0] != '{') throw new FormatException("JSON text is not an object.");

            var result = new T();
            try
            {
                JsonUtility.FromJsonOverwrite(json, result);
            }
            catch (Exception e)
            {
                throw new FormatException(e.Message, e);
            }
            return result;
        }
    }
}
