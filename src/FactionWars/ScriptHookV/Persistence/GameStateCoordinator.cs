using FactionWars.Core.Interfaces;
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
            _isSaving = true;
            try
            {
                _gameStateManager.SaveToSlot(slotNumber, null);
            }
            finally
            {
                _isSaving = false;
            }
        }

        /// <inheritdoc />
        public void LoadFromSlot(int slotNumber)
        {
            _isLoading = true;
            try
            {
                _gameStateManager.LoadFromSlot(slotNumber);
            }
            finally
            {
                _isLoading = false;
            }
        }
    }
}
