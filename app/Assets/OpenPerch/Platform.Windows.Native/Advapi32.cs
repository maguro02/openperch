using System;
using System.Runtime.InteropServices;

namespace OpenPerch.App.Platform.Windows.Native
{
    /// <summary>advapi32.dll declarations: whether the own process token is elevated.</summary>
    internal static class Advapi32
    {
        private const string Dll = "advapi32.dll";

        public const uint TOKEN_QUERY = 0x0008;

        /// <summary><c>TOKEN_INFORMATION_CLASS.TokenElevation</c>, answered with <see cref="TOKEN_ELEVATION"/>.</summary>
        public const int TokenElevation = 20;

        [DllImport(Dll, CharSet = CharSet.Unicode, ExactSpelling = true, SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern bool OpenProcessToken(IntPtr ProcessHandle, uint DesiredAccess, out IntPtr TokenHandle);

        [DllImport(Dll, CharSet = CharSet.Unicode, ExactSpelling = true, SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern bool GetTokenInformation(
            IntPtr TokenHandle,
            int TokenInformationClass,
            out TOKEN_ELEVATION TokenInformation,
            uint TokenInformationLength,
            out uint ReturnLength);
    }
}
