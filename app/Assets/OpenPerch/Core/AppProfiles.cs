using System;
using OpenPerch.Format;

namespace OpenPerch.App.Core
{
    /// <summary>The compatibility profile packages are checked against by this build of the app.</summary>
    public static class AppProfiles
    {
        /// <summary>Returns a new instance on every call, so no caller can change the profile another one sees.</summary>
        public static AppProfile Current => new AppProfile
        {
            // The player's own editor version: a bundle from a newer patch may use serialized data this player cannot read.
            AppUnity = new UnityVersion(2022, 3, 62, 'f', 3),

            // Must equal OpenPerch.Runtime.RuntimeSchema.Current of the runtime package this app is built with. App.Core
            // cannot reference OpenPerch.Runtime (it depends on UnityEngine), so the value is repeated here. If it were
            // lower, packages the app can deserialize would be refused as SchemaTooNew; if higher, packages using runtime
            // types or fields this player lacks would be accepted, and Unity drops such data silently when loading.
            AppSchema = 1,

            SupportedFormats = new[] { 1 },
            AcceptLegacy2022 = false,
            KnownBadExporters = Array.Empty<KnownBad>(),
        };
    }
}
