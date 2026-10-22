using System;
using System.IO;
using NUnit.Framework;
using OpenPerch.App.Core;
using OpenPerch.App.Tests.Shared;
using OpenPerch.Core.Tests.Support;

namespace OpenPerch.Core.Tests.Settings
{
    [Category("T-SET")]
    public class SettingsStoreTests
    {
        private static readonly DateTime Start = new DateTime(2026, 10, 22, 10, 0, 0, DateTimeKind.Utc);
        private const string BackupName = "settings.json.bak-20261022T100000Z";

        private readonly StjCodec _codec = new StjCodec();
        private TempDir _dir;
        private FakeTimeSource _time;

        private string SettingsPath => _dir.Combine("settings.json");

        [SetUp]
        public void SetUp()
        {
            _dir = new TempDir();
            _time = new FakeTimeSource(Start, 100);
        }

        [TearDown]
        public void TearDown()
        {
            _dir.Dispose();
        }

        [Test]
        public void MissingFileGivesDefaultsWithoutWriting()
        {
            var loaded = NewStore().Load();

            Assert.That(PersistComparer.Differences(new AppSettings(), loaded), Is.Empty);
            Assert.That(Directory.GetFileSystemEntries(_dir.Root), Is.Empty);
        }

        [Test]
        public void MissingFolderGivesDefaults()
        {
            var store = new SettingsStore(_dir.Combine("absent"), _codec, _time);

            Assert.That(PersistComparer.Differences(new AppSettings(), store.Load()), Is.Empty);
        }

        [Test]
        public void FirstSaveCreatesTheFile()
        {
            var store = NewStore();

            store.SaveDebounced(new AppSettings { targetFpsCap = 30 });
            store.Flush();

            Assert.That(NewStore().Load().targetFpsCap, Is.EqualTo(30));
            Assert.That(Directory.GetFileSystemEntries(_dir.Root), Is.EqualTo(new[] { SettingsPath }));
        }

        [Test]
        public void FirstSaveCreatesAMissingFolder()
        {
            var folder = _dir.Combine("data");
            var store = new SettingsStore(folder, _codec, _time);

            store.SaveDebounced(new AppSettings { targetFpsCap = 30 });
            store.Flush();

            Assert.That(new SettingsStore(folder, _codec, _time).Load().targetFpsCap, Is.EqualTo(30));
        }

        [Test]
        public void LaterSaveReplacesTheFile()
        {
            var store = NewStore();
            store.SaveDebounced(new AppSettings { targetFpsCap = 30 });
            store.Flush();

            store.SaveDebounced(new AppSettings { targetFpsCap = 45 });
            store.Flush();

            Assert.That(NewStore().Load().targetFpsCap, Is.EqualTo(45));
            Assert.That(Directory.GetFileSystemEntries(_dir.Root), Is.EqualTo(new[] { SettingsPath }));
        }

        [Test]
        public void SavedFileHoldsEveryField()
        {
            var filled = PersistFiller.Fill<AppSettings>();
            var store = NewStore();

            store.SaveDebounced(filled);
            store.Flush();

            var restored = _codec.Deserialize<AppSettings>(File.ReadAllText(SettingsPath));
            Assert.That(PersistComparer.Differences(filled, restored), Is.Empty);
        }

        [Test]
        public void PumpWritesOneSecondAfterTheChange()
        {
            var store = NewStore();
            store.SaveDebounced(new AppSettings { targetFpsCap = 30 });

            _time.Advance(0.5);
            store.Pump();
            Assert.That(File.Exists(SettingsPath), Is.False);

            _time.Advance(0.5);
            store.Pump();
            Assert.That(NewStore().Load().targetFpsCap, Is.EqualTo(30));
        }

        [Test]
        public void EachChangeRestartsTheDelay()
        {
            var store = NewStore();
            var settings = new AppSettings();
            store.SaveDebounced(settings);
            _time.Advance(0.75);
            settings.targetFpsCap = 45;
            store.SaveDebounced(settings);

            _time.Advance(0.75);
            store.Pump();
            Assert.That(File.Exists(SettingsPath), Is.False);

            _time.Advance(0.25);
            store.Pump();
            Assert.That(NewStore().Load().targetFpsCap, Is.EqualTo(45));
        }

        [Test]
        public void WritesTheStateAtWriteTime()
        {
            var store = NewStore();
            var settings = new AppSettings();
            store.SaveDebounced(settings);

            settings.targetFpsCap = 50;
            _time.Advance(1);
            store.Pump();

            Assert.That(NewStore().Load().targetFpsCap, Is.EqualTo(50));
        }

        [Test]
        public void DelayFollowsTheMonotonicClock()
        {
            var store = NewStore();
            store.SaveDebounced(new AppSettings());

            _time.AdvanceWallClockOnly(3600);
            store.Pump();

            Assert.That(File.Exists(SettingsPath), Is.False);
        }

        [Test]
        public void PumpWritesOnlyOncePerChange()
        {
            var store = NewStore();
            store.SaveDebounced(new AppSettings());
            _time.Advance(1);
            store.Pump();
            File.Delete(SettingsPath);

            _time.Advance(5);
            store.Pump();

            Assert.That(File.Exists(SettingsPath), Is.False);
        }

        [Test]
        public void FlushWritesWithoutWaiting()
        {
            var store = NewStore();
            store.SaveDebounced(new AppSettings { targetFpsCap = 30 });

            store.Flush();

            Assert.That(NewStore().Load().targetFpsCap, Is.EqualTo(30));
        }

        [Test]
        public void FlushWithoutChangesWritesNothing()
        {
            var store = NewStore();

            store.Flush();
            store.Pump();

            Assert.That(File.Exists(SettingsPath), Is.False);
        }

        [Test]
        public void FailedWriteStaysPendingAndRetriesAfterTheDelay()
        {
            var codec = new FailingCodec(_codec, 1);
            var store = new SettingsStore(_dir.Root, codec, _time);
            store.SaveDebounced(new AppSettings { targetFpsCap = 30 });
            _time.Advance(1);

            store.Pump();
            Assert.That(store.LastWriteError, Is.InstanceOf<IOException>());
            _time.Advance(0.5);
            store.Pump();
            Assert.That(codec.SerializeCalls, Is.EqualTo(1));

            _time.Advance(0.5);
            store.Pump();
            Assert.That(codec.SerializeCalls, Is.EqualTo(2));
            Assert.That(store.LastWriteError, Is.Null);
            Assert.That(NewStore().Load().targetFpsCap, Is.EqualTo(30));
        }

        [TestCase(typeof(IOException))]
        [TestCase(typeof(UnauthorizedAccessException))]
        public void FailedFlushDoesNotThrowAndKeepsTheChange(Type errorType)
        {
            var codec = new FailingCodec(_codec, 1, () => (Exception)Activator.CreateInstance(errorType));
            var store = new SettingsStore(_dir.Root, codec, _time);
            store.SaveDebounced(new AppSettings { targetFpsCap = 30 });

            store.Flush();

            Assert.That(store.LastWriteError, Is.InstanceOf(errorType));
            Assert.That(File.Exists(SettingsPath), Is.False);

            store.Flush();

            Assert.That(store.LastWriteError, Is.Null);
            Assert.That(NewStore().Load().targetFpsCap, Is.EqualTo(30));
        }

        [Test]
        public void FailureOnDiskIsReportedWithoutThrowing()
        {
            // A folder where the file belongs makes the final rename fail, as a file locked by another process would.
            Directory.CreateDirectory(SettingsPath);
            var store = NewStore();
            store.SaveDebounced(new AppSettings { targetFpsCap = 30 });

            store.Flush();

            Assert.That(store.LastWriteError, Is.Not.Null);
            Directory.Delete(SettingsPath);
            store.Flush();
            Assert.That(store.LastWriteError, Is.Null);
            Assert.That(NewStore().Load().targetFpsCap, Is.EqualTo(30));
        }

        [Test]
        public void OtherErrorsPropagateAndKeepTheChange()
        {
            var codec = new FailingCodec(_codec, 1, () => new InvalidOperationException());
            var store = new SettingsStore(_dir.Root, codec, _time);
            store.SaveDebounced(new AppSettings { targetFpsCap = 30 });

            Assert.Throws<InvalidOperationException>(() => store.Flush());

            store.Flush();
            Assert.That(NewStore().Load().targetFpsCap, Is.EqualTo(30));
        }

        [Test]
        public void CorruptFileIsMovedAsideAndDefaultsAreUsed()
        {
            File.WriteAllText(SettingsPath, "{\"targetFpsCap\": ");

            var loaded = NewStore().Load();

            Assert.That(PersistComparer.Differences(new AppSettings(), loaded), Is.Empty);
            Assert.That(File.Exists(SettingsPath), Is.False);
            Assert.That(File.ReadAllText(_dir.Combine(BackupName)), Is.EqualTo("{\"targetFpsCap\": "));
        }

        [TestCase("")]
        [TestCase("[]")]
        [TestCase("null")]
        public void NonObjectTextCountsAsCorrupt(string text)
        {
            File.WriteAllText(SettingsPath, text);

            NewStore().Load();

            Assert.That(File.Exists(_dir.Combine(BackupName)), Is.True);
        }

        [Test]
        public void SecondBackupInTheSameSecondGetsAnotherName()
        {
            File.WriteAllText(SettingsPath, "first");
            NewStore().Load();
            File.WriteAllText(SettingsPath, "second");

            NewStore().Load();

            Assert.That(File.ReadAllText(_dir.Combine(BackupName)), Is.EqualTo("first"));
            Assert.That(File.ReadAllText(_dir.Combine(BackupName + "-1")), Is.EqualTo("second"));
        }

        [Test]
        public void CorruptFileThatCannotBeMovedIsLeftForTheNextSave()
        {
            File.WriteAllText(SettingsPath, "{");
            // A folder holding the backup's name makes the move fail, as a file locked by another process would.
            Directory.CreateDirectory(_dir.Combine(BackupName));
            var store = NewStore();

            var loaded = store.Load();

            Assert.That(PersistComparer.Differences(new AppSettings(), loaded), Is.Empty);
            Assert.That(File.ReadAllText(SettingsPath), Is.EqualTo("{"));

            loaded.targetFpsCap = 30;
            store.SaveDebounced(loaded);
            store.Flush();
            Assert.That(NewStore().Load().targetFpsCap, Is.EqualTo(30));
        }

        [Test]
        public void UnreadableFileIsNotReplacedWithDefaults()
        {
            // A folder where the file belongs makes reading it fail with an access error.
            Directory.CreateDirectory(SettingsPath);

            Assert.Throws<UnauthorizedAccessException>(() => NewStore().Load());
        }

        [Test]
        public void SaveAfterACorruptFileWritesAFreshOne()
        {
            File.WriteAllText(SettingsPath, "{");
            var store = NewStore();
            var settings = store.Load();

            settings.targetFpsCap = 30;
            store.SaveDebounced(settings);
            store.Flush();

            Assert.That(NewStore().Load().targetFpsCap, Is.EqualTo(30));
            Assert.That(File.Exists(_dir.Combine(BackupName)), Is.True);
        }

        [Test]
        public void SchemaVersionZeroIsMigrated()
        {
            File.WriteAllText(SettingsPath,
                "{\"schemaVersion\": 0, \"language\": \"ja\", \"targetFpsCap\": 30, \"pomodoro\": {\"workMin\": 50}," +
                " \"avatars\": [{\"exportId\": \"3f2c9a0d6b7e4c1fa2b3c4d5e6f70812\", \"displayHeightDip\": 420}]}");

            var loaded = NewStore().Load();

            Assert.That(loaded.schemaVersion, Is.EqualTo(1));
            Assert.That(loaded.language, Is.EqualTo("ja"));
            Assert.That(loaded.targetFpsCap, Is.EqualTo(30));
            Assert.That(loaded.pomodoro.workMin, Is.EqualTo(50));
            Assert.That(loaded.avatars, Has.Count.EqualTo(1));
            Assert.That(loaded.avatars[0].exportId, Is.EqualTo("3f2c9a0d6b7e4c1fa2b3c4d5e6f70812"));
            Assert.That(loaded.avatars[0].displayHeightDip, Is.EqualTo(420));
        }

        [Test]
        public void FileFromANewerBuildIsCopiedAside()
        {
            const string newer = "{\"schemaVersion\": 2, \"targetFpsCap\": 30, \"addedByANewerBuild\": true}";
            File.WriteAllText(SettingsPath, newer);

            var loaded = NewStore().Load();

            Assert.That(loaded.schemaVersion, Is.EqualTo(1));
            Assert.That(loaded.targetFpsCap, Is.EqualTo(30));
            Assert.That(File.ReadAllText(SettingsPath), Is.EqualTo(newer));
            Assert.That(File.ReadAllText(_dir.Combine(BackupName)), Is.EqualTo(newer));
        }

        [TestCase("{\"schemaVersion\": 0}")]
        [TestCase("{\"schemaVersion\": 1}")]
        public void OlderOrCurrentFileIsNotCopied(string text)
        {
            File.WriteAllText(SettingsPath, text);

            NewStore().Load();

            Assert.That(Directory.GetFileSystemEntries(_dir.Root), Is.EqualTo(new[] { SettingsPath }));
        }

        [Test]
        public void NewerFileThatCannotBeCopiedStillLoads()
        {
            File.WriteAllText(SettingsPath, "{\"schemaVersion\": 2, \"targetFpsCap\": 30}");
            Directory.CreateDirectory(_dir.Combine(BackupName));

            Assert.That(NewStore().Load().targetFpsCap, Is.EqualTo(30));
        }

        [Test]
        public void LoadedSettingsAreNormalized()
        {
            File.WriteAllText(SettingsPath, "{\"targetFpsCap\": 100000, \"notify\": null, \"language\": null}");

            var loaded = NewStore().Load();

            Assert.That(loaded.targetFpsCap, Is.EqualTo(240));
            Assert.That(PersistComparer.Differences(new NotificationSettings(), loaded.notify), Is.Empty);
            Assert.That(loaded.language, Is.EqualTo(""));
        }

        private SettingsStore NewStore() => new SettingsStore(_dir.Root, _codec, _time);
    }
}
