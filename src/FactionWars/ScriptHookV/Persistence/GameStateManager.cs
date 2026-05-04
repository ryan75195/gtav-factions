using FactionWars.Core.Interfaces;
using FactionWars.Core.Models;
using FactionWars.Factions.Interfaces;
using FactionWars.Factions.Models;
using FactionWars.Persistence;
using FactionWars.Persistence.Models;
using FactionWars.ScriptHookV.Data;
using FactionWars.ScriptHookV.Logging;
using FactionWars.Territory.Interfaces;
using FactionWars.Territory.Models;
using System;
using System.Linq;

namespace FactionWars.ScriptHookV.Persistence
{
    /// <summary>
    /// Coordinates save/load operations between domain repositories and persistence layer.
    /// Implements IGameStateProvider for auto-save integration.
    /// </summary>
    public class GameStateManager : IGameStateManager
    {
        private readonly ISidecarStore _sidecarStore;
        private readonly IZoneRepository _zoneRepository;
        private readonly IFactionRepository _factionRepository;
        private readonly IZoneDefenderAllocationRepository _allocationRepository;

        private bool _hasGameLoaded;
        private long _totalPlayTimeSeconds;
        private float _playTimeAccumulator;
        private Difficulty _currentDifficulty = Difficulty.Normal;

        /// <inheritdoc />
        public bool HasGameLoaded => _hasGameLoaded;

        /// <inheritdoc />
        public long TotalPlayTimeSeconds => _totalPlayTimeSeconds;

        /// <inheritdoc />
        public event EventHandler<GameStateSavedEventArgs>? OnGameSaved;

        /// <inheritdoc />
        public event EventHandler<GameStateLoadedEventArgs>? OnGameLoaded;

        public GameStateManager(
            ISidecarStore sidecarStore,
            IZoneRepository zoneRepository,
            IFactionRepository factionRepository,
            IZoneDefenderAllocationRepository allocationRepository)
        {
            _sidecarStore = sidecarStore ?? throw new ArgumentNullException(nameof(sidecarStore));
            _zoneRepository = zoneRepository ?? throw new ArgumentNullException(nameof(zoneRepository));
            _factionRepository = factionRepository ?? throw new ArgumentNullException(nameof(factionRepository));
            _allocationRepository = allocationRepository ?? throw new ArgumentNullException(nameof(allocationRepository));

            _hasGameLoaded = false;
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
            gameState.SaveName = "Unnamed Save";
            gameState.Difficulty = _currentDifficulty;

            gameState.MarkModified();

            return gameState;
        }

        /// <inheritdoc />
        public void WriteCurrentSidecar(SaveFingerprint fingerprint, PlayerPosition position, string nativeSaveFilename)
        {
            if (!_hasGameLoaded)
            {
                FileLogger.Debug("WriteCurrentSidecar: no game loaded; skipping.");
                return;
            }

            if (fingerprint == null) throw new ArgumentNullException(nameof(fingerprint));
            if (position == null) throw new ArgumentNullException(nameof(position));
            if (nativeSaveFilename == null) throw new ArgumentNullException(nameof(nativeSaveFilename));

            var gameState = GetCurrentGameState()!;
            var sidecar = new Sidecar
            {
                Fingerprint = fingerprint,
                WrittenAtUtc = DateTime.UtcNow,
                NativeSaveFilename = nativeSaveFilename,
                PlayerPosition = position,
                GameState = gameState,
            };

            bool success = _sidecarStore.WriteSidecar(sidecar);
            OnGameSaved?.Invoke(this, new GameStateSavedEventArgs(0, gameState.SaveName, success));
        }

        /// <inheritdoc />
        public void HydrateFromSidecar(Sidecar sidecar)
        {
            if (sidecar == null) throw new ArgumentNullException(nameof(sidecar));
            if (sidecar.GameState == null) throw new ArgumentException("Sidecar.GameState required.", nameof(sidecar));

            try
            {
                ApplyGameState(sidecar.GameState);
                _totalPlayTimeSeconds = sidecar.GameState.TotalPlayTimeSeconds;
                _hasGameLoaded = true;

                OnGameLoaded?.Invoke(this, new GameStateLoadedEventArgs(0, sidecar.GameState.SaveName, true));
            }
            catch (Exception ex)
            {
                OnGameLoaded?.Invoke(this, new GameStateLoadedEventArgs(0, sidecar.GameState.SaveName, false, ex));
                throw;
            }
        }

        /// <inheritdoc />
        public void NewGame()
        {
            _hasGameLoaded = true;
            _totalPlayTimeSeconds = 0;
            _playTimeAccumulator = 0f;
            _currentDifficulty = Difficulty.Normal;
            OnGameLoaded?.Invoke(this, new GameStateLoadedEventArgs(0, "Unnamed Save", true));
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
