using FactionWars.Core.Interfaces;
using FactionWars.Territory.Models;

namespace FactionWars.UI.Interfaces
{
    /// <summary>
    /// Service interface for managing map blips representing zones.
    /// Handles creation, update, and removal of blips on the game map.
    /// </summary>
    public interface IMapBlipService
    {
        /// <summary>
        /// Creates a blip on the map for the specified zone.
        /// If a blip already exists for this zone, returns the existing handle.
        /// </summary>
        /// <param name="zone">The zone to create a blip for.</param>
        /// <returns>The blip handle.</returns>
        int CreateBlipForZone(Zone zone);

        /// <summary>
        /// Removes the blip for the specified zone.
        /// </summary>
        /// <param name="zoneId">The zone ID to remove the blip for.</param>
        /// <returns>True if a blip was removed, false if no blip existed.</returns>
        bool RemoveBlipForZone(string zoneId);

        /// <summary>
        /// Updates the color of a zone's blip based on faction ownership.
        /// </summary>
        /// <param name="zoneId">The zone ID to update.</param>
        /// <param name="factionId">The faction ID that owns the zone, or null for neutral.</param>
        /// <returns>True if the blip was updated, false if no blip exists for the zone.</returns>
        bool UpdateBlipColor(string zoneId, string? factionId);

        /// <summary>
        /// Creates blips for all zones from the zone service.
        /// </summary>
        /// <returns>The number of blips created or already existing.</returns>
        int CreateBlipsForAllZones();

        /// <summary>
        /// Removes all tracked blips from the map.
        /// </summary>
        void RemoveAllBlips();

        /// <summary>
        /// Gets the blip handle for a zone.
        /// </summary>
        /// <param name="zoneId">The zone ID to get the blip for.</param>
        /// <returns>The blip handle if exists, null otherwise.</returns>
        int? GetBlipHandle(string zoneId);

        /// <summary>
        /// Checks if a blip exists for the specified zone.
        /// </summary>
        /// <param name="zoneId">The zone ID to check.</param>
        /// <returns>True if a blip exists for the zone.</returns>
        bool HasBlipForZone(string zoneId);

        /// <summary>
        /// Gets the appropriate blip color for a faction.
        /// </summary>
        /// <param name="factionId">The faction ID, or null for neutral.</param>
        /// <returns>The blip color to use.</returns>
        BlipColor GetBlipColorForFaction(string? factionId);

        /// <summary>
        /// Synchronizes a blip's color with the current zone ownership state.
        /// </summary>
        /// <param name="zoneId">The zone ID to sync.</param>
        void SyncBlipWithZone(string zoneId);

        /// <summary>
        /// Gets the count of zones with tracked blips.
        /// </summary>
        /// <returns>The number of tracked blips.</returns>
        int GetTrackedZoneCount();
    }
}
