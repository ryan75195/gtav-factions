using System;
using System.Collections.Generic;
using System.Linq;
using FactionWars.Combat.Interfaces;
using FactionWars.Combat.Models;
using FactionWars.Core.Interfaces;
using FactionWars.Core.Models;
using FactionWars.Factions.Interfaces;
using FactionWars.ScriptHookV.Logging;
using FactionWars.Territory.Interfaces;
using FactionWars.Territory.Models;

namespace FactionWars.Combat.Services
{
    /// <summary>
    /// Unified manager for all battle lifecycle operations.
    /// This is the single source of truth for battle state in all zones.
    /// </summary>
    public partial class ZoneBattleManager : IZoneBattleManager
    {
        private readonly Dictionary<string, ZoneBattle> _battlesByZone;
        private readonly Random _random;
        private readonly IZoneDefenderAllocationService _allocationService;
        private readonly IFactionService _factionService;
        private readonly IZoneService _zoneService;
        private string? _playerFactionId;

        // Battle pacing tuning constants
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
        public bool IsPlayerInBattle() => GetPlayerCurrentBattle() != null;

        /// <inheritdoc />
        public ZoneBattle? GetPlayerCurrentBattle()
        {
            if (_playerFactionId == null) return null;
            foreach (var battle in _battlesByZone.Values)
            {
                foreach (var participant in battle.Participants)
                {
                    if (participant.IsPlayer && participant.FactionId == _playerFactionId)
                        return battle;
                }
            }
            return null;
        }

        /// <inheritdoc />
        public bool JoinAsAttacker(
            string zoneId,
            string factionId,
            bool isPlayer,
            Func<int>? aliveCountCallback,
            Dictionary<DefenderTier, int>? troops)
        {
            if (string.IsNullOrEmpty(zoneId)) throw new ArgumentNullException(nameof(zoneId));
            if (string.IsNullOrEmpty(factionId)) throw new ArgumentNullException(nameof(factionId));

            // v1: only the player can be a third party (Q2.A).
            if (!isPlayer)
            {
                FileLogger.Combat($"JoinAsAttacker: rejected non-player faction '{factionId}' (v1 only allows player third parties).");
                return false;
            }

            if (!_battlesByZone.TryGetValue(zoneId, out var battle))
            {
                FileLogger.Combat($"JoinAsAttacker: rejected — no battle in zone '{zoneId}'.");
                return false;
            }

            int currentAttackers = battle.Participants.Count(p => p.Role == BattleRole.Attacker);
            if (currentAttackers >= 2)
            {
                FileLogger.Combat($"JoinAsAttacker: rejected — zone '{zoneId}' already has {currentAttackers} attackers.");
                return false;
            }

            if (battle.Participants.Any(p => p.FactionId == factionId))
            {
                FileLogger.Combat($"JoinAsAttacker: rejected — faction '{factionId}' already in battle '{zoneId}'.");
                return false;
            }

            var newParticipant = CreateAttackerParticipant(factionId, isPlayer, aliveCountCallback, troops);

            battle.AddParticipant(newParticipant);
            FileLogger.Combat($"JoinAsAttacker: added '{factionId}' (isPlayer={isPlayer}) to zone '{zoneId}'.");
            return true;
        }

        private static BattleParticipant CreateAttackerParticipant(
            string factionId,
            bool isPlayer,
            Func<int>? aliveCountCallback,
            Dictionary<DefenderTier, int>? troops)
        {
            if (isPlayer)
            {
                if (aliveCountCallback == null)
                    throw new ArgumentNullException(nameof(aliveCountCallback),
                        "aliveCountCallback is required when isPlayer is true.");
                return BattleParticipant.ForPlayer(factionId, BattleRole.Attacker, aliveCountCallback);
            }

            if (troops == null)
                throw new ArgumentNullException(nameof(troops),
                    "troops is required when isPlayer is false.");
            return BattleParticipant.ForAi(factionId, BattleRole.Attacker, troops);
        }

        /// <inheritdoc />
        public ZoneBattle? StartPlayerCombat(
            Zone zone,
            string playerFactionId,
            Func<int> aliveCountCallback)
        {
            if (zone == null) throw new ArgumentNullException(nameof(zone));
            if (string.IsNullOrEmpty(playerFactionId)) throw new ArgumentNullException(nameof(playerFactionId));
            if (aliveCountCallback == null) throw new ArgumentNullException(nameof(aliveCountCallback));
            if (string.IsNullOrEmpty(zone.OwnerFactionId))
                throw new ArgumentException("Zone must have an owner to start player combat.", nameof(zone));
            var defenderFactionId = zone.OwnerFactionId!;

            if (zone.OwnerFactionId == playerFactionId)
                throw new ArgumentException("Player cannot attack their own zone.", nameof(zone));

            FileLogger.Combat($"StartPlayerCombat: zone={zone.Id}, player={playerFactionId}, defender={defenderFactionId}");

            // Case 2: existing battle — join it.
            if (_battlesByZone.ContainsKey(zone.Id))
            {
                bool joined = JoinAsAttacker(zone.Id, playerFactionId, isPlayer: true,
                    aliveCountCallback: aliveCountCallback, troops: null);
                return joined ? _battlesByZone[zone.Id] : null;
            }

            // Case 1: new battle. Defender troops come from the deployed allocation.
            var allocation = _allocationService.GetAllocation(defenderFactionId, zone.Id);
            var defenderTroops = allocation != null
                ? allocation.GetTroopsCopy()
                : new Dictionary<DefenderTier, int>();

            var defender = BattleParticipant.ForAi(defenderFactionId, BattleRole.Defender, defenderTroops);
            var attacker = BattleParticipant.ForPlayer(playerFactionId, BattleRole.Attacker, aliveCountCallback);
            var battle = new ZoneBattle(zone.Id,
                new List<BattleParticipant> { defender, attacker },
                playerFactionId: playerFactionId);
            battle.IsPlayerPresent = true;
            _battlesByZone[zone.Id] = battle;
            BattleStarted?.Invoke(battle);
            FileLogger.Combat($"StartPlayerCombat: created new battle id={battle.Id} in zone {zone.Id}");
            return battle;
        }

        /// <inheritdoc />
        public bool RemoveParticipant(string zoneId, string factionId)
        {
            if (string.IsNullOrEmpty(zoneId)) throw new ArgumentNullException(nameof(zoneId));
            if (string.IsNullOrEmpty(factionId)) throw new ArgumentNullException(nameof(factionId));

            if (!_battlesByZone.TryGetValue(zoneId, out var battle))
            {
                FileLogger.Combat($"RemoveParticipant: no battle in zone '{zoneId}'.");
                return false;
            }

            bool removed = battle.RemoveParticipant(factionId);
            if (!removed)
            {
                FileLogger.Combat($"RemoveParticipant: faction '{factionId}' not in battle '{zoneId}'.");
                return false;
            }

            FileLogger.Combat($"RemoveParticipant: removed '{factionId}' from zone '{zoneId}'.");
            ResolveBattleIfDone(battle);
            return true;
        }

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
        /// <param name="zoneService">Source of truth for zone ownership. Used to neutralize zones when the player wins (Q5.A).</param>
        /// <param name="playerFactionId">The player's faction ID, if known.</param>
        public ZoneBattleManager(
            IZoneDefenderAllocationService allocationService,
            IFactionService factionService,
            IZoneService zoneService,
            string? playerFactionId = null)
        {
            _allocationService = allocationService ?? throw new ArgumentNullException(nameof(allocationService));
            _factionService = factionService ?? throw new ArgumentNullException(nameof(factionService));
            _zoneService = zoneService ?? throw new ArgumentNullException(nameof(zoneService));
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
                        EndBattleAtTick(battle, battlesToRemove);
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
                        EndBattleAtTick(battle, battlesToRemove);
                    }
                }
            }

            // Remove completed battles
            foreach (var zoneId in battlesToRemove)
            {
                _battlesByZone.Remove(zoneId);
            }
        }

        /// <summary>
        /// Routes a Tick-driven battle end through the same outcome-application
        /// pipeline as <see cref="ResolveBattleIfDone"/>, so player wins still
        /// neutralize the zone (Q5.A) regardless of which path detected the end.
        /// </summary>
        private void EndBattleAtTick(ZoneBattle battle, List<string> battlesToRemove)
        {
            var outcome = DetermineOutcome(battle);
            var alive = battle.Participants.Where(p => p.AliveCount > 0).ToList();
            battlesToRemove.Add(battle.ZoneId);
            ApplyBattleOutcome(battle, outcome, alive);
            BattleEnded?.Invoke(battle, outcome);
        }

        /// <inheritdoc />
        public void ReportTroopKilled(string zoneId, string factionId, DefenderTier tier)
        {
            if (string.IsNullOrEmpty(zoneId) || string.IsNullOrEmpty(factionId))
                return;

            if (!_battlesByZone.TryGetValue(zoneId, out var battle))
                return;

            var victim = battle.Participants.FirstOrDefault(p => p.FactionId == factionId);
            if (victim == null)
            {
                FileLogger.Combat($"ReportTroopKilled: faction '{factionId}' not in battle '{zoneId}'.");
                return;
            }

            bool removed = victim.RemoveTroop(tier);
            if (removed)
            {
                string side = victim.Role == BattleRole.Defender ? "defender" : "attacker";
                TroopKilled?.Invoke(battle, tier, side);
                ResolveBattleIfDone(battle);
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

    }
}
