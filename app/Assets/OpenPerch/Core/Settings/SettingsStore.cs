using System;
using System.IO;

namespace OpenPerch.App.Core
{
    /// <summary>
    /// Reads and writes <c>settings.json</c>. Saving is debounced: <see cref="SaveDebounced"/> only records the change and
    /// <see cref="Pump"/> writes once no change has arrived for one second. Not thread-safe; use it from the main thread.
    /// </summary>
    public sealed class SettingsStore
    {
        private const string FileName = "settings.json";
        private const double DebounceSeconds = 1.0;

        private readonly string _path;
        private readonly ITextCodec _codec;
        private readonly ITimeSource _time;
        private AppSettings _pending;
        private double _changedAt;

        /// <param name="directory">Folder that holds <c>settings.json</c>; created on the first save if missing.</param>
        /// <param name="codec">JSON codec; a parse failure must surface as <see cref="FormatException"/>.</param>
        /// <param name="time">Clock for the save delay (monotonic) and the backup file name (UTC).</param>
        public SettingsStore(string directory, ITextCodec codec, ITimeSource time)
        {
            if (directory == null) throw new ArgumentNullException(nameof(directory));
            _codec = codec ?? throw new ArgumentNullException(nameof(codec));
            _time = time ?? throw new ArgumentNullException(nameof(time));
            _path = Path.Combine(directory, FileName);
        }

        /// <summary>
        /// The exception that made the last write attempt fail, or null when the last attempt succeeded or none was made.
        /// While it is set, the change that failed is still pending.
        /// </summary>
        public Exception LastWriteError { get; private set; }

        /// <summary>
        /// Reads the settings and passes them through <see cref="SettingsNormalizer"/>. A missing file yields the defaults.
        /// A file the codec cannot parse is moved to <c>settings.json.bak-&lt;UTC time&gt;</c> and the defaults are returned;
        /// when it cannot be moved it stays in place until the next save replaces it. A file written by a newer build, whose
        /// <see cref="AppSettings.schemaVersion"/> is above <see cref="SettingsNormalizer.CurrentSchemaVersion"/>, is first
        /// copied to the same kind of name, because saving the returned settings drops the fields only that build knows.
        /// </summary>
        /// <exception cref="IOException">
        /// The file exists but cannot be read, for example while another process holds it open without sharing. The defaults
        /// are not returned in this case, because saving them later would overwrite settings that are intact.
        /// </exception>
        /// <exception cref="UnauthorizedAccessException">The file exists but reading it is denied; handled as above.</exception>
        public AppSettings Load()
        {
            string text;
            try
            {
                text = File.ReadAllText(_path);
            }
            catch (FileNotFoundException)
            {
                return SettingsNormalizer.Normalize(new AppSettings());
            }
            catch (DirectoryNotFoundException)
            {
                return SettingsNormalizer.Normalize(new AppSettings());
            }

            AppSettings loaded;
            try
            {
                loaded = _codec.Deserialize<AppSettings>(text);
            }
            catch (FormatException)
            {
                PersistedFile.TryMoveAside(_path, _time.UtcNow);
                return SettingsNormalizer.Normalize(new AppSettings());
            }

            if (loaded != null && loaded.schemaVersion > SettingsNormalizer.CurrentSchemaVersion)
            {
                PersistedFile.TryCopyAside(_path, _time.UtcNow);
            }
            return SettingsNormalizer.Normalize(loaded);
        }

        /// <summary>
        /// Records that <paramref name="s"/> changed. The object is serialized when it is written, so later edits to the same
        /// instance are included; each call restarts the one-second delay.
        /// </summary>
        public void SaveDebounced(AppSettings s)
        {
            _pending = s ?? throw new ArgumentNullException(nameof(s));
            _changedAt = _time.MonotonicSeconds;
        }

        /// <summary>
        /// Writes the recorded change once one second has passed since the last <see cref="SaveDebounced"/>. Call it every frame.
        /// A failed write leaves the change pending, sets <see cref="LastWriteError"/>, and makes the next attempt wait another
        /// second. An <see cref="IOException"/> or <see cref="UnauthorizedAccessException"/>, such as a full disk or a file
        /// locked by another process, is not thrown; any other exception propagates.
        /// </summary>
        public void Pump()
        {
            if (_pending == null || _time.MonotonicSeconds - _changedAt < DebounceSeconds) return;
            WritePending();
        }

        /// <summary>
        /// Writes the recorded change now, if there is one. Call it before the app exits. Failures are handled as in
        /// <see cref="Pump"/>, so an I/O error does not stop the exit; check <see cref="LastWriteError"/> to report it.
        /// </summary>
        public void Flush()
        {
            if (_pending == null) return;
            WritePending();
        }

        private void WritePending()
        {
            try
            {
                PersistedFile.WriteAtomic(_path, _codec.Serialize(_pending));
            }
            catch (Exception e)
            {
                // Restart the delay so a lasting failure, such as a full disk, is retried once a second rather than every frame.
                _changedAt = _time.MonotonicSeconds;
                LastWriteError = e;
                if (PersistedFile.IsFileError(e)) return;
                throw;
            }
            _pending = null;
            LastWriteError = null;
        }
    }
}
