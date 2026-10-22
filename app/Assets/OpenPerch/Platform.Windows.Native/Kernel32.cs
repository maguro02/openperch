using System;
using System.Runtime.InteropServices;

namespace OpenPerch.App.Platform.Windows.Native
{
    /// <summary>
    /// kernel32.dll declarations: the single-instance mutex, the high-resolution wait timer and raw file reads.
    /// </summary>
    internal static class Kernel32
    {
        private const string Dll = "kernel32.dll";

        public const int S_OK = 0;

        public const int ERROR_SUCCESS = 0;
        public const int ERROR_FILE_NOT_FOUND = 2;
        public const int ERROR_PATH_NOT_FOUND = 3;
        public const int ERROR_ACCESS_DENIED = 5;
        public const int ERROR_ALREADY_EXISTS = 183;

        public static readonly IntPtr INVALID_HANDLE_VALUE = new IntPtr(-1);

        public const uint GENERIC_READ = 0x80000000;
        public const uint FILE_SHARE_READ = 0x1;
        public const uint FILE_SHARE_WRITE = 0x2;
        public const uint FILE_SHARE_DELETE = 0x4;
        public const uint OPEN_EXISTING = 3;
        public const uint FILE_ATTRIBUTE_NORMAL = 0x80;

        public const uint CREATE_WAITABLE_TIMER_MANUAL_RESET = 0x1;

        /// <summary>Supported from Windows 10 1803; earlier builds fail the create with ERROR_INVALID_PARAMETER.</summary>
        public const uint CREATE_WAITABLE_TIMER_HIGH_RESOLUTION = 0x2;

        public const uint TIMER_ALL_ACCESS = 0x1F0003;

        public const uint WAIT_OBJECT_0 = 0x0;
        public const uint WAIT_TIMEOUT = 0x102;
        public const uint WAIT_FAILED = 0xFFFFFFFF;
        public const uint INFINITE = 0xFFFFFFFF;

        /// <remarks>
        /// Opening an existing named mutex also succeeds; <see cref="Marshal.GetLastWin32Error"/> then reports
        /// <see cref="ERROR_ALREADY_EXISTS"/>.
        /// </remarks>
        [DllImport(Dll, EntryPoint = "CreateMutexW", CharSet = CharSet.Unicode, ExactSpelling = true, SetLastError = true)]
        public static extern IntPtr CreateMutex(IntPtr lpMutexAttributes, [MarshalAs(UnmanagedType.Bool)] bool bInitialOwner, string lpName);

        [DllImport(Dll, CharSet = CharSet.Unicode, ExactSpelling = true, SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern bool CloseHandle(IntPtr hObject);

        /// <returns>The handle, or <see cref="INVALID_HANDLE_VALUE"/> on failure.</returns>
        [DllImport(Dll, EntryPoint = "CreateFileW", CharSet = CharSet.Unicode, ExactSpelling = true, SetLastError = true)]
        public static extern IntPtr CreateFile(
            string lpFileName,
            uint dwDesiredAccess,
            uint dwShareMode,
            IntPtr lpSecurityAttributes,
            uint dwCreationDisposition,
            uint dwFlagsAndAttributes,
            IntPtr hTemplateFile);

        [DllImport(Dll, CharSet = CharSet.Unicode, ExactSpelling = true, SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern bool ReadFile(IntPtr hFile, [Out] byte[] lpBuffer, uint nNumberOfBytesToRead, out uint lpNumberOfBytesRead, IntPtr lpOverlapped);

        [DllImport(Dll, EntryPoint = "CreateWaitableTimerExW", CharSet = CharSet.Unicode, ExactSpelling = true, SetLastError = true)]
        public static extern IntPtr CreateWaitableTimerEx(IntPtr lpTimerAttributes, string lpTimerName, uint dwFlags, uint dwDesiredAccess);

        /// <param name="lpDueTime">In 100 ns units; negative values are relative to now, positive values are absolute UTC file times.</param>
        [DllImport(Dll, CharSet = CharSet.Unicode, ExactSpelling = true, SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern bool SetWaitableTimer(
            IntPtr hTimer,
            [In] ref long lpDueTime,
            int lPeriod,
            IntPtr pfnCompletionRoutine,
            IntPtr lpArgToCompletionRoutine,
            [MarshalAs(UnmanagedType.Bool)] bool fResume);

        [DllImport(Dll, CharSet = CharSet.Unicode, ExactSpelling = true, SetLastError = true)]
        public static extern uint WaitForSingleObject(IntPtr hHandle, uint dwMilliseconds);

        [DllImport(Dll, EntryPoint = "GetModuleHandleW", CharSet = CharSet.Unicode, ExactSpelling = true, SetLastError = true)]
        public static extern IntPtr GetModuleHandle(string lpModuleName);

        /// <returns>A pseudo handle that needs no <see cref="CloseHandle"/>.</returns>
        [DllImport(Dll, CharSet = CharSet.Unicode, ExactSpelling = true)]
        public static extern IntPtr GetCurrentProcess();

        [DllImport(Dll, CharSet = CharSet.Unicode, ExactSpelling = true)]
        public static extern uint GetCurrentProcessId();

        [DllImport(Dll, CharSet = CharSet.Unicode, ExactSpelling = true)]
        public static extern uint GetCurrentThreadId();
    }
}
