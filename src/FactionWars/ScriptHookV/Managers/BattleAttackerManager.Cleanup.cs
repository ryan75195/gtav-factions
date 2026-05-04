using System.Collections.Generic;
using FactionWars.Combat.Events;
using FactionWars.Core.Models;
using FactionWars.ScriptHookV.Logging;

namespace FactionWars.ScriptHookV.Managers
{
    public partial class BattleAttackerManager
    {
        private void HandleAttackerDeath(string zoneId, int pedHandle, DefenderTier tier)
        {
            FileLogger.Combat($"BattleAttackerManager: Attacker died in {zoneId}, tier={tier}");

            // Look up the battle once; used both for the telemetry event and later side-effects.
            var battle = _zoneBattleManager.GetBattleForZone(zoneId);

            // Raise telemetry event before any other side-effects so the killer is still resolvable.
            var attackerFactionId = battle?.AttackerFactionId;
            if (attackerFactionId != null)
            {
                int killerHandle = _gameBridge.GetPedKiller(pedHandle);
                AttackerKilled?.Invoke(this, new AttackerKilledEventArgs(
                    zoneId, attackerFactionId, tier, pedHandle, killerHandle));
                FileLogger.Combat($"AttackerKilled raised: ped {pedHandle} (tier={tier}) in zone {zoneId}, killed by ped {killerHandle}");
            }

            // Track death time for corpse cleanup (don't despawn yet - leave corpse visible)
            _corpseDeathTimes[pedHandle] = _gameBridge.GetGameTime();

            // Remove from active tracking (no longer counts toward spawned attackers)
            if (_spawnedPedTierByZone.TryGetValue(zoneId, out var pedTiers))
            {
                pedTiers.Remove(pedHandle);
            }

            // Remove blip immediately (dead peds shouldn't show on radar)
            _pedBlipService.RemoveBlipForPed(pedHandle);

            // IMPORTANT: Untrack from ped pool to free spawn slot (but keep corpse visible)
            _pedDespawnService.UntrackPed(pedHandle);

            // Report kill to active battle manager
            if (battle != null && battle.IsPlayerPresent)
            {
                _zoneBattleManager.ReportTroopKilled(zoneId, battle.AttackerFactionId, tier);

                // Mirror simulated-kill behavior in ZoneBattleManager.ProcessKill: real
                // attacker deaths must also debit the attacking faction's reserve so
                // attacks deplete forces (today's "free deployment" never debited).
                _factionService.GetFactionState(battle.AttackerFactionId)
                    ?.RemoveReserveTroops(tier, 1);
            }

            // Try to spawn replacement from remaining battle troops
            TrySpawnReplacement(zoneId, tier, battle);
        }

        /// <summary>
        /// Cleans up corpses that have exceeded the corpse delay time.
        /// </summary>
        private void CleanupExpiredCorpses(int currentGameTime)
        {
            var expiredCorpses = new List<int>();

            foreach (var kvp in _corpseDeathTimes)
            {
                if (currentGameTime - kvp.Value >= CorpseDelayMs)
                {
                    expiredCorpses.Add(kvp.Key);
                }
            }

            foreach (var pedHandle in expiredCorpses)
            {
                // Delete the visual entity (already untracked from pool on death)
                _pedDespawnService.DeletePedEntity(pedHandle);
                _corpseDeathTimes.Remove(pedHandle);
                FileLogger.Combat($"BattleAttackerManager: Despawned corpse {pedHandle} after {CorpseDelayMs}ms delay");
            }
        }

        /// <summary>
        /// Tries to spawn a replacement attacker from remaining battle troops.
        /// </summary>
    }
}
