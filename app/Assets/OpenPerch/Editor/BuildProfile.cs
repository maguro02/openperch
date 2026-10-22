namespace OpenPerch.App.Editor
{
    /// <summary>Unity stream a player build is made with. Each profile builds only with one pinned patch of its stream.</summary>
    public enum BuildProfile
    {
        /// <summary>Unity 2022.3 LTS, pinned to 2022.3.62f3.</summary>
        U2022 = 0,

        /// <summary>
        /// Unity 6000.3 LTS. Same settings as <see cref="U2022"/>, but without the splash screen. No patch is pinned yet,
        /// so building with this profile fails until one is.
        /// </summary>
        U6000 = 1,
    }
}
