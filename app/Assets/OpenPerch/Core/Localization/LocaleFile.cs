using System;
using System.Collections.Generic;

namespace OpenPerch.App.Core
{
    /// <summary>
    /// On-disk locale table. Entries are a list rather than a dictionary because JsonUtility cannot serialize dictionaries.
    /// </summary>
    [Serializable]
    public sealed class LocaleFile
    {
        public int schemaVersion = 1;
        public string lang = "";
        public List<LocaleEntry> entries = new();
    }
}
