using System;

namespace FactionWars.ScriptHookV.Persistence
{
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
}
