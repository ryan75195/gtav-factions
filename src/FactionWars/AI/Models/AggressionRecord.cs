using System;

namespace FactionWars.AI.Models
{
    /// <summary>
    /// Represents a recorded instance of aggression against a faction.
    /// Used to track player and AI attacks for response calculations.
    /// </summary>
    public class AggressionRecord
    {
        /// <summary>
        /// The ID of the faction or player that initiated the aggression.
        /// </summary>
        public string AggressorId { get; }

        /// <summary>
        /// The ID of the zone that was attacked.
        /// </summary>
        public string TargetZoneId { get; }

        /// <summary>
        /// The amount of damage dealt in the attack (troops lost, control lost, etc.).
        /// </summary>
        public int DamageDealt { get; }

        /// <summary>
        /// When the aggression occurred.
        /// </summary>
        public DateTime Timestamp { get; }

        /// <summary>
        /// Creates a new aggression record.
        /// </summary>
        /// <param name="aggressorId">The attacking entity's ID.</param>
        /// <param name="targetZoneId">The attacked zone's ID.</param>
        /// <param name="damageDealt">The damage dealt.</param>
        /// <exception cref="ArgumentNullException">Thrown if aggressorId or targetZoneId is null.</exception>
        /// <exception cref="ArgumentException">Thrown if aggressorId or targetZoneId is empty.</exception>
        /// <exception cref="ArgumentOutOfRangeException">Thrown if damageDealt is negative.</exception>
        public AggressionRecord(string aggressorId, string targetZoneId, int damageDealt)
        {
            if (aggressorId == null)
                throw new ArgumentNullException(nameof(aggressorId));
            if (string.IsNullOrWhiteSpace(aggressorId))
                throw new ArgumentException("Aggressor ID cannot be empty or whitespace.", nameof(aggressorId));

            if (targetZoneId == null)
                throw new ArgumentNullException(nameof(targetZoneId));
            if (string.IsNullOrWhiteSpace(targetZoneId))
                throw new ArgumentException("Target zone ID cannot be empty or whitespace.", nameof(targetZoneId));

            if (damageDealt < 0)
                throw new ArgumentOutOfRangeException(nameof(damageDealt), "Damage dealt cannot be negative.");

            AggressorId = aggressorId;
            TargetZoneId = targetZoneId;
            DamageDealt = damageDealt;
            Timestamp = DateTime.UtcNow;
        }

        /// <summary>
        /// Creates a new aggression record with a specific timestamp (for testing).
        /// </summary>
        internal AggressionRecord(string aggressorId, string targetZoneId, int damageDealt, DateTime timestamp)
            : this(aggressorId, targetZoneId, damageDealt)
        {
            Timestamp = timestamp;
        }

        public override string ToString()
        {
            return $"Aggression: {AggressorId} attacked {TargetZoneId} for {DamageDealt} damage at {Timestamp}";
        }
    }
}
