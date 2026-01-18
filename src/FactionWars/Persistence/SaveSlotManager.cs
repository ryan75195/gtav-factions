using FactionWars.Core.Interfaces;
using FactionWars.Persistence.Models;
using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;

namespace FactionWars.Persistence
{
    /// <summary>
    /// Manages multiple save slots for game state persistence.
    /// </summary>
    public class SaveSlotManager : ISaveSlotManager
    {
        private const string SaveFilePrefix = "save_slot_";
        private const string SaveFileExtension = ".json";
        private const int DefaultMaxSlots = 10;

        private readonly IPersistenceService _persistenceService;
        private readonly string _saveDirectory;
        private readonly int _maxSlots;

        /// <inheritdoc />
        public int MaxSlots => _maxSlots;

        /// <inheritdoc />
        public string SaveDirectory => _saveDirectory;

        /// <summary>
        /// Creates a new SaveSlotManager.
        /// </summary>
        /// <param name="persistenceService">The persistence service for reading/writing save files.</param>
        /// <param name="saveDirectory">The directory to store save files in.</param>
        /// <param name="maxSlots">Maximum number of save slots (default 10).</param>
        public SaveSlotManager(IPersistenceService persistenceService, string saveDirectory, int maxSlots = DefaultMaxSlots)
        {
            if (persistenceService == null)
            {
                throw new ArgumentNullException(nameof(persistenceService));
            }

            if (saveDirectory == null)
            {
                throw new ArgumentNullException(nameof(saveDirectory));
            }

            if (string.IsNullOrEmpty(saveDirectory))
            {
                throw new ArgumentException("Save directory cannot be empty.", nameof(saveDirectory));
            }

            if (maxSlots <= 0)
            {
                throw new ArgumentException("Max slots must be greater than zero.", nameof(maxSlots));
            }

            _persistenceService = persistenceService;
            _saveDirectory = saveDirectory;
            _maxSlots = maxSlots;

            // Create directory if it doesn't exist
            if (!Directory.Exists(_saveDirectory))
            {
                Directory.CreateDirectory(_saveDirectory);
            }
        }

        /// <inheritdoc />
        public string GetSlotFilePath(int slotNumber)
        {
            ValidateSlotNumber(slotNumber);
            return Path.Combine(_saveDirectory, $"{SaveFilePrefix}{slotNumber}{SaveFileExtension}");
        }

        /// <inheritdoc />
        public bool IsSlotOccupied(int slotNumber)
        {
            ValidateSlotNumber(slotNumber);
            var filePath = GetSlotFilePath(slotNumber);
            return _persistenceService.Exists(filePath);
        }

        /// <inheritdoc />
        public void SaveToSlot(int slotNumber, GameState gameState)
        {
            ValidateSlotNumber(slotNumber);

            if (gameState == null)
            {
                throw new ArgumentNullException(nameof(gameState));
            }

            var filePath = GetSlotFilePath(slotNumber);
            _persistenceService.Save(gameState, filePath);
        }

        /// <inheritdoc />
        public async Task SaveToSlotAsync(int slotNumber, GameState gameState)
        {
            ValidateSlotNumber(slotNumber);

            if (gameState == null)
            {
                throw new ArgumentNullException(nameof(gameState));
            }

            var filePath = GetSlotFilePath(slotNumber);
            await _persistenceService.SaveAsync(gameState, filePath);
        }

        /// <inheritdoc />
        public GameState LoadFromSlot(int slotNumber)
        {
            ValidateSlotNumber(slotNumber);

            var filePath = GetSlotFilePath(slotNumber);
            if (!_persistenceService.Exists(filePath))
            {
                throw new InvalidOperationException($"Save slot {slotNumber} is empty.");
            }

            return _persistenceService.Load(filePath);
        }

        /// <inheritdoc />
        public async Task<GameState> LoadFromSlotAsync(int slotNumber)
        {
            ValidateSlotNumber(slotNumber);

            var filePath = GetSlotFilePath(slotNumber);
            if (!_persistenceService.Exists(filePath))
            {
                throw new InvalidOperationException($"Save slot {slotNumber} is empty.");
            }

            return await _persistenceService.LoadAsync(filePath);
        }

        /// <inheritdoc />
        public void DeleteSlot(int slotNumber)
        {
            ValidateSlotNumber(slotNumber);
            var filePath = GetSlotFilePath(slotNumber);
            _persistenceService.Delete(filePath);
        }

        /// <inheritdoc />
        public SaveSlotInfo GetSlotInfo(int slotNumber)
        {
            ValidateSlotNumber(slotNumber);

            var info = new SaveSlotInfo(slotNumber);
            var filePath = GetSlotFilePath(slotNumber);

            if (_persistenceService.Exists(filePath))
            {
                var gameState = _persistenceService.Load(filePath);
                info.SetOccupied(gameState.SaveName, gameState.ModifiedAt, gameState.TotalPlayTimeSeconds);
            }

            return info;
        }

        /// <inheritdoc />
        public IReadOnlyList<SaveSlotInfo> GetAllSlotInfo()
        {
            var slotInfos = new List<SaveSlotInfo>(_maxSlots);

            for (int i = 0; i < _maxSlots; i++)
            {
                slotInfos.Add(GetSlotInfo(i));
            }

            return slotInfos.AsReadOnly();
        }

        /// <inheritdoc />
        public int? GetFirstEmptySlot()
        {
            for (int i = 0; i < _maxSlots; i++)
            {
                if (!IsSlotOccupied(i))
                {
                    return i;
                }
            }

            return null;
        }

        /// <inheritdoc />
        public int GetOccupiedSlotCount()
        {
            int count = 0;
            for (int i = 0; i < _maxSlots; i++)
            {
                if (IsSlotOccupied(i))
                {
                    count++;
                }
            }
            return count;
        }

        /// <inheritdoc />
        public void CopySlot(int sourceSlot, int destinationSlot)
        {
            ValidateSlotNumber(sourceSlot);
            ValidateSlotNumber(destinationSlot);

            if (sourceSlot == destinationSlot)
            {
                throw new ArgumentException("Source and destination slots cannot be the same.");
            }

            var sourcePath = GetSlotFilePath(sourceSlot);
            if (!_persistenceService.Exists(sourcePath))
            {
                throw new InvalidOperationException($"Source slot {sourceSlot} is empty.");
            }

            var gameState = _persistenceService.Load(sourcePath);
            var destPath = GetSlotFilePath(destinationSlot);
            _persistenceService.Save(gameState, destPath);
        }

        private void ValidateSlotNumber(int slotNumber)
        {
            if (slotNumber < 0 || slotNumber >= _maxSlots)
            {
                throw new ArgumentOutOfRangeException(nameof(slotNumber),
                    $"Slot number must be between 0 and {_maxSlots - 1}.");
            }
        }
    }
}
