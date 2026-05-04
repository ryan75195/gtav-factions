using System;
using System.Collections.Generic;
using FactionWars.AI.Interfaces;
using FactionWars.AI.Models;
using FactionWars.Combat.Models;
using FactionWars.Core.Models;
using FactionWars.Factions.Models;
using FactionWars.ScriptHookV.Logging;

namespace FactionWars.AI.Controllers
{
    public partial class AIController
    {
        private void ExecuteAttackDecision(string attackerFactionId, AIDecision decision)
        {
            if (decision.TargetZoneId == null)
            {
                FileLogger.AI($"      ExecuteAttack: No target zone specified");
                return;
            }

            // NOTE: Deployment cost removed - troops are free to deploy once recruited
            // Cash is only spent during recruitment, not during attack execution

            FileLogger.AI($"      ExecuteAttack: {attackerFactionId} attacking {decision.TargetZoneId} with {decision.TroopsToCommit} troops (deployment free)");

            // Raise attack started event
            OnAttackStarted?.Invoke(this, new AIAttackEventArgs(
                attackerFactionId,
                decision.TargetZoneId,
                decision.TroopsToCommit));

            // Always start the battle (creates ActiveBattle for timed simulation)
            // Even if player is in zone - the battle needs to exist for attacker spawning
            bool playerInZone = _playerZoneId == decision.TargetZoneId;
            FileLogger.AI($"      ExecuteAttack: Starting battle (player in zone: {playerInZone})...");
            SimulateBattle(attackerFactionId, decision, playerInZone);
        }

        private void ExecuteDefendDecision(string factionId, FactionState factionState, AIDecision decision)
        {
            if (decision.TargetZoneId == null)
            {
                FileLogger.AI($"      ExecuteDefend: No target zone specified");
                return;
            }

            // Desperation scaling based on zones owned
            float deployPercent = factionState.ZoneCount switch
            {
                1 => 0.80f,  // Last stand - deploy 80%
                2 => 0.50f,  // Significant threat - deploy 50%
                _ => 0.30f   // Conservative - deploy 30%
            };

            FileLogger.AI($"      ExecuteDefend: {factionId} reinforcing {decision.TargetZoneId} ({factionState.ZoneCount} zones, {deployPercent:P0} deploy)");

            int totalDeployed = 0;

            // Deploy all tiers proportionally
            foreach (var tier in new[] { DefenderTier.Basic, DefenderTier.Medium, DefenderTier.Heavy, DefenderTier.Elite })
            {
                int reserves = factionState.GetReserveTroops(tier);
                int toDeploy = (int)(reserves * deployPercent);

                if (toDeploy > 0)
                {
                    if (_allocationService.AllocateTroops(factionState, decision.TargetZoneId, tier, toDeploy))
                    {
                        totalDeployed += toDeploy;
                        FileLogger.AI($"      ExecuteDefend: Allocated {toDeploy} {tier} to {decision.TargetZoneId}");
                    }
                }
            }

            FileLogger.AI($"      ExecuteDefend: Total {totalDeployed} troops allocated to {decision.TargetZoneId}");
        }

        private void SimulateBattle(string attackerFactionId, AIDecision decision, bool playerInZone = false)
        {
            if (!TryGetAttackZone(attackerFactionId, decision, out var zone))
                return;

            var attackerFaction = _factionService.GetFaction(attackerFactionId);
            var attackerFactionName = attackerFaction?.Name ?? attackerFactionId;

            if (zone.OwnerFactionId == null)
            {
                CaptureNeutralZone(attackerFactionId, attackerFactionName, decision, zone);
                return;
            }

            var defenderFactionId = zone.OwnerFactionId;
            FileLogger.AI($"      SimulateBattle: {attackerFactionId} vs {defenderFactionId} for {zone.Name}");

            // Build troop dictionaries for timed battle
            var attackerTroopDict = new Dictionary<DefenderTier, int>
            {
                { DefenderTier.Basic, decision.TroopsToCommit },
                { DefenderTier.Medium, 0 },
                { DefenderTier.Heavy, 0 }
            };

            var defenderTroopDict = BuildDefenderTroopsDictionary(defenderFactionId, decision.TargetZoneId!);

            FileLogger.AI($"      SimulateBattle: AttackerTroops={decision.TroopsToCommit} Basic, DefenderTroops={defenderTroopDict[DefenderTier.Basic]}B/{defenderTroopDict[DefenderTier.Medium]}M/{defenderTroopDict[DefenderTier.Heavy]}H");

            // Start timed battle instead of instant simulation
            var battle = _zoneBattleManager.StartBattle(
                decision.TargetZoneId!,
                attackerFactionId,
                defenderFactionId,
                attackerTroopDict,
                defenderTroopDict);

            FileLogger.AI($"      SimulateBattle: Started timed battle {battle.Id} for {zone.Name}, TotalAttackers={battle.TotalAttackerTroops}, TotalDefenders={battle.TotalDefenderTroops}");

            // If player is already in the zone, mark battle as player-present to pause tick simulation
            if (playerInZone)
            {
                _zoneBattleManager.OnPlayerEnteredZone(zone);
                FileLogger.AI($"      SimulateBattle: Player is in zone, marked battle as player-present");
            }

            return; // Battle resolution handled by ZoneBattleManager
        }

        private bool TryGetAttackZone(
            string attackerFactionId,
            AIDecision decision,
            out Territory.Models.Zone zone)
        {
            zone = _zoneService.GetZone(decision.TargetZoneId!)!;
            if (zone == null)
            {
                FileLogger.AI($"      SimulateBattle: Zone not found: {decision.TargetZoneId}");
                return false;
            }

            if (zone.OwnerFactionId == attackerFactionId)
            {
                FileLogger.AI($"      SimulateBattle: Cannot attack own zone");
                return false;
            }

            if (_zoneBattleManager.GetBattleForZone(decision.TargetZoneId!) != null)
            {
                FileLogger.AI($"      SimulateBattle: Battle already in progress in {decision.TargetZoneId}, skipping");
                return false;
            }

            return true;
        }

        private void CaptureNeutralZone(
            string attackerFactionId,
            string attackerFactionName,
            AIDecision decision,
            Territory.Models.Zone zone)
        {
            FileLogger.AI($"      SimulateBattle: Capturing neutral zone {zone.Name}");
            _zoneService.TransferZoneOwnership(decision.TargetZoneId!, attackerFactionId);

            int defendersToAllocate = decision.TroopsToCommit > 0
                ? Math.Max(1, Math.Min((decision.TroopsToCommit + 1) / 2, 5))
                : 0;
            if (defendersToAllocate > 0)
            {
                _allocationService.SetAllocation(attackerFactionId, decision.TargetZoneId!, DefenderTier.Basic, defendersToAllocate);
                FileLogger.AI($"      SimulateBattle: Allocated {defendersToAllocate} defenders to {zone.Name}");
            }

            _gameBridge.ShowNotification($"~y~{attackerFactionName}~w~ captured ~b~{zone.Name}");
            OnBattleResolved?.Invoke(this, new AIBattleResultEventArgs(
                attackerFactionId, "neutral", decision.TargetZoneId!, true, 0, 0));
        }

        private TroopComposition BuildDefenderTroops(string defenderFactionId, string zoneId)
        {
            var allocation = _allocationService.GetAllocation(defenderFactionId, zoneId);
            if (allocation == null)
                return TroopComposition.Empty;

            return new TroopComposition(
                allocation.GetTroopCount(DefenderTier.Basic),
                allocation.GetTroopCount(DefenderTier.Medium),
                allocation.GetTroopCount(DefenderTier.Heavy));
        }

        private Dictionary<DefenderTier, int> BuildDefenderTroopsDictionary(string defenderFactionId, string zoneId)
        {
            var result = new Dictionary<DefenderTier, int>
            {
                { DefenderTier.Basic, 0 },
                { DefenderTier.Medium, 0 },
                { DefenderTier.Heavy, 0 }
            };

            var allocation = _allocationService.GetAllocation(defenderFactionId, zoneId);
            if (allocation != null)
            {
                result[DefenderTier.Basic] = allocation.GetTroopCount(DefenderTier.Basic);
                result[DefenderTier.Medium] = allocation.GetTroopCount(DefenderTier.Medium);
                result[DefenderTier.Heavy] = allocation.GetTroopCount(DefenderTier.Heavy);
            }

            return result;
        }

        private void ApplyBattleResult(BattleSimulationResult result, int attackingTroops)
        {
            int attackerCasualties = result.AttackerCasualties.TotalCount;
            if (attackerCasualties > 0)
            {
                _factionService.LoseTroops(result.AttackerFactionId, attackerCasualties);
            }

            int defenderCasualties = result.DefenderCasualties.TotalCount;
            if (defenderCasualties > 0)
            {
                _factionService.LoseTroops(result.DefenderFactionId, defenderCasualties);
            }

            if (result.AttackerWon)
            {
                _zoneService.TransferZoneOwnership(result.ZoneId, result.AttackerFactionId);

                // Allocate surviving troops as defenders
                // Always allocate at least 1 defender to prevent "owned with 0 defenders" state
                int survivors = attackingTroops - attackerCasualties;
                int defendersToAllocate = survivors > 0 ? Math.Max(1, Math.Min((survivors + 1) / 2, 5)) : 0;
                if (defendersToAllocate > 0)
                {
                    _allocationService.SetAllocation(result.AttackerFactionId, result.ZoneId, DefenderTier.Basic, defendersToAllocate);
                    FileLogger.AI($"      ApplyBattleResult: Allocated {defendersToAllocate} defenders to {result.ZoneId}");
                }
            }
        }
    }
}
