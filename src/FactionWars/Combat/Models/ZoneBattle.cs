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
        /// <summary>
        /// Unique identifier for this battle.
        /// </summary>
        public string Id { get; }

        /// <summary>
        /// The zone being contested.
        /// </summary>
        public string ZoneId { get; }

        /// <summary>
        /// The faction attacking the zone.
        /// </summary>
        public string AttackerFactionId { get; }

        /// <summary>
        /// The faction defending the zone.
        /// </summary>
        public string DefenderFactionId { get; }

        /// <summary>
        /// Attacker troop counts by tier. Mutable during battle.
        /// </summary>
        public Dictionary<DefenderTier, int> AttackerTroops { get; }

        /// <summary>
        /// Defender troop counts by tier. Mutable during battle.
        /// </summary>
        public Dictionary<DefenderTier, int> DefenderTroops { get; }

        /// <summary>
        /// Initial total attacker troops at battle start (immutable).
        /// </summary>
        public int InitialAttackerTroops { get; }

        /// <summary>
        /// Initial total defender troops at battle start (immutable).
        /// </summary>
        public int InitialDefenderTroops { get; }

        /// <summary>
        /// Maps ped handles to their tier for spawned attackers.
        /// </summary>
        public Dictionary<int, DefenderTier> SpawnedAttackers { get; }

        /// <summary>
        /// Maps ped handles to their tier for spawned defenders.
        /// </summary>
        public Dictionary<int, DefenderTier> SpawnedDefenders { get; }

        /// <summary>
        /// Whether the player is currently present in this zone.
        /// When true, physical combat is active; when false, tick-based simulation runs.
        /// </summary>
        public bool IsPlayerPresent { get; set; }

        /// <summary>
        /// The player's faction ID, if known. Used to determine IsPlayerDefending/IsPlayerAttacking.
        /// </summary>
        public string? PlayerFactionId { get; }

        /// <summary>
        /// Time elapsed since battle start in seconds.
        /// </summary>
        public float ElapsedTime { get; private set; }

        /// <summary>
        /// Time until next kill event in seconds (for tick-based simulation).
        /// </summary>
        public float TimeUntilNextKill { get; private set; }

        /// <summary>
        /// The interval between kill events in seconds.
        /// </summary>
        public float KillInterval { get; private set; }

        /// <summary>
        /// Gets total attacker troop count across all tiers.
        /// </summary>
        public int TotalAttackerTroops => GetTotalTroops(AttackerTroops);

        /// <summary>
        /// Gets total defender troop count across all tiers.
        /// </summary>
        public int TotalDefenderTroops => GetTotalTroops(DefenderTroops);

        /// <summary>
        /// Gets total spawned attacker count.
        /// </summary>
        public int TotalSpawnedAttackers => SpawnedAttackers.Count;

        /// <summary>
        /// Gets total spawned defender count.
        /// </summary>
        public int TotalSpawnedDefenders => SpawnedDefenders.Count;

        /// <summary>
        /// Gets whether the battle is still ongoing (both sides have troops).
        /// </summary>
        public bool IsOngoing => TotalAttackerTroops > 0 && TotalDefenderTroops > 0;

        /// <summary>
        /// Gets whether attackers won (defenders eliminated, attackers remain).
        /// </summary>
        public bool AttackersWon => TotalDefenderTroops <= 0 && TotalAttackerTroops > 0;

        /// <summary>
        /// Gets whether defenders won (attackers eliminated, defenders remain).
        /// </summary>
        public bool DefendersWon => TotalAttackerTroops <= 0 && TotalDefenderTroops > 0;

        /// <summary>
        /// Gets whether the player is on the defending side.
        /// </summary>
        public bool IsPlayerDefending => PlayerFactionId != null && PlayerFactionId == DefenderFactionId;

        /// <summary>
        /// Gets whether the player is on the attacking side.
        /// </summary>
        public bool IsPlayerAttacking => PlayerFactionId != null && PlayerFactionId == AttackerFactionId;

        public ZoneBattle(
            string attackerFactionId,
            string defenderFactionId,
            string zoneId,
            Dictionary<DefenderTier, int> attackerTroops,
            Dictionary<DefenderTier, int> defenderTroops,
            string? playerFactionId = null)
        {
            Id = Guid.NewGuid().ToString("N").Substring(0, 8);
            AttackerFactionId = attackerFactionId ?? throw new ArgumentNullException(nameof(attackerFactionId));
            DefenderFactionId = defenderFactionId ?? throw new ArgumentNullException(nameof(defenderFactionId));
            ZoneId = zoneId ?? throw new ArgumentNullException(nameof(zoneId));
            AttackerTroops = new Dictionary<DefenderTier, int>(attackerTroops ?? throw new ArgumentNullException(nameof(attackerTroops)));
            DefenderTroops = new Dictionary<DefenderTier, int>(defenderTroops ?? throw new ArgumentNullException(nameof(defenderTroops)));
            InitialAttackerTroops = GetTotalTroops(AttackerTroops);
            InitialDefenderTroops = GetTotalTroops(DefenderTroops);
            SpawnedAttackers = new Dictionary<int, DefenderTier>();
            SpawnedDefenders = new Dictionary<int, DefenderTier>();
            PlayerFactionId = playerFactionId;
            IsPlayerPresent = false;
            ElapsedTime = 0f;
            TimeUntilNextKill = 0f;
            KillInterval = 0f;
        }

        /// <summary>
        /// Advances elapsed time and decrements the kill timer.
        /// </summary>
        public void AdvanceTime(float deltaSeconds)
        {
            ElapsedTime += deltaSeconds;
            TimeUntilNextKill -= deltaSeconds;
        }

        /// <summary>
        /// Resets the kill timer to the current kill interval.
        /// </summary>
        public void ResetKillTimer()
        {
            TimeUntilNextKill = KillInterval;
        }

        /// <summary>
        /// Sets the kill interval and resets the timer.
        /// </summary>
        public void SetKillInterval(float interval)
        {
            KillInterval = interval;
            TimeUntilNextKill = interval;
        }

        /// <summary>
        /// Removes one troop of the specified tier from the attacker.
        /// </summary>
        /// <returns>True if a troop was removed, false if no troops of that tier exist.</returns>
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
        /// <returns>True if a troop was removed, false if no troops of that tier exist.</returns>
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
        /// Adds troops of the specified tier to the attacker.
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

        /// <summary>
        /// Adds troops of the specified tier to the defender.
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
        /// Registers a spawned attacker ped with its tier.
        /// </summary>
        public void RegisterSpawnedAttacker(int pedHandle, DefenderTier tier)
        {
            SpawnedAttackers[pedHandle] = tier;
        }

        /// <summary>
        /// Registers a spawned defender ped with its tier.
        /// </summary>
        public void RegisterSpawnedDefender(int pedHandle, DefenderTier tier)
        {
            SpawnedDefenders[pedHandle] = tier;
        }

        /// <summary>
        /// Unregisters a spawned attacker ped.
        /// </summary>
        /// <returns>True if the ped was found and removed, false otherwise.</returns>
        public bool UnregisterSpawnedAttacker(int pedHandle)
        {
            return SpawnedAttackers.Remove(pedHandle);
        }

        /// <summary>
        /// Unregisters a spawned defender ped.
        /// </summary>
        /// <returns>True if the ped was found and removed, false otherwise.</returns>
        public bool UnregisterSpawnedDefender(int pedHandle)
        {
            return SpawnedDefenders.Remove(pedHandle);
        }

        /// <summary>
        /// Gets the tier of a spawned attacker by ped handle.
        /// </summary>
        /// <returns>The tier if found, null otherwise.</returns>
        public DefenderTier? GetSpawnedAttackerTier(int pedHandle)
        {
            if (SpawnedAttackers.TryGetValue(pedHandle, out var tier))
            {
                return tier;
            }
            return null;
        }

        /// <summary>
        /// Gets the tier of a spawned defender by ped handle.
        /// </summary>
        /// <returns>The tier if found, null otherwise.</returns>
        public DefenderTier? GetSpawnedDefenderTier(int pedHandle)
        {
            if (SpawnedDefenders.TryGetValue(pedHandle, out var tier))
            {
                return tier;
            }
            return null;
        }

        /// <summary>
        /// Clears all spawned ped tracking.
        /// </summary>
        public void ClearSpawnedPeds()
        {
            SpawnedAttackers.Clear();
            SpawnedDefenders.Clear();
        }

        /// <summary>
        /// Gets the count of spawned attackers for a specific tier.
        /// </summary>
        public int GetSpawnedAttackerCountByTier(DefenderTier tier)
        {
            return SpawnedAttackers.Values.Count(t => t == tier);
        }

        /// <summary>
        /// Gets the count of spawned defenders for a specific tier.
        /// </summary>
        public int GetSpawnedDefenderCountByTier(DefenderTier tier)
        {
            return SpawnedDefenders.Values.Count(t => t == tier);
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
