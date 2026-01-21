# Timed AI Territory Battles Implementation Plan

> **For Claude:** REQUIRED SUB-SKILL: Use superpowers:executing-plans to implement this plan task-by-task.

**Goal:** Replace instant AI battle calculations with timed battles (60-300 seconds) featuring a kill feed and HUD troop display.

**Architecture:** New `ActiveBattleManager` service tracks ongoing battles, processes tick-based combat, and coordinates with existing systems. When player enters a contested zone, physical NPCs spawn and their deaths update the battle state. Kill feed uses existing notification system.

**Tech Stack:** C# .NET 4.8, ScriptHookVDotNet, existing service/interface patterns

---

## Task 1: Create ActiveBattle Model

**Files:**
- Create: `src/FactionWars/Combat/Models/ActiveBattle.cs`

**Step 1: Create the ActiveBattle class**

```csharp
using System;
using System.Collections.Generic;
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
```

**Step 2: Commit**

```bash
git add src/FactionWars/Combat/Models/ActiveBattle.cs
git commit -m "feat: add ActiveBattle model for timed battles"
```

---

## Task 2: Create Battle Event Models

**Files:**
- Create: `src/FactionWars/Combat/Models/BattleKillEvent.cs`
- Create: `src/FactionWars/Combat/Models/BattleEndedEvent.cs`

**Step 1: Create BattleKillEvent class**

```csharp
using FactionWars.Core.Models;

namespace FactionWars.Combat.Models
{
    /// <summary>
    /// Event data for when one faction kills an enemy troop in a timed battle.
    /// </summary>
    public class BattleKillEvent
    {
        /// <summary>
        /// The battle this kill occurred in.
        /// </summary>
        public string BattleId { get; }

        /// <summary>
        /// The faction that got the kill.
        /// </summary>
        public string KillerFactionId { get; }

        /// <summary>
        /// The tier of the troop that got the kill.
        /// </summary>
        public DefenderTier KillerTier { get; }

        /// <summary>
        /// The faction that lost a troop.
        /// </summary>
        public string VictimFactionId { get; }

        /// <summary>
        /// The tier of the troop that was killed.
        /// </summary>
        public DefenderTier VictimTier { get; }

        /// <summary>
        /// The zone where the battle is taking place.
        /// </summary>
        public string ZoneId { get; }

        /// <summary>
        /// The name of the zone (for display).
        /// </summary>
        public string ZoneName { get; }

        public BattleKillEvent(
            string battleId,
            string killerFactionId,
            DefenderTier killerTier,
            string victimFactionId,
            DefenderTier victimTier,
            string zoneId,
            string zoneName)
        {
            BattleId = battleId;
            KillerFactionId = killerFactionId;
            KillerTier = killerTier;
            VictimFactionId = victimFactionId;
            VictimTier = victimTier;
            ZoneId = zoneId;
            ZoneName = zoneName;
        }
    }
}
```

**Step 2: Create BattleEndedEvent class**

```csharp
namespace FactionWars.Combat.Models
{
    /// <summary>
    /// Event data for when a timed battle ends.
    /// </summary>
    public class BattleEndedEvent
    {
        /// <summary>
        /// The battle that ended.
        /// </summary>
        public string BattleId { get; }

        /// <summary>
        /// The attacking faction.
        /// </summary>
        public string AttackerFactionId { get; }

        /// <summary>
        /// The defending faction.
        /// </summary>
        public string DefenderFactionId { get; }

        /// <summary>
        /// The zone that was contested.
        /// </summary>
        public string ZoneId { get; }

        /// <summary>
        /// The name of the zone (for display).
        /// </summary>
        public string ZoneName { get; }

        /// <summary>
        /// Whether the attacker won.
        /// </summary>
        public bool AttackerWon { get; }

        /// <summary>
        /// Total casualties the attacker suffered.
        /// </summary>
        public int AttackerCasualties { get; }

        /// <summary>
        /// Total casualties the defender suffered.
        /// </summary>
        public int DefenderCasualties { get; }

        public BattleEndedEvent(
            string battleId,
            string attackerFactionId,
            string defenderFactionId,
            string zoneId,
            string zoneName,
            bool attackerWon,
            int attackerCasualties,
            int defenderCasualties)
        {
            BattleId = battleId;
            AttackerFactionId = attackerFactionId;
            DefenderFactionId = defenderFactionId;
            ZoneId = zoneId;
            ZoneName = zoneName;
            AttackerWon = attackerWon;
            AttackerCasualties = attackerCasualties;
            DefenderCasualties = defenderCasualties;
        }
    }
}
```

**Step 3: Commit**

```bash
git add src/FactionWars/Combat/Models/BattleKillEvent.cs src/FactionWars/Combat/Models/BattleEndedEvent.cs
git commit -m "feat: add BattleKillEvent and BattleEndedEvent models"
```

---

## Task 3: Create IActiveBattleManager Interface

**Files:**
- Create: `src/FactionWars/Combat/Interfaces/IActiveBattleManager.cs`

**Step 1: Create the interface**

```csharp
using System;
using System.Collections.Generic;
using FactionWars.Combat.Models;
using FactionWars.Core.Models;

namespace FactionWars.Combat.Interfaces
{
    /// <summary>
    /// Manages active timed battles between AI factions.
    /// </summary>
    public interface IActiveBattleManager
    {
        /// <summary>
        /// Gets all currently active battles.
        /// </summary>
        IReadOnlyList<ActiveBattle> ActiveBattles { get; }

        /// <summary>
        /// Gets the number of active battles.
        /// </summary>
        int BattleCount { get; }

        /// <summary>
        /// Starts a new timed battle.
        /// </summary>
        ActiveBattle StartBattle(
            string attackerFactionId,
            string defenderFactionId,
            string zoneId,
            Dictionary<DefenderTier, int> attackerTroops,
            Dictionary<DefenderTier, int> defenderTroops);

        /// <summary>
        /// Gets the battle for a specific zone, if any.
        /// </summary>
        ActiveBattle? GetBattleForZone(string zoneId);

        /// <summary>
        /// Gets a battle by its ID.
        /// </summary>
        ActiveBattle? GetBattle(string battleId);

        /// <summary>
        /// Updates all active battles. Should be called each frame.
        /// </summary>
        void Tick(float deltaTimeSeconds);

        /// <summary>
        /// Called when the player enters a zone with an active battle.
        /// Pauses tick-based simulation for that battle.
        /// </summary>
        void OnPlayerEnterZone(string zoneId);

        /// <summary>
        /// Called when the player exits a zone with an active battle.
        /// Resumes tick-based simulation.
        /// </summary>
        void OnPlayerExitZone(string zoneId);

        /// <summary>
        /// Reports that a troop was killed by the player or physical combat.
        /// Used when IsPlayerPresent is true.
        /// </summary>
        void ReportTroopKilled(string zoneId, string factionId, DefenderTier tier);

        /// <summary>
        /// Raised when a kill occurs in a battle.
        /// </summary>
        event EventHandler<BattleKillEvent>? OnKill;

        /// <summary>
        /// Raised when a battle ends.
        /// </summary>
        event EventHandler<BattleEndedEvent>? OnBattleEnded;
    }
}
```

**Step 2: Commit**

```bash
git add src/FactionWars/Combat/Interfaces/IActiveBattleManager.cs
git commit -m "feat: add IActiveBattleManager interface"
```

---

## Task 4: Create ActiveBattleManager Service

**Files:**
- Create: `src/FactionWars/Combat/Services/ActiveBattleManager.cs`

**Step 1: Create the service implementation**

```csharp
using System;
using System.Collections.Generic;
using System.Linq;
using FactionWars.Combat.Interfaces;
using FactionWars.Combat.Models;
using FactionWars.Core.Models;
using FactionWars.Factions.Interfaces;
using FactionWars.Territory.Interfaces;
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
            int initialAttackers = battle.AttackerTroops.Values.Sum();
            int initialDefenders = battle.DefenderTroops.Values.Sum();
            int attackerCasualties = initialAttackers - battle.TotalAttackerTroops;
            int defenderCasualties = initialDefenders - battle.TotalDefenderTroops;

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
```

**Step 2: Commit**

```bash
git add src/FactionWars/Combat/Services/ActiveBattleManager.cs
git commit -m "feat: add ActiveBattleManager service for timed battles"
```

---

## Task 5: Create BattleHudData Model

**Files:**
- Create: `src/FactionWars/UI/Models/BattleHudData.cs`

**Step 1: Create the BattleHudData class**

```csharp
namespace FactionWars.UI.Models
{
    /// <summary>
    /// Data model for the battle HUD display showing active AI battles.
    /// </summary>
    public class BattleHudData
    {
        /// <summary>
        /// The zone name where the battle is occurring.
        /// </summary>
        public string ZoneName { get; }

        /// <summary>
        /// The name of the attacking faction.
        /// </summary>
        public string AttackerName { get; }

        /// <summary>
        /// Total troop count for the attacker.
        /// </summary>
        public int AttackerTroops { get; }

        /// <summary>
        /// The name of the defending faction.
        /// </summary>
        public string DefenderName { get; }

        /// <summary>
        /// Total troop count for the defender.
        /// </summary>
        public int DefenderTroops { get; }

        /// <summary>
        /// Current battle index (1-based) for display.
        /// </summary>
        public int CurrentBattleIndex { get; }

        /// <summary>
        /// Total number of active battles.
        /// </summary>
        public int TotalBattles { get; }

        /// <summary>
        /// Gets whether there are multiple battles active.
        /// </summary>
        public bool HasMultipleBattles => TotalBattles > 1;

        public BattleHudData(
            string zoneName,
            string attackerName,
            int attackerTroops,
            string defenderName,
            int defenderTroops,
            int currentBattleIndex,
            int totalBattles)
        {
            ZoneName = zoneName;
            AttackerName = attackerName;
            AttackerTroops = attackerTroops;
            DefenderName = defenderName;
            DefenderTroops = defenderTroops;
            CurrentBattleIndex = currentBattleIndex;
            TotalBattles = totalBattles;
        }
    }
}
```

**Step 2: Commit**

```bash
git add src/FactionWars/UI/Models/BattleHudData.cs
git commit -m "feat: add BattleHudData model for battle HUD display"
```

---

## Task 6: Create BattleHudRenderer

**Files:**
- Create: `src/FactionWars/ScriptHookV/UI/BattleHudRenderer.cs`

**Step 1: Create the renderer**

```csharp
using System.Drawing;
using FactionWars.UI.Models;
using GTA.UI;

namespace FactionWars.ScriptHookV.UI
{
    /// <summary>
    /// Renders the active battle HUD showing AI battle troop counts.
    /// Displays at top-left of screen, below minimap area.
    /// </summary>
    public class BattleHudRenderer
    {
        // Position constants - left side, below minimap
        private const float BoxX = 0.085f;      // Left side of screen
        private const float BoxY = 0.35f;       // Below minimap
        private const float BoxWidth = 0.15f;
        private const float BoxPadding = 0.005f;
        private const float AccentBarWidth = 0.003f;

        // Text scales
        private const float TitleScale = 0.32f;
        private const float TroopScale = 0.30f;
        private const float HintScale = 0.24f;

        // Colors
        private static readonly Color BackgroundColor = Color.FromArgb(120, 0, 0, 0);
        private static readonly Color AccentColor = Color.FromArgb(255, 255, 180, 50);  // Orange/amber
        private static readonly Color AttackerColor = Color.FromArgb(255, 255, 100, 100);
        private static readonly Color DefenderColor = Color.FromArgb(255, 100, 180, 255);

        private BattleHudData? _currentData;
        private bool _isVisible;

        /// <summary>
        /// Gets whether the HUD is visible.
        /// </summary>
        public bool IsVisible => _isVisible;

        /// <summary>
        /// Updates the HUD with new battle data.
        /// </summary>
        public void SetData(BattleHudData? data)
        {
            _currentData = data;
            _isVisible = data != null;
        }

        /// <summary>
        /// Hides the battle HUD.
        /// </summary>
        public void Hide()
        {
            _currentData = null;
            _isVisible = false;
        }

        /// <summary>
        /// Draws the battle HUD. Should be called each frame.
        /// </summary>
        public void Draw()
        {
            if (!_isVisible || _currentData == null)
                return;

            var data = _currentData;
            float boxHeight = 0.065f;
            float centerY = BoxY + (boxHeight / 2);

            // Draw background box with accent
            DrawBox(BoxX, centerY, BoxWidth, boxHeight, AccentColor);

            float textX = BoxX - (BoxWidth / 2) + BoxPadding + AccentBarWidth + 0.005f;

            // Zone name as title
            DrawTextLeft(data.ZoneName.ToUpperInvariant(), textX, BoxY + 0.005f, TitleScale, AccentColor);

            // Troop counts: "Attacker 12 vs Defender 8"
            string troopText = $"{data.AttackerName} {data.AttackerTroops} vs {data.DefenderName} {data.DefenderTroops}";
            DrawTextLeft(troopText, textX, BoxY + 0.024f, TroopScale, Color.White);

            // Battle count indicator and hint
            if (data.HasMultipleBattles)
            {
                string hintText = $"Battle {data.CurrentBattleIndex}/{data.TotalBattles} - Press B to cycle";
                DrawTextLeft(hintText, textX, BoxY + 0.044f, HintScale, Color.LightGray);
            }
        }

        /// <summary>
        /// Draws a styled box with accent bar.
        /// </summary>
        private void DrawBox(float x, float y, float width, float height, Color accentColor)
        {
            // Background
            DrawRect(x, y, width, height, BackgroundColor);

            // Left accent bar
            float accentX = x - (width / 2) + (AccentBarWidth / 2);
            DrawRect(accentX, y, AccentBarWidth, height, accentColor);
        }

        private void DrawRect(float x, float y, float width, float height, Color color)
        {
            GTA.Native.Function.Call(
                GTA.Native.Hash.DRAW_RECT,
                x, y, width, height,
                color.R, color.G, color.B, color.A);
        }

        private void DrawTextLeft(string text, float x, float y, float scale, Color color)
        {
            var textElement = new TextElement(text, new PointF(x * 1280f, y * 720f), scale, color)
            {
                Alignment = Alignment.Left,
                Shadow = true
            };
            textElement.ScaledDraw();
        }
    }
}
```

**Step 2: Commit**

```bash
git add src/FactionWars/ScriptHookV/UI/BattleHudRenderer.cs
git commit -m "feat: add BattleHudRenderer for displaying active AI battles"
```

---

## Task 7: Register ActiveBattleManager in ServiceContainer

**Files:**
- Modify: `src/FactionWars/ScriptHookV/Data/ServiceContainer.cs`

**Step 1: Find existing registration pattern**

Search for `IBattleSimulationService` registration to understand the pattern.

**Step 2: Add IActiveBattleManager registration**

Add the following registration after other combat services:

```csharp
// In the Register method, add:
Register<IActiveBattleManager>(() => new ActiveBattleManager(
    Resolve<IFactionService>(),
    Resolve<IZoneService>(),
    Resolve<IZoneDefenderAllocationService>()));
```

Add the using statements at the top:

```csharp
using FactionWars.Combat.Interfaces;
using FactionWars.Combat.Services;
```

**Step 3: Commit**

```bash
git add src/FactionWars/ScriptHookV/Data/ServiceContainer.cs
git commit -m "feat: register ActiveBattleManager in ServiceContainer"
```

---

## Task 8: Modify AIController to Start Timed Battles

**Files:**
- Modify: `src/FactionWars/AI/Controllers/AIController.cs`

**Step 1: Add IActiveBattleManager dependency**

In the constructor and fields, add:

```csharp
private readonly IActiveBattleManager _activeBattleManager;
```

Update constructor to accept and store `IActiveBattleManager`.

**Step 2: Modify SimulateBattle method**

Replace the instant battle simulation with starting a timed battle:

Instead of:
```csharp
var result = _battleSimulationService.SimulateBattle(...);
ApplyBattleResult(result, decision.TroopsToCommit);
```

Use:
```csharp
// Build troop dictionaries
var attackerTroops = new Dictionary<DefenderTier, int>
{
    { DefenderTier.Basic, decision.TroopsToCommit },
    { DefenderTier.Medium, 0 },
    { DefenderTier.Heavy, 0 }
};

var defenderTroops = BuildDefenderTroopsDictionary(defenderFactionId, decision.TargetZoneId!);

// Start timed battle instead of instant simulation
_activeBattleManager.StartBattle(
    attackerFactionId,
    defenderFactionId,
    decision.TargetZoneId!,
    attackerTroops,
    defenderTroops);
```

**Step 3: Add helper method**

```csharp
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
```

**Step 4: Commit**

```bash
git add src/FactionWars/AI/Controllers/AIController.cs
git commit -m "feat: integrate AIController with ActiveBattleManager for timed battles"
```

---

## Task 9: Integrate ActiveBattleManager into GameLoopController

**Files:**
- Modify: `src/FactionWars/ScriptHookV/GameLoopController.cs`

**Step 1: Add fields**

```csharp
private IActiveBattleManager? _activeBattleManager;
private BattleHudRenderer? _battleHudRenderer;
private int _currentBattleHudIndex = 0;
private const int BattleCycleKeyCode = 0x42; // B key
```

**Step 2: Initialize in InitializeGameData**

After initializing other managers:

```csharp
// Initialize active battle manager
_activeBattleManager = _container.Resolve<IActiveBattleManager>();
_activeBattleManager.OnKill += OnBattleKill;
_activeBattleManager.OnBattleEnded += OnBattleEnded;

// Initialize battle HUD renderer
_battleHudRenderer = new BattleHudRenderer();
```

**Step 3: Update OnTick to tick battles and draw HUD**

Add after `_aiController?.Update(deltaTime)`:

```csharp
// Update active battle manager
_activeBattleManager?.Tick(deltaTime);
```

In `UpdateAndDrawHud`, add:

```csharp
// Draw battle HUD
UpdateAndDrawBattleHud();
```

**Step 4: Add battle HUD update method**

```csharp
private void UpdateAndDrawBattleHud()
{
    if (_activeBattleManager == null || _battleHudRenderer == null)
        return;

    var battles = _activeBattleManager.ActiveBattles;
    if (battles.Count == 0)
    {
        _battleHudRenderer.Hide();
        return;
    }

    // Clamp index
    if (_currentBattleHudIndex >= battles.Count)
        _currentBattleHudIndex = 0;

    var battle = battles[_currentBattleHudIndex];
    var zone = _zoneService?.GetZone(battle.ZoneId);
    var attackerFaction = _factionService.GetFaction(battle.AttackerFactionId);
    var defenderFaction = _factionService.GetFaction(battle.DefenderFactionId);

    var hudData = new BattleHudData(
        zone?.Name ?? battle.ZoneId,
        attackerFaction?.Name ?? battle.AttackerFactionId,
        battle.TotalAttackerTroops,
        defenderFaction?.Name ?? battle.DefenderFactionId,
        battle.TotalDefenderTroops,
        _currentBattleHudIndex + 1,
        battles.Count);

    _battleHudRenderer.SetData(hudData);
    _battleHudRenderer.Draw();
}
```

**Step 5: Add key handler for battle cycling**

In `OnKeyDown`:

```csharp
// Handle battle HUD cycle key
if (keyCode == BattleCycleKeyCode && _activeBattleManager != null)
{
    var battles = _activeBattleManager.ActiveBattles;
    if (battles.Count > 1)
    {
        _currentBattleHudIndex = (_currentBattleHudIndex + 1) % battles.Count;
    }
    return;
}
```

**Step 6: Add event handlers**

```csharp
private void OnBattleKill(object? sender, BattleKillEvent e)
{
    var killerFaction = _factionService.GetFaction(e.KillerFactionId);
    var victimFaction = _factionService.GetFaction(e.VictimFactionId);

    string killerName = killerFaction?.Name ?? e.KillerFactionId;
    string victimName = victimFaction?.Name ?? e.VictimFactionId;

    // Format: "[Ballas] Heavy killed [Grove St] Basic in Davis"
    string message = $"~y~[{killerName}]~w~ {e.KillerTier} killed ~r~[{victimName}]~w~ {e.VictimTier} in {e.ZoneName}";
    _gameBridge.ShowNotification(message);
}

private void OnBattleEnded(object? sender, BattleEndedEvent e)
{
    var attackerFaction = _factionService.GetFaction(e.AttackerFactionId);
    var defenderFaction = _factionService.GetFaction(e.DefenderFactionId);

    string attackerName = attackerFaction?.Name ?? e.AttackerFactionId;
    string defenderName = defenderFaction?.Name ?? e.DefenderFactionId;

    if (e.AttackerWon)
    {
        _gameBridge.ShowNotification($"~g~[{attackerName}]~w~ captured ~b~{e.ZoneName}~w~ from ~r~[{defenderName}]");
    }
    else
    {
        _gameBridge.ShowNotification($"~g~[{defenderName}]~w~ defended ~b~{e.ZoneName}~w~ against ~r~[{attackerName}]");
    }
}
```

**Step 7: Wire zone events to ActiveBattleManager**

In `OnZoneEntered`:

```csharp
// Notify active battle manager of player entering zone
_activeBattleManager?.OnPlayerEnterZone(zone.Id);
```

In `OnZoneExited`:

```csharp
// Notify active battle manager of player exiting zone
_activeBattleManager?.OnPlayerExitZone(zone.Id);
```

**Step 8: Cleanup in OnAbort**

```csharp
if (_activeBattleManager != null)
{
    _activeBattleManager.OnKill -= OnBattleKill;
    _activeBattleManager.OnBattleEnded -= OnBattleEnded;
}
_activeBattleManager = null;
_battleHudRenderer = null;
```

**Step 9: Commit**

```bash
git add src/FactionWars/ScriptHookV/GameLoopController.cs
git commit -m "feat: integrate ActiveBattleManager and BattleHudRenderer into GameLoopController"
```

---

## Task 10: Modify EnemyDefenderManager to Report Kills

**Files:**
- Modify: `src/FactionWars/ScriptHookV/Managers/EnemyDefenderManager.cs`

**Step 1: Add IActiveBattleManager dependency**

```csharp
private readonly IActiveBattleManager? _activeBattleManager;
```

Update constructor to accept optional `IActiveBattleManager`:

```csharp
public EnemyDefenderManager(
    IGameBridge gameBridge,
    IZoneDefenderAllocationService allocationService,
    IPedSpawningService pedSpawningService,
    IDefenderTierService defenderTierService,
    IPedBlipService pedBlipService,
    IZoneService zoneService,
    IActiveBattleManager? activeBattleManager = null)
{
    // ... existing assignments ...
    _activeBattleManager = activeBattleManager;
}
```

**Step 2: Report deaths to ActiveBattleManager**

In `HandleDefenderDeath`, after removing the troop from allocation, add:

```csharp
// Report to active battle manager if there's an ongoing battle
var battle = _activeBattleManager?.GetBattleForZone(zoneId);
if (battle != null && battle.IsPlayerPresent)
{
    _activeBattleManager?.ReportTroopKilled(zoneId, enemyFactionId, tier);
}
```

**Step 3: Commit**

```bash
git add src/FactionWars/ScriptHookV/Managers/EnemyDefenderManager.cs
git commit -m "feat: report enemy defender deaths to ActiveBattleManager"
```

---

## Task 11: Update ServiceContainer with EnemyDefenderManager Changes

**Files:**
- Modify: `src/FactionWars/ScriptHookV/Data/ServiceContainer.cs`

**Step 1: Update EnemyDefenderManager creation in GameLoopController**

Since `EnemyDefenderManager` is created directly in `GameLoopController.InitializeGameData`, update that initialization to pass the `IActiveBattleManager`:

In `GameLoopController.cs`:

```csharp
// Initialize enemy defender manager
_enemyDefenderManager = new EnemyDefenderManager(
    _gameBridge,
    allocationService,
    pedSpawningService,
    defenderTierService,
    pedBlipService,
    _zoneService,
    _activeBattleManager);  // Pass the battle manager
```

**Step 2: Commit**

```bash
git add src/FactionWars/ScriptHookV/GameLoopController.cs
git commit -m "feat: pass ActiveBattleManager to EnemyDefenderManager"
```

---

## Task 12: Build and Test

**Step 1: Build the project**

```bash
cd "C:/Users/ryan7/programming/gtav-factions/src/FactionWars" && dotnet build
```

**Step 2: Fix any compilation errors**

Address any missing using statements, type mismatches, or other build errors.

**Step 3: Deploy to GTA V**

```bash
cp "C:/Users/ryan7/programming/gtav-factions/src/FactionWars/bin/Debug/net48/FactionWars.dll" "E:/SteamLibrary/steamapps/common/Grand Theft Auto V/scripts/"
```

**Step 4: Commit successful build**

```bash
git add -A
git commit -m "feat: complete timed AI battles implementation"
```

---

## Summary

This plan implements timed AI territorial battles with:

1. **ActiveBattle model** - Tracks battle state, troop counts, and timing
2. **BattleKillEvent/BattleEndedEvent** - Event data for UI notifications
3. **IActiveBattleManager interface** - Clean abstraction for battle management
4. **ActiveBattleManager service** - Core logic for tick-based combat simulation
5. **BattleHudData/BattleHudRenderer** - HUD display for active battles
6. **AIController integration** - Starts timed battles instead of instant resolution
7. **GameLoopController integration** - Ticks battles, handles HUD, processes events
8. **EnemyDefenderManager integration** - Reports NPC deaths to battle manager

Key features:
- Battles last 60-300 seconds based on troop count
- Kill feed: `"[Faction] Tier killed [Faction] Tier in Zone"`
- HUD shows troop counts, press B to cycle through multiple battles
- Physical NPC combat when player is present, seamless transition back to simulation
