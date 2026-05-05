using System;
using System.Collections.Generic;
using System.Linq;
using FactionWars.Combat.Interfaces;
using FactionWars.Combat.Models;
using FactionWars.Core.Interfaces;
using FactionWars.Core.Models;
using FactionWars.ScriptHookV.Logging;
using FactionWars.Territory.Models;

namespace FactionWars.Combat.Services
{
    public partial class ZoneBattleManager
    {
        private void ProcessKill(ZoneBattle battle)
        {
            // Calculate weighted strength for each side
            float attackerStrength = CalculateStrength(battle.AttackerTroops);
            float defenderStrength = CalculateStrength(battle.DefenderTroops) * DefenderAdvantage;

            float totalStrength = attackerStrength + defenderStrength;
            if (totalStrength <= 0) return;

            // Determine which side gets the kill
            float attackerChance = attackerStrength / totalStrength;
            bool attackerGetsKill = _random.NextDouble() < attackerChance;

            DefenderTier victimTier;
            string victimSide;

            if (attackerGetsKill)
            {
                // Attacker kills a defender
                victimTier = SelectVictimTier(battle.DefenderTroops);
                battle.RemoveDefenderTroop(victimTier);
                // Reconcile the simulated kill back to the defender's allocation so the
                // next player visit doesn't see a phantom troop that was already lost.
                _allocationService.GetAllocation(battle.DefenderFactionId, battle.ZoneId)
                    ?.RemoveTroops(victimTier, 1);
                victimSide = "defender";
            }
            else
            {
                // Defender kills an attacker
                victimTier = SelectVictimTier(battle.AttackerTroops);
                battle.RemoveAttackerTroop(victimTier);
                // Decrement the attacking faction's reserve so combat losses actually
                // deplete the attacker's forces (today's "free deployment" never debited).
                _factionService.GetFactionState(battle.AttackerFactionId)
                    ?.RemoveReserveTroops(victimTier, 1);
                victimSide = "attacker";
            }

            TroopKilled?.Invoke(battle, victimTier, victimSide);
        }

        /// <summary>
        /// Counts alive participants and ends the battle if exactly one remains
        /// (defender or sole-attacker survivor). Caller already handled removal/decrement.
        /// </summary>
        private void ResolveBattleIfDone(ZoneBattle battle)
        {
            if (TryCollapseDefeatedDefender(battle))
                return;

            var alive = battle.Participants.Where(p => p.AliveCount > 0).ToList();
            if (alive.Count >= 2) return;

            BattleOutcome outcome;
            if (alive.Count == 0)
            {
                outcome = BattleOutcome.DefendersWon;
            }
            else
            {
                outcome = alive[0].Role == BattleRole.Defender
                    ? BattleOutcome.DefendersWon
                    : BattleOutcome.AttackersWon;
            }

            _battlesByZone.Remove(battle.ZoneId);
            ApplyBattleOutcome(battle, outcome, alive);
            BattleEnded?.Invoke(battle, outcome);
            FileLogger.Combat($"ResolveBattleIfDone: battle '{battle.ZoneId}' ended, outcome={outcome}.");
        }

        private bool TryCollapseDefeatedDefender(ZoneBattle battle)
        {
            bool defenderAlive = battle.Participants.Any(p =>
                p.Role == BattleRole.Defender && p.AliveCount > 0);
            if (defenderAlive)
                return false;

            var aliveAttackers = battle.Participants
                .Where(p => p.Role == BattleRole.Attacker && p.AliveCount > 0)
                .ToList();
            if (aliveAttackers.Count <= 1)
                return false;

            var newDefender = aliveAttackers[0];
            if (!battle.PromoteAttackerToDefender(newDefender.FactionId))
                return false;

            _zoneService.TransferZoneOwnership(battle.ZoneId, newDefender.FactionId);
            FileLogger.Combat(
                $"TryCollapseDefeatedDefender: '{newDefender.FactionId}' claimed '{battle.ZoneId}'.");
            return true;
        }

        /// <summary>
        /// Applies the side-effects of a battle outcome.
        /// Player win → zone goes neutral (Q5.A). AI-side outcomes are handled by the
        /// existing BattleEnded subscribers (no-op here).
        /// </summary>
        private void ApplyBattleOutcome(
            ZoneBattle battle,
            BattleOutcome outcome,
            IList<BattleParticipant> aliveParticipants)
        {
            BattleParticipant? winner = aliveParticipants.Count == 1 ? aliveParticipants[0] : null;

            if (outcome == BattleOutcome.AttackersWon && winner != null && winner.IsPlayer)
            {
                // Q5.A: player win → zone goes neutral. Two-step capture is preserved by
                // leaving downstream "claim zone" gameplay untouched (player must re-enter
                // the now-neutral zone to claim it).
                _zoneService.TransferZoneOwnership(battle.ZoneId, null);
                FileLogger.Combat($"ApplyBattleOutcome: player won zone '{battle.ZoneId}' — set to neutral.");
            }
        }

        private BattleOutcome DetermineOutcome(ZoneBattle battle)
        {
            var alive = battle.Participants.Where(p => p.AliveCount > 0).ToList();
            if (alive.Count == 1)
                return alive[0].Role == BattleRole.Defender
                    ? BattleOutcome.DefendersWon
                    : BattleOutcome.AttackersWon;
            if (alive.Count == 0)
                return BattleOutcome.DefendersWon;
            return BattleOutcome.Draw;
        }

        private float CalculateStrength(Dictionary<DefenderTier, int> troops)
        {
            float strength = 0;
            if (troops.TryGetValue(DefenderTier.Basic, out int basic))
                strength += basic * BasicStrength;
            if (troops.TryGetValue(DefenderTier.Medium, out int medium))
                strength += medium * MediumStrength;
            if (troops.TryGetValue(DefenderTier.Heavy, out int heavy))
                strength += heavy * HeavyStrength;
            return strength;
        }

        private DefenderTier SelectVictimTier(Dictionary<DefenderTier, int> troops)
        {
            // Weighted selection - Basic troops more likely to die
            var weighted = new List<(DefenderTier tier, int weight)>();

            if (troops.TryGetValue(DefenderTier.Basic, out int basic) && basic > 0)
                weighted.Add((DefenderTier.Basic, basic * BasicDeathWeight));
            if (troops.TryGetValue(DefenderTier.Medium, out int medium) && medium > 0)
                weighted.Add((DefenderTier.Medium, medium * MediumDeathWeight));
            if (troops.TryGetValue(DefenderTier.Heavy, out int heavy) && heavy > 0)
                weighted.Add((DefenderTier.Heavy, heavy * HeavyDeathWeight));

            if (weighted.Count == 0) return DefenderTier.Basic;

            int totalWeight = weighted.Sum(w => w.weight);
            int roll = _random.Next(totalWeight);
            int cumulative = 0;

            foreach (var (tier, weight) in weighted)
            {
                cumulative += weight;
                if (roll < cumulative) return tier;
            }

            return weighted[0].tier;
        }
    }
}
