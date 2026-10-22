namespace OpenPerch.Format
{
    /// <summary>Turns manifest bytes into a <see cref="PackageManifest"/> without validating its values.</summary>
    public interface IManifestParser
    {
        /// <returns>False with a <see cref="ManifestErrorCode.JsonInvalid"/> error when the bytes are not a JSON object.</returns>
        bool TryParse(byte[] utf8, out PackageManifest manifest, out ManifestError error);
    }
}
