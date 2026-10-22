using System;
using System.Linq;
using System.Reflection;
using System.Text;
using NUnit.Framework;
using OpenPerch.Format;
using OpenPerch.Format.Json;

namespace OpenPerch.Runtime.Tests.Editor
{
    [Category("T-FMT-JSON")]
    public class ManifestJsonTests
    {
        private const string ValidJson =
            "{\"formatVersion\":1,\"kind\":\"avatar\",\"exportId\":\"3f2c9a0d6b7e4c1fa2b3c4d5e6f70812\"," +
            "\"lineageId\":\"9a1b3c5d7e9f4a2b8c6d0e1f2a3b4c5d\",\"displayName\":\"MyAvatar\",\"createdAtUtc\":\"2026-10-22T10:00:00Z\"," +
            "\"exporter\":{\"version\":\"1.0.0\",\"unityVersion\":\"2022.3.22f1\",\"buildTarget\":\"StandaloneWindows64\"," +
            "\"colorSpace\":\"Linear\",\"vrcSdkVersion\":\"3.10.5\",\"tools\":[\"nadena.dev.ndmf@1.15.0\"]}," +
            "\"runtimeSchemaVersion\":1," +
            "\"bundle\":{\"name\":\"op-3f2c9a0d6b7e4c1fa2b3c4d5e6f70812\",\"crc\":2864434397," +
            "\"sha256\":\"0123456789abcdef0123456789abcdef0123456789abcdef0123456789abcdef\",\"length\":123456789,\"rootAsset\":\"definition\"}," +
            "\"features\":{\"fx\":true,\"menuPages\":5,\"physBones\":14,\"constraints\":3,\"contacts\":2,\"eyeLook\":true,\"blink\":true}," +
            "\"compositing\":{\"requiresPrecise\":true,\"reasons\":[\"additive:1\",\"grabpass:1\"]}," +
            "\"stats\":{\"triangles\":69000,\"materials\":18,\"skinnedMeshes\":6,\"physBoneTransforms\":220," +
            "\"estimatedTextureBytes\":412000000,\"estimatedMeshBytes\":38000000}," +
            "\"warnings\":[{\"code\":\"W_REMOVED_COMPONENT\",\"detail\":\"VRCSpatialAudioSource x2\"}],\"motions\":[]}";

        private static readonly PackageHeader Header = new PackageHeader(1, 1000, 0, 0, 4096, 123456789);

        private static bool Parse(string json, out PackageManifest manifest, out ManifestError error) =>
            ManifestJson.Instance.TryParse(Encoding.UTF8.GetBytes(json), out manifest, out error);

        /// <summary>Every field, nested ones and array elements included, holds a value other than its default.</summary>
        private static PackageManifest FullManifest() => new PackageManifest
        {
            formatVersion = 1,
            kind = "avatar",
            exportId = "3f2c9a0d6b7e4c1fa2b3c4d5e6f70812",
            lineageId = "9a1b3c5d7e9f4a2b8c6d0e1f2a3b4c5d",
            displayName = "MyAvatar",
            createdAtUtc = "2026-10-22T10:00:00Z",
            exporter = new ExporterInfo
            {
                version = "1.0.0",
                unityVersion = "2022.3.22f1",
                buildTarget = "StandaloneWindows64",
                colorSpace = "Linear",
                vrcSdkVersion = "3.10.5",
                tools = new[] { "nadena.dev.ndmf@1.15.0", "com.anatawa12.avatar-optimizer@1.8.0" },
            },
            runtimeSchemaVersion = 1,
            bundle = new BundleInfo
            {
                name = "op-3f2c9a0d6b7e4c1fa2b3c4d5e6f70812",
                crc = 2864434397,
                sha256 = "0123456789abcdef0123456789abcdef0123456789abcdef0123456789abcdef",
                length = 123456789,
                rootAsset = "definition",
            },
            features = new FeatureInfo { fx = true, menuPages = 5, physBones = 14, constraints = 3, contacts = 2, eyeLook = true, blink = true },
            compositing = new CompositingInfo { requiresPrecise = true, reasons = new[] { "additive:1", "grabpass:1" } },
            stats = new StatsInfo
            {
                triangles = 69000,
                materials = 18,
                skinnedMeshes = 6,
                physBoneTransforms = 220,
                estimatedTextureBytes = 5000000000L,
                estimatedMeshBytes = 38000000,
            },
            warnings = new[]
            {
                new WarningInfo { code = "W_REMOVED_COMPONENT", detail = "VRCSpatialAudioSource x2" },
                new WarningInfo { code = "W_DEF_CLAMPED", detail = "PhysBone pull 1.5" },
            },
            motions = new[]
            {
                new MotionInfo { id = "walk", role = "Walk", sourceName = "Walk.fbx", length = 1.25f, loop = true, hasRootMotion = true },
            },
        };

        /// <summary>{"formatVersion":1,"x":[[…]]} whose deepest array sits <paramref name="depth"/> levels down, counting the root.</summary>
        private static string NestedArrays(int depth) =>
            "{\"formatVersion\":1,\"x\":" + new string('[', depth - 1) + new string(']', depth - 1) + "}";

        private static bool IsLeaf(Type type) => type.IsPrimitive || type.IsEnum || type == typeof(string);

        private static void AssertJsonInvalid(string json)
        {
            Assert.That(Parse(json, out PackageManifest m, out ManifestError e), Is.False);
            Assert.That(e.Code, Is.EqualTo(ManifestErrorCode.JsonInvalid));
            Assert.That(m, Is.Null);
        }

        private static void AssertNoDefaults(object value, string path)
        {
            Assert.That(value, Is.Not.Null, path);
            Type type = value.GetType();
            if (IsLeaf(type))
            {
                object unset = type == typeof(string) ? "" : Activator.CreateInstance(type);
                Assert.That(value, Is.Not.EqualTo(unset), path + " keeps its default, so losing it in the round trip would go unnoticed");
            }
            else if (type.IsArray)
            {
                var array = (Array)value;
                Assert.That(array.Length, Is.GreaterThan(0), path);
                for (int i = 0; i < array.Length; i++) AssertNoDefaults(array.GetValue(i), path + "[" + i + "]");
            }
            else
            {
                foreach (FieldInfo field in type.GetFields(BindingFlags.Public | BindingFlags.Instance))
                {
                    AssertNoDefaults(field.GetValue(value), path + "." + field.Name);
                }
            }
        }

        private static void AssertSameFields(object expected, object actual, string path)
        {
            if (expected == null || actual == null)
            {
                Assert.That(actual, Is.EqualTo(expected), path);
                return;
            }
            Type type = expected.GetType();
            Assert.That(actual.GetType(), Is.EqualTo(type), path);
            if (IsLeaf(type))
            {
                Assert.That(actual, Is.EqualTo(expected), path);
            }
            else if (type.IsArray)
            {
                var e = (Array)expected;
                var a = (Array)actual;
                Assert.That(a.Length, Is.EqualTo(e.Length), path + ".Length");
                for (int i = 0; i < e.Length; i++) AssertSameFields(e.GetValue(i), a.GetValue(i), path + "[" + i + "]");
            }
            else
            {
                foreach (FieldInfo field in type.GetFields(BindingFlags.Public | BindingFlags.Instance))
                {
                    AssertSameFields(field.GetValue(expected), field.GetValue(actual), path + "." + field.Name);
                }
            }
        }

        [Test]
        public void ValidManifestParsesAndValidates()
        {
            Assert.That(Parse(ValidJson, out PackageManifest m, out _), Is.True);

            Assert.That(m.exportId, Is.EqualTo("3f2c9a0d6b7e4c1fa2b3c4d5e6f70812"));
            Assert.That(m.exporter.tools, Is.EqualTo(new[] { "nadena.dev.ndmf@1.15.0" }));
            Assert.That(m.bundle.crc, Is.EqualTo(2864434397u));
            Assert.That(m.features.physBones, Is.EqualTo(14));
            Assert.That(m.stats.estimatedTextureBytes, Is.EqualTo(412000000L));
            Assert.That(m.warnings.Length, Is.EqualTo(1));
            Assert.That(m.warnings[0].detail, Is.EqualTo("VRCSpatialAudioSource x2"));
            Assert.That(ManifestValidator.Validate(m, Header), Is.Empty);
        }

        [Test]
        public void TruncatedJsonIsJsonInvalid()
        {
            AssertJsonInvalid(ValidJson.Substring(0, ValidJson.Length / 2));
        }

        [Test]
        public void JsonMissingOnlyTheLastBraceIsJsonInvalid()
        {
            AssertJsonInvalid(ValidJson.Substring(0, ValidJson.Length - 1));
        }

        [Test]
        public void JsonCutInsideAStringAndClosedWithABraceIsJsonInvalid()
        {
            string json = ValidJson.Substring(0, ValidJson.Length / 2) + "}";
            Assert.That(json.Count(c => c == '"') % 2, Is.EqualTo(1), "the cut must leave a string open");

            AssertJsonInvalid(json);
        }

        // Each case starts with '{' and ends with '}', so only JsonUtility itself can reject it.
        [TestCase("{\"formatVersion\":1,}")]
        [TestCase("{\"formatVersion\":}")]
        [TestCase("{\"exporter\":{\"version\":\"1.0.0\"}")]
        public void BracedButMalformedJsonIsJsonInvalid(string json)
        {
            AssertJsonInvalid(json);
        }

        [TestCase("[]")]
        [TestCase("[{\"formatVersion\":1}]")]
        [TestCase("null")]
        [TestCase("1")]
        [TestCase("\"text\"")]
        [TestCase("")]
        [TestCase("   ")]
        public void NonObjectRootIsJsonInvalid(string json)
        {
            AssertJsonInvalid(json);
        }

        [Test]
        public void InvalidUtf8IsJsonInvalid()
        {
            byte[] bytes = Encoding.UTF8.GetBytes(ValidJson);
            bytes[40] = 0xFF;

            Assert.That(ManifestJson.Instance.TryParse(bytes, out _, out ManifestError e), Is.False);
            Assert.That(e.Code, Is.EqualTo(ManifestErrorCode.JsonInvalid));
        }

        [Test]
        public void NullBytesAreJsonInvalid()
        {
            Assert.That(ManifestJson.Instance.TryParse(null, out _, out ManifestError e), Is.False);
            Assert.That(e.Code, Is.EqualTo(ManifestErrorCode.JsonInvalid));
        }

        [Test]
        public void NestingUpToTheLimitIsAccepted()
        {
            Assert.That(Parse(NestedArrays(ManifestJson.MaxNestingDepth), out PackageManifest m, out _), Is.True);
            Assert.That(m.formatVersion, Is.EqualTo(1));
        }

        [TestCase(ManifestJson.MaxNestingDepth + 1)]
        [TestCase(100000)]
        public void NestingBeyondTheLimitIsJsonInvalid(int depth)
        {
            AssertJsonInvalid(NestedArrays(depth));
        }

        [Test]
        public void BracketsAndEscapedQuotesInsideStringsDoNotCountAsNesting()
        {
            string json = "{\"formatVersion\":1,\"displayName\":\"\\\"" + new string('[', 100) + "{\"}";

            Assert.That(Parse(json, out PackageManifest m, out _), Is.True);
            Assert.That(m.displayName, Is.EqualTo("\"" + new string('[', 100) + "{"));
        }

        [Test]
        public void MissingFormatVersionKeyBecomesZeroAndMissingField()
        {
            string json = ValidJson.Replace("\"formatVersion\":1,", "");
            Assert.That(json, Does.Not.Contain("formatVersion"));

            Assert.That(Parse(json, out PackageManifest m, out _), Is.True);

            Assert.That(m.formatVersion, Is.EqualTo(0));
            var errors = ManifestValidator.Validate(m, Header);
            Assert.That(errors.Any(x => x.Code == ManifestErrorCode.MissingField && x.Field == "formatVersion"), Is.True);
        }

        [Test]
        public void MissingNestedObjectKeepsItsDefaults()
        {
            string json = "{\"formatVersion\":1}";

            Assert.That(Parse(json, out PackageManifest m, out _), Is.True);

            Assert.That(m.exporter, Is.Not.Null);
            Assert.That(m.exporter.unityVersion, Is.EqualTo(""));
            Assert.That(m.bundle, Is.Not.Null);
            Assert.That(m.warnings, Is.Not.Null);
            Assert.That(ManifestValidator.Validate(m, Header).Any(x => x.Field == "exporter.unityVersion"), Is.True);
        }

        [Test]
        public void LeadingBomIsAccepted()
        {
            byte[] body = Encoding.UTF8.GetBytes(ValidJson);
            byte[] withBom = new byte[] { 0xEF, 0xBB, 0xBF }.Concat(body).ToArray();

            Assert.That(ManifestJson.Instance.TryParse(withBom, out PackageManifest m, out _), Is.True);
            Assert.That(m.formatVersion, Is.EqualTo(1));
            Assert.That(ManifestValidator.Validate(m, Header), Is.Empty);
        }

        [Test]
        public void SurroundingWhitespaceIsAccepted()
        {
            Assert.That(Parse("\r\n  " + ValidJson + "\n", out PackageManifest m, out _), Is.True);
            Assert.That(m.formatVersion, Is.EqualTo(1));
        }

        [Test]
        public void UnknownKeysAreIgnored()
        {
            string json = ValidJson.Replace("\"kind\":\"avatar\",", "\"kind\":\"avatar\",\"futureField\":{\"a\":[1,2,3]},\"futureFlag\":true,");

            Assert.That(Parse(json, out PackageManifest m, out _), Is.True);
            Assert.That(m.kind, Is.EqualTo("avatar"));
            Assert.That(ManifestValidator.Validate(m, Header), Is.Empty);
        }

        [Test]
        public void ExtremeCrcAndLengthSurviveARoundTrip()
        {
            Assert.That(Parse(ValidJson, out PackageManifest m, out _), Is.True);
            m.bundle.crc = uint.MaxValue;
            m.bundle.length = int.MaxValue;

            byte[] utf8 = ManifestJson.ToUtf8(m);
            Assert.That(ManifestJson.Instance.TryParse(utf8, out PackageManifest back, out _), Is.True);

            Assert.That(back.bundle.crc, Is.EqualTo(4294967295u));
            Assert.That(back.bundle.length, Is.EqualTo(2147483647L));
        }

        [Test]
        public void ExtremeCrcAndLengthAreWrittenAsPlainDecimals()
        {
            PackageManifest m = FullManifest();
            m.bundle.crc = uint.MaxValue;
            m.bundle.length = int.MaxValue;

            string json = Encoding.UTF8.GetString(ManifestJson.ToUtf8(m));

            Assert.That(json, Does.Contain("\"crc\":4294967295"));
            Assert.That(json, Does.Contain("\"length\":2147483647"));
        }

        [Test]
        public void ExtremeCrcAndLengthAreReadFromHandWrittenJson()
        {
            string json = ValidJson
                .Replace("\"crc\":2864434397", "\"crc\":4294967295")
                .Replace("\"length\":123456789", "\"length\":2147483647");
            Assert.That(json, Does.Contain("\"crc\":4294967295"));
            Assert.That(json, Does.Contain("\"length\":2147483647"));

            Assert.That(Parse(json, out PackageManifest m, out _), Is.True);

            Assert.That(m.bundle.crc, Is.EqualTo(4294967295u));
            Assert.That(m.bundle.length, Is.EqualTo(2147483647L));
        }

        [Test]
        public void RoundTripKeepsEveryField()
        {
            PackageManifest m = FullManifest();
            AssertNoDefaults(m, "manifest");

            Assert.That(ManifestJson.Instance.TryParse(ManifestJson.ToUtf8(m), out PackageManifest back, out _), Is.True);

            AssertSameFields(m, back, "manifest");
        }

        [Test]
        public void OutputHasNoByteOrderMark()
        {
            byte[] utf8 = ManifestJson.ToUtf8(new PackageManifest());

            Assert.That(utf8[0], Is.EqualTo((byte)'{'));
        }
    }
}
