using System;
using System.Collections.Generic;
using System.Linq;
using FactionWars.Core.Models;

namespace FactionWars.Combat.Models
{
    /// <summary>
    /// Unified state representation for a battle in a zone.
    /// This is the single source of truth for all battle-related data,
    /// including troop counts, spawned peds, player presence, and timing.
    /// </summary>
    public class ZoneBattle
    {
        private readonly List<BattleParticipant> _participants;

        /// <summary>Unique identifier for this battle.</summary>
        public string Id { get; }

        /// <summary>The zone being contested.</summary>
        public string ZoneId { get; }

        /// <summary>Initial total attacker troops at battle start (immutable).</summary>
        public int InitialAttackerTroops { get; }

        /// <summary>Initial total defender troops at battle start (immutable).</summary>
        public int InitialDefenderTroops { get; }

        /// <summary>Maps ped handles to their tier for spawned attackers.</summary>
        public Dictionary<int, DefenderTier> SpawnedAttackers { get; }

        /// <summary>Maps ped handles to their tier for spawned defenders.</summary>
        public Dictionary<int, DefenderTier> SpawnedDefenders { get; }

        /// <summary>
        /// Whether the player is currently present in this zone.
        /// When true, physical combat is active; when false, tick-based simulation runs.
        /// </summary>
        public bool IsPlayerPresent { get; set; }

        /// <summary>
        /// The player's faction ID, if known. Used by <see cref="IsPlayerDefending"/>
        /// / <see cref="IsPlayerAttacking"/>. (In Plan 1 the player is not yet a
        /// participant — this stays as a passive flag set by the caller.)
        /// </summary>
        public string? PlayerFactionId { get; }

        /// <summary>Time elapsed since battle start in seconds.</summary>
        public float ElapsedTime { get; private set; }

        /// <summary>Time until next kill event in seconds (for tick-based simulation).</summary>
        public float TimeUntilNextKill { get; private set; }

        /// <summary>The interval between kill events in seconds.</summary>
        public float KillInterval { get; private set; }

        /// <summary>
        /// All participants in this battle. In Plan 1 this is always exactly one
        /// Defender + one Attacker (a 2-way battle). Plan 2 will add a second Attacker
        /// for 3-way melees.
        /// </summary>
        public IReadOnlyList<BattleParticipant> Participants => _participants;

        /// <summary>The single Defender-role participant. Throws if missing.</summary>
        public BattleParticipant Defender => _participants.First(p => p.Role == BattleRole.Defender);

        /// <summary>All Attacker-role participants. In Plan 1 always length 1.</summary>
        public IReadOnlyList<BattleParticipant> Attackers
            => _participants.Where(p => p.Role == BattleRole.Attacker).ToList();

        // === Backward-compatible legacy accessors (forward to _participants) ===

        /// <summary>The (single) attacking faction. Backward-compat for Plan 1.</summary>
        public string AttackerFactionId => Attackers[0].FactionId;

        /// <summary>The defending faction.</summary>
        public string DefenderFactionId => Defender.FactionId;

        /// <summary>
        /// Attacker troop counts by tier. Backward-compat: returns the dict of
        /// the single attacker. Mutable — callers that mutate it (existing
        /// behaviour) mutate the participant's storage directly.
        /// </summary>
        public Dictionary<DefenderTier, int> AttackerTroops => Attackers[0].Troops;

        /// <summary>Defender troop counts by tier. Same backward-compat shape.</summary>
        public Dictionary<DefenderTier, int> DefenderTroops => Defender.Troops;

        /// <summary>Sum of all attackers' troop counts.</summary>
        public int TotalAttackerTroops
        {
            get
            {
                int total = 0;
                foreach (var p in _participants)
                    if (p.Role == BattleRole.Attacker) total += p.AliveCount;
                return total;
            }
        }

        /// <summary>Defender's troop count.</summary>
        public int TotalDefenderTroops => Defender.AliveCount;

        public int TotalSpawnedAttackers => SpawnedAttackers.Count;
        public int TotalSpawnedDefenders => SpawnedDefenders.Count;

        public bool IsOngoing => TotalAttackerTroops > 0 && TotalDefenderTroops > 0;
        public bool AttackersWon => TotalDefenderTroops <= 0 && TotalAttackerTroops > 0;
        public bool DefendersWon => TotalAttackerTroops <= 0 && TotalDefenderTroops > 0;

        public bool IsPlayerDefending => PlayerFactionId != null && PlayerFactionId == DefenderFactionId;
        public bool IsPlayerAttacking => PlayerFactionId != null && PlayerFactionId == AttackerFactionId;

        public ZoneBattle(
            string attackerFactionId,
            string defenderFactionId,
            string zoneId,
            Dictionary<DefenderTier, int> attackerTroops,
            Dictionary<DefenderTier, int> defenderTroops,
            string? playerFactionId = null)
        {
            if (attackerFactionId == null) throw new ArgumentNullException(nameof(attackerFactionId));
            if (defenderFactionId == null) throw new ArgumentNullException(nameof(defenderFactionId));
            if (zoneId == null) throw new ArgumentNullException(nameof(zoneId));
            if (attackerTroops == null) throw new ArgumentNullException(nameof(attackerTroops));
            if (defenderTroops == null) throw new ArgumentNullException(nameof(defenderTroops));

            Id = Guid.NewGuid().ToString("N").Substring(0, 8);
            ZoneId = zoneId;
            PlayerFactionId = playerFactionId;
            IsPlayerPresent = false;
            ElapsedTime = 0f;
            TimeUntilNextKill = 0f;
            KillInterval = 0f;
            SpawnedAttackers = new Dictionary<int, DefenderTier>();
            SpawnedDefenders = new Dictionary<int, DefenderTier>();

            _participants = new List<BattleParticipant>
            {
                BattleParticipant.ForAi(defenderFactionId, BattleRole.Defender, defenderTroops),
                BattleParticipant.ForAi(attackerFactionId, BattleRole.Attacker, attackerTroops)
            };

            // Cache initial totals so they remain stable even as participant
            // troop counts decrement during the battle.
            InitialAttackerTroops = TotalAttackerTroops;
            InitialDefenderTroops = TotalDefenderTroops;
        }

        public void AdvanceTime(float deltaSeconds)
        {
            ElapsedTime += deltaSeconds;
            TimeUntilNextKill -= deltaSeconds;
        }

        public void ResetKillTimer()
        {
            TimeUntilNextKill = KillInterval;
        }

        public void SetKillInterval(float interval)
        {
            KillInterval = interval;
            TimeUntilNextKill = interval;
        }

        public bool RemoveAttackerTroop(DefenderTier tier) => Attackers[0].RemoveTroop(tier);
        public bool RemoveDefenderTroop(DefenderTier tier) => Defender.RemoveTroop(tier);

        public void AddAttackerTroops(DefenderTier tier, int count) => Attackers[0].AddTroops(tier, count);
        public void AddDefenderTroops(DefenderTier tier, int count) => Defender.AddTroops(tier, count);

        public void RegisterSpawnedAttacker(int pedHandle, DefenderTier tier)
        {
            SpawnedAttackers[pedHandle] = tier;
        }

        public void RegisterSpawnedDefender(int pedHandle, DefenderTier tier)
        {
            SpawnedDefenders[pedHandle] = tier;
        }

        public bool UnregisterSpawnedAttacker(int pedHandle) => SpawnedAttackers.Remove(pedHandle);
        public bool UnregisterSpawnedDefender(int pedHandle) => SpawnedDefenders.Remove(pedHandle);

        public DefenderTier? GetSpawnedAttackerTier(int pedHandle)
            => SpawnedAttackers.TryGetValue(pedHandle, out var tier) ? tier : (DefenderTier?)null;

        public DefenderTier? GetSpawnedDefenderTier(int pedHandle)
            => SpawnedDefenders.TryGetValue(pedHandle, out var tier) ? tier : (DefenderTier?)null;

        public void ClearSpawnedPeds()
        {
            SpawnedAttackers.Clear();
            SpawnedDefenders.Clear();
        }

        public int GetSpawnedAttackerCountByTier(DefenderTier tier)
            => SpawnedAttackers.Values.Count(t => t == tier);

        public int GetSpawnedDefenderCountByTier(DefenderTier tier)
            => SpawnedDefenders.Values.Count(t => t == tier);
    }
}
