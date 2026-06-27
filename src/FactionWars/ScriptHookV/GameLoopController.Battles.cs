using System;
using System.Collections.Generic;
using System.Linq;
using FactionWars.Core.Interfaces;
using FactionWars.Combat.Models;
using FactionWars.Core.Models;
using FactionWars.ScriptHookV.Logging;

namespace FactionWars.ScriptHookV
{
    public partial class GameLoopController
    {
        private void OnZoneBattleEnded(ZoneBattle battle, BattleOutcome outcome)
        {
            MarkZoneBattleResolved(battle.ZoneId);

            // Mirror of OnZoneBattleStarted's IsContested=true. The
            // TransferZoneOwnership path also clears this, but only fires on
            // capture/neutralize — so a defended outcome (no ownership change)
            // would otherwise leave the blip flashing forever.

            // Identify the surviving participant (if any). Source of truth for
            // outcome routing in 3-way battles — Attackers[0] may be the wiped one.
            var winner = battle.Participants.FirstOrDefault(p => p.AliveCount > 0); bool playerWon = winner?.IsPlayer == true;

            // Resolve names from the participant list rather than the legacy
            // AttackerFactionId getter, which throws when Attackers is empty
            // (e.g. the player retreated as the only attacker).
            string? someAttackerFactionId = battle.Participants.FirstOrDefault(p => p.Role == BattleRole.Attacker)?.FactionId;
            string defenderFactionId = battle.DefenderFactionId;

            // Casualty debits only apply to AI factions — player troop count is
            // callback-based (1 + alive followers) and not pool-managed. We can
            // only debit attacker losses if there's still an attacker we know
            // about; if all attackers retreated, their losses are unaccounted
            // (matches prior behaviour for the retreat path).
            ApplyZoneBattleCasualties(battle, someAttackerFactionId, defenderFactionId);

            if (outcome == BattleOutcome.AttackersWon && _zoneService != null && !playerWon)
            {
                // AI attacker wins — use the actual surviving participant's faction id
                // (in 3-way battles Attackers[0] may be the wiped AI, not the winner).
                string? winnerFactionId = winner?.FactionId ?? someAttackerFactionId;
                if (winnerFactionId != null)
                {
                    _zoneService.TransferZoneOwnership(battle.ZoneId, winnerFactionId);

                    int survivors = winner?.AliveCount ?? battle.TotalAttackerTroops;
                    int toAllocate = survivors > 0 ? Math.Max(1, Math.Min((survivors + 1) / 2, 5)) : 0;
                    if (toAllocate > 0)
                    {
                        _allocationService?.SetAllocation(winnerFactionId, battle.ZoneId, DefenderRole.Grunt, toAllocate);
                    }

                    CheckFactionEliminated(defenderFactionId);
                }
            }
            // Player wins: ZoneBattleManager.ApplyBattleOutcome already neutralized
            // the zone via TransferZoneOwnership(zoneId, null). Re-transferring here
            // would defeat Q5.A.

            GetBattleDisplayNames(battle, someAttackerFactionId, defenderFactionId, out var attackerName, out var defenderName, out var zoneName);

            if (playerWon)
            {
                _gameBridge.ShowNotification($"~g~[{attackerName}]~w~ liberated ~b~{zoneName}~w~ — now neutral");
                FileLogger.Combat($"OnZoneBattleEnded: player liberated {zoneName} (now neutral)");
            }
            else if (outcome == BattleOutcome.AttackersWon)
            {
                string? winnerFactionIdForName = winner?.FactionId ?? someAttackerFactionId;
                string winnerName = winnerFactionIdForName != null
                    ? (_factionService.GetFaction(winnerFactionIdForName)?.Name ?? winnerFactionIdForName)
                    : attackerName;
                _gameBridge.ShowNotification($"~g~[{winnerName}]~w~ captured ~b~{zoneName}~w~ from ~r~[{defenderName}]");
                FileLogger.Combat($"OnZoneBattleEnded: {winnerName} captured {zoneName} from {defenderName}");
            }
            else
            {
                _gameBridge.ShowNotification($"~g~[{defenderName}]~w~ defended ~b~{zoneName}~w~ against ~r~[{attackerName}]");
                FileLogger.Combat($"OnZoneBattleEnded: {defenderName} defended {zoneName} against {attackerName}");
            }
        }

        private void MarkZoneBattleResolved(string zoneId)
        {
            _commanderManager?.OnBattleEnded(zoneId);
            _zoneService?.SetZoneContested(zoneId, false);
        }

        private void GetBattleDisplayNames(
            ZoneBattle battle,
            string? someAttackerFactionId,
            string defenderFactionId,
            out string attackerName,
            out string defenderName,
            out string zoneName)
        {
            attackerName = someAttackerFactionId != null
                ? (_factionService.GetFaction(someAttackerFactionId)?.Name ?? someAttackerFactionId)
                : "Attacker";
            var defenderFaction = _factionService.GetFaction(defenderFactionId);
            defenderName = defenderFaction?.Name ?? defenderFactionId;
            var zone = _zoneService?.GetZone(battle.ZoneId);
            zoneName = zone?.Name ?? battle.ZoneId;
        }

        private void ApplyZoneBattleCasualties(ZoneBattle battle, string? someAttackerFactionId, string defenderFactionId)
        {
            int attackerCasualties = battle.InitialAttackerTroops - battle.TotalAttackerTroops;
            int defenderCasualties = battle.InitialDefenderTroops - battle.TotalDefenderTroops;
            if (attackerCasualties > 0 && someAttackerFactionId != null && someAttackerFactionId != CurrentPlayerFactionId)
                _factionService.LoseTroops(someAttackerFactionId, attackerCasualties);

            if (defenderCasualties > 0 && defenderFactionId != CurrentPlayerFactionId)
                _factionService.LoseTroops(defenderFactionId, defenderCasualties);
        }

        /// <summary>
        /// Checks if a faction has been eliminated (lost all territory) and shows a notification if so.
        /// </summary>
        private void CheckFactionEliminated(string factionId)
        {
            if (string.IsNullOrEmpty(factionId) || _zoneService == null)
                return;

            var zoneCount = _zoneService.GetZoneCount(factionId);
            if (zoneCount == 0)
            {
                var faction = _factionService.GetFaction(factionId);
                var factionName = faction?.Name ?? factionId;
                _gameBridge.ShowNotification($"~r~[{factionName}]~w~ has been eliminated!");
                FileLogger.Combat($"CheckFactionEliminated: {factionName} has been eliminated (0 zones remaining)");
            }
        }

        /// <summary>
        /// Handles battle started events. If player is in the zone being attacked
        /// and is the defender, spawns enemy attackers immediately.
        /// </summary>
        private void OnZoneBattleStarted(ZoneBattle battle)
        {
            // Mark the zone contested so the minimap blip flashes red/white.
            // CombatResultHandler clears this when the battle resolves.
            _zoneService?.SetZoneContested(battle.ZoneId, true);

            // Notify commander manager to switch to sprinting wander
            _commanderManager?.OnBattleStarted(battle.ZoneId);

            // Check if player is in this zone
            var currentZone = _territoryManager?.CurrentZone;
            if (currentZone == null || currentZone.Id != battle.ZoneId)
                return;

            // Check if player is the defender
            if (battle.DefenderFactionId != CurrentPlayerFactionId)
                return;

            FileLogger.Combat($"OnZoneBattleStarted: Player is in zone {battle.ZoneId} as defender, triggering attacker spawn");

            // Spawn attackers immediately since player is already in zone
            _battleAttackerManager?.OnPlayerZoneEntered(currentZone);
        }

        /// <summary>
        /// Configures spawned defender peds with weapons, stats, and combat behavior.
        /// Makes them hostile to the player and ready to fight.
        /// </summary>
        /// <param name="spawnedPeds">The list of spawned ped handles.</param>
        /// <param name="tier">The defender tier for stat configuration.</param>
        /// <param name="defenderFactionId">The faction ID of the defenders.</param>
    }
}
