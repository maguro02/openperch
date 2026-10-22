using System;
using System.IO;

namespace OpenPerch.Core.Tests.Settings
{
    /// <summary>A new folder under the system temp folder, deleted with everything in it on dispose.</summary>
    public sealed class TempDir : IDisposable
    {
        public TempDir()
        {
            Root = Path.Combine(Path.GetTempPath(), "openperch-tests-" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(Root);
        }

        public string Root { get; }

        public string Combine(params string[] parts) => Path.Combine(Root, Path.Combine(parts));

        public void Dispose()
        {
            if (Directory.Exists(Root)) Directory.Delete(Root, true);
        }
    }
}
