using FactionWars.Combat.Models;
using System.Collections.Generic;

namespace FactionWars.Combat.Interfaces
{
    /// <summary>
    /// Service interface for despawning peds from the game world.
    /// Handles removal of peds from both the ped pool and the game.
    /// </summary>
    public interface IPedDespawnService
    {
        /// <summary>
        /// Despawns a single ped from the game world.
        /// Removes the ped from the pool and deletes it from the game.
        /// </summary>
        /// <param name="ped">The ped handle to despawn.</param>
        /// <returns>True if the ped was successfully despawned, false if it didn't exist.</returns>
        bool DespawnPed(PedHandle ped);

        /// <summary>
        /// Despawns a ped by its handle value.
        /// </summary>
        /// <param name="handle">The handle of the ped to despawn.</param>
        /// <returns>True if the ped was successfully despawned, false if it didn't exist.</returns>
        bool DespawnPed(int handle);

        /// <summary>
        /// Despawns all dead peds from the game world.
        /// Uses IGameBridge.IsPedAlive to check ped status.
        /// </summary>
        /// <returns>A list of peds that were despawned.</returns>
        IList<PedHandle> DespawnDeadPeds();

        /// <summary>
        /// Despawns all peds that are farther than the specified distance from the player.
        /// </summary>
        /// <param name="maxDistance">The maximum distance from the player. Peds beyond this are despawned.</param>
        /// <returns>A list of peds that were despawned.</returns>
        IList<PedHandle> DespawnPedsByDistance(float maxDistance);

        /// <summary>
        /// Despawns all peds that have been marked for deletion.
        /// </summary>
        /// <returns>A list of peds that were despawned.</returns>
        IList<PedHandle> DespawnMarkedForDeletion();

        /// <summary>
        /// Despawns all peds in a specific zone.
        /// </summary>
        /// <param name="zoneId">The zone ID to despawn peds from.</param>
        /// <returns>A list of peds that were despawned.</returns>
        IList<PedHandle> DespawnPedsByZone(string zoneId);

        /// <summary>
        /// Despawns all peds belonging to a specific faction.
        /// </summary>
        /// <param name="factionId">The faction ID to despawn peds for.</param>
        /// <returns>A list of peds that were despawned.</returns>
        IList<PedHandle> DespawnPedsByFaction(string factionId);

        /// <summary>
        /// Despawns all peds from the game world.
        /// </summary>
        /// <returns>A list of peds that were despawned.</returns>
        IList<PedHandle> DespawnAll();

        /// <summary>
        /// Despawns the oldest peds in the pool, up to the specified count.
        /// Useful for making room when the pool is nearing capacity.
        /// </summary>
        /// <param name="count">The number of oldest peds to despawn.</param>
        /// <returns>A list of peds that were despawned.</returns>
        IList<PedHandle> DespawnOldest(int count);

        /// <summary>
        /// Despawns all peds belonging to a specific faction in a specific zone.
        /// </summary>
        /// <param name="factionId">The faction ID to despawn peds for.</param>
        /// <param name="zoneId">The zone ID to despawn peds from.</param>
        /// <returns>A list of peds that were despawned.</returns>
        IList<PedHandle> DespawnPedsByFactionAndZone(string factionId, string zoneId);
    }
}
