using FactionWars.Persistence.Models;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace FactionWars.Core.Interfaces
{
    /// <summary>
    /// Interface for managing multiple save slots.
    /// Provides operations for saving, loading, and managing save slots.
    /// </summary>
    public interface ISaveSlotManager
    {
        /// <summary>
        /// Maximum number of save slots available.
        /// </summary>
        int MaxSlots { get; }

        /// <summary>
        /// The directory where save files are stored.
        /// </summary>
        string SaveDirectory { get; }

        /// <summary>
        /// Gets the file path for a specific save slot.
        /// </summary>
        /// <param name="slotNumber">The slot number (0-based).</param>
        /// <returns>The full file path for the slot.</returns>
        string GetSlotFilePath(int slotNumber);

        /// <summary>
        /// Checks if a slot has a save file.
        /// </summary>
        /// <param name="slotNumber">The slot number (0-based).</param>
        /// <returns>True if the slot contains a save file.</returns>
        bool IsSlotOccupied(int slotNumber);

        /// <summary>
        /// Saves a game state to the specified slot.
        /// </summary>
        /// <param name="slotNumber">The slot number (0-based).</param>
        /// <param name="gameState">The game state to save.</param>
        void SaveToSlot(int slotNumber, GameState gameState);

        /// <summary>
        /// Saves a game state to the specified slot asynchronously.
        /// </summary>
        /// <param name="slotNumber">The slot number (0-based).</param>
        /// <param name="gameState">The game state to save.</param>
        Task SaveToSlotAsync(int slotNumber, GameState gameState);

        /// <summary>
        /// Loads a game state from the specified slot.
        /// </summary>
        /// <param name="slotNumber">The slot number (0-based).</param>
        /// <returns>The loaded game state.</returns>
        GameState LoadFromSlot(int slotNumber);

        /// <summary>
        /// Loads a game state from the specified slot asynchronously.
        /// </summary>
        /// <param name="slotNumber">The slot number (0-based).</param>
        /// <returns>The loaded game state.</returns>
        Task<GameState> LoadFromSlotAsync(int slotNumber);

        /// <summary>
        /// Deletes the save file at the specified slot.
        /// </summary>
        /// <param name="slotNumber">The slot number (0-based).</param>
        void DeleteSlot(int slotNumber);

        /// <summary>
        /// Gets information about a specific save slot.
        /// </summary>
        /// <param name="slotNumber">The slot number (0-based).</param>
        /// <returns>Information about the slot.</returns>
        SaveSlotInfo GetSlotInfo(int slotNumber);

        /// <summary>
        /// Gets information about all save slots.
        /// </summary>
        /// <returns>List of information for all slots.</returns>
        IReadOnlyList<SaveSlotInfo> GetAllSlotInfo();

        /// <summary>
        /// Finds the first empty slot.
        /// </summary>
        /// <returns>The slot number of the first empty slot, or null if all slots are full.</returns>
        int? GetFirstEmptySlot();

        /// <summary>
        /// Gets the number of occupied save slots.
        /// </summary>
        /// <returns>The count of occupied slots.</returns>
        int GetOccupiedSlotCount();

        /// <summary>
        /// Copies a save from one slot to another.
        /// </summary>
        /// <param name="sourceSlot">The source slot number (0-based).</param>
        /// <param name="destinationSlot">The destination slot number (0-based).</param>
        void CopySlot(int sourceSlot, int destinationSlot);
    }
}
