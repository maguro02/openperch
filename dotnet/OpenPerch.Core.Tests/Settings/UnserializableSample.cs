namespace OpenPerch.Core.Tests.Settings
{
    /// <summary>Lacks <c>[Serializable]</c>, which JsonUtility needs and System.Text.Json does not.</summary>
    public sealed class UnserializableSample
    {
        public int value;
    }
}
