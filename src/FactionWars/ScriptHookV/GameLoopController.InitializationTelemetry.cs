using System;
using System.IO;
using FactionWars.Configuration;
using FactionWars.Factions.Interfaces;
using FactionWars.Persistence.Models;
using FactionWars.ScriptHookV.Logging;
using FactionWars.ScriptHookV.Persistence;
using FactionWars.Telemetry.Interfaces;
using FactionWars.Telemetry.Services;

namespace FactionWars.ScriptHookV
{
    public partial class GameLoopController
    {
        private void InitializeTelemetryService()
        {
            if (_gameStateManager == null || _zoneService == null)
            {
                FileLogger.Warn("TelemetryService not initialized: game state or zone service missing.");
                return;
            }

            var config = _container.Resolve<GameConfig>();
            var telemetryRoot = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments),
                config.Persistence.SaveDirectoryName,
                "Telemetry");

            _telemetryService = new TelemetryService(
                _container.Resolve<ITelemetrySink>(),
                _factionService,
                _zoneService,
                _gameStateManager,
                new TelemetryServiceOptions
                {
                    GetPlayerPedHandle = () => _gameBridge.GetPlayerPedHandle(),
                    GetPlayerFactionId = () => CurrentPlayerFactionId,
                    GetPlayerMoney = () => _gameBridge.GetPlayerMoney(),
                    IsPlayerDead = () => _gameBridge.IsPlayerDead(),
                    GetCurrentZoneId = () => _territoryManager?.CurrentZone?.Id,
                    GetPlayerPosition = () => _gameBridge.GetPlayerPosition(),
                    IsFirstTimeSeenSave = save => !Directory.Exists(Path.Combine(telemetryRoot, save)),
                    ZoneBattleManager = _zoneBattleManager,
                    AIController = _aiController,
                    AllocationService = _allocationService,
                    ResourceTickService = _resourceTickService,
                    BattleAttackerManager = _battleAttackerManager,
                    VictoryManager = _victoryManager,
                    DifficultyService = _difficultyService,
                    NativeSaveWatcher = _container.Resolve<NativeSaveWatcher>()
                });
        }

        /// <summary>
        /// Applies per-session GTA flags. Must run every launch because GTA does
        /// not persist these across sessions. Does NOT touch weapons or cash —
        /// those are owned by the native save now.
        /// </summary>
        private void ConfigureSessionSettings()
        {
            _gameBridge.ConfigurePlayerSettings();
        }

        /// <summary>
        /// Syncs player state to their faction's economic state on character switch.
        /// Sets cash to faction capital. The player's weapon loadout is preserved
        /// across switches.
        /// </summary>
        private void SyncPlayerToFactionState(string? factionId)
        {
            if (string.IsNullOrEmpty(factionId))
                return;
            var factionKey = factionId!;

            // Get faction state and sync player's GTA money to faction's cash
            var factionState = _factionRepository.GetState(factionKey);
            if (factionState != null)
            {
                var factionCash = factionState.Cash;
                _gameBridge.SetPlayerMoney(factionCash);
                FileLogger.Info($"SyncPlayerToFactionState: {factionKey} - Set cash to faction's ${factionCash:N0}");
            }
            else
            {
                FileLogger.Warn($"SyncPlayerToFactionState: No state found for faction {factionKey}");
            }

            // Configure weapon persistence
            _gameBridge.ConfigurePlayerSettings();
        }

        /// <summary>
        /// Handles item selection from the main menu to navigate to submenus.
        /// </summary>
    }
}
