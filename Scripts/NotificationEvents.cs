using System;

namespace RAXY.Notification
{
    /// <summary>
    /// Raised once per notification after its dismiss cycle completes (exit animation finished and view hidden or instance destroyed).
    /// Not raised when fullscreen views are hidden during manager lane setup.
    /// </summary>
    public static class NotificationEvents
    {
        public static event Action<NotificationDismissedInfo> Dismissed;

        internal static void RaiseDismissed(NotificationDismissedInfo info)
        {
            Dismissed?.Invoke(info);
        }
    }
}
