using System;

namespace FactionWars.UI.Models
{
    /// <summary>
    /// Represents a game event that should be shown to the player as an alert.
    /// </summary>
    public class EventAlert : IEquatable<EventAlert>
    {
        /// <summary>
        /// Unique identifier for this event alert.
        /// </summary>
        public Guid Id { get; }

        /// <summary>
        /// The type of event.
        /// </summary>
        public EventAlertType Type { get; }

        /// <summary>
        /// The name of the zone involved in this event.
        /// </summary>
        public string ZoneName { get; }

        /// <summary>
        /// The name of the primary faction involved in this event.
        /// </summary>
        public string FactionName { get; }

        /// <summary>
        /// The name of the target/opposing faction, if applicable.
        /// </summary>
        public string? TargetFactionName { get; }

        /// <summary>
        /// When this alert was created.
        /// </summary>
        public DateTime CreatedAt { get; }

        /// <summary>
        /// Creates a new event alert.
        /// </summary>
        /// <param name="type">The type of event.</param>
        /// <param name="zoneName">The zone involved in this event.</param>
        /// <param name="factionName">The primary faction involved.</param>
        /// <param name="targetFactionName">Optional target/opposing faction.</param>
        public EventAlert(EventAlertType type, string zoneName, string factionName, string? targetFactionName)
        {
            if (zoneName == null)
                throw new ArgumentNullException(nameof(zoneName));
            if (string.IsNullOrWhiteSpace(zoneName))
                throw new ArgumentException("Zone name cannot be empty or whitespace.", nameof(zoneName));

            if (factionName == null)
                throw new ArgumentNullException(nameof(factionName));
            if (string.IsNullOrWhiteSpace(factionName))
                throw new ArgumentException("Faction name cannot be empty or whitespace.", nameof(factionName));

            Id = Guid.NewGuid();
            Type = type;
            ZoneName = zoneName;
            FactionName = factionName;
            TargetFactionName = targetFactionName;
            CreatedAt = DateTime.UtcNow;
        }

        /// <summary>
        /// Gets the display title for this event alert.
        /// </summary>
        public string GetTitle()
        {
            return Type switch
            {
                EventAlertType.ZoneCaptured => "Zone Captured",
                EventAlertType.ZoneLost => "Zone Lost",
                EventAlertType.AttackIncoming => "Attack Incoming",
                EventAlertType.AttackLaunched => "Attack Launched",
                EventAlertType.ReinforcementsArriving => "Reinforcements",
                EventAlertType.ZoneContested => "Zone Contested",
                EventAlertType.VictoryImminent => "Victory Imminent",
                EventAlertType.DefeatImminent => "Defeat Imminent",
                _ => "Event Alert"
            };
        }

        /// <summary>
        /// Gets the display message for this event alert.
        /// </summary>
        public string GetMessage()
        {
            return Type switch
            {
                EventAlertType.ZoneCaptured => $"{ZoneName} is now under {FactionName}'s control!",
                EventAlertType.ZoneLost => $"{ZoneName} has been captured by {TargetFactionName ?? "enemies"}!",
                EventAlertType.AttackIncoming => $"{TargetFactionName ?? "Enemies"} attacking {ZoneName}!",
                EventAlertType.AttackLaunched => $"{FactionName} attacking {ZoneName}!",
                EventAlertType.ReinforcementsArriving => $"Reinforcements arriving at {ZoneName}!",
                EventAlertType.ZoneContested => $"{ZoneName} is under heavy combat!",
                EventAlertType.VictoryImminent => $"{FactionName} is close to total victory!",
                EventAlertType.DefeatImminent => $"{TargetFactionName ?? "The enemy"} is close to defeating {FactionName}!",
                _ => $"Event at {ZoneName}"
            };
        }

        /// <summary>
        /// Gets the notification type for displaying this alert.
        /// </summary>
        public NotificationType GetNotificationType()
        {
            return Type switch
            {
                EventAlertType.ZoneCaptured => NotificationType.Success,
                EventAlertType.ZoneLost => NotificationType.Error,
                EventAlertType.AttackIncoming => NotificationType.Warning,
                EventAlertType.AttackLaunched => NotificationType.Info,
                EventAlertType.ReinforcementsArriving => NotificationType.Info,
                EventAlertType.ZoneContested => NotificationType.Warning,
                EventAlertType.VictoryImminent => NotificationType.Success,
                EventAlertType.DefeatImminent => NotificationType.Error,
                _ => NotificationType.Info
            };
        }

        /// <summary>
        /// Gets the notification priority for displaying this alert.
        /// </summary>
        public NotificationPriority GetNotificationPriority()
        {
            return Type switch
            {
                EventAlertType.ZoneCaptured => NotificationPriority.High,
                EventAlertType.ZoneLost => NotificationPriority.High,
                EventAlertType.AttackIncoming => NotificationPriority.Critical,
                EventAlertType.AttackLaunched => NotificationPriority.Normal,
                EventAlertType.ReinforcementsArriving => NotificationPriority.Normal,
                EventAlertType.ZoneContested => NotificationPriority.High,
                EventAlertType.VictoryImminent => NotificationPriority.Critical,
                EventAlertType.DefeatImminent => NotificationPriority.Critical,
                _ => NotificationPriority.Normal
            };
        }

        public bool Equals(EventAlert? other)
        {
            if (other is null) return false;
            if (ReferenceEquals(this, other)) return true;
            return Id.Equals(other.Id);
        }

        public override bool Equals(object? obj)
        {
            return Equals(obj as EventAlert);
        }

        public override int GetHashCode()
        {
            return Id.GetHashCode();
        }
    }
}
