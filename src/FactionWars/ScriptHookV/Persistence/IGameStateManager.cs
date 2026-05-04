using FactionWars.Core.Interfaces;
using FactionWars.Core.Models;
using FactionWars.Persistence.Models;
using System;
using System.Threading.Tasks;

namespace FactionWars.ScriptHookV.Persistence
{
    /// <summary>
    /// Interface for coordinating save/load operations between domain services and persistence layer.
    /// Combines IGameStateProvider functionality with high-level save/load operations.
    /// </summary>
    public interface IGameStateManager : IGameStateProvider
    {
        /// <summary>
        /// Gets whether a game is currently loaded.
        /// </summary>
        bool HasGameLoaded { get; }

        /// <summary>
        /// Gets the total play time in seconds for the current session.
        /// </summary>
        long TotalPlayTimeSeconds { get; }

        /// <summary>
        /// Event raised when a game state is saved.
        /// </summary>
        event EventHandler<GameStateSavedEventArgs>? OnGameSaved;

        /// <summary>
        /// Event raised when a game state is loaded.
        /// </summary>
        event EventHandler<GameStateLoadedEventArgs>? OnGameLoaded;

        /// <summary>
        /// Captures the current game state into a sidecar tagged with the supplied fingerprint
        /// and writes it via the SidecarStore. Failures are logged and swallowed.
        /// </summary>
        void WriteCurrentSidecar(SaveFingerprint fingerprint, PlayerPosition position, string nativeSaveFilename);

        /// <summary>
        /// Applies the given sidecar's GameState to the current world.
        /// </summary>
        void HydrateFromSidecar(Sidecar sidecar);

        /// <summary>
        /// Creates a new game with default starting conditions.
        /// </summary>
        void NewGame();

        /// <summary>
        /// Updates the play time tracker. Should be called each game tick.
        /// </summary>
        /// <param name="deltaTimeSeconds">The time elapsed since the last update.</param>
        void UpdatePlayTime(float deltaTimeSeconds);

        /// <summary>
        /// Applies a loaded game state to the domain services.
        /// </summary>
        /// <param name="gameState">The game state to apply.</param>
        void ApplyGameState(GameState gameState);

        /// <summary>
        /// Sets the current difficulty level to be included in saved game states.
        /// </summary>
        /// <param name="difficulty">The current difficulty level.</param>
        void SetCurrentDifficulty(Difficulty difficulty);
    }

}
