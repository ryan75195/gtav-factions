using System;
using FactionWars.Core.Interfaces;

namespace FactionWars.Combat.Models
{
    /// <summary>
    /// Represents a request for reinforcements during a combat encounter.
    /// </summary>
    public class ReinforcementRequest
    {
        /// <summary>
        /// The ID of the combat encounter this request is for.
        /// </summary>
        public string EncounterId { get; }

        /// <summary>
        /// The faction ID requesting reinforcements.
        /// </summary>
        public string FactionId { get; }

        /// <summary>
        /// The zone ID where reinforcements should spawn.
        /// </summary>
        public string ZoneId { get; }

        /// <summary>
        /// The number of peds requested.
        /// </summary>
        public int RequestedCount { get; }

        /// <summary>
        /// The position where reinforcements should spawn.
        /// </summary>
        public Vector3 SpawnPosition { get; }

        /// <summary>
        /// The UTC time when this request was created.
        /// </summary>
        public DateTime RequestedAt { get; }

        /// <summary>
        /// Creates a new reinforcement request.
        /// </summary>
        /// <param name="encounterId">The ID of the combat encounter.</param>
        /// <param name="factionId">The faction requesting reinforcements.</param>
        /// <param name="zoneId">The zone where reinforcements should spawn.</param>
        /// <param name="requestedCount">The number of peds to spawn.</param>
        /// <param name="spawnPosition">The spawn position for the reinforcements.</param>
        /// <exception cref="ArgumentException">Thrown if any string parameter is null, empty, or whitespace.</exception>
        /// <exception cref="ArgumentOutOfRangeException">Thrown if requestedCount is less than 1.</exception>
        public ReinforcementRequest(
            string encounterId,
            string factionId,
            string zoneId,
            int requestedCount,
            Vector3 spawnPosition)
        {
            if (string.IsNullOrWhiteSpace(encounterId))
                throw new ArgumentException("Encounter ID cannot be null, empty, or whitespace.", nameof(encounterId));
            if (string.IsNullOrWhiteSpace(factionId))
                throw new ArgumentException("Faction ID cannot be null, empty, or whitespace.", nameof(factionId));
            if (string.IsNullOrWhiteSpace(zoneId))
                throw new ArgumentException("Zone ID cannot be null, empty, or whitespace.", nameof(zoneId));
            if (requestedCount <= 0)
                throw new ArgumentOutOfRangeException(nameof(requestedCount), "Requested count must be at least 1.");

            EncounterId = encounterId;
            FactionId = factionId;
            ZoneId = zoneId;
            RequestedCount = requestedCount;
            SpawnPosition = spawnPosition;
            RequestedAt = DateTime.UtcNow;
        }
    }
}
