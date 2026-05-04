using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using FactionWars.AI.Interfaces;
using FactionWars.AI.Models;
using FactionWars.Combat.Interfaces;
using FactionWars.Combat.Models;
using FactionWars.Configuration;
using FactionWars.Core.Interfaces;
using FactionWars.Core.Models;
using FactionWars.Core.Services;
using FactionWars.Persistence.Models;
using FactionWars.Economy.Interfaces;
using FactionWars.Factions.Interfaces;
using FactionWars.ScriptHookV.Data;
using FactionWars.ScriptHookV.Logging;
using FactionWars.ScriptHookV.Managers;
using FactionWars.ScriptHookV.Models;
using FactionWars.ScriptHookV.Persistence;
using FactionWars.ScriptHookV.UI;
using FactionWars.Telemetry.Interfaces;
using FactionWars.Telemetry.Services;
using FactionWars.Territory.Interfaces;
using FactionWars.Territory.Models;
using FactionWars.UI.Interfaces;
using FactionWars.UI.Models;
using GTA.Native;

namespace FactionWars.ScriptHookV
{
    public partial class GameLoopController
    {
        private void HandleCharacterSwitched(string? oldFactionId, string? newFactionId)
        {
            // Dismiss all followers for the old faction when switching characters
            if (!string.IsNullOrEmpty(oldFactionId) && _followerManager != null)
            {
                _followerManager.DismissAllFollowers(oldFactionId!);
            }

            // Despawn all friendly defenders and update faction for the new character
            if (_friendlyDefenderManager != null)
            {
                _friendlyDefenderManager.DespawnAllDefenders();
                if (!string.IsNullOrEmpty(newFactionId))
                {
                    _friendlyDefenderManager.SetPlayerFaction(newFactionId!);
                }
            }

            // Despawn all enemy defenders and battle attackers - they have stale
            // relationship groups that were configured against the OLD player
            // character's group. New peds will spawn fresh with correct
            // relationships when the player enters zones.
            FileLogger.Spawn("HandleCharacterSwitched: Despawning all enemy defenders (stale relationships)");
            _enemyDefenderManager?.DespawnAllDefenders();
            FileLogger.Spawn("HandleCharacterSwitched: Despawning all battle attackers (stale relationships)");
            _battleAttackerManager?.DespawnAllAttackers();

            // Update battle attacker manager faction
            if (!string.IsNullOrEmpty(newFactionId))
            {
                var factionId = newFactionId!;
                _battleAttackerManager?.SetPlayerFaction(factionId);
            }

            // Update managers with new faction
            FileLogger.Info($"HandleCharacterSwitched: Updating all managers to new faction: {newFactionId}");
            _economyManager?.SetPlayerFactionId(newFactionId);
            _aiManager?.SetPlayerFactionId(newFactionId);
            _aiController?.SetPlayerFactionId(newFactionId);
            _zoneBattleManager?.SetPlayerFaction(newFactionId);

            // Sync player state to new faction (clear weapons, set cash to faction capital)
            SyncPlayerToFactionState(newFactionId);
            RequestOwnedTerritoryPlacement(newFactionId, "character-switch");

            // Show notification to player
            var newCharacterName = GetCharacterDisplayName(newFactionId);
            _gameBridge.ShowNotification($"~b~FactionWars:~w~ Switched to {newCharacterName}'s faction");

            // Raise the public event for other systems to respond
            OnCharacterSwitched?.Invoke(oldFactionId, newFactionId);
            FileLogger.Info("HandleCharacterSwitched: Complete");
        }

        private void RequestOwnedTerritoryPlacement(string? factionId, string reason)
        {
            if (string.IsNullOrEmpty(factionId))
            {
                return;
            }

            _pendingOwnedTerritoryFactionId = factionId;
            _pendingOwnedTerritoryReason = reason;
            _pendingOwnedTerritoryAttempts = OwnedTerritoryPlacementRetryTicks;
            _pendingOwnedTerritoryLoggedSuccess = false;
            _pendingOwnedTerritoryWaitingForControlLogged = false;
            FileLogger.Info($"RequestOwnedTerritoryPlacement: faction={factionId} reason={reason}");
        }

        private void ProcessPendingOwnedTerritoryPlacement()
        {
            if (_pendingOwnedTerritoryAttempts <= 0 || string.IsNullOrEmpty(_pendingOwnedTerritoryFactionId))
            {
                return;
            }

            if (!_gameBridge.CanControlCharacter())
            {
                if (!_pendingOwnedTerritoryWaitingForControlLogged)
                {
                    FileLogger.Info($"ProcessPendingOwnedTerritoryPlacement: waiting for player control faction={_pendingOwnedTerritoryFactionId} reason={_pendingOwnedTerritoryReason}");
                    _pendingOwnedTerritoryWaitingForControlLogged = true;
                }

                return;
            }

            _pendingOwnedTerritoryWaitingForControlLogged = false;

            var result = MovePlayerToOwnedTerritory(
                _pendingOwnedTerritoryFactionId,
                _pendingOwnedTerritoryReason ?? "unknown",
                logAlreadyOwned: !_pendingOwnedTerritoryLoggedSuccess);

            if (result == OwnedTerritoryPlacementResult.AlreadyOwned || result == OwnedTerritoryPlacementResult.Moved)
            {
                _pendingOwnedTerritoryLoggedSuccess = true;
            }

            if (result == OwnedTerritoryPlacementResult.NoTarget || result == OwnedTerritoryPlacementResult.NoPlayerPed)
            {
                _pendingOwnedTerritoryAttempts = 0;
                _pendingOwnedTerritoryFactionId = null;
                _pendingOwnedTerritoryReason = null;
                return;
            }

            _pendingOwnedTerritoryAttempts--;
            if (_pendingOwnedTerritoryAttempts <= 0)
            {
                FileLogger.Info($"ProcessPendingOwnedTerritoryPlacement: completed retry window for faction={_pendingOwnedTerritoryFactionId} reason={_pendingOwnedTerritoryReason}");
                _pendingOwnedTerritoryFactionId = null;
                _pendingOwnedTerritoryReason = null;
            }
        }

        private OwnedTerritoryPlacementResult MovePlayerToOwnedTerritory(string? factionId, string reason, bool logAlreadyOwned)
        {
            if (string.IsNullOrEmpty(factionId) || _zoneService == null)
            {
                return OwnedTerritoryPlacementResult.NoTarget;
            }

            var playerPosition = _gameBridge.GetPlayerPosition();
            var currentZone = _zoneService.GetZoneAtPosition(playerPosition);
            if (currentZone != null && currentZone.OwnerFactionId == factionId)
            {
                if (logAlreadyOwned)
                {
                    FileLogger.Info($"MovePlayerToOwnedTerritory: already in owned zone {currentZone.Id} reason={reason}");
                }

                return OwnedTerritoryPlacementResult.AlreadyOwned;
            }

            var ownedZones = _zoneService.GetZonesByOwner(factionId)
                .OrderBy(z => z.IsContested)
                .ThenBy(z => z.Center.DistanceTo2D(playerPosition))
                .ToList();

            var targetZone = ownedZones.FirstOrDefault();
            if (targetZone == null)
            {
                FileLogger.Warn($"MovePlayerToOwnedTerritory: no owned zones found for faction {factionId}");
                return OwnedTerritoryPlacementResult.NoTarget;
            }

            var landingPosition = GetOwnedTerritoryLandingPosition(targetZone);
            var playerPed = _gameBridge.GetPlayerPedHandle();
            if (playerPed <= 0)
            {
                FileLogger.Warn("MovePlayerToOwnedTerritory: no valid player ped handle");
                return OwnedTerritoryPlacementResult.NoPlayerPed;
            }

            _gameBridge.SetPedPosition(playerPed, landingPosition);
            FileLogger.Info($"MovePlayerToOwnedTerritory: moved {factionId} player to {targetZone.Name} ({targetZone.Id}) at ({landingPosition.X:F1},{landingPosition.Y:F1},{landingPosition.Z:F1}) reason={reason}");
            return OwnedTerritoryPlacementResult.Moved;
        }

        private Vector3 GetOwnedTerritoryLandingPosition(Zone targetZone)
        {
            var center = targetZone.Center;
            var candidates = GetLandingCandidates(targetZone);

            foreach (var candidate in candidates)
            {
                if (!targetZone.Boundary.Contains(candidate))
                    continue;

                var groundCandidate = new Vector3(
                    candidate.X,
                    candidate.Y,
                    _gameBridge.GetGroundZ(candidate.X, candidate.Y, candidate.Z));
                if (targetZone.Boundary.Contains(groundCandidate))
                    return groundCandidate;

                var safeCandidate = _gameBridge.GetSafeCoordForPed(candidate);
                if (targetZone.Boundary.Contains(safeCandidate))
                    return safeCandidate;
            }

            return new Vector3(center.X, center.Y, _gameBridge.GetGroundZ(center.X, center.Y, center.Z));
        }

        private static List<Vector3> GetLandingCandidates(Zone targetZone)
        {
            var center = targetZone.Center;
            var candidates = new List<Vector3>
            {
                center
            };

            float inwardDistance = Math.Max(10f, Math.Min(targetZone.Radius * 0.2f, 40f));
            float[] distanceFractions = { 0.1f, 0.15f, 0.2f, 0.25f };
            double[] angles =
            {
                0.0,
                Math.PI / 4.0,
                Math.PI / 2.0,
                3.0 * Math.PI / 4.0,
                Math.PI,
                5.0 * Math.PI / 4.0,
                3.0 * Math.PI / 2.0,
                7.0 * Math.PI / 4.0
            };

            foreach (float fraction in distanceFractions)
            {
                float radius = Math.Min(inwardDistance, targetZone.Radius * fraction);
                foreach (double angle in angles)
                {
                    candidates.Add(new Vector3(
                        center.X + (float)(Math.Cos(angle) * radius),
                        center.Y + (float)(Math.Sin(angle) * radius),
                        center.Z));
                }
            }

            return candidates;
        }

        private void UpdatePlayerRespawnPlacement()
        {
            bool isDead;
            try
            {
                isDead = _gameBridge.IsPlayerDead();
            }
            catch (Exception ex)
            {
                FileLogger.Error("UpdatePlayerRespawnPlacement: failed to read player death state", ex);
                return;
            }

            if (!_hasReadPlayerDeathState)
            {
                _wasPlayerDead = isDead;
                _hasReadPlayerDeathState = true;
                return;
            }

            if (_wasPlayerDead && !isDead)
            {
                RequestOwnedTerritoryPlacement(CurrentPlayerFactionId, "respawn");
            }

            _wasPlayerDead = isDead;
        }

        private enum OwnedTerritoryPlacementResult
        {
            AlreadyOwned,
            Moved,
            NoTarget,
            NoPlayerPed
        }

        /// <summary>
        /// Gets the display name for a character based on their faction ID.
        /// </summary>
        private static string GetCharacterDisplayName(string? factionId)
        {
            return factionId switch
            {
                CharacterModelFactionDetector.MichaelFactionId => "Michael",
                CharacterModelFactionDetector.FranklinFactionId => "Franklin",
                CharacterModelFactionDetector.TrevorFactionId => "Trevor",
                _ => "Unknown"
            };
        }

        /// <summary>
        /// Called when the player enters a zone.
        /// Triggers combat if entering enemy territory.
        /// </summary>
    }
}
