using System;
using FactionWars.UI.Models;

namespace FactionWars.UI.Interfaces
{
    /// <summary>
    /// Service interface for managing notifications.
    /// Handles queuing, priority, and display of notifications.
    /// </summary>
    public interface INotificationService
    {
        /// <summary>
        /// Gets the number of notifications currently displayed.
        /// </summary>
        int ActiveNotificationCount { get; }

        /// <summary>
        /// Gets the number of notifications waiting in the queue.
        /// </summary>
        int QueuedNotificationCount { get; }

        /// <summary>
        /// Shows a notification.
        /// </summary>
        /// <param name="notification">The notification to show.</param>
        void Show(Notification notification);

        /// <summary>
        /// Shows an info notification.
        /// </summary>
        /// <param name="title">The notification title.</param>
        /// <param name="message">The notification message.</param>
        /// <param name="priority">The priority level (default: Normal).</param>
        /// <param name="durationSeconds">Display duration in seconds (default: 3.0).</param>
        void ShowInfo(string title, string message, NotificationPriority priority = NotificationPriority.Normal, float durationSeconds = 3.0f);

        /// <summary>
        /// Shows a success notification.
        /// </summary>
        /// <param name="title">The notification title.</param>
        /// <param name="message">The notification message.</param>
        /// <param name="priority">The priority level (default: Normal).</param>
        /// <param name="durationSeconds">Display duration in seconds (default: 3.0).</param>
        void ShowSuccess(string title, string message, NotificationPriority priority = NotificationPriority.Normal, float durationSeconds = 3.0f);

        /// <summary>
        /// Shows a warning notification.
        /// </summary>
        /// <param name="title">The notification title.</param>
        /// <param name="message">The notification message.</param>
        /// <param name="priority">The priority level (default: Normal).</param>
        /// <param name="durationSeconds">Display duration in seconds (default: 3.0).</param>
        void ShowWarning(string title, string message, NotificationPriority priority = NotificationPriority.Normal, float durationSeconds = 3.0f);

        /// <summary>
        /// Shows an error notification.
        /// </summary>
        /// <param name="title">The notification title.</param>
        /// <param name="message">The notification message.</param>
        /// <param name="priority">The priority level (default: Normal).</param>
        /// <param name="durationSeconds">Display duration in seconds (default: 3.0).</param>
        void ShowError(string title, string message, NotificationPriority priority = NotificationPriority.Normal, float durationSeconds = 3.0f);

        /// <summary>
        /// Dismisses a specific notification.
        /// </summary>
        /// <param name="id">The notification ID to dismiss.</param>
        void Dismiss(Guid id);

        /// <summary>
        /// Clears all notifications (displayed and queued).
        /// </summary>
        void ClearAll();

        /// <summary>
        /// Processes the notification queue, showing queued notifications if slots are available.
        /// Should be called periodically (e.g., each frame or tick).
        /// </summary>
        void ProcessQueue();
    }
}
