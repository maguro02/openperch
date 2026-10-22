namespace OpenPerch.Runtime
{
    /// <summary>Whether a PhysBone interaction (collision, grabbing, posing) is allowed.</summary>
    public enum PbAllow
    {
        False = 0,
        True = 1,

        /// <summary>Decided by the separate self / others flags next to the setting.</summary>
        Other = 2,
    }
}
