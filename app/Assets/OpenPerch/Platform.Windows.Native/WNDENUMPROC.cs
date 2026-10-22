using System;
using System.Runtime.InteropServices;

namespace OpenPerch.App.Platform.Windows.Native
{
    /// <summary>Callback of <see cref="User32.EnumWindows"/>; return false to stop the enumeration.</summary>
    [UnmanagedFunctionPointer(CallingConvention.Winapi)]
    [return: MarshalAs(UnmanagedType.Bool)]
    internal delegate bool WNDENUMPROC(IntPtr hwnd, IntPtr lParam);
}
