using System;
using System.Runtime.InteropServices;

namespace OpenPerch.App.Platform.Windows.Native
{
    /// <summary>
    /// The <c>lParam</c> of <see cref="User32.WM_POWERBROADCAST"/> with <see cref="User32.PBT_POWERSETTINGCHANGE"/>.
    /// </summary>
    [StructLayout(LayoutKind.Sequential)]
    internal struct POWERBROADCAST_SETTING
    {
        public Guid PowerSetting;
        public uint DataLength;

        /// <summary>
        /// The first four bytes of the variable-length payload, which is the whole value for DWORD settings such as
        /// <see cref="User32.GUID_CONSOLE_DISPLAY_STATE"/>.
        /// </summary>
        public uint Data;
    }
}
