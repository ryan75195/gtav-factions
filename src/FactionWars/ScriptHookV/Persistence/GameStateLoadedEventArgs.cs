using System;
using FactionWars.Persistence.Models;

namespace FactionWars.ScriptHookV.Persistence
{
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

        public RuntimeWorldState? RuntimeWorldState { get; }

        public GameStateLoadedEventArgs(
            int slotNumber,
            string saveName,
            bool success,
            Exception? error = null,
            RuntimeWorldState? runtimeWorldState = null)
        {
            SlotNumber = slotNumber;
            SaveName = saveName;
            Success = success;
            Error = error;
            RuntimeWorldState = runtimeWorldState;
        }
    }
}
