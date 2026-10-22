using System;

namespace OpenPerch.Format
{
    /// <summary>What a build of the app can read. The values for the shipping app live in <c>OpenPerch.App.Core.AppProfiles</c>.</summary>
    public sealed class AppProfile
    {
        /// <summary>The Unity version the app player is built with.</summary>
        public UnityVersion AppUnity;

        /// <summary>The highest runtimeSchemaVersion the app can deserialize.</summary>
        public int AppSchema;

        public int[] SupportedFormats = { 1 };

        /// <summary>Whether a Unity 6 build of the app accepts packages exported with Unity 2022.3.</summary>
        public bool AcceptLegacy2022;

        public KnownBad[] KnownBadExporters = Array.Empty<KnownBad>();
    }
}
