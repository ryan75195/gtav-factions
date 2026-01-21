using System;
using System.Collections.Generic;
using System.Linq;
using FactionWars.Combat.Interfaces;
using FactionWars.Combat.Models;
using FactionWars.Core.Models;
using FactionWars.Factions.Interfaces;
using FactionWars.Territory.Interfaces;
using FactionWars.Core.Interfaces;
using FactionWars.ScriptHookV.Logging;

namespace FactionWars.Combat.Services
{
    /// <summary>
    /// Manages active timed battles between AI factions.
    /// Handles tick-based combat simulation, kill events, and battle resolution.
    /// </summary>
    public class ActiveBattleManager : IActiveBattleManager
    {
        private readonly IFactionService _factionService;
        private readonly IZoneService _zoneService;
        private readonly IZoneDefenderAllocationService _allocationService;
        private readonly List<ActiveBattle> _activeBattles;
        private readonly Random _random;

        // Configuration
        private const float MinBattleDuration = 60f;    // 1 minute minimum
        private const float MaxBattleDuration = 300f;   // 5 minutes maximum
        private const float SecondsPerTroop = 6f;       // Duration scaling factor
        private const float DefenderAdvantage = 1.5f;   // Defender strength multiplier

        // Tier strength modifiers (for combat calculations)
        private const float BasicStrength = 1.0f;
        private const float MediumStrength = 1.5f;
        private const float HeavyStrength = 2.0f;

        // Tier death weights (inverse - higher = more likely to die)
        private const int BasicDeathWeight = 3;
        private const int MediumDeathWeight = 2;
        private const int HeavyDeathWeight = 1;

        public IReadOnlyList<ActiveBattle> ActiveBattles => _activeBattles.AsReadOnly();
        public int BattleCount => _activeBattles.Count;

        public event EventHandler<BattleKillEvent>? OnKill;
        public event EventHandler<BattleEndedEvent>? OnBattleEnded;

        public ActiveBattleManager(
            IFactionService factionService,
            IZoneService zoneService,
            IZoneDefenderAllocationService allocationService)
        {
            _factionService = factionService ?? throw new ArgumentNullException(nameof(factionService));
            _zoneService = zoneService ?? throw new ArgumentNullException(nameof(zoneService));
            _allocationService = allocationService ?? throw new ArgumentNullException(nameof(allocationService));
            _activeBattles = new List<ActiveBattle>();
            _random = new Random();
        }

        public ActiveBattle StartBattle(
            string attackerFactionId,
            string defenderFactionId,
            string zoneId,
            Dictionary<DefenderTier, int> attackerTroops,
            Dictionary<DefenderTier, int> defenderTroops)
        {
            // Check if battle already exists for this zone
            var existing = GetBattleForZone(zoneId);
            if (existing != null)
            {
                FileLogger.Combat($"ActiveBattleManager: Battle already exists for zone {zoneId}");
                return existing;
            }

            // Calculate duration based on total troops
            int totalTroops = attackerTroops.Values.Sum() + defenderTroops.Values.Sum();
            float duration = Math.Max(MinBattleDuration, Math.Min(MaxBattleDuration, totalTroops * SecondsPerTroop));
            float killInterval = duration / Math.Max(1, totalTroops - 1);

            var battle = new ActiveBattle(
                attackerFactionId,
                defenderFactionId,
                zoneId,
                attackerTroops,
                defenderTroops,
                duration,
                killInterval);

            _activeBattles.Add(battle);

            FileLogger.Combat($"ActiveBattleManager: Started battle {battle.Id} in {zoneId} - {attackerFactionId} vs {defenderFactionId}, duration={duration:F0}s, interval={killInterval:F1}s");

            return battle;
        }

        public ActiveBattle? GetBattleForZone(string zoneId)
        {
            return _activeBattles.FirstOrDefault(b => b.ZoneId == zoneId);
        }

        public ActiveBattle? GetBattle(string battleId)
        {
            return _activeBattles.FirstOrDefault(b => b.Id == battleId);
        }

        public void Tick(float deltaTimeSeconds)
        {
            var battlesToRemove = new List<ActiveBattle>();

            foreach (var battle in _activeBattles)
            {
                // Skip tick-based combat if player is present (physical combat takes over)
                if (battle.IsPlayerPresent)
                {
                    // Still check for battle end
                    if (!battle.IsOngoing)
                    {
                        EndBattle(battle);
                        battlesToRemove.Add(battle);
                    }
                    continue;
                }

                // Advance time
                battle.AdvanceTime(deltaTimeSeconds);

                // Check if it's time for a kill
                if (battle.TimeUntilNextKill <= 0)
                {
                    ProcessKill(battle);
                    battle.ResetKillTimer();

                    // Check if battle ended
                    if (!battle.IsOngoing)
                    {
                        EndBattle(battle);
                        battlesToRemove.Add(battle);
                    }
                }
            }

            // Remove completed battles
            foreach (var battle in battlesToRemove)
            {
                _activeBattles.Remove(battle);
            }
        }

        public void OnPlayerEnterZone(string zoneId)
        {
            var battle = GetBattleForZone(zoneId);
            if (battle != null)
            {
                battle.IsPlayerPresent = true;
                FileLogger.Combat($"ActiveBattleManager: Player entered battle zone {zoneId}, pausing tick simulation");
            }
        }

        public void OnPlayerExitZone(string zoneId)
        {
            var battle = GetBattleForZone(zoneId);
            if (battle != null)
            {
                battle.IsPlayerPresent = false;
                FileLogger.Combat($"ActiveBattleManager: Player exited battle zone {zoneId}, resuming tick simulation");
            }
        }

        public void ReportTroopKilled(string zoneId, string factionId, DefenderTier tier)
        {
            var battle = GetBattleForZone(zoneId);
            if (battle == null) return;

            bool removed = false;
            string killerFactionId;
            DefenderTier killerTier;

            if (factionId == battle.AttackerFactionId)
            {
                removed = battle.RemoveAttackerTroop(tier);
                killerFactionId = battle.DefenderFactionId;
                killerTier = SelectRandomTier(battle.DefenderTroops);
            }
            else if (factionId == battle.DefenderFactionId)
            {
                removed = battle.RemoveDefenderTroop(tier);
                killerFactionId = battle.AttackerFactionId;
                killerTier = SelectRandomTier(battle.AttackerTroops);
            }
            else
            {
                return;
            }

            if (removed)
            {
                var zone = _zoneService.GetZone(zoneId);
                var killEvent = new BattleKillEvent(
                    battle.Id,
                    killerFactionId,
                    killerTier,
                    factionId,
                    tier,
                    zoneId,
                    zone?.Name ?? zoneId);

                OnKill?.Invoke(this, killEvent);
                FileLogger.Combat($"ActiveBattleManager: Troop killed - {killerFactionId} {killerTier} killed {factionId} {tier}");

                // Check if battle ended
                if (!battle.IsOngoing)
                {
                    EndBattle(battle);
                    _activeBattles.Remove(battle);
                }
            }
        }

        public bool AddDefenderTroops(string zoneId, DefenderTier tier, int count)
        {
            var battle = GetBattleForZone(zoneId);
            if (battle == null) return false;

            battle.AddDefenderTroops(tier, count);
            FileLogger.Combat($"ActiveBattleManager: Added {count} {tier} defenders to battle in {zoneId}, new total: {battle.TotalDefenderTroops}");
            return true;
        }

        private void ProcessKill(ActiveBattle battle)
        {
            // Calculate weighted strength for each side
            float attackerStrength = CalculateStrength(battle.AttackerTroops);
            float defenderStrength = CalculateStrength(battle.DefenderTroops) * DefenderAdvantage;

            float totalStrength = attackerStrength + defenderStrength;
            if (totalStrength <= 0) return;

            // Determine which side gets the kill
            float attackerChance = attackerStrength / totalStrength;
            bool attackerGetsKill = _random.NextDouble() < attackerChance;

            string killerFactionId;
            string victimFactionId;
            DefenderTier killerTier;
            DefenderTier victimTier;

            if (attackerGetsKill)
            {
                killerFactionId = battle.AttackerFactionId;
                victimFactionId = battle.DefenderFactionId;
                killerTier = SelectRandomTier(battle.AttackerTroops);
                victimTier = SelectVictimTier(battle.DefenderTroops);
                battle.RemoveDefenderTroop(victimTier);
            }
            else
            {
                killerFactionId = battle.DefenderFactionId;
                victimFactionId = battle.AttackerFactionId;
                killerTier = SelectRandomTier(battle.DefenderTroops);
                victimTier = SelectVictimTier(battle.AttackerTroops);
                battle.RemoveAttackerTroop(victimTier);
            }

            // Raise kill event
            var zone = _zoneService.GetZone(battle.ZoneId);
            var killEvent = new BattleKillEvent(
                battle.Id,
                killerFactionId,
                killerTier,
                victimFactionId,
                victimTier,
                battle.ZoneId,
                zone?.Name ?? battle.ZoneId);

            OnKill?.Invoke(this, killEvent);
            FileLogger.Combat($"ActiveBattleManager: Kill in {battle.ZoneId} - [{killerFactionId}] {killerTier} killed [{victimFactionId}] {victimTier}");
        }

        private void EndBattle(ActiveBattle battle)
        {
            var zone = _zoneService.GetZone(battle.ZoneId);
            bool attackerWon = battle.AttackersWon;

            // Calculate total casualties (initial - remaining)
            int attackerCasualties = battle.InitialAttackerTroops - battle.TotalAttackerTroops;
            int defenderCasualties = battle.InitialDefenderTroops - battle.TotalDefenderTroops;

            // Apply remaining casualties to faction troop counts
            if (attackerCasualties > 0)
            {
                _factionService.LoseTroops(battle.AttackerFactionId, attackerCasualties);
            }
            if (defenderCasualties > 0)
            {
                _factionService.LoseTroops(battle.DefenderFactionId, defenderCasualties);
            }

            // Transfer zone if attacker won
            if (attackerWon)
            {
                _zoneService.TransferZoneOwnership(battle.ZoneId, battle.AttackerFactionId);

                // Allocate surviving attackers as defenders
                int survivors = battle.TotalAttackerTroops;
                int toAllocate = Math.Min(survivors / 2, 5);
                if (toAllocate > 0)
                {
                    _allocationService.SetAllocation(battle.AttackerFactionId, battle.ZoneId, DefenderTier.Basic, toAllocate);
                }
            }

            // Raise battle ended event
            var endEvent = new BattleEndedEvent(
                battle.Id,
                battle.AttackerFactionId,
                battle.DefenderFactionId,
                battle.ZoneId,
                zone?.Name ?? battle.ZoneId,
                attackerWon,
                attackerCasualties,
                defenderCasualties);

            OnBattleEnded?.Invoke(this, endEvent);
            FileLogger.Combat($"ActiveBattleManager: Battle {battle.Id} ended - {(attackerWon ? "Attacker" : "Defender")} victory in {zone?.Name ?? battle.ZoneId}");
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

        private DefenderTier SelectRandomTier(Dictionary<DefenderTier, int> troops)
        {
            // Select a random tier that has troops (for "killer" attribution)
            var available = troops.Where(kvp => kvp.Value > 0).Select(kvp => kvp.Key).ToList();
            if (available.Count == 0) return DefenderTier.Basic;
            return available[_random.Next(available.Count)];
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
