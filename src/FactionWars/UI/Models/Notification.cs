using System;

namespace FactionWars.UI.Models
{
    /// <summary>
    /// Represents a notification to be displayed to the player.
    /// </summary>
    public class Notification : IEquatable<Notification>
    {
        private const float DefaultDurationSeconds = 3.0f;

        /// <summary>
        /// Unique identifier for this notification.
        /// </summary>
        public Guid Id { get; }

        /// <summary>
        /// The notification title.
        /// </summary>
        public string Title { get; }

        /// <summary>
        /// The notification message body.
        /// </summary>
        public string Message { get; }

        /// <summary>
        /// The type of notification.
        /// </summary>
        public NotificationType Type { get; }

        /// <summary>
        /// The priority level of this notification.
        /// </summary>
        public NotificationPriority Priority { get; }

        /// <summary>
        /// How long the notification should be displayed in seconds.
        /// </summary>
        public float DurationSeconds { get; }

        /// <summary>
        /// When this notification was created.
        /// </summary>
        public DateTime CreatedAt { get; }

        /// <summary>
        /// Creates a new notification with all options specified.
        /// </summary>
        /// <param name="title">The notification title.</param>
        /// <param name="message">The notification message body.</param>
        /// <param name="type">The type of notification.</param>
        /// <param name="priority">The priority level.</param>
        /// <param name="durationSeconds">How long to display the notification.</param>
        public Notification(
            string title,
            string message,
            NotificationType type,
            NotificationPriority priority,
            float durationSeconds)
        {
            if (title == null)
                throw new ArgumentNullException(nameof(title));
            if (string.IsNullOrWhiteSpace(title))
                throw new ArgumentException("Title cannot be empty or whitespace.", nameof(title));

            if (message == null)
                throw new ArgumentNullException(nameof(message));
            if (string.IsNullOrEmpty(message))
                throw new ArgumentException("Message cannot be empty.", nameof(message));

            if (durationSeconds <= 0)
                throw new ArgumentOutOfRangeException(nameof(durationSeconds), "Duration must be greater than zero.");

            Id = Guid.NewGuid();
            Title = title;
            Message = message;
            Type = type;
            Priority = priority;
            DurationSeconds = durationSeconds;
            CreatedAt = DateTime.UtcNow;
        }

        /// <summary>
        /// Creates a new notification with default priority and duration.
        /// </summary>
        /// <param name="title">The notification title.</param>
        /// <param name="message">The notification message body.</param>
        /// <param name="type">The type of notification.</param>
        public Notification(string title, string message, NotificationType type)
            : this(title, message, type, NotificationPriority.Normal, DefaultDurationSeconds)
        {
        }

        /// <summary>
        /// Creates a new notification with default duration.
        /// </summary>
        /// <param name="title">The notification title.</param>
        /// <param name="message">The notification message body.</param>
        /// <param name="type">The type of notification.</param>
        /// <param name="priority">The priority level.</param>
        public Notification(string title, string message, NotificationType type, NotificationPriority priority)
            : this(title, message, type, priority, DefaultDurationSeconds)
        {
        }

        public bool Equals(Notification? other)
        {
            if (other is null) return false;
            if (ReferenceEquals(this, other)) return true;
            return Id.Equals(other.Id);
        }

        public override bool Equals(object? obj)
        {
            return Equals(obj as Notification);
        }

        public override int GetHashCode()
        {
            return Id.GetHashCode();
        }
    }
}
