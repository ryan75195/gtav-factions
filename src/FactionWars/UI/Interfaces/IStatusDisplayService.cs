using FactionWars.UI.Models;

namespace FactionWars.UI.Interfaces
{
    /// <summary>
    /// Service for displaying faction status information to the player.
    /// Abstraction allows for different display implementations (NativeUI, notifications, etc.).
    /// </summary>
    public interface IStatusDisplayService
    {
        /// <summary>
        /// Gets the faction ID of the player's current faction.
        /// </summary>
        /// <returns>The player's faction ID, or null if not assigned to a faction.</returns>
        string? GetPlayerFactionId();

        /// <summary>
        /// Displays the faction's overall status.
        /// </summary>
        /// <param name="info">The faction status information to display.</param>
        void ShowFactionStatus(FactionStatusInfo info);

        /// <summary>
        /// Displays the faction's resource status.
        /// </summary>
        /// <param name="info">The resource status information to display.</param>
        void ShowResourceStatus(ResourceStatusInfo info);

        /// <summary>
        /// Displays the faction's territory status.
        /// </summary>
        /// <param name="info">The territory status information to display.</param>
        void ShowTerritoryStatus(TerritoryStatusInfo info);

        /// <summary>
        /// Displays an error message to the player.
        /// </summary>
        /// <param name="message">The error message to display.</param>
        void ShowError(string message);
    }
}
