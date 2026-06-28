using System;
using System.Collections.Generic;
using System.Linq;

namespace FactionWars.Core.Models
{
    /// <summary>
    /// Represents the allocation of defender troops to a specific zone.
    /// Tracks troops by tier (Basic, Medium, Heavy, Elite) that have been assigned
    /// from a faction's reserve pool to defend this zone.
    /// </summary>
    public class ZoneDefenderAllocation
    {
        private readonly Dictionary<DefenderRole, int> _troops;

        /// <summary>
        /// The ID of the faction that owns this allocation.
        /// </summary>
        public string FactionId { get; }

        /// <summary>
        /// The ID of the zone this allocation is for.
        /// </summary>
        public string ZoneId { get; }

        /// <summary>
        /// Total number of troops allocated across all tiers.
        /// </summary>
        public int TotalTroops => _troops.Values.Sum();

        /// <summary>
        /// Creates a new zone defender allocation.
        /// </summary>
        /// <param name="factionId">The faction that owns this allocation.</param>
        /// <param name="zoneId">The zone this allocation is for.</param>
        /// <exception cref="ArgumentNullException">Thrown if factionId or zoneId is null.</exception>
        /// <exception cref="ArgumentException">Thrown if factionId or zoneId is empty or whitespace.</exception>
        public ZoneDefenderAllocation(string factionId, string zoneId)
        {
            if (factionId == null)
                throw new ArgumentNullException(nameof(factionId));
            if (string.IsNullOrWhiteSpace(factionId))
                throw new ArgumentException("Faction ID cannot be empty or whitespace.", nameof(factionId));

            if (zoneId == null)
                throw new ArgumentNullException(nameof(zoneId));
            if (string.IsNullOrWhiteSpace(zoneId))
                throw new ArgumentException("Zone ID cannot be empty or whitespace.", nameof(zoneId));

            FactionId = factionId;
            ZoneId = zoneId;
            _troops = new Dictionary<DefenderRole, int>
            {
                { DefenderRole.Grunt, 0 },
                { DefenderRole.Gunner, 0 },
                { DefenderRole.Rifleman, 0 },
                { DefenderRole.Rocketeer, 0 }
            };
        }

        /// <summary>
        /// Gets the number of troops allocated for a specific tier.
        /// </summary>
        /// <param name="tier">The defender tier to query.</param>
        /// <returns>The number of troops in that tier.</returns>
        public int GetTroopCount(DefenderRole tier)
        {
            return _troops.TryGetValue(tier, out var count) ? count : 0;
        }

        /// <summary>
        /// Adds troops to this allocation for a specific tier.
        /// </summary>
        /// <param name="tier">The defender tier to add to.</param>
        /// <param name="count">The number of troops to add (must be non-negative).</param>
        /// <exception cref="ArgumentOutOfRangeException">Thrown if count is negative.</exception>
        public void AddTroops(DefenderRole tier, int count)
        {
            if (count < 0)
                throw new ArgumentOutOfRangeException(nameof(count), "Count cannot be negative.");

            if (!_troops.ContainsKey(tier))
                _troops[tier] = 0;

            _troops[tier] += count;
        }

        /// <summary>
        /// Removes troops from this allocation for a specific tier.
        /// </summary>
        /// <param name="tier">The defender tier to remove from.</param>
        /// <param name="count">The number of troops to remove (must be non-negative).</param>
        /// <returns>True if the troops were removed, false if insufficient troops.</returns>
        /// <exception cref="ArgumentOutOfRangeException">Thrown if count is negative.</exception>
        public bool RemoveTroops(DefenderRole tier, int count)
        {
            if (count < 0)
                throw new ArgumentOutOfRangeException(nameof(count), "Count cannot be negative.");

            if (!_troops.TryGetValue(tier, out var current) || current < count)
                return false;

            _troops[tier] = current - count;
            return true;
        }

        /// <summary>
        /// Checks if this allocation has at least a specified number of troops for a tier.
        /// </summary>
        /// <param name="tier">The defender tier to check.</param>
        /// <param name="count">The minimum troop count to check for.</param>
        /// <returns>True if the tier has enough troops, false otherwise.</returns>
        public bool HasTroops(DefenderRole tier, int count)
        {
            return _troops.TryGetValue(tier, out var current) && current >= count;
        }

        /// <summary>
        /// Returns a copy of the troop counts by tier.
        /// </summary>
        /// <returns>A new dictionary with troop counts by tier.</returns>
        public Dictionary<DefenderRole, int> GetTroopsCopy()
        {
            return new Dictionary<DefenderRole, int>(_troops);
        }

        public override string ToString()
        {
            return $"ZoneDefenderAllocation[{FactionId}/{ZoneId}]: Basic={GetTroopCount(DefenderRole.Grunt)}, Medium={GetTroopCount(DefenderRole.Gunner)}, Heavy={GetTroopCount(DefenderRole.Rifleman)}, Elite={GetTroopCount(DefenderRole.Rocketeer)}";
        }
    }
}
