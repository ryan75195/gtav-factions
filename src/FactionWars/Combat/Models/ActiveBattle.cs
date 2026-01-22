using System;
using System.Collections.Generic;
using System.Linq;
using FactionWars.Core.Models;

namespace FactionWars.Combat.Models
{
    /// <summary>
    /// Represents an ongoing territorial battle between two factions.
    /// Tracks troop counts, timing, and player presence for timed combat resolution.
    /// </summary>
    public class ActiveBattle
    {
        /// <summary>
        /// Unique identifier for this battle.
        /// </summary>
        public string Id { get; }

        /// <summary>
        /// The faction attacking the zone.
        /// </summary>
        public string AttackerFactionId { get; }

        /// <summary>
        /// The faction defending the zone.
        /// </summary>
        public string DefenderFactionId { get; }

        /// <summary>
        /// The zone being contested.
        /// </summary>
        public string ZoneId { get; }

        /// <summary>
        /// Attacker troop counts by tier. Mutable during battle.
        /// </summary>
        public Dictionary<DefenderTier, int> AttackerTroops { get; }

        /// <summary>
        /// Defender troop counts by tier. Mutable during battle.
        /// </summary>
        public Dictionary<DefenderTier, int> DefenderTroops { get; }

        /// <summary>
        /// Initial attacker troop count at battle start.
        /// </summary>
        public int InitialAttackerTroops { get; }

        /// <summary>
        /// Initial defender troop count at battle start.
        /// </summary>
        public int InitialDefenderTroops { get; }

        /// <summary>
        /// When the battle started.
        /// </summary>
        public DateTime StartTime { get; }

        /// <summary>
        /// Total battle duration in seconds.
        /// </summary>
        public float Duration { get; }

        /// <summary>
        /// Interval between kills in seconds.
        /// </summary>
        public float KillInterval { get; }

        /// <summary>
        /// Time elapsed since battle start in seconds.
        /// </summary>
        public float ElapsedTime { get; private set; }

        /// <summary>
        /// Time until next kill event in seconds.
        /// </summary>
        public float TimeUntilNextKill { get; private set; }

        /// <summary>
        /// Whether the player is currently in the contested zone.
        /// When true, tick-based simulation pauses and physical combat takes over.
        /// </summary>
        public bool IsPlayerPresent { get; set; }

        /// <summary>
        /// Gets total attacker troop count.
        /// </summary>
        public int TotalAttackerTroops => GetTotalTroops(AttackerTroops);

        /// <summary>
        /// Gets total defender troop count.
        /// </summary>
        public int TotalDefenderTroops => GetTotalTroops(DefenderTroops);

        /// <summary>
        /// Gets whether the battle is still ongoing (both sides have troops).
        /// </summary>
        public bool IsOngoing => TotalAttackerTroops > 0 && TotalDefenderTroops > 0;

        /// <summary>
        /// Gets whether attackers won (defenders eliminated).
        /// </summary>
        public bool AttackersWon => TotalDefenderTroops <= 0 && TotalAttackerTroops > 0;

        /// <summary>
        /// Gets whether defenders won (attackers eliminated).
        /// </summary>
        public bool DefendersWon => TotalAttackerTroops <= 0 && TotalDefenderTroops > 0;

        public ActiveBattle(
            string attackerFactionId,
            string defenderFactionId,
            string zoneId,
            Dictionary<DefenderTier, int> attackerTroops,
            Dictionary<DefenderTier, int> defenderTroops,
            float duration,
            float killInterval)
        {
            Id = Guid.NewGuid().ToString("N").Substring(0, 8);
            AttackerFactionId = attackerFactionId ?? throw new ArgumentNullException(nameof(attackerFactionId));
            DefenderFactionId = defenderFactionId ?? throw new ArgumentNullException(nameof(defenderFactionId));
            ZoneId = zoneId ?? throw new ArgumentNullException(nameof(zoneId));
            AttackerTroops = new Dictionary<DefenderTier, int>(attackerTroops);
            DefenderTroops = new Dictionary<DefenderTier, int>(defenderTroops);
            InitialAttackerTroops = attackerTroops.Values.Sum();
            InitialDefenderTroops = defenderTroops.Values.Sum();
            StartTime = DateTime.UtcNow;
            Duration = duration;
            KillInterval = killInterval;
            ElapsedTime = 0f;
            TimeUntilNextKill = killInterval;
            IsPlayerPresent = false;
        }

        /// <summary>
        /// Advances elapsed time and updates kill timer.
        /// </summary>
        public void AdvanceTime(float deltaSeconds)
        {
            ElapsedTime += deltaSeconds;
            TimeUntilNextKill -= deltaSeconds;
        }

        /// <summary>
        /// Resets the kill timer after a kill occurs.
        /// </summary>
        public void ResetKillTimer()
        {
            TimeUntilNextKill = KillInterval;
        }

        /// <summary>
        /// Removes one troop of the specified tier from the attacker.
        /// </summary>
        public bool RemoveAttackerTroop(DefenderTier tier)
        {
            if (AttackerTroops.TryGetValue(tier, out int count) && count > 0)
            {
                AttackerTroops[tier] = count - 1;
                return true;
            }
            return false;
        }

        /// <summary>
        /// Removes one troop of the specified tier from the defender.
        /// </summary>
        public bool RemoveDefenderTroop(DefenderTier tier)
        {
            if (DefenderTroops.TryGetValue(tier, out int count) && count > 0)
            {
                DefenderTroops[tier] = count - 1;
                return true;
            }
            return false;
        }

        /// <summary>
        /// Adds troops of the specified tier to the defender.
        /// Used when player allocates reinforcements during active battle.
        /// </summary>
        public void AddDefenderTroops(DefenderTier tier, int count)
        {
            if (count <= 0) return;

            if (DefenderTroops.ContainsKey(tier))
            {
                DefenderTroops[tier] += count;
            }
            else
            {
                DefenderTroops[tier] = count;
            }
        }

        /// <summary>
        /// Adds troops of the specified tier to the attacker.
        /// Used to restore despawned attackers when player re-enters a battle zone.
        /// </summary>
        public void AddAttackerTroops(DefenderTier tier, int count)
        {
            if (count <= 0) return;

            if (AttackerTroops.ContainsKey(tier))
            {
                AttackerTroops[tier] += count;
            }
            else
            {
                AttackerTroops[tier] = count;
            }
        }

        private static int GetTotalTroops(Dictionary<DefenderTier, int> troops)
        {
            int total = 0;
            foreach (var kvp in troops)
            {
                total += kvp.Value;
            }
            return total;
        }
    }
}
