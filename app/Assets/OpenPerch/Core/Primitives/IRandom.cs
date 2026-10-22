namespace OpenPerch.App.Core
{
    public interface IRandom
    {
        /// <summary>Returns a value in [0, 1).</summary>
        float Next01();

        /// <summary>Returns a value in [<paramref name="minInclusive"/>, <paramref name="maxExclusive"/>).</summary>
        int Range(int minInclusive, int maxExclusive);
    }
}
