using System;
using System.Collections.Generic;
using System.Linq;
using FactionWars.Combat.Models;
using FactionWars.Core.Interfaces;
using FactionWars.Core.Models;
using FactionWars.ScriptHookV.Logging;
using FactionWars.ScriptHookV.Services;
using FactionWars.Territory.Models;

namespace FactionWars.ScriptHookV.Managers
{
    public partial class FriendlyDefenderManager
    {
        public void Update()
        {
            var newlyDeadPeds = new List<(string zoneId, int pedHandle, DefenderTier tier)>();
            var streamedOutPeds = new List<(string zoneId, int pedHandle)>();
            var currentGameTime = _gameBridge.GetGameTime();

            // Check all spawned defenders for death
            foreach (var kvp in _spawnedPedTierByZone)
            {
                var zoneId = kvp.Key;
                var pedTiers = kvp.Value;

                foreach (var pedKvp in pedTiers)
                {
                    var pedHandle = pedKvp.Key;
                    var tier = pedKvp.Value;

                    // Skip if already tracked as corpse
                    if (_corpseDeathTimes.ContainsKey(pedHandle))
                        continue;

                    // Distinguish "ped culled by GTA's streaming/population manager"
                    // (entity gone) from "ped died in combat" (entity still here, just
                    // dead). Decrementing allocation for streamed-out peds would shed
                    // troops we never actually lost.
                    if (!_gameBridge.DoesPedExist(pedHandle))
                    {
                        streamedOutPeds.Add((zoneId, pedHandle));
                    }
                    else if (!_gameBridge.IsPedAlive(pedHandle))
                    {
                        newlyDeadPeds.Add((zoneId, pedHandle, tier));
                    }
                }
            }

            // Quietly untrack peds the engine culled — no allocation change, no event.
            foreach (var (zoneId, pedHandle) in streamedOutPeds)
            {
                if (_spawnedPedTierByZone.TryGetValue(zoneId, out var pedTiers))
                {
                    pedTiers.Remove(pedHandle);
                }
                _pedBlipService.RemoveBlipForPed(pedHandle);
                _pedDespawnService.UntrackPed(pedHandle);
            }

            // Process each newly dead defender
            foreach (var (zoneId, pedHandle, tier) in newlyDeadPeds)
            {
                HandleDefenderDeath(zoneId, pedHandle, tier);
            }

            // Clean up corpses that have exceeded the delay
            CleanupExpiredCorpses(currentGameTime);

            EnforceZoneLeash(currentGameTime);
        }

        /// <summary>
        /// Every <see cref="ZoneLeashEnforcer.LeashCheckIntervalMs"/>, scan all
        /// tracked defenders. Any whose distance from their zone center exceeds
        /// the hysteresis threshold gets its tasks cleared and a TaskGoToCoord
        /// back to a random point inside the inner half of the zone.
        /// </summary>
        private void EnforceZoneLeash(int currentGameTime)
        {
            if (currentGameTime - _lastLeashCheckMs < ZoneLeashEnforcer.LeashCheckIntervalMs)
                return;
            _lastLeashCheckMs = currentGameTime;

            foreach (var kvp in _spawnedPedTierByZone)
            {
                var zoneId = kvp.Key;
                var pedTiers = kvp.Value;

                var zone = _zoneService.GetZone(zoneId);
                if (zone == null)
                    continue;

                foreach (var pedHandle in pedTiers.Keys)
                {
                    if (_corpseDeathTimes.ContainsKey(pedHandle))
                        continue;
                    if (!_gameBridge.DoesPedExist(pedHandle) || !_gameBridge.IsPedAlive(pedHandle))
                        continue;

                    var pedPos = _gameBridge.GetPedPosition(pedHandle);
                    if (!ZoneLeashEnforcer.ShouldLeash(pedPos, zone.Center, zone.Radius))
                        continue;

                    // Don't yank a ped that's actively fighting — clearing its tasks
                    // cancels combat, the AI immediately re-engages and strays again,
                    // and the leash re-fires every interval (thrash + log spam).
                    if (_gameBridge.IsPedInCombat(pedHandle))
                        continue;

                    var returnPoint = ZoneLeashEnforcer.PickReturnPoint(zone.Center, zone.Radius, _leashRandom);
                    _gameBridge.ClearPedTasks(pedHandle);
                    _gameBridge.TaskGoToCoord(pedHandle, returnPoint);
                    FileLogger.AI($"FriendlyDefenderManager: leashed ped {pedHandle} in zone {zoneId} from ({pedPos.X:F1},{pedPos.Y:F1}) back to ({returnPoint.X:F1},{returnPoint.Y:F1})");
                }
            }
        }

        /// <summary>
        /// Cleans up corpses that have exceeded the corpse delay time.
        /// </summary>
        private void CleanupExpiredCorpses(int currentGameTime)
        {
            var expiredCorpses = new List<int>();

            foreach (var kvp in _corpseDeathTimes)
            {
                var pedHandle = kvp.Key;
                var deathTime = kvp.Value;

                if (currentGameTime - deathTime >= CorpseDelayMs)
                {
                    expiredCorpses.Add(pedHandle);
                }
            }

            foreach (var pedHandle in expiredCorpses)
            {
                // Delete the visual entity (already untracked from pool on death)
                _pedDespawnService.DeletePedEntity(pedHandle);
                _corpseDeathTimes.Remove(pedHandle);
            }
        }

        /// <summary>
        /// Handles the death of a defender, including blip removal, allocation decrement,
        /// replacement spawning, and territory loss detection.
        /// Corpse cleanup is delayed for immersion.
        /// </summary>
        private void HandleDefenderDeath(string zoneId, int pedHandle, DefenderTier tier)
        {
            // Track death time for corpse cleanup (don't despawn yet - leave corpse visible)
            _corpseDeathTimes[pedHandle] = _gameBridge.GetGameTime();

            // Remove from active tracking (no longer counts toward spawned defenders)
            if (_spawnedPedTierByZone.TryGetValue(zoneId, out var pedTiers))
            {
                pedTiers.Remove(pedHandle);
            }

            // Remove blip immediately (dead peds shouldn't show on radar)
            _pedBlipService.RemoveBlipForPed(pedHandle);

            // IMPORTANT: Untrack from ped pool to free spawn slot (but keep corpse visible)
            _pedDespawnService.UntrackPed(pedHandle);

            var allocation = _allocationService.GetAllocation(_playerFactionId, zoneId);

            // ALWAYS decrement allocation when a defender dies (regardless of who killed them)
            // This must happen BEFORE checking for reserves, otherwise we create "phantom reserves"
            // where the spawned count decreases but allocation doesn't, making it look like reserves exist
            if (allocation != null)
            {
                allocation.RemoveTroops(tier, 1);
            }

            // Raise defender died event
            DefenderDied?.Invoke(this, new DefenderDiedEventArgs(zoneId, pedHandle, tier));

            // Try to spawn replacement from actual reserve (allocated > spawned means real reserves exist)
            TrySpawnReplacementFromReserve(zoneId, tier, allocation);

            // Check for territory loss (no spawned defenders AND no reserve)
            if (IsAllDefendersDead(zoneId, allocation))
            {
                HandleTerritoryLost(zoneId);
            }
        }

        /// <summary>
        /// Checks if all defenders are dead (no spawned and no reserve).
        /// </summary>
        private bool IsAllDefendersDead(string zoneId, ZoneDefenderAllocation? allocation)
        {
            var spawnedCount = GetSpawnedDefenderCount(zoneId);
            if (spawnedCount > 0) return false;

            if (allocation == null) return true;

            return allocation.TotalTroops == 0;
        }

        /// <summary>
        /// Handles territory loss when all defenders die.
        /// </summary>
        private void HandleTerritoryLost(string zoneId)
        {
            // Transfer zone to neutral
            _zoneService.TransferZoneOwnership(zoneId, null);

            // Raise event
            TerritoryLost?.Invoke(this, new TerritoryLostEventArgs(zoneId));
        }

        /// <summary>
        /// Tries to spawn a replacement defender from the reserve for a specific tier.
        /// Returns true if a replacement was spawned.
        /// </summary>
    }
}
