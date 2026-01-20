using System;
using System.Collections.Generic;
using System.Linq;
using FactionWars.Core.Models;

namespace FactionWars.Combat.Models
{
    /// <summary>
    /// Tracks the state of wave-based defender spawning during combat.
    /// Records remaining and spawned counts for each tier.
    /// </summary>
    public class WaveState
    {
        private readonly Dictionary<DefenderTier, int> _remaining;
        private readonly Dictionary<DefenderTier, int> _spawned;

        /// <summary>
        /// Gets the total number of peds remaining to spawn across all tiers.
        /// </summary>
        public int TotalRemaining => _remaining.Values.Sum();

        /// <summary>
        /// Gets the total number of peds that have been spawned across all tiers.
        /// </summary>
        public int TotalSpawned => _spawned.Values.Sum();

        /// <summary>
        /// Gets whether all peds have been spawned (all waves complete).
        /// </summary>
        public bool IsComplete => TotalRemaining == 0;

        /// <summary>
        /// Creates a new WaveState with the specified initial ped counts.
        /// </summary>
        /// <param name="basicPeds">Number of basic tier peds to spawn.</param>
        /// <param name="mediumPeds">Number of medium tier peds to spawn.</param>
        /// <param name="heavyPeds">Number of heavy tier peds to spawn.</param>
        public WaveState(int basicPeds, int mediumPeds, int heavyPeds)
        {
            _remaining = new Dictionary<DefenderTier, int>
            {
                { DefenderTier.Basic, Math.Max(0, basicPeds) },
                { DefenderTier.Medium, Math.Max(0, mediumPeds) },
                { DefenderTier.Heavy, Math.Max(0, heavyPeds) }
            };

            _spawned = new Dictionary<DefenderTier, int>
            {
                { DefenderTier.Basic, 0 },
                { DefenderTier.Medium, 0 },
                { DefenderTier.Heavy, 0 }
            };
        }

        /// <summary>
        /// Gets the number of peds remaining to spawn for a specific tier.
        /// </summary>
        /// <param name="tier">The tier to check.</param>
        /// <returns>The number of peds remaining for that tier.</returns>
        public int GetRemaining(DefenderTier tier)
        {
            return _remaining.TryGetValue(tier, out var count) ? count : 0;
        }

        /// <summary>
        /// Gets the number of peds that have been spawned for a specific tier.
        /// </summary>
        /// <param name="tier">The tier to check.</param>
        /// <returns>The number of peds spawned for that tier.</returns>
        public int GetSpawned(DefenderTier tier)
        {
            return _spawned.TryGetValue(tier, out var count) ? count : 0;
        }

        /// <summary>
        /// Records that peds have been spawned for a specific tier.
        /// Decrements remaining and increments spawned counts.
        /// </summary>
        /// <param name="tier">The tier that was spawned.</param>
        /// <param name="count">The number of peds spawned.</param>
        public void RecordSpawned(DefenderTier tier, int count)
        {
            if (count <= 0)
                return;

            int currentRemaining = GetRemaining(tier);
            int actualSpawned = Math.Min(count, currentRemaining);

            _remaining[tier] = currentRemaining - actualSpawned;
            _spawned[tier] = GetSpawned(tier) + actualSpawned;
        }

        /// <summary>
        /// Checks if a specific tier has completed spawning all its peds.
        /// </summary>
        /// <param name="tier">The tier to check.</param>
        /// <returns>True if no more peds remain for this tier.</returns>
        public bool IsTierComplete(DefenderTier tier)
        {
            return GetRemaining(tier) == 0;
        }

        /// <summary>
        /// Returns a copy of the remaining counts by tier.
        /// </summary>
        /// <returns>A dictionary mapping tiers to remaining counts.</returns>
        public Dictionary<DefenderTier, int> GetRemainingCopy()
        {
            return new Dictionary<DefenderTier, int>(_remaining);
        }

        /// <summary>
        /// Returns a copy of the spawned counts by tier.
        /// </summary>
        /// <returns>A dictionary mapping tiers to spawned counts.</returns>
        public Dictionary<DefenderTier, int> GetSpawnedCopy()
        {
            return new Dictionary<DefenderTier, int>(_spawned);
        }

        public override string ToString()
        {
            return $"WaveState[Remaining: H={GetRemaining(DefenderTier.Heavy)}, M={GetRemaining(DefenderTier.Medium)}, B={GetRemaining(DefenderTier.Basic)}, Spawned: H={GetSpawned(DefenderTier.Heavy)}, M={GetSpawned(DefenderTier.Medium)}, B={GetSpawned(DefenderTier.Basic)}]";
        }
    }
}
