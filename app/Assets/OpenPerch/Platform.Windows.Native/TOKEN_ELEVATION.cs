using System.Runtime.InteropServices;

namespace OpenPerch.App.Platform.Windows.Native
{
    [StructLayout(LayoutKind.Sequential)]
    internal struct TOKEN_ELEVATION
    {
        /// <summary>Non-zero when the token is elevated.</summary>
        public uint TokenIsElevated;
    }
}
