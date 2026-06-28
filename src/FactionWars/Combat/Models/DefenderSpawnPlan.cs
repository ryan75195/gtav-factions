using System.Collections.Generic;
using System.Linq;
using FactionWars.Core.Models;

namespace FactionWars.Combat.Models
{
    /// <summary>
    /// Represents a plan for spawning defender peds based on zone troop allocation.
    /// Contains the number of peds to spawn for each tier, scaled from the total troop count.
    /// </summary>
    public class DefenderSpawnPlan
    {
        private readonly Dictionary<DefenderRole, int> _pedsByTier;

        /// <summary>
        /// Total number of peds to spawn across all tiers.
        /// </summary>
        public int TotalPeds => _pedsByTier.Values.Sum();

        /// <summary>
        /// Creates a new defender spawn plan.
        /// </summary>
        public DefenderSpawnPlan()
        {
            _pedsByTier = new Dictionary<DefenderRole, int>
            {
                { DefenderRole.Grunt, 0 },
                { DefenderRole.Gunner, 0 },
                { DefenderRole.Rifleman, 0 }
            };
        }

        /// <summary>
        /// Creates a new defender spawn plan with specified ped counts.
        /// </summary>
        /// <param name="basicPeds">Number of basic tier peds.</param>
        /// <param name="mediumPeds">Number of medium tier peds.</param>
        /// <param name="heavyPeds">Number of heavy tier peds.</param>
        public DefenderSpawnPlan(int basicPeds, int mediumPeds, int heavyPeds)
        {
            _pedsByTier = new Dictionary<DefenderRole, int>
            {
                { DefenderRole.Grunt, basicPeds >= 0 ? basicPeds : 0 },
                { DefenderRole.Gunner, mediumPeds >= 0 ? mediumPeds : 0 },
                { DefenderRole.Rifleman, heavyPeds >= 0 ? heavyPeds : 0 }
            };
        }

        /// <summary>
        /// Gets the number of peds to spawn for a specific tier.
        /// </summary>
        /// <param name="tier">The defender tier.</param>
        /// <returns>The number of peds to spawn for that tier.</returns>
        public int GetPedCount(DefenderRole tier)
        {
            return _pedsByTier.TryGetValue(tier, out var count) ? count : 0;
        }

        /// <summary>
        /// Sets the number of peds to spawn for a specific tier.
        /// </summary>
        /// <param name="tier">The defender tier.</param>
        /// <param name="count">The number of peds (must be non-negative).</param>
        public void SetPedCount(DefenderRole tier, int count)
        {
            _pedsByTier[tier] = count >= 0 ? count : 0;
        }

        /// <summary>
        /// Returns a copy of the ped counts by tier.
        /// </summary>
        /// <returns>A dictionary mapping tiers to ped counts.</returns>
        public Dictionary<DefenderRole, int> GetPedCountsCopy()
        {
            return new Dictionary<DefenderRole, int>(_pedsByTier);
        }

        /// <summary>
        /// Checks if this plan has any peds to spawn.
        /// </summary>
        /// <returns>True if at least one ped should be spawned.</returns>
        public bool HasPedsToSpawn() => TotalPeds > 0;

        public override string ToString()
        {
            return $"DefenderSpawnPlan[Basic={GetPedCount(DefenderRole.Grunt)}, Medium={GetPedCount(DefenderRole.Gunner)}, Heavy={GetPedCount(DefenderRole.Rifleman)}, Total={TotalPeds}]";
        }
    }
}
