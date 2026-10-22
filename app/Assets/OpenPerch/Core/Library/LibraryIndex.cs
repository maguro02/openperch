using System;
using System.Collections.Generic;

namespace OpenPerch.App.Core
{
    [Serializable]
    public sealed class LibraryIndex
    {
        public int schemaVersion = 1;
        public List<LibraryEntry> entries = new();
    }
}
