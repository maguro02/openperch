namespace OpenPerch.App.Editor
{
    /// <summary>Unity stream a player build is made with.</summary>
    public enum BuildProfile
    {
        /// <summary>Unity 2022.3 LTS.</summary>
        U2022 = 0,

        /// <summary>Unity 6000.3 LTS. Same settings as <see cref="U2022"/>, but without the splash screen.</summary>
        U6000 = 1,
    }
}
