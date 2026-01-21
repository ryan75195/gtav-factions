using FactionWars.Combat.Models;

namespace FactionWars.UI.Interfaces
{
    /// <summary>
    /// Service interface for managing the combat HUD.
    /// Coordinates between combat data and HUD rendering.
    /// </summary>
    public interface ICombatHudService
    {
        /// <summary>
        /// Gets whether the combat HUD is currently visible.
        /// </summary>
        bool IsVisible { get; }

        /// <summary>
        /// Updates the combat HUD with current encounter data.
        /// </summary>
        /// <param name="encounter">The current combat encounter.</param>
        /// <param name="playerFactionId">The player's faction ID.</param>
        /// <param name="zoneName">The display name of the zone.</param>
        /// <param name="defenderReserveCount">Number of defender reserves remaining.</param>
        void Update(CombatEncounter encounter, string playerFactionId, string zoneName, int defenderReserveCount = 0);

        /// <summary>
        /// Hides the combat HUD.
        /// </summary>
        void Hide();
    }
}
