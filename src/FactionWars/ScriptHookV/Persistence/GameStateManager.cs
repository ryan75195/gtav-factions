using FactionWars.Core.Interfaces;
using FactionWars.Factions.Interfaces;
using FactionWars.Factions.Models;
using FactionWars.Persistence.Models;
using FactionWars.Territory.Interfaces;
using FactionWars.Territory.Models;
using System;
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
        private readonly IFactionRelationshipRepository _relationshipRepository;

        private bool _hasGameLoaded;
        private string? _currentSaveName;
        private long _totalPlayTimeSeconds;
        private float _playTimeAccumulator;

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
        /// <param name="relationshipRepository">The faction relationship repository.</param>
        public GameStateManager(
            ISaveSlotManager saveSlotManager,
            IZoneRepository zoneRepository,
            IFactionRepository factionRepository,
            IFactionRelationshipRepository relationshipRepository)
        {
            _saveSlotManager = saveSlotManager ?? throw new ArgumentNullException(nameof(saveSlotManager));
            _zoneRepository = zoneRepository ?? throw new ArgumentNullException(nameof(zoneRepository));
            _factionRepository = factionRepository ?? throw new ArgumentNullException(nameof(factionRepository));
            _relationshipRepository = relationshipRepository ?? throw new ArgumentNullException(nameof(relationshipRepository));

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
                _relationshipRepository.GetAll());

            gameState.TotalPlayTimeSeconds = _totalPlayTimeSeconds;
            gameState.SaveName = _currentSaveName ?? "Unnamed Save";
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
            _relationshipRepository.Clear();

            // Apply zones
            foreach (var zoneData in gameState.Zones)
            {
                var zone = zoneData.ToZone();
                _zoneRepository.Add(zone);
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

            // Apply relationships
            foreach (var relationshipData in gameState.Relationships)
            {
                var relationship = relationshipData.ToFactionRelationship();
                _relationshipRepository.Add(relationship);
            }
        }
    }
}
