using FactionWars.Core.Interfaces;
using FactionWars.Persistence.Models;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace FactionWars.Core.Services
{
    /// <summary>
    /// Stub implementation of ISaveSlotManager for use before full persistence is implemented.
    /// Returns empty save slots and performs no actual save/load operations.
    /// </summary>
    public class StubSaveSlotManager : ISaveSlotManager
    {
        private const int DefaultMaxSlots = 5;

        /// <inheritdoc />
        public int MaxSlots => DefaultMaxSlots;

        /// <inheritdoc />
        public string SaveDirectory => string.Empty;

        /// <inheritdoc />
        public string GetSlotFilePath(int slotNumber)
        {
            return $"slot_{slotNumber}.json";
        }

        /// <inheritdoc />
        public bool IsSlotOccupied(int slotNumber)
        {
            return false;
        }

        /// <inheritdoc />
        public void SaveToSlot(int slotNumber, GameState gameState)
        {
            // Stub - no operation
        }

        /// <inheritdoc />
        public Task SaveToSlotAsync(int slotNumber, GameState gameState)
        {
            return Task.CompletedTask;
        }

        /// <inheritdoc />
        public GameState LoadFromSlot(int slotNumber)
        {
            throw new InvalidOperationException($"Save slot {slotNumber} is empty.");
        }

        /// <inheritdoc />
        public Task<GameState> LoadFromSlotAsync(int slotNumber)
        {
            throw new InvalidOperationException($"Save slot {slotNumber} is empty.");
        }

        /// <inheritdoc />
        public void DeleteSlot(int slotNumber)
        {
            // Stub - no operation
        }

        /// <inheritdoc />
        public SaveSlotInfo GetSlotInfo(int slotNumber)
        {
            return new SaveSlotInfo(slotNumber);
        }

        /// <inheritdoc />
        public IReadOnlyList<SaveSlotInfo> GetAllSlotInfo()
        {
            var slots = new List<SaveSlotInfo>(DefaultMaxSlots);
            for (int i = 0; i < DefaultMaxSlots; i++)
            {
                slots.Add(new SaveSlotInfo(i));
            }
            return slots.AsReadOnly();
        }

        /// <inheritdoc />
        public int? GetFirstEmptySlot()
        {
            return 0;
        }

        /// <inheritdoc />
        public int GetOccupiedSlotCount()
        {
            return 0;
        }

        /// <inheritdoc />
        public void CopySlot(int sourceSlot, int destinationSlot)
        {
            // Stub - no operation
        }
    }
}
