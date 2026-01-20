using System;

namespace FactionWars.UI.Models
{
    /// <summary>
    /// Represents an entry in the event feed displayed to the player.
    /// </summary>
    public class EventFeedEntry : IEquatable<EventFeedEntry>
    {
        /// <summary>
        /// Unique identifier for this entry.
        /// </summary>
        public Guid Id { get; }

        /// <summary>
        /// The message text to display.
        /// </summary>
        public string Message { get; }

        /// <summary>
        /// The category of this event.
        /// </summary>
        public EventFeedCategory Category { get; }

        /// <summary>
        /// The faction associated with this event, if any.
        /// Used for color-coding the entry.
        /// </summary>
        public string? FactionName { get; }

        /// <summary>
        /// When this entry was created.
        /// </summary>
        public DateTime Timestamp { get; }

        /// <summary>
        /// Creates a new event feed entry.
        /// </summary>
        /// <param name="message">The message text to display.</param>
        /// <param name="category">The category of this event.</param>
        /// <param name="factionName">Optional faction name for color-coding.</param>
        /// <param name="timestamp">When this entry was created.</param>
        public EventFeedEntry(string message, EventFeedCategory category, string? factionName, DateTime timestamp)
        {
            if (message == null)
                throw new ArgumentNullException(nameof(message));
            if (string.IsNullOrWhiteSpace(message))
                throw new ArgumentException("Message cannot be empty or whitespace.", nameof(message));

            Id = Guid.NewGuid();
            Message = message;
            Category = category;
            FactionName = factionName;
            Timestamp = timestamp;
        }

        public bool Equals(EventFeedEntry? other)
        {
            if (other is null) return false;
            if (ReferenceEquals(this, other)) return true;
            return Id.Equals(other.Id);
        }

        public override bool Equals(object? obj)
        {
            return Equals(obj as EventFeedEntry);
        }

        public override int GetHashCode()
        {
            return Id.GetHashCode();
        }
    }
}
