using FactionWars.Core.Models;

namespace FactionWars.Core.Interfaces
{
    /// <summary>
    /// Service for detecting and managing victory conditions.
    /// Victory is achieved when a faction controls 100% of all zones.
    /// </summary>
    public interface IVictoryConditionService
    {
        /// <summary>
        /// Checks if a specific faction has achieved victory.
        /// </summary>
        /// <param name="factionId">The faction ID to check.</param>
        /// <returns>A VictoryCheckResult containing victory status and progress information.</returns>
        /// <exception cref="System.ArgumentNullException">Thrown if factionId is null.</exception>
        VictoryCheckResult CheckVictoryCondition(string factionId);

        /// <summary>
        /// Gets the victory progress percentage for a faction (0-100).
        /// </summary>
        /// <param name="factionId">The faction ID to check.</param>
        /// <returns>The percentage of zones controlled by the faction (0-100).</returns>
        /// <exception cref="System.ArgumentNullException">Thrown if factionId is null.</exception>
        float GetVictoryProgress(string factionId);

        /// <summary>
        /// Gets the number of zones owned by a faction.
        /// </summary>
        /// <param name="factionId">The faction ID to check.</param>
        /// <returns>The number of zones owned by the faction.</returns>
        /// <exception cref="System.ArgumentNullException">Thrown if factionId is null.</exception>
        int GetFactionZoneCount(string factionId);

        /// <summary>
        /// Gets the total number of zones in the game.
        /// </summary>
        /// <returns>The total number of zones.</returns>
        int GetTotalZoneCount();

        /// <summary>
        /// Checks if the game is over (any faction has achieved victory).
        /// </summary>
        /// <returns>True if a faction has achieved 100% control, false otherwise.</returns>
        bool IsGameOver();

        /// <summary>
        /// Gets the faction ID of the winner, if there is one.
        /// </summary>
        /// <returns>The faction ID of the winner, or null if no faction has won.</returns>
        string? GetWinningFactionId();
    }
}
