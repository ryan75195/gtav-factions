using FactionWars.Core.Interfaces;
using FactionWars.Territory.Events;
using FactionWars.Territory.Models;
using System;
using System.Collections.Generic;

namespace FactionWars.Territory.Interfaces
{
    /// <summary>
    /// Service interface for zone-related business logic and queries.
    /// Provides higher-level operations beyond basic CRUD.
    /// </summary>
    public interface IZoneService
    {
        /// <summary>
        /// Raised when a zone's owner actually changes. Not raised when the new owner equals
        /// the previous owner, and not raised when the zone is not found.
        /// </summary>
        event EventHandler<ZoneOwnershipChangedEventArgs>? ZoneOwnershipChanged;

        /// <summary>
        /// Gets a zone by its unique identifier.
        /// </summary>
        /// <param name="id">The zone ID to find.</param>
        /// <returns>The zone if found, null otherwise.</returns>
        Zone? GetZone(string id);

        /// <summary>
        /// Gets all zones in the game world.
        /// </summary>
        /// <returns>An enumerable of all zones.</returns>
        IEnumerable<Zone> GetAllZones();

        /// <summary>
        /// Finds the zone that contains the specified position.
        /// </summary>
        /// <param name="position">The world position to check.</param>
        /// <returns>The zone containing the position, or null if no zone contains it.</returns>
        Zone? GetZoneAtPosition(Vector3 position);

        /// <summary>
        /// Gets all zones owned by a specific faction.
        /// </summary>
        /// <param name="factionId">The faction ID, or null for neutral zones.</param>
        /// <returns>An enumerable of zones owned by the faction.</returns>
        IEnumerable<Zone> GetZonesByOwner(string? factionId);

        /// <summary>
        /// Gets all zones that are currently contested.
        /// </summary>
        /// <returns>An enumerable of contested zones.</returns>
        IEnumerable<Zone> GetContestedZones();

        /// <summary>
        /// Gets all zones with the specified trait.
        /// </summary>
        /// <param name="trait">The trait to filter by.</param>
        /// <returns>An enumerable of zones with the trait.</returns>
        IEnumerable<Zone> GetZonesByTrait(ZoneTrait trait);

        /// <summary>
        /// Gets zones ordered by strategic value (highest first).
        /// </summary>
        /// <param name="count">Maximum number of zones to return.</param>
        /// <returns>Top zones by strategic value.</returns>
        IEnumerable<Zone> GetHighValueZones(int count);

        /// <summary>
        /// Transfers ownership of a zone to a new faction.
        /// </summary>
        /// <param name="zoneId">The zone to transfer.</param>
        /// <param name="newOwnerFactionId">The new owner faction ID, or null for neutral.</param>
        /// <returns>True if transfer succeeded, false if zone not found.</returns>
        bool TransferZoneOwnership(string zoneId, string? newOwnerFactionId);

        /// <summary>
        /// Updates the control percentage of a zone.
        /// </summary>
        /// <param name="zoneId">The zone to update.</param>
        /// <param name="controlPercentage">The new control percentage (0-100).</param>
        /// <returns>True if update succeeded, false if zone not found.</returns>
        bool UpdateZoneControl(string zoneId, float controlPercentage);

        /// <summary>
        /// Sets the contested state of a zone.
        /// </summary>
        /// <param name="zoneId">The zone to update.</param>
        /// <param name="isContested">Whether the zone is contested.</param>
        /// <returns>True if update succeeded, false if zone not found.</returns>
        bool SetZoneContested(string zoneId, bool isContested);

        /// <summary>
        /// Gets the total strategic value of all zones owned by a faction.
        /// </summary>
        /// <param name="factionId">The faction ID to calculate for.</param>
        /// <returns>Sum of strategic values of owned zones.</returns>
        int GetFactionTerritoryValue(string factionId);

        /// <summary>
        /// Gets the count of zones owned by a faction.
        /// </summary>
        /// <param name="factionId">The faction ID, or null for neutral zones.</param>
        /// <returns>Number of zones owned.</returns>
        int GetZoneCount(string? factionId);

        /// <summary>
        /// Checks if a position is within any zone.
        /// </summary>
        /// <param name="position">The world position to check.</param>
        /// <returns>True if position is inside a zone, false otherwise.</returns>
        bool IsPositionInAnyZone(Vector3 position);

        /// <summary>
        /// Gets all zones that are adjacent (touching or overlapping) to the specified zone.
        /// Adjacency is determined by whether the 2D distance between zone centers
        /// is less than or equal to the sum of their radii.
        /// </summary>
        /// <param name="zoneId">The zone ID to find adjacent zones for.</param>
        /// <returns>An enumerable of adjacent zones (excluding the zone itself). Empty if zone not found.</returns>
        /// <exception cref="ArgumentNullException">Thrown if zoneId is null.</exception>
        IEnumerable<Zone> GetAdjacentZones(string zoneId);

        /// <summary>
        /// Checks if two zones are adjacent (touching or overlapping).
        /// </summary>
        /// <param name="zoneId1">The first zone ID.</param>
        /// <param name="zoneId2">The second zone ID.</param>
        /// <returns>True if zones are adjacent, false if not adjacent or if either zone is not found.</returns>
        /// <exception cref="ArgumentNullException">Thrown if either zone ID is null.</exception>
        bool AreZonesAdjacent(string zoneId1, string zoneId2);

        /// <summary>
        /// Gets all zones that are reachable from the specified zone through a chain of adjacent zones.
        /// Uses breadth-first search to find all connected zones.
        /// </summary>
        /// <param name="zoneId">The starting zone ID.</param>
        /// <returns>An enumerable of all connected zones (excluding the starting zone). Empty if zone not found.</returns>
        /// <exception cref="ArgumentNullException">Thrown if zoneId is null.</exception>
        IEnumerable<Zone> GetConnectedZones(string zoneId);

        /// <summary>
        /// Gets all zones that are reachable from the specified zone through a chain of adjacent zones,
        /// considering only zones owned by the specified faction.
        /// This is useful for calculating supply lines and territory connectivity.
        /// </summary>
        /// <param name="zoneId">The starting zone ID.</param>
        /// <param name="factionId">The faction ID to filter by.</param>
        /// <returns>An enumerable of connected zones owned by the faction. Empty if start zone not found or has different owner.</returns>
        /// <exception cref="ArgumentNullException">Thrown if zoneId or factionId is null.</exception>
        IEnumerable<Zone> GetConnectedZonesByOwner(string zoneId, string factionId);
    }
}
