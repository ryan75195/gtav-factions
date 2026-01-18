using FactionWars.Territory.Models;
using System.Collections.Generic;

namespace FactionWars.Territory.Interfaces
{
    /// <summary>
    /// Repository interface for zone data access.
    /// Provides CRUD operations and query methods for zones.
    /// </summary>
    public interface IZoneRepository
    {
        /// <summary>
        /// Gets the total number of zones in the repository.
        /// </summary>
        int Count { get; }

        /// <summary>
        /// Adds a new zone to the repository.
        /// </summary>
        /// <param name="zone">The zone to add.</param>
        /// <exception cref="System.ArgumentNullException">Thrown if zone is null.</exception>
        /// <exception cref="System.InvalidOperationException">Thrown if a zone with the same ID already exists.</exception>
        void Add(Zone zone);

        /// <summary>
        /// Gets a zone by its unique identifier.
        /// </summary>
        /// <param name="id">The zone ID to find.</param>
        /// <returns>The zone if found, null otherwise.</returns>
        /// <exception cref="System.ArgumentNullException">Thrown if id is null.</exception>
        /// <exception cref="System.ArgumentException">Thrown if id is empty or whitespace.</exception>
        Zone? GetById(string id);

        /// <summary>
        /// Gets all zones in the repository.
        /// </summary>
        /// <returns>An enumerable of all zones.</returns>
        IEnumerable<Zone> GetAll();

        /// <summary>
        /// Updates an existing zone in the repository.
        /// </summary>
        /// <param name="zone">The zone with updated data.</param>
        /// <exception cref="System.ArgumentNullException">Thrown if zone is null.</exception>
        /// <exception cref="System.InvalidOperationException">Thrown if the zone does not exist in the repository.</exception>
        void Update(Zone zone);

        /// <summary>
        /// Removes a zone from the repository.
        /// </summary>
        /// <param name="id">The ID of the zone to remove.</param>
        /// <returns>True if the zone was removed, false if it didn't exist.</returns>
        /// <exception cref="System.ArgumentNullException">Thrown if id is null.</exception>
        bool Remove(string id);

        /// <summary>
        /// Gets all zones owned by a specific faction.
        /// </summary>
        /// <param name="ownerFactionId">The faction ID to filter by, or null for neutral zones.</param>
        /// <returns>An enumerable of zones owned by the specified faction.</returns>
        IEnumerable<Zone> GetByOwner(string? ownerFactionId);

        /// <summary>
        /// Gets all zones that are currently contested.
        /// </summary>
        /// <returns>An enumerable of contested zones.</returns>
        IEnumerable<Zone> GetContested();

        /// <summary>
        /// Checks if a zone with the specified ID exists in the repository.
        /// </summary>
        /// <param name="id">The zone ID to check.</param>
        /// <returns>True if the zone exists, false otherwise.</returns>
        /// <exception cref="System.ArgumentNullException">Thrown if id is null.</exception>
        bool Contains(string id);

        /// <summary>
        /// Removes all zones from the repository.
        /// </summary>
        void Clear();
    }
}
