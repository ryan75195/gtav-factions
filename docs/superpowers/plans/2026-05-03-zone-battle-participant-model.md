# `ZoneBattle` Participant Model Refactor — Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Refactor `ZoneBattle`'s internal storage to a `List<BattleParticipant>` while keeping its existing external API (`AttackerFactionId`, `DefenderFactionId`, troop dicts, helper methods) backward-compatible, as the foundation for Plan 2 (3-way melee + `CombatManager` elimination).

**Architecture:** Add `BattleRole` enum and `BattleParticipant` class. Add a `List<BattleParticipant>` to `ZoneBattle`, populated in the constructor from the existing two-faction args. Forward every existing property/method on `ZoneBattle` to operate on the participant list. No callers change. No behaviour change. New `Participants` / `Defender` / `Attackers` accessors are added for Plan 2 to consume.

**Tech Stack:** C# .NET Framework 4.8, xUnit, Moq.

---

## Scope

In scope:
- New `BattleRole` enum (`Defender`, `Attacker`).
- New `BattleParticipant` class with `FactionId`, `Role`, `IsPlayer`, troop storage, and an `AliveCount` accessor.
- `ZoneBattle` refactored internally to a `List<BattleParticipant>`, with all current public API preserved as forwarding properties/methods.
- New accessors on `ZoneBattle`: `Participants`, `Defender`, `Attackers`.
- Tests for the new types and accessors. All existing `ZoneBattleTests` and `ZoneBattleManagerTests` remain green with no edits.

Out of scope (deferred to Plan 2):
- `JoinAsAttacker` / `RemoveParticipant` / `StartPlayerCombat` lifecycle entry points on `ZoneBattleManager`.
- 3-way kill routing or victory conditions.
- Any change to `CombatManager`, `CombatEncounter`, `CombatResultHandler`, `TakeoverDetector`.
- HUD changes.
- Faction-color blip changes.
- Persistence (none exists — `ZoneBattle` is in-memory only).

---

## File Structure

| File | Change |
|---|---|
| `src/FactionWars/Combat/Models/BattleRole.cs` | NEW — small enum file |
| `src/FactionWars/Combat/Models/BattleParticipant.cs` | NEW — participant class |
| `src/FactionWars/Combat/Models/ZoneBattle.cs` | Modified — internal storage swap, public API preserved |
| `tests/FactionWars.Tests/Unit/Combat/BattleParticipantTests.cs` | NEW — unit tests for the new class |
| `tests/FactionWars.Tests/Unit/Combat/ZoneBattleParticipantsTests.cs` | NEW — tests for the new `ZoneBattle.Participants` / `Defender` / `Attackers` accessors |
| `tests/FactionWars.Tests/Unit/Combat/ZoneBattleTests.cs` | UNCHANGED — existing tests must stay green to prove backward-compat |
| `tests/FactionWars.Tests/Unit/Combat/ZoneBattleManagerTests.cs` | UNCHANGED — same |
| `tests/FactionWars.Tests/Unit/Combat/ZoneBattleManagerAllocationSyncTests.cs` | UNCHANGED — same |

---

## Conventions

- **Domain `Vector3`:** `FactionWars.Core.Interfaces.Vector3` (the file is at `Core/Models/Vector3.cs` but lives in the `Interfaces` namespace — confirmed during recent leash work). Tests therefore `using FactionWars.Core.Interfaces;` for `Vector3`.
- **Build / test commands** (Windows bash):
  - Build: `dotnet build src/FactionWars/FactionWars.csproj -c Debug -v minimal`
  - Single test class: `dotnet test tests/FactionWars.Tests/FactionWars.Tests.csproj -c Debug --nologo --filter "FullyQualifiedName~<TestClassName>"`
  - Full suite: `dotnet test tests/FactionWars.Tests/FactionWars.Tests.csproj -c Debug --nologo -v minimal`
- **Iron rule:** every existing test in `ZoneBattleTests` and `ZoneBattleManagerTests` keeps passing without edits. If one fails, the refactor broke backward compat — fix the refactor, not the test.
- The plan ends with a deploy step copying the DLL to `E:/SteamLibrary/steamapps/common/Grand Theft Auto V/scripts/` for in-game smoke testing (no functional change expected, but verifying the mod still loads).

---

## Task 1: `BattleRole` enum

**Files:**
- Create: `src/FactionWars/Combat/Models/BattleRole.cs`

This is a tiny enum file. No tests — enums are not behaviour. The class that uses it (`BattleParticipant`, Task 2) gets its own tests.

- [ ] **Step 1.1: Create the enum file**

Write `src/FactionWars/Combat/Models/BattleRole.cs`:

```csharp
namespace FactionWars.Combat.Models
{
    /// <summary>
    /// The role a participant plays in a <see cref="ZoneBattle"/>.
    /// A battle has exactly one Defender and one or more Attackers.
    /// </summary>
    public enum BattleRole
    {
        Defender,
        Attacker
    }
}
```

- [ ] **Step 1.2: Build to verify it compiles**

```bash
dotnet build src/FactionWars/FactionWars.csproj -c Debug -v minimal
```

Expected: 0 errors.

- [ ] **Step 1.3: Commit**

```bash
git add src/FactionWars/Combat/Models/BattleRole.cs
git commit -m "feat: add BattleRole enum"
```

---

## Task 2: `BattleParticipant` class

**Files:**
- Create: `src/FactionWars/Combat/Models/BattleParticipant.cs`
- Test: `tests/FactionWars.Tests/Unit/Combat/BattleParticipantTests.cs`

`BattleParticipant` represents one side of a battle — either an AI faction (with a tier-keyed troop dict) or the player (with an external alive-count callback to avoid coupling to the squad service).

- [ ] **Step 2.1: Write the failing tests**

Create `tests/FactionWars.Tests/Unit/Combat/BattleParticipantTests.cs`:

```csharp
using System;
using System.Collections.Generic;
using FactionWars.Combat.Models;
using FactionWars.Core.Models;
using Xunit;

namespace FactionWars.Tests.Unit.Combat
{
    public class BattleParticipantTests
    {
        private static Dictionary<DefenderTier, int> Troops(int basic = 0, int medium = 0, int heavy = 0, int elite = 0)
        {
            return new Dictionary<DefenderTier, int>
            {
                { DefenderTier.Basic, basic },
                { DefenderTier.Medium, medium },
                { DefenderTier.Heavy, heavy },
                { DefenderTier.Elite, elite }
            };
        }

        [Fact]
        public void AiParticipant_StoresFactionRoleAndTroops()
        {
            var p = BattleParticipant.ForAi("trevor", BattleRole.Attacker, Troops(basic: 5, medium: 2));

            Assert.Equal("trevor", p.FactionId);
            Assert.Equal(BattleRole.Attacker, p.Role);
            Assert.False(p.IsPlayer);
            Assert.Equal(5, p.Troops[DefenderTier.Basic]);
            Assert.Equal(2, p.Troops[DefenderTier.Medium]);
        }

        [Fact]
        public void AiParticipant_AliveCount_SumsTroops()
        {
            var p = BattleParticipant.ForAi("michael", BattleRole.Defender, Troops(basic: 3, heavy: 4));

            Assert.Equal(7, p.AliveCount);
        }

        [Fact]
        public void AiParticipant_AliveCount_DropsAsTroopsRemoved()
        {
            var p = BattleParticipant.ForAi("trevor", BattleRole.Attacker, Troops(basic: 5));

            p.RemoveTroop(DefenderTier.Basic);
            Assert.Equal(4, p.AliveCount);

            p.RemoveTroop(DefenderTier.Basic);
            p.RemoveTroop(DefenderTier.Basic);
            Assert.Equal(2, p.AliveCount);
        }

        [Fact]
        public void AiParticipant_RemoveTroop_ReturnsFalseWhenEmpty()
        {
            var p = BattleParticipant.ForAi("trevor", BattleRole.Attacker, Troops(basic: 0));

            Assert.False(p.RemoveTroop(DefenderTier.Basic));
            Assert.Equal(0, p.AliveCount);
        }

        [Fact]
        public void AiParticipant_AddTroops_IncrementsExistingTier()
        {
            var p = BattleParticipant.ForAi("trevor", BattleRole.Attacker, Troops(basic: 1));

            p.AddTroops(DefenderTier.Basic, 4);
            Assert.Equal(5, p.Troops[DefenderTier.Basic]);
            Assert.Equal(5, p.AliveCount);
        }

        [Fact]
        public void PlayerParticipant_HasIsPlayerTrue_AndUsesCallbackForAliveCount()
        {
            int squadCount = 3;
            var p = BattleParticipant.ForPlayer("player_faction", BattleRole.Attacker, () => squadCount);

            Assert.Equal("player_faction", p.FactionId);
            Assert.Equal(BattleRole.Attacker, p.Role);
            Assert.True(p.IsPlayer);
            Assert.Empty(p.Troops);
            Assert.Equal(3, p.AliveCount);

            squadCount = 0;
            Assert.Equal(0, p.AliveCount);
        }

        [Fact]
        public void PlayerParticipant_RemoveTroop_DoesNothing()
        {
            // Player aliveness is owned by the squad callback, not the troop dict.
            // RemoveTroop is a no-op for player participants and returns false.
            var p = BattleParticipant.ForPlayer("player_faction", BattleRole.Attacker, () => 2);

            Assert.False(p.RemoveTroop(DefenderTier.Basic));
            Assert.Equal(2, p.AliveCount);
        }

        [Fact]
        public void ForAi_NullFactionId_Throws()
        {
            Assert.Throws<ArgumentNullException>(() =>
                BattleParticipant.ForAi(null!, BattleRole.Attacker, Troops()));
        }

        [Fact]
        public void ForAi_NullTroops_Throws()
        {
            Assert.Throws<ArgumentNullException>(() =>
                BattleParticipant.ForAi("trevor", BattleRole.Attacker, null!));
        }

        [Fact]
        public void ForPlayer_NullCallback_Throws()
        {
            Assert.Throws<ArgumentNullException>(() =>
                BattleParticipant.ForPlayer("player_faction", BattleRole.Attacker, null!));
        }
    }
}
```

- [ ] **Step 2.2: Run tests to verify they fail**

```bash
dotnet test tests/FactionWars.Tests/FactionWars.Tests.csproj -c Debug --nologo --filter "FullyQualifiedName~BattleParticipantTests"
```

Expected: build error referencing `BattleParticipant` not defined.

- [ ] **Step 2.3: Implement `BattleParticipant`**

Write `src/FactionWars/Combat/Models/BattleParticipant.cs`:

```csharp
using System;
using System.Collections.Generic;
using FactionWars.Core.Models;

namespace FactionWars.Combat.Models
{
    /// <summary>
    /// A single side in a <see cref="ZoneBattle"/> — either an AI faction
    /// with tier-keyed troops, or the player whose alive count comes from
    /// a squad-service callback. Construct via <see cref="ForAi"/> or
    /// <see cref="ForPlayer"/>; do not use the constructor directly.
    /// </summary>
    public class BattleParticipant
    {
        private readonly Func<int>? _playerAliveCountCallback;

        public string FactionId { get; }
        public BattleRole Role { get; }
        public bool IsPlayer { get; }

        /// <summary>
        /// Tier-keyed troop counts for AI participants. Always empty for
        /// player participants — their alive count comes from the
        /// squad callback. Mutable during a battle.
        /// </summary>
        public Dictionary<DefenderTier, int> Troops { get; }

        private BattleParticipant(
            string factionId,
            BattleRole role,
            bool isPlayer,
            Dictionary<DefenderTier, int> troops,
            Func<int>? playerAliveCountCallback)
        {
            FactionId = factionId;
            Role = role;
            IsPlayer = isPlayer;
            Troops = troops;
            _playerAliveCountCallback = playerAliveCountCallback;
        }

        public static BattleParticipant ForAi(string factionId, BattleRole role, Dictionary<DefenderTier, int> troops)
        {
            if (factionId == null) throw new ArgumentNullException(nameof(factionId));
            if (troops == null) throw new ArgumentNullException(nameof(troops));
            return new BattleParticipant(factionId, role, isPlayer: false, new Dictionary<DefenderTier, int>(troops), null);
        }

        public static BattleParticipant ForPlayer(string factionId, BattleRole role, Func<int> aliveCountCallback)
        {
            if (factionId == null) throw new ArgumentNullException(nameof(factionId));
            if (aliveCountCallback == null) throw new ArgumentNullException(nameof(aliveCountCallback));
            return new BattleParticipant(factionId, role, isPlayer: true, new Dictionary<DefenderTier, int>(), aliveCountCallback);
        }

        /// <summary>
        /// Total surviving members on this side. AI: sum of <see cref="Troops"/>.
        /// Player: result of the squad callback.
        /// </summary>
        public int AliveCount
        {
            get
            {
                if (IsPlayer) return _playerAliveCountCallback!();
                int total = 0;
                foreach (var kvp in Troops) total += kvp.Value;
                return total;
            }
        }

        /// <summary>
        /// Decrements one troop of the given tier. No-op for player participants.
        /// </summary>
        /// <returns>True if a troop was removed.</returns>
        public bool RemoveTroop(DefenderTier tier)
        {
            if (IsPlayer) return false;
            if (Troops.TryGetValue(tier, out int count) && count > 0)
            {
                Troops[tier] = count - 1;
                return true;
            }
            return false;
        }

        /// <summary>
        /// Adds <paramref name="count"/> troops of the given tier. No-op for
        /// player participants and for non-positive counts.
        /// </summary>
        public void AddTroops(DefenderTier tier, int count)
        {
            if (IsPlayer || count <= 0) return;
            if (Troops.ContainsKey(tier)) Troops[tier] += count;
            else Troops[tier] = count;
        }
    }
}
```

- [ ] **Step 2.4: Run tests to verify they pass**

```bash
dotnet test tests/FactionWars.Tests/FactionWars.Tests.csproj -c Debug --nologo --filter "FullyQualifiedName~BattleParticipantTests"
```

Expected: 10 passed.

- [ ] **Step 2.5: Commit**

```bash
git add src/FactionWars/Combat/Models/BattleParticipant.cs tests/FactionWars.Tests/Unit/Combat/BattleParticipantTests.cs
git commit -m "feat: add BattleParticipant model"
```

---

## Task 3: Switch `ZoneBattle` internal storage to participant list (backward-compatible)

**Files:**
- Modify: `src/FactionWars/Combat/Models/ZoneBattle.cs`

This is the heart of the refactor. The strategy: add a private `List<BattleParticipant>` populated in the constructor, then rewrite each existing property and method to delegate to it. Every existing call site keeps working unchanged. Existing tests in `ZoneBattleTests` (and `ZoneBattleManagerTests` which depends on `ZoneBattle`) MUST stay green without edits — that's the contract.

There are no failing tests to write for this task on its own; the **existing test suites are the failing test**. We change the implementation, run them, and verify they still pass.

- [ ] **Step 3.1: Replace the `ZoneBattle` class body**

Open `src/FactionWars/Combat/Models/ZoneBattle.cs` and replace the entire body of the class (everything between the opening `{` after `public class ZoneBattle` and the closing `}` of the class) with the implementation below. The file's `using` block, namespace, class declaration, and outermost braces stay the same.

The new body keeps every existing public member with identical signatures and semantics; the only structural change is that each member now reads/writes through `_participants` instead of two named dictionaries.

```csharp
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
```

Note: the existing private static `GetTotalTroops` helper is no longer needed (its callers — the constructor's `Initial*Troops` and the `Total*Troops` properties — now compute via participants). Remove it.

- [ ] **Step 3.2: Run the existing `ZoneBattle` test suite to verify backward compat**

```bash
dotnet test tests/FactionWars.Tests/FactionWars.Tests.csproj -c Debug --nologo --filter "FullyQualifiedName~ZoneBattleTests"
```

Expected: all green. The exact pass count should match what `ZoneBattleTests.cs` had before this change (~30-50 tests). If any fail, the refactor broke a backward-compat invariant — fix the refactor (don't edit the test).

- [ ] **Step 3.3: Run the manager test suite to verify nothing downstream broke**

```bash
dotnet test tests/FactionWars.Tests/FactionWars.Tests.csproj -c Debug --nologo --filter "FullyQualifiedName~ZoneBattleManager"
```

Expected: all green (including `ZoneBattleManagerTests` and `ZoneBattleManagerAllocationSyncTests`). These tests exercise `ZoneBattle` indirectly through `ZoneBattleManager` and would break if any forwarding accessor has the wrong semantics.

- [ ] **Step 3.4: Run the full suite for any other consumers**

```bash
dotnet test tests/FactionWars.Tests/FactionWars.Tests.csproj -c Debug --nologo -v minimal 2>&1 | grep -E "(Failed|Passed)" | tail -3
```

Expected: all green. Two `NativeSaveWatcherTests` are known flakes (pre-existing, unrelated, timing-sensitive file watcher) — if those fail, re-run them in isolation with `--filter "FullyQualifiedName~NativeSaveWatcherTests"` to confirm they're flake.

- [ ] **Step 3.5: Commit**

```bash
git add src/FactionWars/Combat/Models/ZoneBattle.cs
git commit -m "refactor: ZoneBattle uses participant list internally"
```

---

## Task 4: Tests for the new `Participants` / `Defender` / `Attackers` accessors

**Files:**
- Test: `tests/FactionWars.Tests/Unit/Combat/ZoneBattleParticipantsTests.cs`

These accessors are added in Task 3 but only tested implicitly via existing tests (which only see the legacy properties). This task adds direct unit tests for the new API, which Plan 2 will rely on heavily.

- [ ] **Step 4.1: Write the tests**

Create `tests/FactionWars.Tests/Unit/Combat/ZoneBattleParticipantsTests.cs`:

```csharp
using System.Collections.Generic;
using System.Linq;
using FactionWars.Combat.Models;
using FactionWars.Core.Models;
using Xunit;

namespace FactionWars.Tests.Unit.Combat
{
    public class ZoneBattleParticipantsTests
    {
        private static Dictionary<DefenderTier, int> Troops(int basic) =>
            new Dictionary<DefenderTier, int> { { DefenderTier.Basic, basic } };

        [Fact]
        public void Participants_HasExactlyOneDefenderAndOneAttacker()
        {
            var battle = new ZoneBattle("trevor", "michael", "zone_1", Troops(5), Troops(7));

            Assert.Equal(2, battle.Participants.Count);
            Assert.Single(battle.Participants.Where(p => p.Role == BattleRole.Defender));
            Assert.Single(battle.Participants.Where(p => p.Role == BattleRole.Attacker));
        }

        [Fact]
        public void Defender_ReturnsTheDefendingFactionParticipant()
        {
            var battle = new ZoneBattle("trevor", "michael", "zone_1", Troops(5), Troops(7));

            Assert.Equal("michael", battle.Defender.FactionId);
            Assert.Equal(BattleRole.Defender, battle.Defender.Role);
            Assert.Equal(7, battle.Defender.AliveCount);
        }

        [Fact]
        public void Attackers_ReturnsListContainingOnlyTheAttacker()
        {
            var battle = new ZoneBattle("trevor", "michael", "zone_1", Troops(5), Troops(7));

            Assert.Single(battle.Attackers);
            Assert.Equal("trevor", battle.Attackers[0].FactionId);
            Assert.Equal(BattleRole.Attacker, battle.Attackers[0].Role);
            Assert.Equal(5, battle.Attackers[0].AliveCount);
        }

        [Fact]
        public void LegacyAccessor_AttackerFactionId_AgreesWithAttackersList()
        {
            var battle = new ZoneBattle("trevor", "michael", "zone_1", Troops(5), Troops(7));

            Assert.Equal(battle.Attackers[0].FactionId, battle.AttackerFactionId);
        }

        [Fact]
        public void LegacyAccessor_DefenderFactionId_AgreesWithDefender()
        {
            var battle = new ZoneBattle("trevor", "michael", "zone_1", Troops(5), Troops(7));

            Assert.Equal(battle.Defender.FactionId, battle.DefenderFactionId);
        }

        [Fact]
        public void RemovingTroopsViaParticipant_UpdatesLegacyTotals()
        {
            // Mutating through the new participant API should update legacy totals.
            var battle = new ZoneBattle("trevor", "michael", "zone_1", Troops(3), Troops(3));

            battle.Defender.RemoveTroop(DefenderTier.Basic);

            Assert.Equal(2, battle.TotalDefenderTroops);
            Assert.Equal(2, battle.DefenderTroops[DefenderTier.Basic]);
        }

        [Fact]
        public void RemovingTroopsViaLegacyApi_UpdatesParticipantState()
        {
            // Mutating through the legacy API (RemoveAttackerTroop) should be
            // visible on the participant.
            var battle = new ZoneBattle("trevor", "michael", "zone_1", Troops(3), Troops(3));

            battle.RemoveAttackerTroop(DefenderTier.Basic);

            Assert.Equal(2, battle.Attackers[0].AliveCount);
        }

        [Fact]
        public void Participants_AreInDefenderThenAttackerOrder()
        {
            // Plan 2 readers may rely on the index 0 = defender ordering. Lock it in.
            var battle = new ZoneBattle("trevor", "michael", "zone_1", Troops(5), Troops(7));

            Assert.Equal(BattleRole.Defender, battle.Participants[0].Role);
            Assert.Equal(BattleRole.Attacker, battle.Participants[1].Role);
        }
    }
}
```

- [ ] **Step 4.2: Run them to verify they pass**

```bash
dotnet test tests/FactionWars.Tests/FactionWars.Tests.csproj -c Debug --nologo --filter "FullyQualifiedName~ZoneBattleParticipantsTests"
```

Expected: 8 passed. (Tests pass on first run because the implementation in Task 3 already supports them — these tests pin the contract for Plan 2.)

- [ ] **Step 4.3: Commit**

```bash
git add tests/FactionWars.Tests/Unit/Combat/ZoneBattleParticipantsTests.cs
git commit -m "test: pin ZoneBattle participant API for Plan 2"
```

---

## Task 5: Final verification & deploy

- [ ] **Step 5.1: Full suite green**

```bash
dotnet test tests/FactionWars.Tests/FactionWars.Tests.csproj -c Debug --nologo -v minimal 2>&1 | grep -E "(Failed|Passed)" | tail -3
```

Expected: full pass except possibly the known-flaky `NativeSaveWatcherTests.MultipleRapidWritesSamePath_DebouncesToOneEvent` and `SingleSave_FiresOneEvent`. If they fail, isolate-test them:

```bash
dotnet test tests/FactionWars.Tests/FactionWars.Tests.csproj -c Debug --nologo --filter "FullyQualifiedName~NativeSaveWatcherTests"
```

Expected in isolation: PASS (5/5).

- [ ] **Step 5.2: Build clean DLL**

```bash
dotnet build src/FactionWars/FactionWars.csproj -c Debug -v minimal 2>&1 | tail -3
```

Expected: 0 errors.

- [ ] **Step 5.3: Deploy DLL for in-game smoke test**

```bash
cp "C:/Users/ryan7/programming/gtav-factions/src/FactionWars/bin/Debug/net48/FactionWars.dll" "E:/SteamLibrary/steamapps/common/Grand Theft Auto V/scripts/" && echo "deployed"
```

Expected: `deployed`. No functional change is expected in-game — verifying the mod still loads and a battle still works (start an AI vs AI battle by allocating defenders to a zone owned by another faction, or just confirm no errors in `C:\Users\ryan7\Documents\FactionWars\Logs\FactionWars_*.log`).

---

## Spec Coverage Map (relative to `2026-05-03-3-way-zone-battles-design.md`)

| Spec Section | Covered In | Notes |
|---|---|---|
| §1 Data model — `BattleRole`, `BattleParticipant` | Tasks 1, 2 | Player-callback semantics implemented in Task 2 |
| §1 Data model — `ZoneBattle.Participants` / `Defender` / `Attackers` | Task 3 | Backward-compat accessors covered in same change |
| §1 Data model — manager-level cap | Plan 2 | `JoinAsAttacker` is in Plan 2; no enforcement needed yet |
| §2 Lifecycle (start/join/leave) | Plan 2 | Out of scope for Plan 1 |
| §3 Kill routing & victory in 3-way | Plan 2 | Out of scope |
| §4 `CombatManager` migration | Plan 2 | Out of scope |
| §5 UI + persistence — HUD | Plan 2 | Out of scope |
| §5 Persistence migration | n/a | `ZoneBattle` isn't persisted; no migration needed |
| §6 Faction-colored AI ped blips | Plan 2 | Out of scope |

End of Plan 1.
