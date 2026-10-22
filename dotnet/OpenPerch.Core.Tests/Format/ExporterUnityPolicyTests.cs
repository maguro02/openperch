using NUnit.Framework;
using OpenPerch.Format;

namespace OpenPerch.Core.Tests.Format
{
    [Category("T-VER")]
    public class ExporterUnityPolicyTests
    {
        [TestCase("2022.3.22f1", ExporterUnitySupport.Supported)]
        [TestCase("2022.3.22f1c1", ExporterUnitySupport.Supported)]
        [TestCase("2022.3.40f1", ExporterUnitySupport.Supported)]
        [TestCase("2022.3.62f3", ExporterUnitySupport.Supported)]
        [TestCase("2022.3.21f1", ExporterUnitySupport.Unsupported)]
        [TestCase("2022.3.22b1", ExporterUnitySupport.Unsupported)]
        [TestCase("2022.3.62p1", ExporterUnitySupport.Unsupported)]
        [TestCase("2022.3.63f1", ExporterUnitySupport.Unsupported)]
        [TestCase("2021.3.45f1", ExporterUnitySupport.Unsupported)]
        [TestCase("2023.2.20f1", ExporterUnitySupport.Unsupported)]
        [TestCase("6000.0.23f1", ExporterUnitySupport.Unity6AppOnly)]
        [TestCase("6000.3.0f1", ExporterUnitySupport.Unity6AppOnly)]
        [TestCase("6001.0.0a1", ExporterUnitySupport.Unsupported)]
        [TestCase("abc", ExporterUnitySupport.Unsupported)]
        [TestCase(null, ExporterUnitySupport.Unsupported)]
        public void Table(string unityVersion, ExporterUnitySupport expected)
        {
            Assert.That(ExporterUnityPolicy.Evaluate(unityVersion), Is.EqualTo(expected));
        }

        [Test]
        public void UpperBoundIsTheAppEditor()
        {
            Assert.That(ExporterUnityPolicy.MaxAppUnity2022, Is.EqualTo(OpenPerch.App.Core.AppProfiles.Current.AppUnity.ToString()));
        }
    }
}
