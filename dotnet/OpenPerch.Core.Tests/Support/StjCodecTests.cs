using System;
using NUnit.Framework;
using OpenPerch.App.Core;

namespace OpenPerch.Core.Tests.Support
{
    public class StjCodecTests
    {
        [Test]
        public void MissingFieldsKeepInitializerValues()
        {
            var codec = new StjCodec();

            var settings = codec.Deserialize<AppSettings>("{\"targetFpsCap\": 30}");

            Assert.That(settings.targetFpsCap, Is.EqualTo(30));
            Assert.That(settings.schemaVersion, Is.EqualTo(1));
            Assert.That(settings.pomodoro.workMin, Is.EqualTo(25));
            Assert.That(settings.avatars, Is.Not.Null);
        }

        [Test]
        public void MalformedJsonThrowsFormatException()
        {
            var codec = new StjCodec();

            Assert.Throws<FormatException>(() => codec.Deserialize<AppSettings>("{\"targetFpsCap\":"));
        }
    }
}
