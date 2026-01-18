namespace FactionWars.UI.Models
{
    /// <summary>
    /// Defines the priority levels for notifications.
    /// Higher priority notifications may interrupt or take precedence over lower priority ones.
    /// </summary>
    public enum NotificationPriority
    {
        /// <summary>
        /// Low priority notification that can wait.
        /// </summary>
        Low = 0,

        /// <summary>
        /// Normal priority notification (default).
        /// </summary>
        Normal = 1,

        /// <summary>
        /// High priority notification that should be shown promptly.
        /// </summary>
        High = 2,

        /// <summary>
        /// Critical priority notification that requires immediate attention.
        /// </summary>
        Critical = 3
    }
}
