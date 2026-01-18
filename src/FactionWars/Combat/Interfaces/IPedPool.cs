using FactionWars.Combat.Models;
using System.Collections.Generic;

namespace FactionWars.Combat.Interfaces
{
    /// <summary>
    /// Interface for managing a pool of spawned peds (pedestrians/NPCs).
    /// Provides methods for adding, removing, and querying peds by various criteria.
    /// Implementations should enforce configurable capacity limits.
    /// </summary>
    public interface IPedPool
    {
        /// <summary>
        /// Gets the current number of peds in the pool.
        /// </summary>
        int Count { get; }

        /// <summary>
        /// Gets the maximum number of peds allowed in the pool.
        /// </summary>
        int MaxCapacity { get; }

        /// <summary>
        /// Gets whether the pool is at maximum capacity.
        /// </summary>
        bool IsFull { get; }

        /// <summary>
        /// Gets the number of available slots in the pool.
        /// </summary>
        int AvailableSlots { get; }

        /// <summary>
        /// Adds a ped to the pool.
        /// </summary>
        /// <param name="ped">The ped handle to add.</param>
        /// <returns>True if the ped was added successfully, false if the pool is full,
        /// the ped already exists, or the ped handle is invalid.</returns>
        /// <exception cref="System.ArgumentNullException">Thrown if ped is null.</exception>
        bool Add(PedHandle ped);

        /// <summary>
        /// Checks if a ped with the specified handle exists in the pool.
        /// </summary>
        /// <param name="handle">The handle to check.</param>
        /// <returns>True if the ped exists in the pool.</returns>
        bool Contains(int handle);

        /// <summary>
        /// Checks if a ped exists in the pool.
        /// </summary>
        /// <param name="ped">The ped handle to check.</param>
        /// <returns>True if the ped exists in the pool.</returns>
        bool Contains(PedHandle ped);

        /// <summary>
        /// Gets a ped by its handle.
        /// </summary>
        /// <param name="handle">The handle of the ped to retrieve.</param>
        /// <returns>The ped handle if found, null otherwise.</returns>
        PedHandle? GetByHandle(int handle);

        /// <summary>
        /// Gets all peds in the pool.
        /// </summary>
        /// <returns>An enumerable of all peds in the pool.</returns>
        IEnumerable<PedHandle> GetAll();

        /// <summary>
        /// Gets all peds belonging to a specific faction.
        /// </summary>
        /// <param name="factionId">The faction ID to filter by.</param>
        /// <returns>An enumerable of peds belonging to the faction.</returns>
        /// <exception cref="System.ArgumentNullException">Thrown if factionId is null.</exception>
        IEnumerable<PedHandle> GetByFaction(string factionId);

        /// <summary>
        /// Gets the number of peds belonging to a specific faction.
        /// </summary>
        /// <param name="factionId">The faction ID to count.</param>
        /// <returns>The number of peds belonging to the faction.</returns>
        int GetFactionCount(string factionId);

        /// <summary>
        /// Gets all peds in a specific zone.
        /// </summary>
        /// <param name="zoneId">The zone ID to filter by.</param>
        /// <returns>An enumerable of peds in the zone.</returns>
        /// <exception cref="System.ArgumentNullException">Thrown if zoneId is null.</exception>
        IEnumerable<PedHandle> GetByZone(string zoneId);

        /// <summary>
        /// Gets the number of peds in a specific zone.
        /// </summary>
        /// <param name="zoneId">The zone ID to count.</param>
        /// <returns>The number of peds in the zone.</returns>
        int GetZoneCount(string zoneId);

        /// <summary>
        /// Gets all peds belonging to a specific faction in a specific zone.
        /// </summary>
        /// <param name="factionId">The faction ID to filter by.</param>
        /// <param name="zoneId">The zone ID to filter by.</param>
        /// <returns>An enumerable of matching peds.</returns>
        IEnumerable<PedHandle> GetByFactionAndZone(string factionId, string zoneId);

        /// <summary>
        /// Removes a ped from the pool by handle.
        /// </summary>
        /// <param name="handle">The handle of the ped to remove.</param>
        /// <returns>True if the ped was removed, false if it didn't exist.</returns>
        bool Remove(int handle);

        /// <summary>
        /// Removes a ped from the pool.
        /// </summary>
        /// <param name="ped">The ped to remove.</param>
        /// <returns>True if the ped was removed, false if it didn't exist.</returns>
        bool Remove(PedHandle ped);

        /// <summary>
        /// Removes all peds from the pool.
        /// </summary>
        /// <returns>The peds that were removed.</returns>
        IEnumerable<PedHandle> Clear();

        /// <summary>
        /// Gets all peds that have been marked for deletion.
        /// </summary>
        /// <returns>An enumerable of peds marked for deletion.</returns>
        IEnumerable<PedHandle> GetMarkedForDeletion();

        /// <summary>
        /// Removes all peds that have been marked for deletion.
        /// </summary>
        /// <returns>The peds that were removed.</returns>
        IEnumerable<PedHandle> RemoveMarkedForDeletion();

        /// <summary>
        /// Gets the oldest peds in the pool, ordered by creation time.
        /// </summary>
        /// <param name="count">The maximum number of peds to return.</param>
        /// <returns>An enumerable of the oldest peds.</returns>
        IEnumerable<PedHandle> GetOldest(int count);
    }
}
