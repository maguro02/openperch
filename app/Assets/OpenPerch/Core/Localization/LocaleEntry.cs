using System;

namespace OpenPerch.App.Core
{
    [Serializable]
    public sealed class LocaleEntry
    {
        public string key = "";

        /// <summary>Placeholders are written as <c>{name}</c>.</summary>
        public string text = "";
    }
}
