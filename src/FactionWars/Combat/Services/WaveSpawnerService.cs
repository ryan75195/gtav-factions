using System;
using System.Collections.Generic;
using FactionWars.Combat.Interfaces;
using FactionWars.Combat.Models;
using FactionWars.Core.Models;

namespace FactionWars.Combat.Services
{
    /// <summary>
    /// Service for managing wave-based defender spawning.
    /// Spawns defenders in tier order: Heavy → Medium → Basic.
    /// This ensures the most challenging enemies appear first in combat.
    /// </summary>
    public class WaveSpawnerService : IWaveSpawnerService
    {
        /// <summary>
        /// The wave spawn order: Heavy first (strongest), then Medium, then Basic.
        /// </summary>
        private static readonly IReadOnlyList<DefenderRole> WaveOrder = new List<DefenderRole>
        {
            DefenderRole.Rifleman,
            DefenderRole.Gunner,
            DefenderRole.Grunt
        }.AsReadOnly();

        /// <inheritdoc />
        public WaveState CreateWaveState(DefenderSpawnPlan plan)
        {
            if (plan == null)
                throw new ArgumentNullException(nameof(plan));

            return new WaveState(
                basicPeds: plan.GetPedCount(DefenderRole.Grunt),
                mediumPeds: plan.GetPedCount(DefenderRole.Gunner),
                heavyPeds: plan.GetPedCount(DefenderRole.Rifleman));
        }

        /// <inheritdoc />
        public DefenderRole? GetNextWaveTier(WaveState state)
        {
            if (state == null)
                throw new ArgumentNullException(nameof(state));

            // Return the first tier in wave order that still has remaining peds
            foreach (var tier in WaveOrder)
            {
                if (state.GetRemaining(tier) > 0)
                {
                    return tier;
                }
            }

            // All waves complete
            return null;
        }

        /// <inheritdoc />
        public int GetSpawnCountForWave(WaveState state, DefenderRole tier, int maxToSpawn)
        {
            if (state == null)
                throw new ArgumentNullException(nameof(state));

            if (maxToSpawn <= 0)
                return 0;

            int remaining = state.GetRemaining(tier);
            return Math.Min(remaining, maxToSpawn);
        }

        /// <inheritdoc />
        public IReadOnlyList<DefenderRole> GetWaveOrder()
        {
            return WaveOrder;
        }
    }
}
