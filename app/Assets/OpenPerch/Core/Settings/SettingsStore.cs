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
        /// Reads the settings and passes them through <see cref="SettingsNormalizer"/>. A missing file yields the defaults.
        /// A file the codec cannot parse is moved to <c>settings.json.bak-&lt;UTC time&gt;</c> and the defaults are returned.
        /// Nothing is written.
        /// </summary>
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
                PersistedFile.MoveAside(_path, _time.UtcNow);
                return SettingsNormalizer.Normalize(new AppSettings());
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
        /// If the write fails the change stays pending, the exception propagates, and the next attempt waits another second.
        /// </summary>
        public void Pump()
        {
            if (_pending == null || _time.MonotonicSeconds - _changedAt < DebounceSeconds) return;
            WritePending();
        }

        /// <summary>Writes the recorded change now, if there is one. Call it before the app exits.</summary>
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
            catch
            {
                // Restart the delay so a lasting failure, such as a full disk, is retried once a second rather than every frame.
                _changedAt = _time.MonotonicSeconds;
                throw;
            }
            _pending = null;
        }
    }
}
