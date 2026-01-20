using System.Collections.Generic;
using FactionWars.Core.Models;

namespace FactionWars.Core.Interfaces
{
    /// <summary>
    /// Repository for storing and retrieving zone defender allocations.
    /// </summary>
    public interface IZoneDefenderAllocationRepository
    {
        /// <summary>
        /// Adds a new allocation to the repository.
        /// </summary>
        /// <param name="allocation">The allocation to add.</param>
        /// <exception cref="System.ArgumentNullException">Thrown if allocation is null.</exception>
        /// <exception cref="System.InvalidOperationException">Thrown if allocation for this faction/zone already exists.</exception>
        void Add(ZoneDefenderAllocation allocation);

        /// <summary>
        /// Updates an existing allocation in the repository.
        /// </summary>
        /// <param name="allocation">The allocation to update.</param>
        /// <exception cref="System.ArgumentNullException">Thrown if allocation is null.</exception>
        /// <exception cref="System.InvalidOperationException">Thrown if allocation does not exist.</exception>
        void Update(ZoneDefenderAllocation allocation);

        /// <summary>
        /// Gets an allocation by faction ID and zone ID.
        /// </summary>
        /// <param name="factionId">The faction ID.</param>
        /// <param name="zoneId">The zone ID.</param>
        /// <returns>The allocation if found, null otherwise.</returns>
        /// <exception cref="System.ArgumentNullException">Thrown if factionId or zoneId is null.</exception>
        ZoneDefenderAllocation? Get(string factionId, string zoneId);

        /// <summary>
        /// Gets all allocations for a specific faction.
        /// </summary>
        /// <param name="factionId">The faction ID.</param>
        /// <returns>A list of all allocations for the faction.</returns>
        /// <exception cref="System.ArgumentNullException">Thrown if factionId is null.</exception>
        IReadOnlyList<ZoneDefenderAllocation> GetByFaction(string factionId);

        /// <summary>
        /// Removes an allocation from the repository.
        /// </summary>
        /// <param name="factionId">The faction ID.</param>
        /// <param name="zoneId">The zone ID.</param>
        /// <returns>True if removed, false if not found.</returns>
        /// <exception cref="System.ArgumentNullException">Thrown if factionId or zoneId is null.</exception>
        bool Remove(string factionId, string zoneId);

        /// <summary>
        /// Gets all allocations in the repository.
        /// </summary>
        /// <returns>A list of all allocations.</returns>
        IReadOnlyList<ZoneDefenderAllocation> GetAll();
    }
}
