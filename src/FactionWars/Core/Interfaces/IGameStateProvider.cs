using FactionWars.Persistence.Models;

namespace FactionWars.Core.Interfaces
{
    /// <summary>
    /// Interface for providing the current game state.
    /// Used by auto-save and other services that need to capture game state snapshots.
    /// </summary>
    public interface IGameStateProvider
    {
        /// <summary>
        /// Gets the current game state for saving.
        /// </summary>
        /// <returns>The current game state, or null if no state is available.</returns>
        GameState? GetCurrentGameState();
    }
}
