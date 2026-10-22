using System;

namespace OpenPerch.Format
{
    /// <summary>Which Unity editors the exporter accepts, judged before anything is exported.</summary>
    public static class ExporterUnityPolicy
    {
        /// <summary>The editor version VRChat specifies for avatar projects.</summary>
        public const string MinUnity2022 = "2022.3.22f1";

        /// <summary>The editor the 2022.3 build of the app uses; packages from a newer patch would be rejected as newer than the app.</summary>
        public const string MaxAppUnity2022 = "2022.3.62f3";

        private static readonly UnityVersion Min = ParseConstant(MinUnity2022);
        private static readonly UnityVersion Max = ParseConstant(MaxAppUnity2022);

        /// <summary>An unparsable version is <see cref="ExporterUnitySupport.Unsupported"/>.</summary>
        public static ExporterUnitySupport Evaluate(string unityVersion) =>
            UnityVersion.TryParse(unityVersion, out UnityVersion v) ? Evaluate(v) : ExporterUnitySupport.Unsupported;

        /// <summary>2022.3.22f1 to 2022.3.62f3 is supported, any 6000.x continues with a warning, everything else stops.</summary>
        public static ExporterUnitySupport Evaluate(UnityVersion v)
        {
            if (v.Major == 6000) return ExporterUnitySupport.Unity6AppOnly;
            if (v.CompareTo(Min) >= 0 && v.CompareTo(Max) <= 0) return ExporterUnitySupport.Supported;
            return ExporterUnitySupport.Unsupported;
        }

        private static UnityVersion ParseConstant(string s)
        {
            if (!UnityVersion.TryParse(s, out UnityVersion v)) throw new InvalidOperationException("Invalid Unity version constant: " + s);
            return v;
        }
    }
}
