using System;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using NUnit.Framework;
using OpenPerch.App.Core;
using OpenPerch.App.Tests.Shared;
using OpenPerch.Core.Tests.Support;

namespace OpenPerch.Core.Tests.Settings
{
    [Category("T-SET")]
    public class LibraryIndexStoreTests
    {
        private readonly StjCodec _codec = new StjCodec();
        private TempDir _dir;

        private string LibraryDir => _dir.Combine("library");
        private string IndexPath => _dir.Combine("library", "index.json");

        [SetUp]
        public void SetUp()
        {
            _dir = new TempDir();
        }

        [TearDown]
        public void TearDown()
        {
            _dir.Dispose();
        }

        [Test]
        public void MissingFolderGivesAnEmptyIndex()
        {
            var store = NewStore();

            var index = store.Load();

            Assert.That(PersistComparer.Differences(new LibraryIndex(), index), Is.Empty);
            Assert.That(store.LastLoadWasCorrupt, Is.False);
            Assert.That(Directory.Exists(LibraryDir), Is.False);
        }

        [Test]
        public void MissingFileGivesAnEmptyIndex()
        {
            Directory.CreateDirectory(LibraryDir);

            Assert.That(PersistComparer.Differences(new LibraryIndex(), NewStore().Load()), Is.Empty);
        }

        [Test]
        public void FirstSaveCreatesTheFolderAndTheFile()
        {
            var index = new LibraryIndex();
            index.entries.Add(new LibraryEntry { exportId = "3f2c9a0d6b7e4c1fa2b3c4d5e6f70812" });

            NewStore().Save(index);

            Assert.That(Directory.GetFileSystemEntries(LibraryDir), Is.EqualTo(new[] { IndexPath }));
            Assert.That(NewStore().Load().entries.Single().exportId, Is.EqualTo("3f2c9a0d6b7e4c1fa2b3c4d5e6f70812"));
        }

        [Test]
        public void LaterSaveReplacesTheFile()
        {
            var store = NewStore();
            var index = new LibraryIndex();
            index.entries.Add(new LibraryEntry { displayName = "first" });
            store.Save(index);

            index.entries[0].displayName = "second";
            store.Save(index);

            Assert.That(NewStore().Load().entries.Single().displayName, Is.EqualTo("second"));
            Assert.That(Directory.GetFileSystemEntries(LibraryDir), Is.EqualTo(new[] { IndexPath }));
        }

        [Test]
        public void SaveThenLoadKeepsEveryField()
        {
            var filled = PersistFiller.Fill<LibraryIndex>();
            NewStore().Save(filled);

            var loaded = NewStore().Load();

            Assert.That(PersistComparer.Differences(filled, loaded), Is.Empty);
        }

        [Test]
        public void CorruptFileIsMovedAsideWithTheUtcTime()
        {
            Directory.CreateDirectory(LibraryDir);
            File.WriteAllText(IndexPath, "{\"entries\": [");
            var store = NewStore();
            var before = TruncateToSeconds(DateTime.UtcNow);

            var index = store.Load();

            var after = DateTime.UtcNow;
            Assert.That(store.LastLoadWasCorrupt, Is.True);
            Assert.That(PersistComparer.Differences(new LibraryIndex(), index), Is.Empty);
            Assert.That(File.Exists(IndexPath), Is.False);

            var backup = Directory.GetFiles(LibraryDir).Single();
            var match = Regex.Match(Path.GetFileName(backup), @"^index\.json\.bak-(\d{8}T\d{6}Z)$");
            Assert.That(match.Success, Is.True, backup);
            var stamp = DateTime.ParseExact(match.Groups[1].Value, "yyyyMMdd'T'HHmmss'Z'", CultureInfo.InvariantCulture,
                DateTimeStyles.AssumeUniversal | DateTimeStyles.AdjustToUniversal);
            Assert.That(stamp, Is.InRange(before, after));
            Assert.That(File.ReadAllText(backup), Is.EqualTo("{\"entries\": ["));
        }

        [Test]
        public void CorruptFileThatCannotBeMovedIsLeftForTheNextSave()
        {
            Directory.CreateDirectory(LibraryDir);
            File.WriteAllText(IndexPath, "{\"entries\": [");
            BlockBackupNames(DateTime.UtcNow, 10);
            var store = NewStore();

            var index = store.Load();

            Assert.That(store.LastLoadWasCorrupt, Is.True);
            Assert.That(PersistComparer.Differences(new LibraryIndex(), index), Is.Empty);
            Assert.That(File.ReadAllText(IndexPath), Is.EqualTo("{\"entries\": ["));

            index.entries.Add(new LibraryEntry { exportId = "3f2c9a0d6b7e4c1fa2b3c4d5e6f70812" });
            store.Save(index);
            Assert.That(NewStore().Load().entries.Single().exportId, Is.EqualTo("3f2c9a0d6b7e4c1fa2b3c4d5e6f70812"));
        }

        [Test]
        public void UnreadableFileIsNotReplacedWithAnEmptyIndex()
        {
            // A folder where the file belongs makes reading it fail with an access error.
            Directory.CreateDirectory(IndexPath);

            Assert.Throws<UnauthorizedAccessException>(() => NewStore().Load());
        }

        [Test]
        public void CorruptFlagClearsOnTheNextLoad()
        {
            Directory.CreateDirectory(LibraryDir);
            File.WriteAllText(IndexPath, "not json");
            var store = NewStore();
            store.Load();

            store.Save(new LibraryIndex());
            store.Load();

            Assert.That(store.LastLoadWasCorrupt, Is.False);
        }

        [Test]
        public void NullsInTheIndexBecomeDefaults()
        {
            Directory.CreateDirectory(LibraryDir);
            File.WriteAllText(IndexPath,
                "{\"schemaVersion\": 1, \"entries\": [null, {\"exportId\": \"a\", \"displayName\": null, \"status\": null, \"warningCodes\": null}]}");

            var index = NewStore().Load();

            var expected = new LibraryIndex();
            expected.entries.Add(new LibraryEntry { exportId = "a" });
            Assert.That(PersistComparer.Differences(expected, index), Is.Empty);
        }

        [Test]
        public void NullEntriesListBecomesEmpty()
        {
            Directory.CreateDirectory(LibraryDir);
            File.WriteAllText(IndexPath, "{\"entries\": null}");

            Assert.That(NewStore().Load().entries, Is.Empty);
        }

        private LibraryIndexStore NewStore() => new LibraryIndexStore(LibraryDir, _codec);

        // The store names backups with the real clock, so every name the next few seconds could produce is taken by a
        // folder, which makes the move fail the way a file locked by another process would.
        private void BlockBackupNames(DateTime fromUtc, int seconds)
        {
            var first = TruncateToSeconds(fromUtc);
            for (var i = 0; i < seconds; i++)
            {
                var stamp = first.AddSeconds(i).ToString("yyyyMMdd'T'HHmmss'Z'", CultureInfo.InvariantCulture);
                Directory.CreateDirectory(IndexPath + ".bak-" + stamp);
            }
        }

        private static DateTime TruncateToSeconds(DateTime t) => new DateTime(t.Ticks - t.Ticks % TimeSpan.TicksPerSecond, t.Kind);
    }
}
