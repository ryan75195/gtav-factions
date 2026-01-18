using FactionWars.Factions.Models;
using FactionWars.UI.Models;

namespace FactionWars.UI.Interfaces
{
    /// <summary>
    /// Service for managing the faction menu system.
    /// Provides menu generation and handles menu interactions.
    /// </summary>
    public interface IFactionMenuService
    {
        /// <summary>
        /// Shows the main faction menu for the player's faction.
        /// </summary>
        /// <param name="playerFactionId">The faction ID of the player.</param>
        void ShowMainMenu(string playerFactionId);

        /// <summary>
        /// Shows the territory list menu for the player's faction.
        /// </summary>
        /// <param name="playerFactionId">The faction ID of the player.</param>
        void ShowTerritoryListMenu(string playerFactionId);

        /// <summary>
        /// Shows the zone detail menu for a specific zone.
        /// </summary>
        /// <param name="zoneId">The zone ID to show details for.</param>
        /// <param name="playerFactionId">The faction ID of the player.</param>
        void ShowZoneDetailMenu(string zoneId, string playerFactionId);

        /// <summary>
        /// Closes the faction menu if it's currently open.
        /// </summary>
        void CloseMenu();

        /// <summary>
        /// Gets the menu definition for the main faction menu.
        /// </summary>
        /// <param name="playerFactionId">The faction ID of the player.</param>
        /// <returns>The menu definition, or null if the faction is not found.</returns>
        MenuDefinition? BuildMainMenuDefinition(string playerFactionId);

        /// <summary>
        /// Gets the menu definition for the territory list menu.
        /// </summary>
        /// <param name="playerFactionId">The faction ID of the player.</param>
        /// <returns>The menu definition, or null if the faction has no zones.</returns>
        MenuDefinition? BuildTerritoryListMenuDefinition(string playerFactionId);

        /// <summary>
        /// Gets the menu definition for the zone detail menu.
        /// </summary>
        /// <param name="zoneId">The zone ID to show details for.</param>
        /// <param name="playerFactionId">The faction ID of the player.</param>
        /// <returns>The menu definition, or null if the zone is not found.</returns>
        MenuDefinition? BuildZoneDetailMenuDefinition(string zoneId, string playerFactionId);

        /// <summary>
        /// Gets the menu definition for the resource overview menu.
        /// </summary>
        /// <param name="playerFactionId">The faction ID of the player.</param>
        /// <returns>The menu definition, or null if the faction state is not found.</returns>
        MenuDefinition? BuildResourceOverviewMenuDefinition(string playerFactionId);

        /// <summary>
        /// Shows the resource overview menu for the player's faction.
        /// </summary>
        /// <param name="playerFactionId">The faction ID of the player.</param>
        void ShowResourceOverviewMenu(string playerFactionId);

        /// <summary>
        /// Handles a menu item selection.
        /// </summary>
        /// <param name="menuId">The ID of the menu.</param>
        /// <param name="itemId">The ID of the selected item.</param>
        void HandleMenuSelection(string menuId, string itemId);

        /// <summary>
        /// Checks if the faction menu is currently visible.
        /// </summary>
        bool IsMenuVisible { get; }

        /// <summary>
        /// Updates the menu system (should be called each tick).
        /// </summary>
        void Update();

        /// <summary>
        /// Gets the menu definition for the orders menu.
        /// </summary>
        /// <param name="playerFactionId">The faction ID of the player.</param>
        /// <returns>The menu definition, or null if the faction ID is invalid.</returns>
        MenuDefinition? BuildOrdersMenuDefinition(string playerFactionId);

        /// <summary>
        /// Shows the orders menu for the player's faction.
        /// </summary>
        /// <param name="playerFactionId">The faction ID of the player.</param>
        void ShowOrdersMenu(string playerFactionId);

        /// <summary>
        /// Gets the menu definition for the attack targets menu.
        /// Shows zones that can be attacked (adjacent enemy or neutral zones).
        /// </summary>
        /// <param name="playerFactionId">The faction ID of the player.</param>
        /// <returns>The menu definition, or null if the faction ID is invalid.</returns>
        MenuDefinition? BuildAttackTargetsMenuDefinition(string playerFactionId);

        /// <summary>
        /// Shows the attack targets menu for the player's faction.
        /// </summary>
        /// <param name="playerFactionId">The faction ID of the player.</param>
        void ShowAttackTargetsMenu(string playerFactionId);

        /// <summary>
        /// Gets the menu definition for the defend zones menu.
        /// Shows zones that can be defended (player's own zones).
        /// </summary>
        /// <param name="playerFactionId">The faction ID of the player.</param>
        /// <returns>The menu definition, or null if the faction ID is invalid.</returns>
        MenuDefinition? BuildDefendZonesMenuDefinition(string playerFactionId);

        /// <summary>
        /// Shows the defend zones menu for the player's faction.
        /// </summary>
        /// <param name="playerFactionId">The faction ID of the player.</param>
        void ShowDefendZonesMenu(string playerFactionId);

        /// <summary>
        /// Gets the menu definition for the settings menu.
        /// Contains options for AI difficulty, auto-save, and other settings.
        /// </summary>
        /// <param name="playerFactionId">The faction ID of the player.</param>
        /// <returns>The menu definition, or null if the faction ID is invalid.</returns>
        MenuDefinition? BuildSettingsMenuDefinition(string playerFactionId);

        /// <summary>
        /// Shows the settings menu for the player's faction.
        /// </summary>
        /// <param name="playerFactionId">The faction ID of the player.</param>
        void ShowSettingsMenu(string playerFactionId);
    }
}
