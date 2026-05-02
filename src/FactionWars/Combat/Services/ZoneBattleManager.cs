using System;
using System.Collections.Generic;
using System.Linq;
using FactionWars.Combat.Interfaces;
using FactionWars.Combat.Models;
using FactionWars.Core.Interfaces;
using FactionWars.Core.Models;
using FactionWars.Factions.Interfaces;
using FactionWars.Territory.Models;

namespace FactionWars.Combat.Services
{
    /// <summary>
    /// Unified manager for all battle lifecycle operations.
    /// This is the single source of truth for battle state in all zones.
    /// </summary>
    public class ZoneBattleManager : IZoneBattleManager
    {
        private readonly Dictionary<string, ZoneBattle> _battlesByZone;
        private readonly Random _random;
        private readonly IZoneDefenderAllocationService _allocationService;
        private readonly IFactionService _factionService;
        private string? _playerFactionId;

        // Configuration constants (matching ActiveBattleManager)
        private const float MinBattleDuration = 60f;    // 1 minute minimum
        private const float MaxBattleDuration = 300f;   // 5 minutes maximum
        private const float SecondsPerTroop = 6f;       // Duration scaling factor
        private const float DefenderAdvantage = 1.5f;   // Defender strength multiplier

        // Tier strength modifiers
        private const float BasicStrength = 1.0f;
        private const float MediumStrength = 1.5f;
        private const float HeavyStrength = 2.0f;

        // Tier death weights (higher = more likely to die)
        private const int BasicDeathWeight = 3;
        private const int MediumDeathWeight = 2;
        private const int HeavyDeathWeight = 1;

        /// <inheritdoc />
        public int BattleCount => _battlesByZone.Count;

        /// <inheritdoc />
        public event Action<ZoneBattle>? BattleStarted;

        /// <inheritdoc />
        public event Action<ZoneBattle, BattleOutcome>? BattleEnded;

        /// <inheritdoc />
        public event Action<ZoneBattle, DefenderTier, string>? TroopKilled;

        /// <summary>
        /// Creates a new ZoneBattleManager.
        /// </summary>
        /// <param name="allocationService">Source of truth for per-zone defender allocations. Simulated defender losses decrement the matching allocation so the next player visit doesn't see phantom troops.</param>
        /// <param name="factionService">Source of truth for faction reserves. Simulated attacker losses decrement the attacking faction's reserve so attacks actually deplete forces.</param>
        /// <param name="playerFactionId">The player's faction ID, if known.</param>
        public ZoneBattleManager(
            IZoneDefenderAllocationService allocationService,
            IFactionService factionService,
            string? playerFactionId = null)
        {
            _allocationService = allocationService ?? throw new ArgumentNullException(nameof(allocationService));
            _factionService = factionService ?? throw new ArgumentNullException(nameof(factionService));
            _battlesByZone = new Dictionary<string, ZoneBattle>();
            _random = new Random();
            _playerFactionId = playerFactionId;
        }

        /// <inheritdoc />
        public ZoneBattle StartBattle(
            string zoneId,
            string attackerFactionId,
            string defenderFactionId,
            Dictionary<DefenderTier, int> attackerTroops,
            Dictionary<DefenderTier, int> defenderTroops)
        {
            if (string.IsNullOrEmpty(zoneId))
                throw new ArgumentNullException(nameof(zoneId));
            if (string.IsNullOrEmpty(attackerFactionId))
                throw new ArgumentNullException(nameof(attackerFactionId));
            if (string.IsNullOrEmpty(defenderFactionId))
                throw new ArgumentNullException(nameof(defenderFactionId));
            if (attackerTroops == null)
                throw new ArgumentNullException(nameof(attackerTroops));
            if (defenderTroops == null)
                throw new ArgumentNullException(nameof(defenderTroops));

            if (_battlesByZone.ContainsKey(zoneId))
            {
                throw new InvalidOperationException($"A battle already exists in zone '{zoneId}'.");
            }

            var battle = new ZoneBattle(
                attackerFactionId: attackerFactionId,
                defenderFactionId: defenderFactionId,
                zoneId: zoneId,
                attackerTroops: attackerTroops,
                defenderTroops: defenderTroops,
                playerFactionId: _playerFactionId);

            // Calculate kill interval based on total troops
            int totalTroops = battle.TotalAttackerTroops + battle.TotalDefenderTroops;
            float duration = Math.Max(MinBattleDuration, Math.Min(MaxBattleDuration, totalTroops * SecondsPerTroop));
            float killInterval = duration / Math.Max(1, totalTroops - 1);
            battle.SetKillInterval(killInterval);

            _battlesByZone[zoneId] = battle;

            BattleStarted?.Invoke(battle);

            return battle;
        }

        /// <inheritdoc />
        public void EndBattle(string zoneId, BattleOutcome outcome)
        {
            if (string.IsNullOrEmpty(zoneId))
                return;

            if (_battlesByZone.TryGetValue(zoneId, out var battle))
            {
                _battlesByZone.Remove(zoneId);
                BattleEnded?.Invoke(battle, outcome);
            }
        }

        /// <inheritdoc />
        public void OnPlayerEnteredZone(Zone zone)
        {
            if (zone == null)
                return;

            if (_battlesByZone.TryGetValue(zone.Id, out var battle))
            {
                battle.IsPlayerPresent = true;
            }
        }

        /// <inheritdoc />
        public void OnPlayerExitedZone(Zone zone)
        {
            if (zone == null)
                return;

            if (_battlesByZone.TryGetValue(zone.Id, out var battle))
            {
                battle.IsPlayerPresent = false;
            }
        }

        /// <inheritdoc />
        public void SetPlayerFaction(string? factionId)
        {
            _playerFactionId = factionId;
        }

        /// <inheritdoc />
        public void Tick(float deltaTime)
        {
            var battlesToRemove = new List<string>();

            foreach (var kvp in _battlesByZone)
            {
                var battle = kvp.Value;

                // Only process tick-based combat if player is not present
                if (battle.IsPlayerPresent)
                {
                    // Still check for battle end
                    if (!battle.IsOngoing)
                    {
                        var outcome = DetermineOutcome(battle);
                        battlesToRemove.Add(kvp.Key);
                        BattleEnded?.Invoke(battle, outcome);
                    }
                    continue;
                }

                // Advance time
                battle.AdvanceTime(deltaTime);

                // Check if it's time for a kill
                if (battle.TimeUntilNextKill <= 0)
                {
                    ProcessKill(battle);
                    battle.ResetKillTimer();

                    // Check if battle ended
                    if (!battle.IsOngoing)
                    {
                        var outcome = DetermineOutcome(battle);
                        battlesToRemove.Add(kvp.Key);
                        BattleEnded?.Invoke(battle, outcome);
                    }
                }
            }

            // Remove completed battles
            foreach (var zoneId in battlesToRemove)
            {
                _battlesByZone.Remove(zoneId);
            }
        }

        /// <inheritdoc />
        public void ReportTroopKilled(string zoneId, string factionId, DefenderTier tier)
        {
            if (string.IsNullOrEmpty(zoneId) || string.IsNullOrEmpty(factionId))
                return;

            if (!_battlesByZone.TryGetValue(zoneId, out var battle))
                return;

            string side;
            bool removed = false;

            if (factionId == battle.AttackerFactionId)
            {
                removed = battle.RemoveAttackerTroop(tier);
                side = "attacker";
            }
            else if (factionId == battle.DefenderFactionId)
            {
                removed = battle.RemoveDefenderTroop(tier);
                side = "defender";
            }
            else
            {
                // Faction not involved in this battle
                return;
            }

            if (removed)
            {
                TroopKilled?.Invoke(battle, tier, side);

                // Check if battle ended
                if (!battle.IsOngoing)
                {
                    var outcome = DetermineOutcome(battle);
                    _battlesByZone.Remove(zoneId);
                    BattleEnded?.Invoke(battle, outcome);
                }
            }
        }

        /// <inheritdoc />
        public ZoneBattle? GetBattleForZone(string zoneId)
        {
            if (string.IsNullOrEmpty(zoneId))
                return null;

            _battlesByZone.TryGetValue(zoneId, out var battle);
            return battle;
        }

        /// <inheritdoc />
        public IReadOnlyList<ZoneBattle> GetAllActiveBattles()
        {
            return new List<ZoneBattle>(_battlesByZone.Values).AsReadOnly();
        }

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

        private BattleOutcome DetermineOutcome(ZoneBattle battle)
        {
            if (battle.AttackersWon)
                return BattleOutcome.AttackersWon;
            if (battle.DefendersWon)
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
