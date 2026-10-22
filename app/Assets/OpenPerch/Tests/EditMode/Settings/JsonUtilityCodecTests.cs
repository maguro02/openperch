using System;
using NUnit.Framework;
using OpenPerch.App.Boot;
using OpenPerch.App.Core;
using OpenPerch.App.Tests.Shared;

namespace OpenPerch.App.Tests.EditMode
{
    [Category("T-SET-U")]
    public class JsonUtilityCodecTests
    {
        private readonly JsonUtilityCodec _codec = new JsonUtilityCodec();

        [Test]
        public void AppSettingsKeepsEveryField()
        {
            AssertRoundTrip<AppSettings>();
        }

        [Test]
        public void LibraryIndexKeepsEveryField()
        {
            AssertRoundTrip<LibraryIndex>();
        }

        [Test]
        public void LocaleFileKeepsEveryField()
        {
            AssertRoundTrip<LocaleFile>();
        }

        [Test]
        public void MissingFieldsKeepInitializerValues()
        {
            var settings = _codec.Deserialize<AppSettings>("{\"targetFpsCap\": 30}");

            Assert.That(settings.targetFpsCap, Is.EqualTo(30));
            Assert.That(settings.schemaVersion, Is.EqualTo(1));
            Assert.That(settings.checkUpdates, Is.EqualTo(-1));
            Assert.That(settings.pomodoro.workMin, Is.EqualTo(25));
            Assert.That(settings.notify.volume, Is.EqualTo(0.7f));
            Assert.That(settings.avatars, Is.Not.Null);
        }

        [Test]
        public void LeadingByteOrderMarkIsAccepted()
        {
            Assert.That(_codec.Deserialize<AppSettings>("﻿{\"targetFpsCap\": 30}").targetFpsCap, Is.EqualTo(30));
        }

        [TestCase("{\"targetFpsCap\": ")]
        [TestCase("")]
        [TestCase("[]")]
        [TestCase("null")]
        public void UnusableTextThrowsFormatException(string text)
        {
            Assert.Throws<FormatException>(() => _codec.Deserialize<AppSettings>(text));
        }

        private void AssertRoundTrip<T>() where T : class, new()
        {
            var original = PersistFiller.Fill<T>();

            var restored = _codec.Deserialize<T>(_codec.Serialize(original));

            Assert.That(PersistComparer.Differences(original, restored), Is.Empty);
        }
    }
}
