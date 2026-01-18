using FactionWars.Loyalty.Models;
using System.Collections.Generic;

namespace FactionWars.Loyalty.Interfaces
{
    /// <summary>
    /// Manages the full lifecycle of integrating captured zones into the controlling faction.
    /// Coordinates between the integration service and repository.
    /// </summary>
    public interface ICapturedZoneIntegrationManager
    {
        /// <summary>
        /// Called when a zone is captured by a new faction.
        /// Creates an integration state for tracking the zone's integration.
        /// </summary>
        /// <param name="loyalty">The zone's loyalty state after capture.</param>
        void OnZoneCaptured(ZoneLoyalty loyalty);

        /// <summary>
        /// Processes a daily tick for all zones currently integrating.
        /// </summary>
        /// <returns>Results of processing each zone.</returns>
        IEnumerable<IntegrationTickResult> ProcessDailyTick();

        /// <summary>
        /// Gets the resource production multiplier for a zone.
        /// Returns 1.0 for zones that are not integrating or are fully integrated.
        /// </summary>
        /// <param name="zoneId">The zone ID.</param>
        /// <returns>Resource multiplier (0.25-1.0).</returns>
        float GetResourceMultiplier(string zoneId);

        /// <summary>
        /// Gets the defense modifier for a zone.
        /// Returns 0 for zones that are not integrating.
        /// </summary>
        /// <param name="zoneId">The zone ID.</param>
        /// <returns>Defense modifier (-15 to +15).</returns>
        int GetDefenseModifier(string zoneId);

        /// <summary>
        /// Checks if a zone is currently in the process of being integrated.
        /// </summary>
        /// <param name="zoneId">The zone ID.</param>
        /// <returns>True if integrating and not yet complete.</returns>
        bool IsZoneIntegrating(string zoneId);

        /// <summary>
        /// Gets the current integration progress for a zone.
        /// Returns 100 for zones that are not tracking (fully integrated).
        /// </summary>
        /// <param name="zoneId">The zone ID.</param>
        /// <returns>Progress percentage (0-100).</returns>
        int GetIntegrationProgress(string zoneId);

        /// <summary>
        /// Called when an insurgency occurs in a zone.
        /// Reduces integration progress based on insurgency level.
        /// </summary>
        /// <param name="zoneId">The zone ID.</param>
        /// <param name="level">The insurgency level.</param>
        void OnInsurgencyOccurred(string zoneId, InsurgencyLevel level);

        /// <summary>
        /// Attempts to complete integration for a zone and remove it from tracking.
        /// Only succeeds if the zone is at 100% integration.
        /// </summary>
        /// <param name="zoneId">The zone ID.</param>
        /// <returns>True if completed and removed, false if not yet fully integrated.</returns>
        bool CompleteIntegration(string zoneId);

        /// <summary>
        /// Gets the IDs of all zones currently integrating for a faction.
        /// </summary>
        /// <param name="factionId">The faction ID.</param>
        /// <returns>Zone IDs being integrated by the faction.</returns>
        IEnumerable<string> GetIntegratingZonesForFaction(string factionId);
    }
}
