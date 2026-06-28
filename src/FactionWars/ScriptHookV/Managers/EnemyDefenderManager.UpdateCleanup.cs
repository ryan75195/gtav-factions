using System.Collections.Generic;
using FactionWars.Combat.Models;
using FactionWars.Core.Models;
using FactionWars.ScriptHookV.Logging;
using FactionWars.ScriptHookV.Services;

namespace FactionWars.ScriptHookV.Managers
{
    public partial class EnemyDefenderManager
    {
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
                    FileLogger.AI($"EnemyDefenderManager: leashed ped {pedHandle} in zone {zoneId} from ({pedPos.X:F1},{pedPos.Y:F1}) back to ({returnPoint.X:F1},{returnPoint.Y:F1})");
                }
            }
        }

        /// <summary>
        /// Handles the death of an enemy defender.
        /// Tracks corpse for delayed cleanup, removes blip, decrements allocation, spawns replacement.
        /// </summary>
        private void HandleDefenderDeath(string zoneId, int pedHandle, DefenderRole tier, string enemyFactionId)
        {
            FileLogger.Combat($"EnemyDefenderManager: Defender died in {zoneId}, tier={tier}");

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

            // Get allocation and ALWAYS decrement when a defender dies
            var allocation = _allocationService.GetAllocation(enemyFactionId, zoneId);
            if (allocation != null)
            {
                allocation.RemoveTroops(tier, 1);
                FileLogger.Combat($"EnemyDefenderManager: Decremented {tier} allocation in {zoneId}, remaining: {allocation.TotalTroops}");
            }

            // Report kill to ZoneBattleManager so the active battle's troop count stays in sync
            // and victory conditions are checked correctly.
            _zoneBattleManager?.ReportTroopKilled(zoneId, enemyFactionId, tier);

            // Try to spawn replacement from remaining reserves
            TrySpawnReplacement(zoneId, tier, enemyFactionId, allocation);
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
                FileLogger.Combat($"EnemyDefenderManager: Cleaning up corpse {pedHandle} after {CorpseDelayMs}ms delay");
                // Delete the visual entity (already untracked from pool on death)
                _pedDespawnService.DeletePedEntity(pedHandle);
                _corpseDeathTimes.Remove(pedHandle);
            }
        }

        /// <summary>
        /// Tries to spawn a replacement defender from reserves.
        /// </summary>
    }
}
