using System;
using System.Collections.Generic;
using FactionWars.UI.Interfaces;
using FactionWars.UI.Models;

namespace FactionWars.UI.Services
{
    /// <summary>
    /// Service for managing notifications with queuing and priority support.
    /// </summary>
    public class NotificationService : INotificationService
    {
        private const int DefaultMaxVisibleNotifications = 5;
        private const float DefaultDurationSeconds = 3.0f;

        private readonly INotificationRenderer _renderer;
        private readonly int _maxVisibleNotifications;
        private readonly Queue<Notification> _notificationQueue;

        /// <summary>
        /// Gets the number of notifications currently displayed.
        /// </summary>
        public int ActiveNotificationCount => _renderer.ActiveNotificationCount;

        /// <summary>
        /// Gets the number of notifications waiting in the queue.
        /// </summary>
        public int QueuedNotificationCount => _notificationQueue.Count;

        /// <summary>
        /// Creates a new notification service with default settings.
        /// </summary>
        /// <param name="renderer">The notification renderer implementation.</param>
        public NotificationService(INotificationRenderer renderer)
            : this(renderer, DefaultMaxVisibleNotifications)
        {
        }

        /// <summary>
        /// Creates a new notification service with custom settings.
        /// </summary>
        /// <param name="renderer">The notification renderer implementation.</param>
        /// <param name="maxVisibleNotifications">Maximum notifications to display at once.</param>
        public NotificationService(INotificationRenderer renderer, int maxVisibleNotifications)
        {
            _renderer = renderer ?? throw new ArgumentNullException(nameof(renderer));
            _maxVisibleNotifications = maxVisibleNotifications > 0 ? maxVisibleNotifications : DefaultMaxVisibleNotifications;
            _notificationQueue = new Queue<Notification>();
        }

        /// <summary>
        /// Shows a notification.
        /// </summary>
        /// <param name="notification">The notification to show.</param>
        public void Show(Notification notification)
        {
            if (notification == null)
                throw new ArgumentNullException(nameof(notification));

            if (_renderer.ActiveNotificationCount >= _maxVisibleNotifications)
            {
                // Queue the notification if we're at capacity
                _notificationQueue.Enqueue(notification);
            }
            else
            {
                _renderer.ShowNotification(notification);
            }
        }

        /// <summary>
        /// Shows an info notification.
        /// </summary>
        public void ShowInfo(string title, string message, NotificationPriority priority = NotificationPriority.Normal, float durationSeconds = DefaultDurationSeconds)
        {
            var notification = new Notification(title, message, NotificationType.Info, priority, durationSeconds);
            Show(notification);
        }

        /// <summary>
        /// Shows a success notification.
        /// </summary>
        public void ShowSuccess(string title, string message, NotificationPriority priority = NotificationPriority.Normal, float durationSeconds = DefaultDurationSeconds)
        {
            var notification = new Notification(title, message, NotificationType.Success, priority, durationSeconds);
            Show(notification);
        }

        /// <summary>
        /// Shows a warning notification.
        /// </summary>
        public void ShowWarning(string title, string message, NotificationPriority priority = NotificationPriority.Normal, float durationSeconds = DefaultDurationSeconds)
        {
            var notification = new Notification(title, message, NotificationType.Warning, priority, durationSeconds);
            Show(notification);
        }

        /// <summary>
        /// Shows an error notification.
        /// </summary>
        public void ShowError(string title, string message, NotificationPriority priority = NotificationPriority.Normal, float durationSeconds = DefaultDurationSeconds)
        {
            var notification = new Notification(title, message, NotificationType.Error, priority, durationSeconds);
            Show(notification);
        }

        /// <summary>
        /// Dismisses a specific notification.
        /// </summary>
        /// <param name="id">The notification ID to dismiss.</param>
        public void Dismiss(Guid id)
        {
            _renderer.HideNotification(id);
        }

        /// <summary>
        /// Clears all notifications (displayed and queued).
        /// </summary>
        public void ClearAll()
        {
            _notificationQueue.Clear();
            _renderer.ClearAll();
        }

        /// <summary>
        /// Processes the notification queue, showing queued notifications if slots are available.
        /// </summary>
        public void ProcessQueue()
        {
            while (_notificationQueue.Count > 0 && _renderer.ActiveNotificationCount < _maxVisibleNotifications)
            {
                var notification = _notificationQueue.Dequeue();
                _renderer.ShowNotification(notification);
            }
        }
    }
}
