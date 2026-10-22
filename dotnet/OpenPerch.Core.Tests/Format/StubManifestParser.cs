using OpenPerch.Format;

namespace OpenPerch.Core.Tests.Format
{
    /// <summary>Hands back a prepared manifest whatever the bytes are, so the verifier is tested without a JSON library.</summary>
    public sealed class StubManifestParser : IManifestParser
    {
        private readonly PackageManifest _manifest;
        private readonly bool _succeeds;
        private readonly ManifestError _error;

        public StubManifestParser(PackageManifest manifest)
        {
            _manifest = manifest;
            _succeeds = true;
        }

        private StubManifestParser(ManifestError error)
        {
            _succeeds = false;
            _error = error;
        }

        public int Calls { get; private set; }

        public byte[] LastBytes { get; private set; }

        public static StubManifestParser Failing(ManifestError error) => new StubManifestParser(error);

        public bool TryParse(byte[] utf8, out PackageManifest manifest, out ManifestError error)
        {
            Calls++;
            LastBytes = utf8;
            manifest = _succeeds ? _manifest : null;
            error = _succeeds ? default : _error;
            return _succeeds;
        }
    }
}
