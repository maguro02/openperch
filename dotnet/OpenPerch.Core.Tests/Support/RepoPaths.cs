using System;
using System.IO;

namespace OpenPerch.Core.Tests.Support
{
    /// <summary>Locates files in the repository so tests can read docs and fixtures straight from the source tree.</summary>
    public static class RepoPaths
    {
        private static readonly Lazy<string> RootPath = new Lazy<string>(FindRoot);

        public static string Root => RootPath.Value;

        public static string Combine(params string[] parts) => Path.Combine(Root, Path.Combine(parts));

        private static string FindRoot()
        {
            var dir = new DirectoryInfo(AppContext.BaseDirectory);
            while (dir != null)
            {
                if (Directory.Exists(Path.Combine(dir.FullName, "dotnet")) &&
                    Directory.Exists(Path.Combine(dir.FullName, "app")) &&
                    Directory.Exists(Path.Combine(dir.FullName, "packages")))
                {
                    return dir.FullName;
                }
                dir = dir.Parent;
            }
            throw new DirectoryNotFoundException("Repository root not found above " + AppContext.BaseDirectory);
        }
    }
}
