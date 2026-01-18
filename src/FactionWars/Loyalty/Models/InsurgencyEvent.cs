using System;

namespace FactionWars.Loyalty.Models
{
    /// <summary>
    /// Represents an insurgency event that has occurred in a zone.
    /// Events can be uprisings, sabotage, or protests.
    /// </summary>
    public class InsurgencyEvent
    {
        /// <summary>
        /// The ID of the zone where the event occurred.
        /// </summary>
        public string ZoneId { get; }

        /// <summary>
        /// The ID of the faction controlling the zone when the event occurred.
        /// </summary>
        public string ControllingFactionId { get; }

        /// <summary>
        /// The ID of the faction the insurgents support.
        /// </summary>
        public string InsurgentFactionId { get; }

        /// <summary>
        /// The type of insurgency event.
        /// </summary>
        public InsurgencyEventType EventType { get; }

        /// <summary>
        /// The timestamp when the event occurred.
        /// </summary>
        public DateTime Timestamp { get; }

        /// <summary>
        /// Whether the event has been resolved.
        /// </summary>
        public bool IsResolved { get; private set; }

        /// <summary>
        /// The outcome of the event, if resolved.
        /// </summary>
        public InsurgencyOutcome? Outcome { get; private set; }

        /// <summary>
        /// Creates a new insurgency event.
        /// </summary>
        /// <param name="zoneId">The zone where the event occurs.</param>
        /// <param name="controllingFactionId">The faction controlling the zone.</param>
        /// <param name="insurgentFactionId">The faction the insurgents support.</param>
        /// <param name="eventType">The type of insurgency event.</param>
        public InsurgencyEvent(
            string zoneId,
            string controllingFactionId,
            string insurgentFactionId,
            InsurgencyEventType eventType)
        {
            ValidateString(zoneId, nameof(zoneId));
            ValidateString(controllingFactionId, nameof(controllingFactionId));
            ValidateString(insurgentFactionId, nameof(insurgentFactionId));

            ZoneId = zoneId;
            ControllingFactionId = controllingFactionId;
            InsurgentFactionId = insurgentFactionId;
            EventType = eventType;
            Timestamp = DateTime.UtcNow;
            IsResolved = false;
            Outcome = null;
        }

        private static void ValidateString(string value, string paramName)
        {
            if (value == null)
                throw new ArgumentNullException(paramName);
            if (string.IsNullOrWhiteSpace(value))
                throw new ArgumentException($"{paramName} cannot be empty or whitespace.", paramName);
        }

        /// <summary>
        /// Marks the event as resolved with the specified outcome.
        /// </summary>
        /// <param name="outcome">The outcome of the event.</param>
        public void MarkResolved(InsurgencyOutcome outcome)
        {
            IsResolved = true;
            Outcome = outcome;
        }

        public override string ToString()
        {
            var status = IsResolved ? $"Resolved: {Outcome}" : "Pending";
            return $"InsurgencyEvent[{ZoneId}]: {EventType} - {status}";
        }
    }
}
