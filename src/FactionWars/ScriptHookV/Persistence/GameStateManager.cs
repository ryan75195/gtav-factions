using FactionWars.Core.Interfaces;
using FactionWars.Core.Models;
using FactionWars.Factions.Interfaces;
using FactionWars.Factions.Models;
using FactionWars.Persistence.Models;
using FactionWars.ScriptHookV.Data;
using FactionWars.ScriptHookV.Logging;
using FactionWars.Territory.Interfaces;
using FactionWars.Territory.Models;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace FactionWars.ScriptHookV.Persistence
{
    /// <summary>
    /// Coordinates save/load operations between domain repositories and persistence layer.
    /// Implements IGameStateProvider for auto-save integration.
    /// </summary>
    public class GameStateManager : IGameStateManager
    {
        private readonly ISaveSlotManager _saveSlotManager;
        private readonly IZoneRepository _zoneRepository;
        private readonly IFactionRepository _factionRepository;
        private readonly IZoneDefenderAllocationRepository _allocationRepository;

        private bool _hasGameLoaded;
        private string? _currentSaveName;
        private long _totalPlayTimeSeconds;
        private float _playTimeAccumulator;
        private Difficulty _currentDifficulty = Difficulty.Normal;

        /// <inheritdoc />
        public bool HasGameLoaded => _hasGameLoaded;

        /// <inheritdoc />
        public string? CurrentSaveName => _currentSaveName;

        /// <inheritdoc />
        public long TotalPlayTimeSeconds => _totalPlayTimeSeconds;

        /// <inheritdoc />
        public event EventHandler<GameStateSavedEventArgs>? OnGameSaved;

        /// <inheritdoc />
        public event EventHandler<GameStateLoadedEventArgs>? OnGameLoaded;

        /// <summary>
        /// Creates a new GameStateManager with the specified dependencies.
        /// </summary>
        /// <param name="saveSlotManager">The save slot manager for persistence operations.</param>
        /// <param name="zoneRepository">The zone repository.</param>
        /// <param name="factionRepository">The faction repository.</param>
        /// <param name="allocationRepository">The zone defender allocation repository.</param>
        public GameStateManager(
            ISaveSlotManager saveSlotManager,
            IZoneRepository zoneRepository,
            IFactionRepository factionRepository,
            IZoneDefenderAllocationRepository allocationRepository)
        {
            _saveSlotManager = saveSlotManager ?? throw new ArgumentNullException(nameof(saveSlotManager));
            _zoneRepository = zoneRepository ?? throw new ArgumentNullException(nameof(zoneRepository));
            _factionRepository = factionRepository ?? throw new ArgumentNullException(nameof(factionRepository));
            _allocationRepository = allocationRepository ?? throw new ArgumentNullException(nameof(allocationRepository));

            _hasGameLoaded = false;
            _currentSaveName = null;
            _totalPlayTimeSeconds = 0;
            _playTimeAccumulator = 0f;
        }

        /// <inheritdoc />
        public GameState? GetCurrentGameState()
        {
            if (!_hasGameLoaded)
            {
                return null;
            }

            var gameState = GameState.CreateSnapshot(
                _factionRepository.GetAll(),
                _factionRepository.GetAllStates(),
                _zoneRepository.GetAll(),
                Enumerable.Empty<FactionRelationship>(),
                _allocationRepository.GetAll());

            gameState.TotalPlayTimeSeconds = _totalPlayTimeSeconds;
            gameState.SaveName = _currentSaveName ?? "Unnamed Save";
            gameState.Difficulty = _currentDifficulty;
            gameState.MarkModified();

            return gameState;
        }

        /// <inheritdoc />
        public void SaveToSlot(int slotNumber, string? saveName = null)
        {
            if (!_hasGameLoaded)
            {
                throw new InvalidOperationException("Cannot save: no game is currently loaded.");
            }

            var effectiveSaveName = saveName ?? _currentSaveName ?? "Save";

            try
            {
                var gameState = GetCurrentGameState()!;
                gameState.SaveName = effectiveSaveName;
                gameState.MarkModified();

                _saveSlotManager.SaveToSlot(slotNumber, gameState);
                _currentSaveName = effectiveSaveName;

                OnGameSaved?.Invoke(this, new GameStateSavedEventArgs(slotNumber, effectiveSaveName, true));
            }
            catch (Exception ex)
            {
                OnGameSaved?.Invoke(this, new GameStateSavedEventArgs(slotNumber, effectiveSaveName, false, ex));
                throw;
            }
        }

        /// <inheritdoc />
        public async Task SaveToSlotAsync(int slotNumber, string? saveName = null)
        {
            if (!_hasGameLoaded)
            {
                throw new InvalidOperationException("Cannot save: no game is currently loaded.");
            }

            var effectiveSaveName = saveName ?? _currentSaveName ?? "Save";

            try
            {
                var gameState = GetCurrentGameState()!;
                gameState.SaveName = effectiveSaveName;
                gameState.MarkModified();

                await _saveSlotManager.SaveToSlotAsync(slotNumber, gameState);
                _currentSaveName = effectiveSaveName;

                OnGameSaved?.Invoke(this, new GameStateSavedEventArgs(slotNumber, effectiveSaveName, true));
            }
            catch (Exception ex)
            {
                OnGameSaved?.Invoke(this, new GameStateSavedEventArgs(slotNumber, effectiveSaveName, false, ex));
                throw;
            }
        }

        /// <inheritdoc />
        public void LoadFromSlot(int slotNumber)
        {
            string saveName = "Unknown";

            try
            {
                var gameState = _saveSlotManager.LoadFromSlot(slotNumber);
                saveName = gameState.SaveName;

                ApplyGameState(gameState);

                _currentSaveName = gameState.SaveName;
                _totalPlayTimeSeconds = gameState.TotalPlayTimeSeconds;
                _playTimeAccumulator = 0f;
                _hasGameLoaded = true;

                OnGameLoaded?.Invoke(this, new GameStateLoadedEventArgs(slotNumber, saveName, true));
            }
            catch (Exception ex)
            {
                OnGameLoaded?.Invoke(this, new GameStateLoadedEventArgs(slotNumber, saveName, false, ex));
                throw;
            }
        }

        /// <inheritdoc />
        public async Task LoadFromSlotAsync(int slotNumber)
        {
            string saveName = "Unknown";

            try
            {
                var gameState = await _saveSlotManager.LoadFromSlotAsync(slotNumber);
                saveName = gameState.SaveName;

                ApplyGameState(gameState);

                _currentSaveName = gameState.SaveName;
                _totalPlayTimeSeconds = gameState.TotalPlayTimeSeconds;
                _playTimeAccumulator = 0f;
                _hasGameLoaded = true;

                OnGameLoaded?.Invoke(this, new GameStateLoadedEventArgs(slotNumber, saveName, true));
            }
            catch (Exception ex)
            {
                OnGameLoaded?.Invoke(this, new GameStateLoadedEventArgs(slotNumber, saveName, false, ex));
                throw;
            }
        }

        /// <inheritdoc />
        public void NewGame()
        {
            _hasGameLoaded = true;
            _currentSaveName = null;
            _totalPlayTimeSeconds = 0;
            _playTimeAccumulator = 0f;
        }

        /// <inheritdoc />
        public void UpdatePlayTime(float deltaTimeSeconds)
        {
            if (!_hasGameLoaded || deltaTimeSeconds <= 0)
            {
                return;
            }

            _playTimeAccumulator += deltaTimeSeconds;

            // Convert accumulated fractional seconds to whole seconds
            while (_playTimeAccumulator >= 1f)
            {
                _totalPlayTimeSeconds++;
                _playTimeAccumulator -= 1f;
            }
        }

        /// <inheritdoc />
        public void ApplyGameState(GameState gameState)
        {
            if (gameState == null)
            {
                throw new ArgumentNullException(nameof(gameState));
            }

            // Clear existing data
            _zoneRepository.Clear();
            _factionRepository.Clear();
            _allocationRepository.Clear();

            // Apply zones
            foreach (var zoneData in gameState.Zones)
            {
                var zone = zoneData.ToZone();
                _zoneRepository.Add(zone);
            }

            // Migration: Re-setup adjacencies if save file is missing them (pre-adjacency-persistence saves)
            var firstZone = _zoneRepository.GetAll().FirstOrDefault();
            if (firstZone != null && firstZone.AdjacentZoneIds.Count == 0)
            {
                FileLogger.AI("Migrating save: Setting up zone adjacencies (missing from save file)...");
                ZoneDataLoader.SetupZoneAdjacencies(_zoneRepository);

                int totalAdjacencies = _zoneRepository.GetAll().Sum(z => z.AdjacentZoneIds.Count);
                FileLogger.AI($"Migration complete: {totalAdjacencies} adjacency links restored");
            }

            // Apply factions
            foreach (var factionData in gameState.Factions)
            {
                var faction = factionData.ToFaction();
                _factionRepository.Add(faction);
            }

            // Apply faction states
            foreach (var stateData in gameState.FactionStates)
            {
                var state = stateData.ToFactionState();
                _factionRepository.SetState(state);
            }

            // Note: Relationships from save files are ignored (feature removed)

            // Apply zone defender allocations (troop deployments)
            if (gameState.Allocations != null)
            {
                foreach (var allocationData in gameState.Allocations)
                {
                    var allocation = allocationData.ToAllocation();
                    _allocationRepository.Add(allocation);
                }
            }
        }

        /// <inheritdoc />
        public void SetCurrentDifficulty(Difficulty difficulty)
        {
            _currentDifficulty = difficulty;
        }
    }
}
