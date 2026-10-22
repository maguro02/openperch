using System;
using System.Collections.Generic;
using System.IO;

namespace OpenPerch.App.Core
{
    /// <summary>Reads and writes <c>index.json</c> in the library folder.</summary>
    public sealed class LibraryIndexStore
    {
        private const string FileName = "index.json";

        private readonly string _path;
        private readonly ITextCodec _codec;

        /// <param name="libraryDir">The library folder; created on the first save if missing.</param>
        /// <param name="codec">JSON codec; a parse failure must surface as <see cref="FormatException"/>.</param>
        public LibraryIndexStore(string libraryDir, ITextCodec codec)
        {
            if (libraryDir == null) throw new ArgumentNullException(nameof(libraryDir));
            _codec = codec ?? throw new ArgumentNullException(nameof(codec));
            _path = Path.Combine(libraryDir, FileName);
        }

        /// <summary>
        /// True when the last <see cref="Load"/> could not parse <c>index.json</c> and returned an empty index. The store does
        /// not rebuild the entries itself: the caller reads the packages in the library folder again, adds their entries
        /// with <see cref="LibraryEntry.crcVerified"/> false, and calls <see cref="Save"/>.
        /// </summary>
        public bool LastLoadWasCorrupt { get; private set; }

        /// <summary>
        /// Reads the index. A missing file yields an empty index. A file the codec cannot parse is moved to
        /// <c>index.json.bak-&lt;UTC time&gt;</c>, or left in place for the next save to replace when it cannot be moved;
        /// either way <see cref="LastLoadWasCorrupt"/> is set and an empty index is returned. Null collections and strings
        /// are replaced with their defaults.
        /// </summary>
        /// <exception cref="IOException">
        /// The file exists but cannot be read, for example while another process holds it open without sharing. An empty
        /// index is not returned in this case, because saving it later would overwrite an index that is intact.
        /// </exception>
        /// <exception cref="UnauthorizedAccessException">The file exists but reading it is denied; handled as above.</exception>
        public LibraryIndex Load()
        {
            LastLoadWasCorrupt = false;

            string text;
            try
            {
                text = File.ReadAllText(_path);
            }
            catch (FileNotFoundException)
            {
                return new LibraryIndex();
            }
            catch (DirectoryNotFoundException)
            {
                return new LibraryIndex();
            }

            LibraryIndex index;
            try
            {
                index = _codec.Deserialize<LibraryIndex>(text);
            }
            catch (FormatException)
            {
                PersistedFile.TryMoveAside(_path, DateTime.UtcNow);
                LastLoadWasCorrupt = true;
                return new LibraryIndex();
            }
            return FillNulls(index);
        }

        /// <summary>Writes the index, replacing <c>index.json</c> atomically.</summary>
        public void Save(LibraryIndex index)
        {
            if (index == null) throw new ArgumentNullException(nameof(index));
            PersistedFile.WriteAtomic(_path, _codec.Serialize(index));
        }

        private static LibraryIndex FillNulls(LibraryIndex index)
        {
            if (index == null) return new LibraryIndex();
            if (index.entries == null) index.entries = new List<LibraryEntry>();
            index.entries.RemoveAll(e => e == null);

            var d = new LibraryEntry();
            foreach (var e in index.entries)
            {
                e.exportId = e.exportId ?? d.exportId;
                e.lineageId = e.lineageId ?? d.lineageId;
                e.kind = e.kind ?? d.kind;
                e.displayName = e.displayName ?? d.displayName;
                e.sha256 = e.sha256 ?? d.sha256;
                e.unityVersion = e.unityVersion ?? d.unityVersion;
                e.exporterVersion = e.exporterVersion ?? d.exporterVersion;
                e.importedAtUtc = e.importedAtUtc ?? d.importedAtUtc;
                e.status = e.status ?? d.status;
                e.warningCodes = e.warningCodes ?? d.warningCodes;
            }
            return index;
        }
    }
}
