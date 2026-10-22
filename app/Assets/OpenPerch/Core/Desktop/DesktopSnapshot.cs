using System;

namespace OpenPerch.App.Core
{
    /// <summary>
    /// Published by the probe thread and never mutated afterwards, so it can be read from any thread.
    /// </summary>
    public sealed class DesktopSnapshot
    {
        public long Seq;
        public MonitorInfo[] Monitors = Array.Empty<MonitorInfo>();

        /// <summary>Ordered from the topmost window down.</summary>
        public WindowInfo[] Windows = Array.Empty<WindowInfo>();

        public TaskbarInfo[] Taskbars = Array.Empty<TaskbarInfo>();
        public NotificationState Notify = NotificationState.AcceptsNotifications;
        public IntPtr ForegroundHwnd;
    }
}
