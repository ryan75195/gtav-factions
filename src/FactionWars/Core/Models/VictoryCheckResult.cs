using System;

namespace FactionWars.Core.Models
{
    /// <summary>
    /// Represents the result of checking a faction's victory condition.
    /// Contains information about whether victory was achieved and progress toward victory.
    /// </summary>
    public sealed class VictoryCheckResult
    {
        /// <summary>
        /// The faction ID that was checked for victory.
        /// </summary>
        public string FactionId { get; }

        /// <summary>
        /// True if the faction has achieved victory (100% zone control), false otherwise.
        /// </summary>
        public bool IsVictory { get; }

        /// <summary>
        /// The number of zones owned by the faction.
        /// </summary>
        public int ZonesOwned { get; }

        /// <summary>
        /// The total number of zones in the game.
        /// </summary>
        public int TotalZones { get; }

        /// <summary>
        /// The percentage of zones controlled by the faction (0-100).
        /// </summary>
        public float ControlPercentage { get; }

        private VictoryCheckResult(
            string factionId,
            bool isVictory,
            int zonesOwned,
            int totalZones)
        {
            if (factionId == null)
                throw new ArgumentNullException(nameof(factionId));
            if (totalZones <= 0)
                throw new ArgumentOutOfRangeException(nameof(totalZones), "Total zones must be greater than zero.");
            if (zonesOwned < 0)
                throw new ArgumentOutOfRangeException(nameof(zonesOwned), "Zones owned cannot be negative.");
            if (zonesOwned > totalZones)
                throw new ArgumentOutOfRangeException(nameof(zonesOwned), "Zones owned cannot exceed total zones.");

            FactionId = factionId;
            IsVictory = isVictory;
            ZonesOwned = zonesOwned;
            TotalZones = totalZones;
            ControlPercentage = (float)zonesOwned / totalZones * 100f;
        }

        /// <summary>
        /// Creates a VictoryCheckResult representing a victory (100% control achieved).
        /// </summary>
        /// <param name="factionId">The faction that achieved victory.</param>
        /// <param name="zonesOwned">The number of zones owned (should equal totalZones).</param>
        /// <param name="totalZones">The total number of zones.</param>
        /// <returns>A new VictoryCheckResult representing victory.</returns>
        public static VictoryCheckResult Victory(string factionId, int zonesOwned, int totalZones)
        {
            return new VictoryCheckResult(factionId, isVictory: true, zonesOwned, totalZones);
        }

        /// <summary>
        /// Creates a VictoryCheckResult representing ongoing progress (less than 100% control).
        /// </summary>
        /// <param name="factionId">The faction being checked.</param>
        /// <param name="zonesOwned">The number of zones owned.</param>
        /// <param name="totalZones">The total number of zones.</param>
        /// <returns>A new VictoryCheckResult representing in-progress state.</returns>
        public static VictoryCheckResult InProgress(string factionId, int zonesOwned, int totalZones)
        {
            return new VictoryCheckResult(factionId, isVictory: false, zonesOwned, totalZones);
        }

        public override string ToString()
        {
            if (IsVictory)
            {
                return $"{FactionId} achieved VICTORY! ({ZonesOwned}/{TotalZones} zones, {ControlPercentage:F1}%)";
            }
            return $"{FactionId}: {ZonesOwned}/{TotalZones} zones ({ControlPercentage:F1}%)";
        }
    }
}
