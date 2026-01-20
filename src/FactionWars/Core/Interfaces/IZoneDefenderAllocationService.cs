using System.Collections.Generic;
using FactionWars.Core.Models;
using FactionWars.Factions.Models;

namespace FactionWars.Core.Interfaces
{
    /// <summary>
    /// Service for managing the allocation of defender troops from a faction's
    /// reserve pool to specific zones. This enables strategic distribution of
    /// forces across controlled territory.
    /// </summary>
    public interface IZoneDefenderAllocationService
    {
        /// <summary>
        /// Allocates troops from a faction's reserve pool to a specific zone.
        /// </summary>
        /// <param name="factionState">The faction state containing the reserve pool.</param>
        /// <param name="zoneId">The ID of the zone to allocate troops to.</param>
        /// <param name="tier">The defender tier to allocate.</param>
        /// <param name="count">The number of troops to allocate.</param>
        /// <returns>True if allocation succeeded, false if insufficient reserve troops.</returns>
        /// <exception cref="System.ArgumentNullException">Thrown if factionState or zoneId is null.</exception>
        /// <exception cref="System.ArgumentException">Thrown if zoneId is empty or whitespace.</exception>
        /// <exception cref="System.ArgumentOutOfRangeException">Thrown if count is less than or equal to zero.</exception>
        bool AllocateTroops(FactionState factionState, string zoneId, DefenderTier tier, int count);

        /// <summary>
        /// Gets the allocation for a specific zone and faction.
        /// </summary>
        /// <param name="factionId">The faction ID.</param>
        /// <param name="zoneId">The zone ID.</param>
        /// <returns>The allocation if it exists, null otherwise.</returns>
        /// <exception cref="System.ArgumentNullException">Thrown if factionId or zoneId is null.</exception>
        ZoneDefenderAllocation? GetAllocation(string factionId, string zoneId);

        /// <summary>
        /// Gets all allocations for a specific faction.
        /// </summary>
        /// <param name="factionId">The faction ID.</param>
        /// <returns>A list of all allocations for the faction.</returns>
        /// <exception cref="System.ArgumentNullException">Thrown if factionId is null.</exception>
        IReadOnlyList<ZoneDefenderAllocation> GetAllocationsForFaction(string factionId);

        /// <summary>
        /// Gets the total number of troops allocated across all zones for a faction.
        /// </summary>
        /// <param name="factionId">The faction ID.</param>
        /// <returns>The total number of allocated troops.</returns>
        /// <exception cref="System.ArgumentNullException">Thrown if factionId is null.</exception>
        int GetTotalAllocatedTroops(string factionId);

        /// <summary>
        /// Withdraws troops from a zone allocation back to the faction's reserve pool.
        /// </summary>
        /// <param name="factionState">The faction state to return troops to.</param>
        /// <param name="zoneId">The ID of the zone to withdraw from.</param>
        /// <param name="tier">The defender tier to withdraw.</param>
        /// <param name="count">The number of troops to withdraw.</param>
        /// <returns>True if withdrawal succeeded, false if insufficient allocated troops.</returns>
        /// <exception cref="System.ArgumentNullException">Thrown if factionState or zoneId is null.</exception>
        /// <exception cref="System.ArgumentException">Thrown if zoneId is empty or whitespace.</exception>
        /// <exception cref="System.ArgumentOutOfRangeException">Thrown if count is less than or equal to zero.</exception>
        bool WithdrawTroops(FactionState factionState, string zoneId, DefenderTier tier, int count);
    }
}
