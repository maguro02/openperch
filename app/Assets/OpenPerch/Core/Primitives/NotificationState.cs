namespace OpenPerch.App.Core
{
    /// <summary>Values match the Win32 <c>QUERY_USER_NOTIFICATION_STATE</c> enumeration.</summary>
    public enum NotificationState
    {
        NotPresent = 1,
        Busy = 2,
        D3DFullScreen = 3,
        PresentationMode = 4,
        AcceptsNotifications = 5,
        QuietTime = 6,
        App = 7,
    }
}
