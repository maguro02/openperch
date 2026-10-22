using System;

namespace OpenPerch.Format
{
    /// <summary>Thrown when a <c>.opk</c> container breaks a layout rule.</summary>
    public sealed class PackageFormatException : Exception
    {
        public PackageFormatException(PackageError error)
            : base("Invalid .opk container: " + error)
        {
            Error = error;
        }

        public PackageError Error { get; }
    }
}
