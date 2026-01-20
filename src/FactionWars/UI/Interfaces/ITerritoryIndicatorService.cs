using FactionWars.Territory.Models;

namespace FactionWars.UI.Interfaces
{
    /// <summary>
    /// Service interface for managing the territory indicator HUD.
    /// Displays zone name, owner, and control percentage at the top of the screen.
    /// </summary>
    public interface ITerritoryIndicatorService
    {
        /// <summary>
        /// Gets whether the territory indicator is currently visible.
        /// </summary>
        bool IsVisible { get; }

        /// <summary>
        /// Updates the territory indicator with the current zone data.
        /// </summary>
        /// <param name="currentZone">The zone the player is currently in, or null if outside all zones.</param>
        /// <param name="playerFactionId">The player's faction ID.</param>
        void Update(Zone? currentZone, string playerFactionId);

        /// <summary>
        /// Hides the territory indicator.
        /// </summary>
        void Hide();
    }
}
