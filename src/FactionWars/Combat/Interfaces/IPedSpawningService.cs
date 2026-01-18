using FactionWars.Combat.Models;
using FactionWars.Core.Interfaces;
using System.Collections.Generic;

namespace FactionWars.Combat.Interfaces
{
    /// <summary>
    /// Service interface for spawning peds with faction relationship groups.
    /// Handles creation of peds in the game world and their registration in the ped pool.
    /// </summary>
    public interface IPedSpawningService
    {
        /// <summary>
        /// Spawns a ped at the specified position with the given faction.
        /// Sets up the ped's relationship group based on faction and adds it to the pool.
        /// </summary>
        /// <param name="modelName">The model name to use for the ped.</param>
        /// <param name="position">The world position to spawn the ped at.</param>
        /// <param name="factionId">The faction this ped belongs to.</param>
        /// <param name="zoneId">Optional zone ID the ped is assigned to.</param>
        /// <returns>A valid PedHandle if successful, or PedHandle.Invalid if the pool is full.</returns>
        PedHandle SpawnPed(string modelName, Vector3 position, string factionId, string? zoneId);

        /// <summary>
        /// Spawns multiple peds at the specified position.
        /// Stops spawning if the pool becomes full.
        /// </summary>
        /// <param name="modelName">The model name to use for all peds.</param>
        /// <param name="position">The world position to spawn the peds at.</param>
        /// <param name="factionId">The faction these peds belong to.</param>
        /// <param name="zoneId">Optional zone ID the peds are assigned to.</param>
        /// <param name="count">The number of peds to spawn.</param>
        /// <returns>A list of successfully spawned PedHandles (may be less than count if pool fills).</returns>
        IList<PedHandle> SpawnMultiplePeds(string modelName, Vector3 position, string factionId, string? zoneId, int count);

        /// <summary>
        /// Checks if at least one ped can be spawned.
        /// </summary>
        /// <returns>True if the pool has space for at least one more ped.</returns>
        bool CanSpawn();

        /// <summary>
        /// Gets the number of peds that can currently be spawned.
        /// </summary>
        /// <returns>The number of available slots in the pool.</returns>
        int CanSpawnCount();

        /// <summary>
        /// Gets the relationship group name for a given faction ID.
        /// </summary>
        /// <param name="factionId">The faction ID.</param>
        /// <returns>The relationship group name in uppercase.</returns>
        string GetRelationshipGroup(string factionId);
    }
}
