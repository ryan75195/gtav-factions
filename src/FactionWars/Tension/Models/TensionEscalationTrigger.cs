using System;

namespace FactionWars.Tension.Models
{
    /// <summary>
    /// Represents an event that causes tension to escalate between two factions.
    /// Triggers have a type, severity, and can optionally be associated with a specific zone.
    /// </summary>
    public class TensionEscalationTrigger
    {
        /// <summary>
        /// The type of trigger event that occurred.
        /// </summary>
        public TensionTriggerType TriggerType { get; }

        /// <summary>
        /// The faction that initiated the aggressive action.
        /// </summary>
        public string AggressorFactionId { get; }

        /// <summary>
        /// The faction that was the target of the aggressive action.
        /// </summary>
        public string TargetFactionId { get; }

        /// <summary>
        /// The zone where the trigger event occurred, if applicable.
        /// </summary>
        public string? ZoneId { get; }

        /// <summary>
        /// The severity of the trigger event, which modifies the tension increase.
        /// </summary>
        public TriggerSeverity Severity { get; }

        /// <summary>
        /// The UTC time when this trigger was created.
        /// </summary>
        public DateTime Timestamp { get; }

        /// <summary>
        /// Optional metadata about the trigger event (e.g., amount raided, specific details).
        /// </summary>
        public string? Metadata { get; }

        /// <summary>
        /// The base tension increase for this trigger type before severity modifiers.
        /// </summary>
        public int BaseTensionIncrease
        {
            get
            {
                return TriggerType switch
                {
                    TensionTriggerType.BorderIncursion => 5,
                    TensionTriggerType.ZoneAttack => 15,
                    TensionTriggerType.ZoneCapture => 25,
                    TensionTriggerType.MemberKilled => 10,
                    TensionTriggerType.LeaderKilled => 30,
                    TensionTriggerType.ResourceRaided => 8,
                    TensionTriggerType.Sabotage => 12,
                    TensionTriggerType.TerritoryThreat => 7,
                    TensionTriggerType.RepeatedAggression => 20,
                    TensionTriggerType.AllyAttacked => 10,
                    _ => 5 // Default fallback
                };
            }
        }

        /// <summary>
        /// Creates a new tension escalation trigger.
        /// </summary>
        /// <param name="triggerType">The type of trigger event.</param>
        /// <param name="aggressorFactionId">The faction that initiated the action.</param>
        /// <param name="targetFactionId">The faction that was targeted.</param>
        /// <param name="zoneId">Optional zone where the event occurred.</param>
        /// <param name="severity">The severity of the event (default: Normal).</param>
        /// <param name="metadata">Optional metadata about the event.</param>
        /// <exception cref="ArgumentNullException">Thrown if aggressor or target faction ID is null.</exception>
        /// <exception cref="ArgumentException">Thrown if faction IDs are empty, whitespace, or the same.</exception>
        public TensionEscalationTrigger(
            TensionTriggerType triggerType,
            string aggressorFactionId,
            string targetFactionId,
            string? zoneId = null,
            TriggerSeverity severity = TriggerSeverity.Normal,
            string? metadata = null)
        {
            if (aggressorFactionId == null)
                throw new ArgumentNullException(nameof(aggressorFactionId));
            if (string.IsNullOrWhiteSpace(aggressorFactionId))
                throw new ArgumentException("Aggressor faction ID cannot be empty or whitespace.", nameof(aggressorFactionId));

            if (targetFactionId == null)
                throw new ArgumentNullException(nameof(targetFactionId));
            if (string.IsNullOrWhiteSpace(targetFactionId))
                throw new ArgumentException("Target faction ID cannot be empty or whitespace.", nameof(targetFactionId));

            if (aggressorFactionId == targetFactionId)
                throw new ArgumentException("Aggressor faction cannot be the same as target faction.", nameof(targetFactionId));

            TriggerType = triggerType;
            AggressorFactionId = aggressorFactionId;
            TargetFactionId = targetFactionId;
            ZoneId = zoneId;
            Severity = severity;
            Timestamp = DateTime.UtcNow;
            Metadata = metadata;
        }

        /// <summary>
        /// Calculates the effective tension increase after applying the severity multiplier.
        /// </summary>
        /// <returns>The tension increase value to apply.</returns>
        public int GetEffectiveTensionIncrease()
        {
            float multiplier = Severity switch
            {
                TriggerSeverity.Minor => 0.5f,
                TriggerSeverity.Normal => 1.0f,
                TriggerSeverity.Major => 1.5f,
                TriggerSeverity.Critical => 2.0f,
                _ => 1.0f
            };

            return (int)(BaseTensionIncrease * multiplier);
        }

        /// <summary>
        /// Checks if this trigger involves the specified faction.
        /// </summary>
        /// <param name="factionId">The faction ID to check.</param>
        /// <returns>True if the faction is either the aggressor or target.</returns>
        public bool InvolvesFaction(string factionId)
        {
            if (string.IsNullOrEmpty(factionId))
                return false;

            return AggressorFactionId == factionId || TargetFactionId == factionId;
        }

        public override string ToString()
        {
            return $"TensionTrigger[{TriggerType}]: {AggressorFactionId} -> {TargetFactionId} ({Severity}, +{GetEffectiveTensionIncrease()})";
        }
    }
}
