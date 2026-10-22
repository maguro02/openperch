namespace OpenPerch.Core.Tests.Settings
{
    /// <summary>Backed by 64 bits, so Unity drops a field of this type while System.Text.Json keeps it.</summary>
    public enum WideSampleKind : long
    {
        None = 0,
        Far = 1L << 40,
    }
}
