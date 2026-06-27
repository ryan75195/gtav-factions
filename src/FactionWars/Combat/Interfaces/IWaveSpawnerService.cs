using System.Collections.Generic;
using FactionWars.Combat.Models;
using FactionWars.Core.Models;

namespace FactionWars.Combat.Interfaces
{
    /// <summary>
    /// Service for managing wave-based defender spawning.
    /// Spawns defenders in tier order: Heavy → Medium → Basic.
    /// This ensures the most challenging enemies appear first in combat.
    /// </summary>
    public interface IWaveSpawnerService
    {
        /// <summary>
        /// Creates a new wave state from a defender spawn plan.
        /// The state tracks remaining and spawned peds for each tier.
        /// </summary>
        /// <param name="plan">The spawn plan defining how many peds of each tier to spawn.</param>
        /// <returns>A new wave state initialized with the plan's ped counts.</returns>
        /// <exception cref="System.ArgumentNullException">Thrown if plan is null.</exception>
        WaveState CreateWaveState(DefenderSpawnPlan plan);

        /// <summary>
        /// Gets the next tier that should spawn, following wave order (Heavy → Medium → Basic).
        /// Returns null if all waves are complete.
        /// </summary>
        /// <param name="state">The current wave state.</param>
        /// <returns>The next tier to spawn, or null if all tiers are complete.</returns>
        DefenderRole? GetNextWaveTier(WaveState state);

        /// <summary>
        /// Gets the number of peds to spawn for a specific wave/tier, respecting a maximum limit.
        /// Returns the lesser of remaining peds for the tier or the max to spawn.
        /// </summary>
        /// <param name="state">The current wave state.</param>
        /// <param name="tier">The tier to get spawn count for.</param>
        /// <param name="maxToSpawn">The maximum number of peds to spawn this tick.</param>
        /// <returns>The number of peds to spawn (0 if tier is complete).</returns>
        int GetSpawnCountForWave(WaveState state, DefenderRole tier, int maxToSpawn);

        /// <summary>
        /// Gets the wave spawn order (Heavy → Medium → Basic).
        /// </summary>
        /// <returns>A list of tiers in spawn order.</returns>
        IReadOnlyList<DefenderRole> GetWaveOrder();
    }
}
