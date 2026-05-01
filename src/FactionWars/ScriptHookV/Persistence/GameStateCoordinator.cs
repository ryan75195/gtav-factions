using FactionWars.Core.Interfaces;
using FactionWars.ScriptHookV.Logging;
using System;

namespace FactionWars.ScriptHookV.Persistence
{
    /// <summary>
    /// Coordinates game state save/load operations by wrapping IGameStateManager.
    /// Implements IGameStateCoordinator to provide a simplified interface for UI components.
    /// </summary>
    public class GameStateCoordinator : IGameStateCoordinator
    {
        private readonly IGameStateManager _gameStateManager;
        private bool _isSaving;
        private bool _isLoading;

        /// <inheritdoc />
        public bool IsSaving => _isSaving;

        /// <inheritdoc />
        public bool IsLoading => _isLoading;

        /// <summary>
        /// Creates a new GameStateCoordinator with the specified game state manager.
        /// </summary>
        /// <param name="gameStateManager">The game state manager for persistence operations.</param>
        /// <exception cref="ArgumentNullException">Thrown if gameStateManager is null.</exception>
        public GameStateCoordinator(IGameStateManager gameStateManager)
        {
            _gameStateManager = gameStateManager ?? throw new ArgumentNullException(nameof(gameStateManager));
            _isSaving = false;
            _isLoading = false;
        }

        /// <inheritdoc />
        public void SaveToSlot(int slotNumber)
        {
            FileLogger.Warn($"GameStateCoordinator.SaveToSlot({slotNumber}) called but slot-based saves are deprecated; sidecar saves are tied to native saves.");
        }

        /// <inheritdoc />
        public void LoadFromSlot(int slotNumber)
        {
            FileLogger.Warn($"GameStateCoordinator.LoadFromSlot({slotNumber}) called but slot-based loads are deprecated; sidecar loads are triggered by native saves.");
        }
    }
}
