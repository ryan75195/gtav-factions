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
        /// Gets the current save name, if any.
        /// </summary>
        string? CurrentSaveName { get; }

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
        /// Saves the current game state to the specified slot.
        /// </summary>
        /// <param name="slotNumber">The slot number (0-based).</param>
        /// <param name="saveName">Optional name for the save.</param>
        void SaveToSlot(int slotNumber, string? saveName = null);

        /// <summary>
        /// Saves the current game state to the specified slot asynchronously.
        /// </summary>
        /// <param name="slotNumber">The slot number (0-based).</param>
        /// <param name="saveName">Optional name for the save.</param>
        Task SaveToSlotAsync(int slotNumber, string? saveName = null);

        /// <summary>
        /// Loads a game state from the specified slot.
        /// </summary>
        /// <param name="slotNumber">The slot number (0-based).</param>
        void LoadFromSlot(int slotNumber);

        /// <summary>
        /// Loads a game state from the specified slot asynchronously.
        /// </summary>
        /// <param name="slotNumber">The slot number (0-based).</param>
        Task LoadFromSlotAsync(int slotNumber);

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

    /// <summary>
    /// Event arguments for the OnGameSaved event.
    /// </summary>
    public class GameStateSavedEventArgs : EventArgs
    {
        /// <summary>
        /// Gets the slot number the game was saved to.
        /// </summary>
        public int SlotNumber { get; }

        /// <summary>
        /// Gets the save name.
        /// </summary>
        public string SaveName { get; }

        /// <summary>
        /// Gets whether the save was successful.
        /// </summary>
        public bool Success { get; }

        /// <summary>
        /// Gets the error if the save failed.
        /// </summary>
        public Exception? Error { get; }

        public GameStateSavedEventArgs(int slotNumber, string saveName, bool success, Exception? error = null)
        {
            SlotNumber = slotNumber;
            SaveName = saveName;
            Success = success;
            Error = error;
        }
    }

    /// <summary>
    /// Event arguments for the OnGameLoaded event.
    /// </summary>
    public class GameStateLoadedEventArgs : EventArgs
    {
        /// <summary>
        /// Gets the slot number the game was loaded from.
        /// </summary>
        public int SlotNumber { get; }

        /// <summary>
        /// Gets the save name of the loaded game.
        /// </summary>
        public string SaveName { get; }

        /// <summary>
        /// Gets whether the load was successful.
        /// </summary>
        public bool Success { get; }

        /// <summary>
        /// Gets the error if the load failed.
        /// </summary>
        public Exception? Error { get; }

        public GameStateLoadedEventArgs(int slotNumber, string saveName, bool success, Exception? error = null)
        {
            SlotNumber = slotNumber;
            SaveName = saveName;
            Success = success;
            Error = error;
        }
    }
}
