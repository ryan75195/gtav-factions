using FactionWars.Territory.Models;
using FactionWars.UI.Models;

namespace FactionWars.UI.Interfaces
{
    /// <summary>
    /// Service interface for rendering zone visual boundaries on the game map.
    /// Handles drawing zone outlines, colors, and visibility.
    /// </summary>
    public interface IZoneBoundaryRenderer
    {
        /// <summary>
        /// Renders the visual boundary for a zone on the map.
        /// If the boundary is already rendered, does nothing.
        /// </summary>
        /// <param name="zone">The zone to render a boundary for.</param>
        void RenderZoneBoundary(Zone zone);

        /// <summary>
        /// Removes the rendered boundary for a zone.
        /// </summary>
        /// <param name="zoneId">The zone ID to remove the boundary for.</param>
        /// <returns>True if a boundary was removed, false if no boundary existed.</returns>
        bool RemoveZoneBoundary(string zoneId);

        /// <summary>
        /// Updates the color of a zone's boundary based on faction ownership.
        /// </summary>
        /// <param name="zoneId">The zone ID to update.</param>
        /// <param name="factionId">The faction ID that owns the zone, or null for neutral.</param>
        /// <returns>True if the boundary was updated, false if no boundary exists.</returns>
        bool UpdateZoneBoundaryColor(string zoneId, string? factionId);

        /// <summary>
        /// Renders boundaries for all zones from the zone service.
        /// </summary>
        /// <returns>The number of boundaries rendered (including already existing).</returns>
        int RenderAllZoneBoundaries();

        /// <summary>
        /// Removes all rendered zone boundaries.
        /// </summary>
        void RemoveAllZoneBoundaries();

        /// <summary>
        /// Checks if a boundary is currently rendered for the specified zone.
        /// </summary>
        /// <param name="zoneId">The zone ID to check.</param>
        /// <returns>True if a boundary is rendered for the zone.</returns>
        bool IsZoneBoundaryRendered(string zoneId);

        /// <summary>
        /// Gets the render data for a zone's boundary.
        /// </summary>
        /// <param name="zoneId">The zone ID to get render data for.</param>
        /// <returns>The render data if the boundary is rendered, null otherwise.</returns>
        ZoneBoundaryRenderData? GetZoneBoundaryRenderData(string zoneId);

        /// <summary>
        /// Gets the count of zones with rendered boundaries.
        /// </summary>
        /// <returns>The number of rendered boundaries.</returns>
        int GetRenderedZoneCount();

        /// <summary>
        /// Synchronizes a boundary's color with the current zone ownership state.
        /// </summary>
        /// <param name="zoneId">The zone ID to sync.</param>
        void SyncWithZone(string zoneId);

        /// <summary>
        /// Gets the appropriate boundary color for a faction.
        /// </summary>
        /// <param name="factionId">The faction ID, or null for neutral.</param>
        /// <returns>The boundary color to use.</returns>
        BoundaryColor GetBoundaryColorForFaction(string? factionId);

        /// <summary>
        /// Sets the alpha/transparency for a zone's boundary.
        /// </summary>
        /// <param name="zoneId">The zone ID to update.</param>
        /// <param name="alpha">The alpha value (0-255).</param>
        /// <returns>True if the boundary was updated, false if no boundary exists.</returns>
        bool SetBoundaryAlpha(string zoneId, int alpha);

        /// <summary>
        /// Sets the alpha/transparency for all rendered boundaries.
        /// </summary>
        /// <param name="alpha">The alpha value (0-255).</param>
        void SetGlobalBoundaryAlpha(int alpha);

        /// <summary>
        /// Shows (makes visible) a zone's boundary.
        /// </summary>
        /// <param name="zoneId">The zone ID to show.</param>
        /// <returns>True if the boundary was shown, false if no boundary exists.</returns>
        bool ShowZoneBoundary(string zoneId);

        /// <summary>
        /// Hides a zone's boundary.
        /// </summary>
        /// <param name="zoneId">The zone ID to hide.</param>
        /// <returns>True if the boundary was hidden, false if no boundary exists.</returns>
        bool HideZoneBoundary(string zoneId);
    }
}
