using FactionWars.Core.Interfaces;

namespace FactionWars.UI.Interfaces
{
    /// <summary>
    /// Service for displaying quick action feedback to the player.
    /// Abstraction allows for different display implementations (NativeUI, notifications, etc.).
    /// </summary>
    public interface IQuickActionDisplayService
    {
        /// <summary>
        /// Gets the faction ID of the player's current faction.
        /// </summary>
        /// <returns>The player's faction ID, or null if not assigned to a faction.</returns>
        string? GetPlayerFactionId();

        /// <summary>
        /// Displays feedback that reinforcements have been requested.
        /// </summary>
        /// <param name="count">The number of reinforcements requested.</param>
        void ShowReinforcementsRequested(int count);

        /// <summary>
        /// Displays feedback that a rally order has been issued.
        /// </summary>
        /// <param name="position">The position troops are rallying to.</param>
        void ShowRallyOrdered(Vector3 position);

        /// <summary>
        /// Displays feedback that an attack has been initiated on a zone.
        /// </summary>
        /// <param name="zoneId">The ID of the zone being attacked.</param>
        /// <param name="zoneName">The display name of the zone.</param>
        void ShowAttackInitiated(string zoneId, string zoneName);

        /// <summary>
        /// Displays feedback that defense has been initiated on a zone.
        /// </summary>
        /// <param name="zoneId">The ID of the zone being defended.</param>
        /// <param name="zoneName">The display name of the zone.</param>
        void ShowDefenseInitiated(string zoneId, string zoneName);

        /// <summary>
        /// Displays an error message to the player.
        /// </summary>
        /// <param name="message">The error message to display.</param>
        void ShowError(string message);
    }
}
