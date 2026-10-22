using System;
using System.Runtime.InteropServices;

namespace OpenPerch.App.Platform.Windows.Native
{
    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
    internal struct WNDCLASSEXW
    {
        public uint cbSize;
        public uint style;

        /// <summary>
        /// A pointer from <c>Marshal.GetFunctionPointerForDelegate</c> on a <see cref="WNDPROC"/> held in a static field.
        /// The pointer is valid only while that delegate is alive, so keep it alive for as long as the class is registered.
        /// </summary>
        public IntPtr lpfnWndProc;

        public int cbClsExtra;
        public int cbWndExtra;
        public IntPtr hInstance;
        public IntPtr hIcon;
        public IntPtr hCursor;
        public IntPtr hbrBackground;
        public string lpszMenuName;
        public string lpszClassName;
        public IntPtr hIconSm;
    }
}
