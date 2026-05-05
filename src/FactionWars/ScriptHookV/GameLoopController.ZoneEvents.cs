using System;
using System.Collections.Generic;
using System.Linq;
using FactionWars.AI.Interfaces;
using FactionWars.AI.Models;
using FactionWars.Combat.Models;
using FactionWars.Core.Interfaces;
using FactionWars.Core.Models;
using FactionWars.ScriptHookV.Logging;
using FactionWars.ScriptHookV.Managers;
using FactionWars.Territory.Models;
using FactionWars.UI.Models;

namespace FactionWars.ScriptHookV
{
    public partial class GameLoopController
    {
        private void OnZoneEntered(object? sender, Zone zone)
        {
            FileLogger.Separator("ZONE ENTERED");
            FileLogger.Zone($"OnZoneEntered triggered");

            if (_zoneBattleManager == null || zone == null)
            {
                FileLogger.Error($"OnZoneEntered: zoneBattleManager={_zoneBattleManager != null}, zone={zone != null}");
                return;
            }

            // Tell battle simulator to skip battles in player's current zone
            _backgroundBattleSimulator?.SetPlayerZone(zone.Id);
            _aiController?.SetPlayerZone(zone.Id);

            LogZoneEntry(zone);

            if (!TryGetPlayerFactionKey(out var playerFactionKey))
                return;

            // Debug: Show zone info
            _gameBridge.ShowNotification($"~b~Entered:~w~ {zone.Name} (Owner: {zone.OwnerFactionId ?? "NONE"})");

            // Check if this is enemy territory
            var ownerFactionId = zone.OwnerFactionId;
            bool isEnemyTerritory = ownerFactionId != null && ownerFactionId != playerFactionKey;
            FileLogger.Zone($"Is Enemy Territory: {isEnemyTerritory}");

            if (isEnemyTerritory)
            {
                FileLogger.Combat($"Starting combat in {zone.Name}");

                AddCombatStartedEvent(zone, playerFactionKey, ownerFactionId!);

                // Start combat in enemy zone via ZoneBattleManager.
                Func<int> aliveCountCallback = () => GetPlayerCombatAliveCount(playerFactionKey);
                var battle = _zoneBattleManager.StartPlayerCombat(zone, playerFactionKey, aliveCountCallback);
                if (battle == null)
                {
                    FileLogger.Combat($"OnZoneEntered: StartPlayerCombat returned null for zone {zone.Id} — caller skipping.");
                    return;
                }
                ActivateEnemyZoneCombat(zone, ownerFactionId!, battle);
            }
            else if (zone.OwnerFactionId == null)
            {
                FileLogger.Zone($"{zone.Name} is NEUTRAL");
                _gameBridge.ShowNotification($"~y~{zone.Name} is NEUTRAL (no owner)");
            }
            else
            {
                FileLogger.Zone($"{zone.Name} is FRIENDLY (player owns)");
                _gameBridge.ShowNotification($"~g~{zone.Name} is YOUR territory");
            }

            // Notify zone battle manager that player entered this zone
            _zoneBattleManager?.OnPlayerEnteredZone(zone);
        }

        private bool TryGetPlayerFactionKey(out string playerFactionKey)
        {
            var playerFactionId = CurrentPlayerFactionId;
            FileLogger.Zone($"Player Faction: {playerFactionId ?? "NULL"}");
            if (!string.IsNullOrEmpty(playerFactionId))
            {
                playerFactionKey = playerFactionId!;
                return true;
            }

            FileLogger.Error("No player faction detected!");
            _gameBridge.ShowNotification("~r~DEBUG: No player faction detected!");
            playerFactionKey = string.Empty;
            return false;
        }

        private static void LogZoneEntry(Zone zone)
        {
            FileLogger.Zone($"Zone: {zone.Name} (ID: {zone.Id})");
            FileLogger.Zone($"Zone Owner: {zone.OwnerFactionId ?? "NULL/NONE"}");
            FileLogger.Zone($"Zone Center: ({zone.Center.X:F1}, {zone.Center.Y:F1}, {zone.Center.Z:F1}), Radius: {zone.Radius}");
        }

        private void ActivateEnemyZoneCombat(Zone zone, string ownerFactionId, ZoneBattle battle)
        {
            FileLogger.Combat($"Combat battle created: ID={battle.Id}");
            FileLogger.Combat($"Defending Faction: {battle.Defender.FactionId}");
            _gameBridge.ShowNotification($"~r~COMBAT STARTED in:~w~ {zone.Name}");
            CheckAndRespondToVehicleThreat(zone, ownerFactionId);
            _enemyDefenderManager?.OnEnemyZoneEntered(zone, ownerFactionId);
            _battleAttackerManager?.OnPlayerZoneEntered(zone);
        }

        /// <summary>
        /// Checks if the player is in a vehicle and responds to vehicle threats by deploying Elite units.
        /// </summary>
        /// <param name="zone">The enemy zone entered.</param>
        /// <param name="enemyFactionId">The faction defending the zone.</param>
        private void CheckAndRespondToVehicleThreat(Zone zone, string enemyFactionId)
        {
            // Check if services are available
            if (_vehicleThreatService == null || _antiVehicleResponseService == null)
            {
                FileLogger.AI("CheckAndRespondToVehicleThreat: Vehicle threat services not initialized");
                return;
            }

            if (!TryGetPlayerVehicleModel(out var vehicleModel))
                return;

            FileLogger.AI($"CheckAndRespondToVehicleThreat: Player vehicle detected - model={vehicleModel}");

            // Get threat level
            var threatLevel = _vehicleThreatService.GetThreatLevel(vehicleModel);
            FileLogger.AI($"CheckAndRespondToVehicleThreat: Threat level for {vehicleModel} = {threatLevel}");

            // If no threat, don't deploy
            if (threatLevel == VehicleThreatLevel.None)
            {
                FileLogger.AI("CheckAndRespondToVehicleThreat: No significant threat, skipping Elite deployment");
                return;
            }

            // Deploy Elite units as anti-vehicle response
            FileLogger.AI($"CheckAndRespondToVehicleThreat: Deploying Elite units for {threatLevel} threat in zone {zone.Id}");
            int deployed = _antiVehicleResponseService.RespondToVehicleThreat(enemyFactionId, zone.Id, threatLevel);

            if (deployed > 0)
            {
                FileLogger.Combat($"CheckAndRespondToVehicleThreat: Allocated {deployed} Elite RPG defenders against {vehicleModel} ({threatLevel} threat)");
                _gameBridge.ShowNotification($"~o~Enemy deploying {deployed} RPG units against your {vehicleModel}!");
                // Elite units will be spawned by the subsequent call to OnEnemyZoneEntered
            }
            else
            {
                FileLogger.AI($"CheckAndRespondToVehicleThreat: Failed to allocate Elite units (insufficient funds or reserves)");
            }
        }

        private void AddCombatStartedEvent(Zone zone, string playerFactionKey, string ownerFactionId)
        {
            if (_eventFeedService == null)
                return;

            var attackerFaction = _factionService.GetFaction(playerFactionKey);
            var defenderFaction = _factionService.GetFaction(ownerFactionId);
            var attackerName = attackerFaction?.Name ?? "Player";
            var defenderName = defenderFaction?.Name ?? "Defender";
            _eventFeedService.AddCombatStarted(
                zone.Name,
                attackerName,
                defenderName);
        }

        private bool TryGetPlayerVehicleModel(out string vehicleModel)
        {
            vehicleModel = string.Empty;
            if (!_gameBridge.IsPlayerInVehicle())
            {
                FileLogger.AI("CheckAndRespondToVehicleThreat: Player is not in a vehicle, no threat response needed");
                return false;
            }

            int vehicleHandle = _gameBridge.GetPlayerVehicle();
            if (vehicleHandle <= 0)
            {
                FileLogger.AI($"CheckAndRespondToVehicleThreat: Invalid vehicle handle ({vehicleHandle})");
                return false;
            }

            vehicleModel = _gameBridge.GetVehicleModelName(vehicleHandle);
            if (!string.IsNullOrEmpty(vehicleModel))
                return true;

            FileLogger.AI("CheckAndRespondToVehicleThreat: Could not get vehicle model name");
            return false;
        }

        /// <summary>
        /// Called when the player exits a zone.
        /// May end combat if leaving the contested zone.
        /// </summary>
        private void OnZoneExited(object? sender, Zone zone)
        {
            // Clear player zone tracking
            _backgroundBattleSimulator?.SetPlayerZone(null);
            _aiController?.SetPlayerZone(null);

            // Notify zone battle manager that player exited this zone
            _zoneBattleManager?.OnPlayerExitedZone(zone);

            if (zone == null)
                return;

            // If exiting an enemy zone, despawn enemy defenders
            if (zone.OwnerFactionId != null && zone.OwnerFactionId != CurrentPlayerFactionId)
            {
                _enemyDefenderManager?.OnEnemyZoneExited(zone);
            }

            // If we were in combat in this zone, remove the player as a participant (retreat)
            if (_zoneBattleManager != null && _zoneBattleManager.IsPlayerInBattle())
            {
                var currentBattle = _zoneBattleManager.GetPlayerCurrentBattle();
                string? playerFactionId = CurrentPlayerFactionId;
                if (currentBattle != null && currentBattle.ZoneId == zone.Id && playerFactionId is { Length: > 0 })
                {
                    _zoneBattleManager.RemoveParticipant(zone.Id, playerFactionId);
                    _gameBridge.ShowNotification($"~y~Retreated from:~w~ {zone.Name}");
                }
            }
        }

        /// <summary>
        /// Called when the player enters a neutral (unowned) zone.
        /// Shows the claim prompt.
        /// </summary>
        private void OnNeutralZoneEntered(object? sender, Zone zone)
        {
            _currentNeutralZone = zone;
            _showingClaimPrompt = true;

            var cost = GetBasicTroopCost();
            _gameBridge.ShowNotification($"~y~Unclaimed territory: {zone.Name}");
        }

        /// <summary>
        /// Called when the player exits a zone (for claim state tracking).
        /// Clears the claim prompt if exiting the current neutral zone.
        /// </summary>
        private void OnZoneExitedForClaim(object? sender, Zone zone)
        {
            if (_currentNeutralZone?.Id == zone.Id)
            {
                _currentNeutralZone = null;
                _showingClaimPrompt = false;
            }
        }

        /// <summary>
        /// Gets the cost of a basic troop from the defender tier service.
        /// </summary>
    }
}
