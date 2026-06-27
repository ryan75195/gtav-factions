using System.Collections.Generic;
using FactionWars.Combat.Models;
using FactionWars.Core.Models;

namespace FactionWars.ScriptHookV.Managers
{
    public partial class EnemyDefenderManager
    {
        public void Update(string? enemyFactionId)
        {
            if (_currentEnemyZoneId == null || string.IsNullOrEmpty(enemyFactionId)) return;
            var defenderFactionId = enemyFactionId!;

            var newlyDeadPeds = new List<(string zoneId, int pedHandle, DefenderRole tier)>();
            var streamedOutPeds = new List<(string zoneId, int pedHandle)>();
            var currentGameTime = _gameBridge.GetGameTime();

            // Check all spawned enemy defenders for death
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

                    // Streamed-out (entity gone) is not a kill — don't shed allocation.
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

            // Quietly untrack peds the engine culled.
            foreach (var (zoneId, pedHandle) in streamedOutPeds)
            {
                if (_spawnedPedTierByZone.TryGetValue(zoneId, out var pedTiers))
                {
                    pedTiers.Remove(pedHandle);
                }
                _pedBlipService.RemoveBlipForPed(pedHandle);
                _pedDespawnService.UntrackPed(pedHandle);
            }

            // Process each newly dead defender (track as corpse, decrement allocation, spawn replacement)
            foreach (var (zoneId, pedHandle, tier) in newlyDeadPeds)
            {
                HandleDefenderDeath(zoneId, pedHandle, tier, defenderFactionId);
            }

            // Clean up corpses that have exceeded the delay
            CleanupExpiredCorpses(currentGameTime);

            EnforceZoneLeash(currentGameTime);
        }

        /// <summary>
        /// Every <see cref="ZoneLeashEnforcer.LeashCheckIntervalMs"/>, scan all
        /// tracked enemy defenders. Any whose distance from their zone center
        /// exceeds the hysteresis threshold gets its tasks cleared and a
        /// TaskGoToCoord back to a random point inside the inner half of the
        /// zone.
        /// </summary>
    }
}
