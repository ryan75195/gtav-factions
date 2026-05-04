using System.Collections.Generic;
using FactionWars.Core.Models;
using FactionWars.ScriptHookV.Logging;

namespace FactionWars.ScriptHookV.Managers
{
    public partial class BattleAttackerManager
    {
        public void Update()
        {
            if (_currentBattleZoneId == null) return;

            var currentGameTime = _gameBridge.GetGameTime();
            var deadPeds = new List<(string zoneId, int pedHandle, DefenderTier tier)>();
            var streamedOutPeds = new List<(string zoneId, int pedHandle)>();

            // Check all spawned attackers for death
            foreach (var kvp in _spawnedPedTierByZone)
            {
                var zoneId = kvp.Key;
                var pedTiers = kvp.Value;

                foreach (var pedKvp in pedTiers)
                {
                    var pedHandle = pedKvp.Key;
                    var tier = pedKvp.Value;

                    // Skip if already tracked as corpse (death already processed)
                    if (_corpseDeathTimes.ContainsKey(pedHandle))
                        continue;

                    // Streamed-out (entity gone) is not a kill — don't report to battle.
                    if (!_gameBridge.DoesPedExist(pedHandle))
                    {
                        streamedOutPeds.Add((zoneId, pedHandle));
                    }
                    else if (!_gameBridge.IsPedAlive(pedHandle))
                    {
                        deadPeds.Add((zoneId, pedHandle, tier));
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

            // Process each dead attacker
            foreach (var (zoneId, pedHandle, tier) in deadPeds)
            {
                HandleAttackerDeath(zoneId, pedHandle, tier);
            }

            // Cleanup corpses that have exceeded the delay
            CleanupExpiredCorpses(currentGameTime);
        }

        /// <summary>
        /// Handles the death of an enemy attacker.
        /// </summary>
    }
}
