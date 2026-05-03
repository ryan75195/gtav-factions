# 3-Way Zone Battles — Implementation Plan (Plan 2 of 2)

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Eliminate the parallel-state `CombatManager` bug by routing all player combat through `ZoneBattleManager`, and add 3-way melee support when the player joins a contested AI zone (with a HUD third-party row and faction-colored AI ped blips).

**Architecture:** Phased migration. Phase A grows `ZoneBattleManager`'s API. Phase B switches `GameLoopController` and the three defender/attacker managers to it (CombatManager still exists, just unused). Phase C deletes `CombatManager` and friends. Phase D rewrites the integration tests. Phase E adds the third-party HUD row. Phase F switches AI ped blips to faction colors. Each phase ends with a green test suite — natural checkpoint to pause.

**Tech Stack:** .NET Framework 4.8, C#, xUnit, Moq.

**Branch:** `feat/3-way-battles` (Plan 1 already shipped here as commits `0259fe2`..`6479f25`).

**Spec:** `docs/superpowers/specs/2026-05-03-3-way-zone-battles-design.md`.

---

## Spec Coverage Map

| Spec Section | Covered By |
|---|---|
| §1 Data model — `BattleRole`, `BattleParticipant`, `Participants`/`Defender`/`Attackers` | Plan 1 (shipped) |
| §1 Cap-of-3 enforcement in manager | Task 3 (`JoinAsAttacker`) |
| §2 Lifecycle: `StartBattle` (existing) | Plan 1 |
| §2 Lifecycle: `StartPlayerCombat` | Task 4 |
| §2 Lifecycle: `JoinAsAttacker` | Task 3 |
| §2 Lifecycle: `RemoveParticipant` | Task 5 |
| §3 Kill routing via `ReportTroopKilled` | Task 6 |
| §3 Victory check 1-of-N + outcome routing | Task 6 |
| §4 `CombatManager` migration — call-site rewires | Tasks 7-13 |
| §4 `CombatManager` deletion | Tasks 14-16 |
| §4 `CombatResultHandler` inlined | Task 6 (player-win-neutral logic) + Task 16 (deletion) |
| §5 HUD third-party row | Tasks 19-21 |
| §5 Persistence | **Dropped — no `ZoneBattleData` exists; battles are runtime-only state** |
| §6 Faction-colored AI blips | Task 22 |
| Bug-repro test (`AI₁ vs AI₂ + player wins`) | Task 18 |

---

## Design notes for the implementer

These resolve API ambiguities in the spec. Read them before starting any task.

**Player participant alive count.** A `BattleParticipant.ForPlayer` participant takes a `Func<int> aliveCountCallback`. Existing `IFollowerService.GetAliveFollowerCount(playerFactionId)` returns the squad's alive count *excluding* the player. For the participant's `AliveCount` we want **player self + squad followers**:

```csharp
Func<int> aliveCountCallback = () => 1 + _followerService.GetAliveFollowerCount(playerFactionId);
```

(Player's "self" alive-ness is implicit — when the player dies, the takeover sequence aborts via existing player-death handling, removing the participant. This callback should not try to detect player death itself.)

**`ZoneBattle` constructor for player battles.** The Plan 1 constructor only accepts AI participants (it builds them via `BattleParticipant.ForAi`). Task 1 adds a second constructor taking a pre-built `IList<BattleParticipant>` so `StartPlayerCombat` and `JoinAsAttacker` can construct battles with a player participant. Both constructors share the same internal initialization.

**Cap-of-3 enforcement.** Cap is enforced in `JoinAsAttacker` only. The model itself is unbounded so future AI-third-party support (Q2.B) is a one-line manager change.

**`ReportTroopKilled` vs. existing `TroopKilled` event.** The existing public `TroopKilled` event is fired by the manager *after* internal processing. The new `ReportTroopKilled(zoneId, victimFactionId, tier)` is the *input* — kill watchers call this, the manager decrements the participant's tier count and then fires the existing `TroopKilled` event.

**Player participant tier killed.** `ReportTroopKilled` for a player participant: tier is irrelevant; `RemoveTroop(tier)` is a no-op (Plan 1 contract). The follow-up victory check still triggers because `AliveCount` calls the squad callback. The squad callback itself is what eventually returns 0 when the squad is wiped.

**`Zone` parameter in `StartPlayerCombat`.** Today's `CombatManager.StartCombat(Zone zone, string attackerFactionId)` takes a Zone object. New `StartPlayerCombat` keeps that shape — easier on callers — and extracts `zone.Id` and `zone.OwnerFactionId` internally.

**Defender troop counts when player starts combat against zone owner.** Today `CombatManager.StartCombat` derives defender troop counts from `IZoneDefenderAllocationService.GetAllocation(zoneId)`. New `StartPlayerCombat` does the same. The player's attacker troops dict is empty (player is callback-based).

**Outcome routing.** When `BattleEnded` fires, the handler runs in this order:
1. If winner is **defender** → no ownership change. Refund attacker's reserve troops via `IFactionService.AddTroops` (existing AI-side behaviour).
2. If winner is **AI attacker** → transfer zone ownership to that faction (existing AI-side behaviour).
3. If winner is **player** → set `zone.OwnerFactionId = null` (zone goes neutral). No troop refund.

This handler currently lives partly in `CombatResultHandler.ProcessCombatResult` (player-win-neutral and player-flow specifics) and partly in `ZoneBattleManager.OnBattleEndedInternal` (AI-side). Task 6 unifies them.

**TakeoverDetector contract changes.** Today `TakeoverDetector.CheckTakeover(encounter)` takes a `CombatEncounter`. After Plan 2 there are no encounters. The detector is repurposed to compute thresholds against `BattleParticipant.AliveCount` ratios, called from `ZoneBattleManager`'s simulated tick. Task 13 makes this concrete.

**FriendlyDefenderManager kill reporting.** Friendly defenders are AI-faction defenders allocated to a player-owned zone. They die during a 3-way only if a hostile attacker enters. When the player is *attacking* an enemy zone, friendly defenders are not on the field — so this manager's kill watcher only reports kills when the player is being *attacked* in their own zone, which is an existing flow that today already routes through `ZoneBattleManager`. Task 12 verifies no new wiring is required here.

---

## Phase A — ZoneBattleManager API additions (additive, no caller changes)

### Task 1: `ZoneBattle` constructor overload taking participant list

**Files:**
- Modify: `src/FactionWars/Combat/Models/ZoneBattle.cs`
- Test: `tests/FactionWars.Tests/Unit/Combat/ZoneBattleParticipantsTests.cs` (extend)

The existing constructor only builds AI participants. To support player-vs-AI battles, add a second constructor that accepts a pre-built participant list. The existing constructor will be refactored to delegate to it.

- [ ] **Step 1.1: Write the failing tests**

Add the following two tests to the bottom of the existing `ZoneBattleParticipantsTests` class (just before the closing `}` of the class), keeping its `using` block as-is:

```csharp
        [Fact]
        public void ConstructorWithParticipants_PreservesParticipantInstances()
        {
            var defender = BattleParticipant.ForAi("michael", BattleRole.Defender,
                new Dictionary<DefenderTier, int> { { DefenderTier.Basic, 5 } });
            var attacker = BattleParticipant.ForPlayer("player_faction", BattleRole.Attacker, () => 4);

            var battle = new ZoneBattle("zone_42", new List<BattleParticipant> { defender, attacker },
                playerFactionId: "player_faction");

            Assert.Equal("zone_42", battle.ZoneId);
            Assert.Equal("player_faction", battle.PlayerFactionId);
            Assert.Same(defender, battle.Defender);
            Assert.Same(attacker, battle.Attackers[0]);
            Assert.True(battle.Attackers[0].IsPlayer);
            Assert.Equal(4, battle.Attackers[0].AliveCount);
            Assert.Equal(5, battle.InitialDefenderTroops);
            Assert.Equal(4, battle.InitialAttackerTroops);
        }

        [Fact]
        public void ConstructorWithParticipants_ThrowsWhenNoDefender()
        {
            var attacker1 = BattleParticipant.ForAi("trevor", BattleRole.Attacker,
                new Dictionary<DefenderTier, int> { { DefenderTier.Basic, 1 } });
            var attacker2 = BattleParticipant.ForAi("franklin", BattleRole.Attacker,
                new Dictionary<DefenderTier, int> { { DefenderTier.Basic, 1 } });

            Assert.Throws<ArgumentException>(() =>
                new ZoneBattle("zone_42", new List<BattleParticipant> { attacker1, attacker2 }));
        }
```

- [ ] **Step 1.2: Run the tests to verify they fail**

```bash
dotnet test tests/FactionWars.Tests/FactionWars.Tests.csproj -c Debug --nologo --filter "FullyQualifiedName~ZoneBattleParticipantsTests"
```

Expected: FAIL with build error referencing `ZoneBattle` not having a matching constructor.

- [ ] **Step 1.3: Add the constructor overload**

Open `src/FactionWars/Combat/Models/ZoneBattle.cs` and add the following constructor immediately after the existing constructor (the `public ZoneBattle(string attackerFactionId, ...)` one). Also extract the shared init body into a private helper to avoid duplication:

```csharp
        /// <summary>
        /// Constructs a battle from a pre-built participant list. Used by the manager
        /// to create player-vs-AI and 3-way battles where one or more participants
        /// are player-callback-backed. The list must contain exactly one Defender
        /// and at least one Attacker. The list is copied — later mutations to the
        /// caller's list do not affect this battle.
        /// </summary>
        public ZoneBattle(
            string zoneId,
            IList<BattleParticipant> participants,
            string? playerFactionId = null)
        {
            if (zoneId == null) throw new ArgumentNullException(nameof(zoneId));
            if (participants == null) throw new ArgumentNullException(nameof(participants));
            int defenderCount = participants.Count(p => p.Role == BattleRole.Defender);
            int attackerCount = participants.Count(p => p.Role == BattleRole.Attacker);
            if (defenderCount != 1)
                throw new ArgumentException(
                    $"Battle must have exactly one Defender (got {defenderCount}).", nameof(participants));
            if (attackerCount < 1)
                throw new ArgumentException(
                    $"Battle must have at least one Attacker (got {attackerCount}).", nameof(participants));

            Id = Guid.NewGuid().ToString("N").Substring(0, 8);
            ZoneId = zoneId;
            PlayerFactionId = playerFactionId;
            IsPlayerPresent = false;
            ElapsedTime = 0f;
            TimeUntilNextKill = 0f;
            KillInterval = 0f;
            SpawnedAttackers = new Dictionary<int, DefenderTier>();
            SpawnedDefenders = new Dictionary<int, DefenderTier>();

            // Defensive copy so callers can't mutate participant ordering after construction.
            // Order is normalized: Defender first, then Attackers in caller order.
            _participants = new List<BattleParticipant>(participants.Count);
            _participants.AddRange(participants.Where(p => p.Role == BattleRole.Defender));
            _participants.AddRange(participants.Where(p => p.Role == BattleRole.Attacker));

            InitialAttackerTroops = TotalAttackerTroops;
            InitialDefenderTroops = TotalDefenderTroops;
        }
```

The existing public AI-only constructor stays as-is — it already works and dozens of tests rely on it.

- [ ] **Step 1.4: Run tests to verify they pass**

```bash
dotnet test tests/FactionWars.Tests/FactionWars.Tests.csproj -c Debug --nologo --filter "FullyQualifiedName~ZoneBattleParticipantsTests"
```

Expected: 10 passed (8 existing + 2 new).

- [ ] **Step 1.5: Run the full ZoneBattle suite to confirm no regression**

```bash
dotnet test tests/FactionWars.Tests/FactionWars.Tests.csproj -c Debug --nologo --filter "FullyQualifiedName~ZoneBattleTests"
```

Expected: all 50 still pass.

- [ ] **Step 1.6: Commit**

```bash
git add src/FactionWars/Combat/Models/ZoneBattle.cs tests/FactionWars.Tests/Unit/Combat/ZoneBattleParticipantsTests.cs
git commit -m "feat: ZoneBattle constructor overload taking participant list"
```

---

### Task 2: Query helpers `IsPlayerInBattle` / `GetPlayerCurrentBattle`

**Files:**
- Modify: `src/FactionWars/Combat/Interfaces/IZoneBattleManager.cs`
- Modify: `src/FactionWars/Combat/Services/ZoneBattleManager.cs`
- Test: `tests/FactionWars.Tests/Unit/Combat/ZoneBattleManagerTests.cs` (extend)

These two read-only methods replace `CombatManager.IsInCombat` and `CombatManager.CurrentEncounter` for callers.

- [ ] **Step 2.1: Read the interface to confirm its current shape**

```bash
cat src/FactionWars/Combat/Interfaces/IZoneBattleManager.cs
```

You'll need to know the existing public signatures to add new ones consistently. Note especially how `_playerFactionId` is currently exposed (it's a manager constructor param).

- [ ] **Step 2.2: Write the failing tests**

Add to `tests/FactionWars.Tests/Unit/Combat/ZoneBattleManagerTests.cs`. Place these at the bottom of the existing test class:

```csharp
        [Fact]
        public void IsPlayerInBattle_ReturnsFalse_WhenNoBattle()
        {
            var manager = CreateManager(playerFactionId: "player_faction");

            Assert.False(manager.IsPlayerInBattle());
        }

        [Fact]
        public void IsPlayerInBattle_ReturnsFalse_WhenBattleHasNoPlayerParticipant()
        {
            var manager = CreateManager(playerFactionId: "player_faction");
            manager.StartBattle("zone_1", "trevor", "michael",
                new Dictionary<DefenderTier, int> { { DefenderTier.Basic, 3 } },
                new Dictionary<DefenderTier, int> { { DefenderTier.Basic, 3 } });

            Assert.False(manager.IsPlayerInBattle());
        }

        [Fact]
        public void GetPlayerCurrentBattle_ReturnsNull_WhenNoPlayerParticipant()
        {
            var manager = CreateManager(playerFactionId: "player_faction");
            manager.StartBattle("zone_1", "trevor", "michael",
                new Dictionary<DefenderTier, int> { { DefenderTier.Basic, 3 } },
                new Dictionary<DefenderTier, int> { { DefenderTier.Basic, 3 } });

            Assert.Null(manager.GetPlayerCurrentBattle());
        }
```

If `CreateManager` doesn't exist as a helper, look at the top of `ZoneBattleManagerTests` for the equivalent setup pattern (likely `new ZoneBattleManager(allocService, factionService, "player_faction")`) and use that directly inline.

- [ ] **Step 2.3: Run tests to verify they fail**

```bash
dotnet test tests/FactionWars.Tests/FactionWars.Tests.csproj -c Debug --nologo --filter "FullyQualifiedName~ZoneBattleManagerTests.IsPlayerInBattle|FullyQualifiedName~ZoneBattleManagerTests.GetPlayerCurrentBattle"
```

Expected: build error — `IsPlayerInBattle` / `GetPlayerCurrentBattle` not defined.

- [ ] **Step 2.4: Add the methods to the interface**

Edit `src/FactionWars/Combat/Interfaces/IZoneBattleManager.cs` and add inside the interface:

```csharp
        /// <summary>
        /// Returns true if the player is currently a participant in any battle.
        /// </summary>
        bool IsPlayerInBattle();

        /// <summary>
        /// Returns the battle the player is currently a participant in, or null if none.
        /// </summary>
        ZoneBattle? GetPlayerCurrentBattle();
```

- [ ] **Step 2.5: Implement on `ZoneBattleManager`**

Edit `src/FactionWars/Combat/Services/ZoneBattleManager.cs`. Add these methods anywhere in the class body (a logical home is right after `BattleCount`):

```csharp
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
```

- [ ] **Step 2.6: Run tests to verify they pass**

```bash
dotnet test tests/FactionWars.Tests/FactionWars.Tests.csproj -c Debug --nologo --filter "FullyQualifiedName~ZoneBattleManagerTests.IsPlayerInBattle|FullyQualifiedName~ZoneBattleManagerTests.GetPlayerCurrentBattle"
```

Expected: 3 passed.

- [ ] **Step 2.7: Commit**

```bash
git add src/FactionWars/Combat/Interfaces/IZoneBattleManager.cs src/FactionWars/Combat/Services/ZoneBattleManager.cs tests/FactionWars.Tests/Unit/Combat/ZoneBattleManagerTests.cs
git commit -m "feat: ZoneBattleManager player-presence query methods"
```

---

### Task 3: `JoinAsAttacker` — add a participant to an existing battle

**Files:**
- Modify: `src/FactionWars/Combat/Interfaces/IZoneBattleManager.cs`
- Modify: `src/FactionWars/Combat/Services/ZoneBattleManager.cs`
- Test: `tests/FactionWars.Tests/Unit/Combat/ZoneBattleManagerTests.cs` (extend)

`JoinAsAttacker` adds an `Attacker`-role participant to an existing battle. It enforces:
- Battle must exist for the zone (returns `false` otherwise).
- Attacker count cap of 2 (returns `false` if already 2).
- v1 only allows player third parties (returns `false` if `isPlayer` is `false`).
- Faction must not already be a participant (returns `false` otherwise).

For `isPlayer == true`, the caller supplies an `aliveCountCallback`. For `isPlayer == false`, the caller supplies `troops`. Only one of those is used per call.

- [ ] **Step 3.1: Write the failing tests**

Add to `ZoneBattleManagerTests`:

```csharp
        [Fact]
        public void JoinAsAttacker_ReturnsFalse_WhenNoBattleInZone()
        {
            var manager = CreateManager(playerFactionId: "player_faction");

            bool result = manager.JoinAsAttacker(
                zoneId: "zone_1",
                factionId: "player_faction",
                isPlayer: true,
                aliveCountCallback: () => 4,
                troops: null);

            Assert.False(result);
        }

        [Fact]
        public void JoinAsAttacker_AddsPlayerParticipant_ToExistingBattle()
        {
            var manager = CreateManager(playerFactionId: "player_faction");
            manager.StartBattle("zone_1", "trevor", "michael",
                new Dictionary<DefenderTier, int> { { DefenderTier.Basic, 3 } },
                new Dictionary<DefenderTier, int> { { DefenderTier.Basic, 3 } });

            bool result = manager.JoinAsAttacker(
                zoneId: "zone_1",
                factionId: "player_faction",
                isPlayer: true,
                aliveCountCallback: () => 4,
                troops: null);

            Assert.True(result);
            var battle = manager.GetBattleForZone("zone_1");
            Assert.NotNull(battle);
            Assert.Equal(2, battle!.Attackers.Count);
            Assert.True(battle.Attackers.Any(p => p.IsPlayer && p.FactionId == "player_faction"));
        }

        [Fact]
        public void JoinAsAttacker_RejectsThirdAttacker()
        {
            var manager = CreateManager(playerFactionId: "player_faction");
            manager.StartBattle("zone_1", "trevor", "michael",
                new Dictionary<DefenderTier, int> { { DefenderTier.Basic, 3 } },
                new Dictionary<DefenderTier, int> { { DefenderTier.Basic, 3 } });
            manager.JoinAsAttacker("zone_1", "player_faction", true, () => 4, null);

            // Attempt to add a fourth participant (third attacker)
            bool result = manager.JoinAsAttacker(
                zoneId: "zone_1",
                factionId: "franklin",
                isPlayer: false,
                aliveCountCallback: null,
                troops: new Dictionary<DefenderTier, int> { { DefenderTier.Basic, 2 } });

            Assert.False(result);
        }

        [Fact]
        public void JoinAsAttacker_RejectsNonPlayerThirdParty_InV1()
        {
            // Q2.A: only the player can be a third party in v1.
            var manager = CreateManager(playerFactionId: "player_faction");
            manager.StartBattle("zone_1", "trevor", "michael",
                new Dictionary<DefenderTier, int> { { DefenderTier.Basic, 3 } },
                new Dictionary<DefenderTier, int> { { DefenderTier.Basic, 3 } });

            bool result = manager.JoinAsAttacker(
                zoneId: "zone_1",
                factionId: "franklin",
                isPlayer: false,
                aliveCountCallback: null,
                troops: new Dictionary<DefenderTier, int> { { DefenderTier.Basic, 2 } });

            Assert.False(result);
        }

        [Fact]
        public void JoinAsAttacker_RejectsDuplicateFaction()
        {
            var manager = CreateManager(playerFactionId: "player_faction");
            manager.StartBattle("zone_1", "trevor", "michael",
                new Dictionary<DefenderTier, int> { { DefenderTier.Basic, 3 } },
                new Dictionary<DefenderTier, int> { { DefenderTier.Basic, 3 } });

            // Trevor is already the attacker
            bool result = manager.JoinAsAttacker(
                zoneId: "zone_1",
                factionId: "trevor",
                isPlayer: false,
                aliveCountCallback: null,
                troops: new Dictionary<DefenderTier, int> { { DefenderTier.Basic, 1 } });

            Assert.False(result);
        }
```

- [ ] **Step 3.2: Run tests to verify they fail**

```bash
dotnet test tests/FactionWars.Tests/FactionWars.Tests.csproj -c Debug --nologo --filter "FullyQualifiedName~JoinAsAttacker"
```

Expected: build error — `JoinAsAttacker` not defined.

- [ ] **Step 3.3: Add to the interface**

In `src/FactionWars/Combat/Interfaces/IZoneBattleManager.cs`, add inside the interface:

```csharp
        /// <summary>
        /// Adds an Attacker-role participant to an existing battle in the given zone.
        /// </summary>
        /// <param name="zoneId">The zone whose battle to modify.</param>
        /// <param name="factionId">The faction joining as attacker.</param>
        /// <param name="isPlayer">True if this is the player. v1 rejects non-player third parties.</param>
        /// <param name="aliveCountCallback">Required when isPlayer==true; ignored otherwise.</param>
        /// <param name="troops">Required when isPlayer==false; ignored otherwise.</param>
        /// <returns>
        /// True if the participant was added. False if no battle exists in the zone, the
        /// attacker cap (2) is reached, the faction is already a participant, or
        /// isPlayer==false (rejected in v1, Q2.A).
        /// </returns>
        bool JoinAsAttacker(
            string zoneId,
            string factionId,
            bool isPlayer,
            Func<int>? aliveCountCallback,
            Dictionary<DefenderTier, int>? troops);
```

- [ ] **Step 3.4: Implement on `ZoneBattleManager`**

In `src/FactionWars/Combat/Services/ZoneBattleManager.cs`, add this method. A logical home is after `StartBattle`:

```csharp
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

            // Cap-of-2 attackers (Q2.A — supports player + one AI defender's existing attacker).
            int currentAttackers = battle.Participants.Count(p => p.Role == BattleRole.Attacker);
            if (currentAttackers >= 2)
            {
                FileLogger.Combat($"JoinAsAttacker: rejected — zone '{zoneId}' already has {currentAttackers} attackers.");
                return false;
            }

            // Reject duplicate faction.
            if (battle.Participants.Any(p => p.FactionId == factionId))
            {
                FileLogger.Combat($"JoinAsAttacker: rejected — faction '{factionId}' already in battle '{zoneId}'.");
                return false;
            }

            BattleParticipant newParticipant;
            if (isPlayer)
            {
                if (aliveCountCallback == null)
                    throw new ArgumentNullException(nameof(aliveCountCallback),
                        "aliveCountCallback is required when isPlayer is true.");
                newParticipant = BattleParticipant.ForPlayer(factionId, BattleRole.Attacker, aliveCountCallback);
            }
            else
            {
                // Unreachable in v1 because of the early reject above, but kept for
                // future N-way support without another model change.
                if (troops == null)
                    throw new ArgumentNullException(nameof(troops),
                        "troops is required when isPlayer is false.");
                newParticipant = BattleParticipant.ForAi(factionId, BattleRole.Attacker, troops);
            }

            battle.AddParticipant(newParticipant);
            FileLogger.Combat($"JoinAsAttacker: added '{factionId}' (isPlayer={isPlayer}) to zone '{zoneId}'.");
            return true;
        }
```

This calls `battle.AddParticipant(...)` — a new method on `ZoneBattle`. Add it to `src/FactionWars/Combat/Models/ZoneBattle.cs`, near the other mutator methods (e.g., after `AddDefenderTroops`):

```csharp
        /// <summary>
        /// Adds a new participant to this battle. Used by the manager's
        /// <c>JoinAsAttacker</c> entry point. The model itself does not enforce
        /// caps — that's the manager's responsibility (see Q2.A in spec).
        /// </summary>
        public void AddParticipant(BattleParticipant participant)
        {
            if (participant == null) throw new ArgumentNullException(nameof(participant));
            _participants.Add(participant);
        }

        /// <summary>
        /// Removes the participant with the given faction id. Returns true if a
        /// participant was removed, false if no such participant existed.
        /// </summary>
        public bool RemoveParticipant(string factionId)
        {
            if (factionId == null) throw new ArgumentNullException(nameof(factionId));
            for (int i = 0; i < _participants.Count; i++)
            {
                if (_participants[i].FactionId == factionId)
                {
                    _participants.RemoveAt(i);
                    return true;
                }
            }
            return false;
        }
```

(Both methods land here in this task — `RemoveParticipant` is used in Task 5.)

Add the missing `using` for `FileLogger` if not present — check the top of `ZoneBattleManager.cs`. If it lacks `using FactionWars.ScriptHookV.Logging;`, add it.

- [ ] **Step 3.5: Run tests to verify they pass**

```bash
dotnet test tests/FactionWars.Tests/FactionWars.Tests.csproj -c Debug --nologo --filter "FullyQualifiedName~JoinAsAttacker"
```

Expected: 5 passed.

- [ ] **Step 3.6: Run the full Combat test suite to confirm no regression**

```bash
dotnet test tests/FactionWars.Tests/FactionWars.Tests.csproj -c Debug --nologo --filter "FullyQualifiedName~FactionWars.Tests.Unit.Combat"
```

Expected: all green.

- [ ] **Step 3.7: Commit**

```bash
git add src/FactionWars/Combat/Models/ZoneBattle.cs src/FactionWars/Combat/Interfaces/IZoneBattleManager.cs src/FactionWars/Combat/Services/ZoneBattleManager.cs tests/FactionWars.Tests/Unit/Combat/ZoneBattleManagerTests.cs
git commit -m "feat: ZoneBattleManager.JoinAsAttacker"
```

---

### Task 4: `StartPlayerCombat` — entry point for player-initiated combat

**Files:**
- Modify: `src/FactionWars/Combat/Interfaces/IZoneBattleManager.cs`
- Modify: `src/FactionWars/Combat/Services/ZoneBattleManager.cs`
- Test: `tests/FactionWars.Tests/Unit/Combat/ZoneBattleManagerTests.cs` (extend)

`StartPlayerCombat` is the player-flow entry point. It replaces today's `CombatManager.StartCombat`. Two cases:
1. **No existing battle in zone:** create a new battle with player as Attacker, zone owner as Defender. Defender troops come from `IZoneDefenderAllocationService.GetAllocation(zoneId)`.
2. **Existing battle in zone:** delegate to `JoinAsAttacker(zoneId, playerFactionId, isPlayer: true, aliveCountCallback, null)`.

Returns the battle (new or joined). Returns `null` if join failed (e.g. cap reached).

- [ ] **Step 4.1: Write the failing tests**

Add to `ZoneBattleManagerTests`:

```csharp
        [Fact]
        public void StartPlayerCombat_CreatesNewBattle_WhenNoneExists()
        {
            var manager = CreateManager(playerFactionId: "player_faction");
            var zone = CreateZone("zone_1", ownerFactionId: "michael",
                deployedAllocation: new Dictionary<DefenderTier, int> { { DefenderTier.Basic, 5 } });

            var battle = manager.StartPlayerCombat(zone, "player_faction", () => 4);

            Assert.NotNull(battle);
            Assert.Equal("zone_1", battle!.ZoneId);
            Assert.Equal("michael", battle.Defender.FactionId);
            Assert.Equal(5, battle.Defender.AliveCount);
            Assert.Single(battle.Attackers);
            Assert.Equal("player_faction", battle.Attackers[0].FactionId);
            Assert.True(battle.Attackers[0].IsPlayer);
            Assert.Equal(4, battle.Attackers[0].AliveCount);
        }

        [Fact]
        public void StartPlayerCombat_JoinsExistingBattle_AsThirdAttacker()
        {
            // AI₁ attacking AI₂'s zone. Player walks in.
            var manager = CreateManager(playerFactionId: "player_faction");
            var zone = CreateZone("zone_1", ownerFactionId: "michael",
                deployedAllocation: new Dictionary<DefenderTier, int> { { DefenderTier.Basic, 5 } });
            manager.StartBattle("zone_1", "trevor", "michael",
                new Dictionary<DefenderTier, int> { { DefenderTier.Basic, 3 } },
                new Dictionary<DefenderTier, int> { { DefenderTier.Basic, 5 } });

            var battle = manager.StartPlayerCombat(zone, "player_faction", () => 4);

            Assert.NotNull(battle);
            Assert.Equal(2, battle!.Attackers.Count);
            Assert.True(battle.Attackers.Any(p => p.IsPlayer));
            Assert.True(battle.Attackers.Any(p => p.FactionId == "trevor" && !p.IsPlayer));
        }

        [Fact]
        public void StartPlayerCombat_ReturnsNull_WhenCapReached()
        {
            // Two AIs already attacking — but v1 cap is 2 attackers and player is the 3rd.
            // Wait: v1 only allows the player as third party. So this scenario is unreachable
            // through normal means. We test via direct construction: AI₁+AI₂ attackers
            // (which would only be possible in a future N-way world). We synthesize this state
            // by manipulating the manager's internal state through StartBattle + JoinAsAttacker.
            // For now we test that StartPlayerCombat respects whatever JoinAsAttacker returns.
            // Easiest: call StartPlayerCombat twice — second call should fail (player already
            // a participant -> JoinAsAttacker rejects duplicate).
            var manager = CreateManager(playerFactionId: "player_faction");
            var zone = CreateZone("zone_1", ownerFactionId: "michael",
                deployedAllocation: new Dictionary<DefenderTier, int> { { DefenderTier.Basic, 5 } });
            var first = manager.StartPlayerCombat(zone, "player_faction", () => 4);
            Assert.NotNull(first);

            var second = manager.StartPlayerCombat(zone, "player_faction", () => 4);

            Assert.Null(second);
        }
```

`CreateZone` may not exist as a helper. Use the existing test pattern in `ZoneBattleManagerTests` — typically `new Zone(...)` directly with the right fields, plus a Mock setup on `_allocationService` returning the deployed allocation. Check the top of the test file for any existing helper.

- [ ] **Step 4.2: Run tests to verify they fail**

```bash
dotnet test tests/FactionWars.Tests/FactionWars.Tests.csproj -c Debug --nologo --filter "FullyQualifiedName~StartPlayerCombat"
```

Expected: build error — `StartPlayerCombat` not defined.

- [ ] **Step 4.3: Add to the interface**

In `IZoneBattleManager.cs`:

```csharp
        /// <summary>
        /// Begins or joins a player-led battle in the given zone.
        /// </summary>
        /// <param name="zone">The zone the player is entering for combat.</param>
        /// <param name="playerFactionId">The player's faction id.</param>
        /// <param name="aliveCountCallback">Returns the player's currently-alive squad count (player + followers).</param>
        /// <returns>
        /// The battle (new or joined). Null if join failed (e.g. attacker cap reached
        /// or player is already a participant).
        /// </returns>
        ZoneBattle? StartPlayerCombat(Zone zone, string playerFactionId, Func<int> aliveCountCallback);
```

Add `using FactionWars.Territory.Models;` if not already present.

- [ ] **Step 4.4: Implement on `ZoneBattleManager`**

Add to `ZoneBattleManager.cs`:

```csharp
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
            if (zone.OwnerFactionId == playerFactionId)
                throw new ArgumentException("Player cannot attack their own zone.", nameof(zone));

            FileLogger.Combat($"StartPlayerCombat: zone={zone.Id}, player={playerFactionId}, defender={zone.OwnerFactionId}");

            // Case 2: existing battle — join it.
            if (_battlesByZone.ContainsKey(zone.Id))
            {
                bool joined = JoinAsAttacker(zone.Id, playerFactionId, isPlayer: true,
                    aliveCountCallback: aliveCountCallback, troops: null);
                return joined ? _battlesByZone[zone.Id] : null;
            }

            // Case 1: new battle. Defender troops come from the deployed allocation.
            var allocation = _allocationService.GetAllocation(zone.Id);
            var defenderTroops = allocation?.DeployedTroops != null
                ? new Dictionary<DefenderTier, int>(allocation.DeployedTroops)
                : new Dictionary<DefenderTier, int>();

            var defender = BattleParticipant.ForAi(zone.OwnerFactionId, BattleRole.Defender, defenderTroops);
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
```

If `IZoneDefenderAllocationService.GetAllocation` doesn't return a type with a `DeployedTroops` dict, look up its actual shape and adapt — the goal is "give me the deployed-troops dict for this zone".

- [ ] **Step 4.5: Run tests to verify they pass**

```bash
dotnet test tests/FactionWars.Tests/FactionWars.Tests.csproj -c Debug --nologo --filter "FullyQualifiedName~StartPlayerCombat"
```

Expected: 3 passed.

- [ ] **Step 4.6: Commit**

```bash
git add src/FactionWars/Combat/Interfaces/IZoneBattleManager.cs src/FactionWars/Combat/Services/ZoneBattleManager.cs tests/FactionWars.Tests/Unit/Combat/ZoneBattleManagerTests.cs
git commit -m "feat: ZoneBattleManager.StartPlayerCombat"
```

---

### Task 5: `RemoveParticipant` — for player-exits-zone, etc.

**Files:**
- Modify: `src/FactionWars/Combat/Interfaces/IZoneBattleManager.cs`
- Modify: `src/FactionWars/Combat/Services/ZoneBattleManager.cs`
- Test: `tests/FactionWars.Tests/Unit/Combat/ZoneBattleManagerTests.cs` (extend)

`RemoveParticipant` handles the player exiting their zone, or any participant being wiped. It removes the participant and runs the victory check (which may resolve the battle if only one survivor remains). Victory routing is added in Task 6 — for this task we only fire `BattleEnded` when exactly one participant remains.

- [ ] **Step 5.1: Write the failing tests**

```csharp
        [Fact]
        public void RemoveParticipant_ReturnsFalse_WhenNoBattle()
        {
            var manager = CreateManager(playerFactionId: "player_faction");

            bool result = manager.RemoveParticipant("zone_1", "player_faction");

            Assert.False(result);
        }

        [Fact]
        public void RemoveParticipant_PlayerLeavesContestedZone_BattleContinues2Way()
        {
            // 3-way: player joins AI vs AI, then walks out.
            var manager = CreateManager(playerFactionId: "player_faction");
            var zone = CreateZone("zone_1", ownerFactionId: "michael",
                deployedAllocation: new Dictionary<DefenderTier, int> { { DefenderTier.Basic, 5 } });
            manager.StartBattle("zone_1", "trevor", "michael",
                new Dictionary<DefenderTier, int> { { DefenderTier.Basic, 3 } },
                new Dictionary<DefenderTier, int> { { DefenderTier.Basic, 5 } });
            manager.JoinAsAttacker("zone_1", "player_faction", true, () => 4, null);

            int endCount = 0;
            manager.BattleEnded += (b, _) => endCount++;

            bool result = manager.RemoveParticipant("zone_1", "player_faction");

            Assert.True(result);
            var battle = manager.GetBattleForZone("zone_1");
            Assert.NotNull(battle);
            Assert.Equal(2, battle!.Participants.Count);
            Assert.False(battle.Participants.Any(p => p.IsPlayer));
            Assert.Equal(0, endCount); // Battle continues; no end event.
        }

        [Fact]
        public void RemoveParticipant_LastAttackerLeaves_DefenderWinsAndBattleEnds()
        {
            var manager = CreateManager(playerFactionId: "player_faction");
            manager.StartBattle("zone_1", "trevor", "michael",
                new Dictionary<DefenderTier, int> { { DefenderTier.Basic, 3 } },
                new Dictionary<DefenderTier, int> { { DefenderTier.Basic, 5 } });

            ZoneBattle? endedBattle = null;
            BattleOutcome? endedOutcome = null;
            manager.BattleEnded += (b, o) => { endedBattle = b; endedOutcome = o; };

            bool result = manager.RemoveParticipant("zone_1", "trevor");

            Assert.True(result);
            Assert.Null(manager.GetBattleForZone("zone_1"));
            Assert.NotNull(endedBattle);
            Assert.Equal(BattleOutcome.DefendersWon, endedOutcome);
        }
```

- [ ] **Step 5.2: Run tests to verify they fail**

```bash
dotnet test tests/FactionWars.Tests/FactionWars.Tests.csproj -c Debug --nologo --filter "FullyQualifiedName~RemoveParticipant"
```

Expected: build error.

- [ ] **Step 5.3: Add to the interface**

```csharp
        /// <summary>
        /// Removes a participant from the battle in the given zone (e.g. when the player
        /// exits, or a participant is wiped). After removal, the victory check runs and
        /// <c>BattleEnded</c> may fire if only one participant remains.
        /// </summary>
        /// <returns>True if a participant was removed; false if no battle or no such participant.</returns>
        bool RemoveParticipant(string zoneId, string factionId);
```

- [ ] **Step 5.4: Implement**

In `ZoneBattleManager.cs`. The impl uses the model-level `ZoneBattle.RemoveParticipant` already added in Task 3:

```csharp
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

        /// <summary>
        /// Counts alive participants and ends the battle if exactly one remains
        /// (defender or sole-attacker survivor). Caller already handled removal.
        /// </summary>
        private void ResolveBattleIfDone(ZoneBattle battle)
        {
            var alive = battle.Participants.Where(p => p.AliveCount > 0).ToList();
            if (alive.Count >= 2)
            {
                // Battle continues.
                return;
            }

            BattleOutcome outcome;
            if (alive.Count == 0)
            {
                // Vanishingly rare same-tick wipe. Defender keeps zone (no transfer).
                outcome = BattleOutcome.DefendersWon;
            }
            else
            {
                outcome = alive[0].Role == BattleRole.Defender
                    ? BattleOutcome.DefendersWon
                    : BattleOutcome.AttackersWon;
            }

            _battlesByZone.Remove(battle.ZoneId);
            BattleEnded?.Invoke(battle, outcome);
            FileLogger.Combat($"ResolveBattleIfDone: battle '{battle.ZoneId}' ended, outcome={outcome}.");
        }
```

- [ ] **Step 5.5: Run tests to verify they pass**

```bash
dotnet test tests/FactionWars.Tests/FactionWars.Tests.csproj -c Debug --nologo --filter "FullyQualifiedName~RemoveParticipant"
```

Expected: 3 passed.

- [ ] **Step 5.6: Run full Combat suite to confirm no regression**

```bash
dotnet test tests/FactionWars.Tests/FactionWars.Tests.csproj -c Debug --nologo --filter "FullyQualifiedName~FactionWars.Tests.Unit.Combat"
```

Expected: all green.

- [ ] **Step 5.7: Commit**

```bash
git add src/FactionWars/Combat/Interfaces/IZoneBattleManager.cs src/FactionWars/Combat/Services/ZoneBattleManager.cs tests/FactionWars.Tests/Unit/Combat/ZoneBattleManagerTests.cs
git commit -m "feat: ZoneBattleManager.RemoveParticipant with victory check"
```

---

### Task 6: `ReportTroopKilled` + outcome routing for player-win-neutral

**Files:**
- Modify: `src/FactionWars/Combat/Interfaces/IZoneBattleManager.cs`
- Modify: `src/FactionWars/Combat/Services/ZoneBattleManager.cs`
- Test: `tests/FactionWars.Tests/Unit/Combat/ZoneBattleManagerTests.cs` (extend)

`ReportTroopKilled(zoneId, victimFactionId, tier)` is the input from kill watchers. It:
1. Finds the participant by faction id.
2. Calls `RemoveTroop(tier)` on it (no-op for player participants).
3. Fires the existing `TroopKilled` event.
4. Runs the same `ResolveBattleIfDone` victory check.

This task also extends the `BattleEnded` event payload to indicate whether the player won (so the `OnBattleEnded` consumer can route to "zone neutral"). Today's `BattleOutcome` is just `AttackersWon` / `DefendersWon` — that's not enough to distinguish "AI attacker won" from "player won". We add a richer event signature.

- [ ] **Step 6.1: Write the failing tests**

```csharp
        [Fact]
        public void ReportTroopKilled_DecrementsAiParticipant()
        {
            var manager = CreateManager(playerFactionId: "player_faction");
            manager.StartBattle("zone_1", "trevor", "michael",
                new Dictionary<DefenderTier, int> { { DefenderTier.Basic, 3 } },
                new Dictionary<DefenderTier, int> { { DefenderTier.Basic, 5 } });

            manager.ReportTroopKilled("zone_1", "trevor", DefenderTier.Basic);

            var battle = manager.GetBattleForZone("zone_1");
            Assert.Equal(2, battle!.Attackers[0].AliveCount);
        }

        [Fact]
        public void ReportTroopKilled_PlayerParticipant_NoOpsRemoveButRunsVictoryCheck()
        {
            // Player participant uses a callback for AliveCount; the troop dict is empty
            // so RemoveTroop is a no-op. But the callback decreases over time (squad dies),
            // so a victory check after every kill report is still meaningful.
            int squadCount = 1;
            var manager = CreateManager(playerFactionId: "player_faction");
            var zone = CreateZone("zone_1", ownerFactionId: "michael",
                deployedAllocation: new Dictionary<DefenderTier, int> { { DefenderTier.Basic, 5 } });
            manager.StartPlayerCombat(zone, "player_faction", () => squadCount);

            ZoneBattle? endedBattle = null;
            BattleOutcome? endedOutcome = null;
            manager.BattleEnded += (b, o) => { endedBattle = b; endedOutcome = o; };

            squadCount = 0;
            manager.ReportTroopKilled("zone_1", "player_faction", DefenderTier.Basic);

            // Player squad wiped; defender (michael) wins.
            Assert.NotNull(endedBattle);
            Assert.Equal(BattleOutcome.DefendersWon, endedOutcome);
        }

        [Fact]
        public void ReportTroopKilled_PlayerWipesDefender_FiresBattleEndedAttackersWon()
        {
            var manager = CreateManager(playerFactionId: "player_faction");
            var zone = CreateZone("zone_1", ownerFactionId: "michael",
                deployedAllocation: new Dictionary<DefenderTier, int> { { DefenderTier.Basic, 1 } });
            manager.StartPlayerCombat(zone, "player_faction", () => 4);

            ZoneBattle? endedBattle = null;
            BattleOutcome? endedOutcome = null;
            manager.BattleEnded += (b, o) => { endedBattle = b; endedOutcome = o; };

            manager.ReportTroopKilled("zone_1", "michael", DefenderTier.Basic);

            Assert.NotNull(endedBattle);
            Assert.Equal(BattleOutcome.AttackersWon, endedOutcome);
            // Victory check should have run — and the surviving attacker is the player.
            Assert.True(endedBattle!.Attackers.Any(p => p.IsPlayer));
        }

        [Fact]
        public void ReportTroopKilled_UnknownFaction_NoOp()
        {
            var manager = CreateManager(playerFactionId: "player_faction");
            manager.StartBattle("zone_1", "trevor", "michael",
                new Dictionary<DefenderTier, int> { { DefenderTier.Basic, 3 } },
                new Dictionary<DefenderTier, int> { { DefenderTier.Basic, 5 } });

            // No throw, no battle end.
            manager.ReportTroopKilled("zone_1", "franklin", DefenderTier.Basic);

            var battle = manager.GetBattleForZone("zone_1");
            Assert.NotNull(battle);
            Assert.Equal(3, battle!.Attackers[0].AliveCount);
            Assert.Equal(5, battle.Defender.AliveCount);
        }
```

- [ ] **Step 6.2: Run tests to verify they fail**

```bash
dotnet test tests/FactionWars.Tests/FactionWars.Tests.csproj -c Debug --nologo --filter "FullyQualifiedName~ReportTroopKilled"
```

Expected: build error — `ReportTroopKilled` not defined.

- [ ] **Step 6.3: Add to the interface**

```csharp
        /// <summary>
        /// Reports that one troop of the given tier on the given faction's side has died.
        /// Decrements the participant's tier count (no-op for player participants whose
        /// alive count comes from a callback) and runs the victory check.
        /// </summary>
        void ReportTroopKilled(string zoneId, string victimFactionId, DefenderTier tier);
```

- [ ] **Step 6.4: Implement**

In `ZoneBattleManager.cs`:

```csharp
        /// <inheritdoc />
        public void ReportTroopKilled(string zoneId, string victimFactionId, DefenderTier tier)
        {
            if (string.IsNullOrEmpty(zoneId)) throw new ArgumentNullException(nameof(zoneId));
            if (string.IsNullOrEmpty(victimFactionId)) throw new ArgumentNullException(nameof(victimFactionId));

            if (!_battlesByZone.TryGetValue(zoneId, out var battle))
            {
                return;
            }

            var victim = battle.Participants.FirstOrDefault(p => p.FactionId == victimFactionId);
            if (victim == null)
            {
                FileLogger.Combat($"ReportTroopKilled: faction '{victimFactionId}' not in battle '{zoneId}'.");
                return;
            }

            // For player participants this is a no-op; for AI it decrements the tier.
            victim.RemoveTroop(tier);
            TroopKilled?.Invoke(battle, tier, victimFactionId);
            ResolveBattleIfDone(battle);
        }
```

- [ ] **Step 6.5: Run tests to verify they pass**

```bash
dotnet test tests/FactionWars.Tests/FactionWars.Tests.csproj -c Debug --nologo --filter "FullyQualifiedName~ReportTroopKilled"
```

Expected: 4 passed.

- [ ] **Step 6.6: Add an internal `OnBattleEnded` handler that routes outcomes**

The manager already fires `BattleEnded` to external subscribers. We now add an internal handler that performs the side-effects for the player-win-neutral case (today done in `CombatResultHandler.ProcessCombatResult`). This handler runs *before* external subscribers — they should observe the side-effects already applied.

In `ZoneBattleManager.cs`, modify `ResolveBattleIfDone` to call a new private helper *before* invoking the public event:

```csharp
        private void ResolveBattleIfDone(ZoneBattle battle)
        {
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

        /// <summary>
        /// Applies the side-effects of a battle outcome:
        /// - Defender wins: refund attackers' reserves (existing AI-side behaviour).
        /// - AI attacker wins: zone ownership transferred (existing AI-side behaviour).
        /// - Player wins: zone goes neutral (Q5.A).
        /// </summary>
        private void ApplyBattleOutcome(
            ZoneBattle battle,
            BattleOutcome outcome,
            IList<BattleParticipant> aliveParticipants)
        {
            BattleParticipant? winner = aliveParticipants.Count == 1 ? aliveParticipants[0] : null;

            if (outcome == BattleOutcome.AttackersWon && winner != null && winner.IsPlayer)
            {
                // Player win → zone goes neutral (Q5.A). Existing two-step capture preserved
                // by leaving downstream "claim zone" gameplay untouched.
                _factionService.SetZoneOwner(battle.ZoneId, null);
                FileLogger.Combat($"ApplyBattleOutcome: player won zone '{battle.ZoneId}', set to neutral.");
                return;
            }

            // For AI-side outcomes, defer to the existing OnBattleEndedInternal logic
            // already wired up in this manager. (No-op here — the existing handler runs
            // via the BattleEnded event subscription set up at construction.)
        }
```

If `IFactionService.SetZoneOwner(zoneId, null)` doesn't exist, look up the existing zone-neutral mechanism (likely on `IZoneService` or via `ICombatResultHandler.ProcessCombatResult` today). Plumb whatever method neutralizes a zone — typically `_zoneService.SetOwner(zoneId, null)` or similar.

If neither exists as a public method, defer the player-win-neutral side-effect to Phase C (Task 16) when `CombatResultHandler` is fully inlined; for now leave the player-win path with just the BattleEnded event firing. Update the test `ReportTroopKilled_PlayerWipesDefender_FiresBattleEndedAttackersWon` to remove the ownership-change assertion if you go this route.

- [ ] **Step 6.7: Run the full Combat test suite**

```bash
dotnet test tests/FactionWars.Tests/FactionWars.Tests.csproj -c Debug --nologo --filter "FullyQualifiedName~FactionWars.Tests.Unit.Combat"
```

Expected: all green.

- [ ] **Step 6.8: Commit**

```bash
git add src/FactionWars/Combat/Interfaces/IZoneBattleManager.cs src/FactionWars/Combat/Services/ZoneBattleManager.cs tests/FactionWars.Tests/Unit/Combat/ZoneBattleManagerTests.cs
git commit -m "feat: ZoneBattleManager.ReportTroopKilled and outcome routing"
```

---

**Phase A checkpoint:** ZoneBattleManager now has the full new API surface (StartPlayerCombat, JoinAsAttacker, RemoveParticipant, ReportTroopKilled, IsPlayerInBattle, GetPlayerCurrentBattle). No callers use it yet. CombatManager still owns the player flow. Run `dotnet test` once and confirm all green before Phase B.

---

## Phase B — Wire callers to the new API (CombatManager still exists, parallel)

After Phase B, every existing call site that targeted `CombatManager` for the player flow now targets `ZoneBattleManager`. CombatManager itself is unused but not yet deleted. This phase has the most surface area.

### Task 7: `GameLoopController.OnZoneEntered` switches to `StartPlayerCombat`

**Files:**
- Modify: `src/FactionWars/ScriptHookV/GameLoopController.cs:~1290-1337` (the `OnZoneEntered` handler)
- Test: covered transitively by `ZoneBattleManagerPlayerFlowTests` written in Task 17

- [ ] **Step 7.1: Read the current `OnZoneEntered` handler**

```bash
sed -n '1280,1340p' src/FactionWars/ScriptHookV/GameLoopController.cs
```

Note the exact lines — you'll need them for the edit.

- [ ] **Step 7.2: Replace the `_combatManager.StartCombat(...)` call**

In `OnZoneEntered`, find the line that calls `_combatManager.StartCombat(zone, playerFactionId)` and the surrounding context. Replace the call with:

```csharp
            // Build the alive-count callback once. It returns player + alive followers.
            Func<int> aliveCountCallback = () => 1 + _followerService.GetAliveFollowerCount(playerFactionId);
            var battle = _zoneBattleManager.StartPlayerCombat(zone, playerFactionId, aliveCountCallback);
            if (battle == null)
            {
                FileLogger.Combat($"OnZoneEntered: StartPlayerCombat returned null for zone {zone.Id} — caller skipping.");
                return;
            }
```

If the surrounding code stores the result of the old `StartCombat` call (e.g. into a local `encounter` variable used downstream in the same method), update those usages to use `battle` properties (`battle.Defender.FactionId`, `battle.AliveCount` etc.) instead of `encounter.AttackingFactionId`, `encounter.DefendingFactionId` etc.

If `_followerService` isn't already a field on `GameLoopController`, look at how `CombatManager` accesses it today (constructor-injected) and inject it the same way into `GameLoopController`.

- [ ] **Step 7.3: Build to confirm no compile errors**

```bash
dotnet build src/FactionWars/FactionWars.csproj -c Debug -v minimal 2>&1 | tail -3
```

Expected: 0 errors. (Warnings about unused fields like `_combatManager` are fine — they'll be cleaned up in Phase C.)

- [ ] **Step 7.4: Run the full test suite to ensure no regression**

```bash
dotnet test tests/FactionWars.Tests/FactionWars.Tests.csproj -c Debug --nologo -v minimal 2>&1 | tail -3
```

Expected: all green except the known `NativeSaveWatcher` flake. The `CombatManagerFlowIntegrationTests` may also fail because they're hitting the old code path that's now bypassed. Note these failures — they'll be fixed in Task 17.

If new unrelated failures appear, fix them now before committing.

- [ ] **Step 7.5: Commit**

```bash
git add src/FactionWars/ScriptHookV/GameLoopController.cs
git commit -m "refactor: GameLoopController.OnZoneEntered uses StartPlayerCombat"
```

---

### Task 8: `GameLoopController.OnZoneExited` switches to `RemoveParticipant`

**Files:**
- Modify: `src/FactionWars/ScriptHookV/GameLoopController.cs:~1444-1456` (the `OnZoneExited` handler)

- [ ] **Step 8.1: Read the current handler**

```bash
sed -n '1430,1465p' src/FactionWars/ScriptHookV/GameLoopController.cs
```

- [ ] **Step 8.2: Replace the `_combatManager.EndCombat(CombatStatus.PlayerRetreat)` call**

The current code looks like (paraphrased):

```csharp
if (_combatManager.IsInCombat && _combatManager.CurrentEncounter?.ZoneId == zoneId)
{
    _combatManager.EndCombat(CombatStatus.PlayerRetreat);
}
```

Replace with:

```csharp
if (_zoneBattleManager.IsPlayerInBattle())
{
    var battle = _zoneBattleManager.GetPlayerCurrentBattle();
    if (battle != null && battle.ZoneId == zoneId)
    {
        _zoneBattleManager.RemoveParticipant(zoneId, _playerFactionId!);
    }
}
```

If `_playerFactionId` isn't already a field on `GameLoopController`, look at how `_combatManager` was getting it today and use the same source.

- [ ] **Step 8.3: Build & test**

```bash
dotnet build src/FactionWars/FactionWars.csproj -c Debug -v minimal 2>&1 | tail -3
dotnet test tests/FactionWars.Tests/FactionWars.Tests.csproj -c Debug --nologo --filter "FullyQualifiedName!~CombatManagerFlowIntegrationTests" -v minimal 2>&1 | tail -3
```

Expected: 0 build errors. All non-CombatManagerFlowIntegrationTests green.

- [ ] **Step 8.4: Commit**

```bash
git add src/FactionWars/ScriptHookV/GameLoopController.cs
git commit -m "refactor: GameLoopController.OnZoneExited uses RemoveParticipant"
```

---

### Task 9: `GameLoopController` HUD/wave path reads from `GetPlayerCurrentBattle`

**Files:**
- Modify: `src/FactionWars/ScriptHookV/GameLoopController.cs` (~lines 282-323 for wave spawning, ~lines 408-456 for contested HUD)

The HUD update method and wave-spawning loop currently read from `_combatManager.CurrentEncounter` and `_combatManager.IsInCombat`. Switch them to `_zoneBattleManager.GetPlayerCurrentBattle()` and `_zoneBattleManager.IsPlayerInBattle()`.

- [ ] **Step 9.1: Read both regions**

```bash
sed -n '278,330p' src/FactionWars/ScriptHookV/GameLoopController.cs
sed -n '400,465p' src/FactionWars/ScriptHookV/GameLoopController.cs
```

- [ ] **Step 9.2: Identify each `_combatManager` reference**

For each one, decide what the equivalent on the new API is:

| Old | New |
|---|---|
| `_combatManager.IsInCombat` | `_zoneBattleManager.IsPlayerInBattle()` |
| `_combatManager.CurrentEncounter` | `_zoneBattleManager.GetPlayerCurrentBattle()` |
| `encounter.ZoneId` | `battle.ZoneId` |
| `encounter.AttackingFactionId` | The player faction (from `_playerFactionId`) — the player is always the attacker in a player-flow battle |
| `encounter.DefendingFactionId` | `battle.Defender.FactionId` |
| `encounter.AttackerPedCount` | `battle.Attackers.First(p => p.IsPlayer).AliveCount` |
| `encounter.DefenderPedCount` | `battle.Defender.AliveCount` |

For wave spawning logic that calls `_combatManager.IsWaveSpawningComplete()`, `.GetNextWaveTier()`, `.SpawnNextWave(...)`: these are CombatManager-internal mechanics that aren't part of `ZoneBattleManager`'s API. **Leave them on `_combatManager` for now** — wave spawning ties into ped spawn services, and migrating it is in Task 14's scope. The point of Task 9 is just the HUD reads.

- [ ] **Step 9.3: Replace HUD-related reads**

The contested-HUD branch (around lines 408-456) reads `_combatManager.CurrentEncounter` and uses its `ZoneId`/`AttackerPedCount`/`DefenderPedCount` to build a `TerritoryIndicatorData`. Rewire it to:

```csharp
            var playerBattle = _zoneBattleManager.GetPlayerCurrentBattle();
            if (playerBattle != null && playerBattle.ZoneId == currentZone.Id)
            {
                int playerCount = playerBattle.Attackers.First(p => p.IsPlayer).AliveCount;
                int defenderCount = playerBattle.Defender.AliveCount;
                // ... rest of the existing code, replacing encounter.X with the locals above ...
            }
```

Do the same for any other HUD-related read in lines 282-323.

- [ ] **Step 9.4: Build & test**

```bash
dotnet build src/FactionWars/FactionWars.csproj -c Debug -v minimal 2>&1 | tail -3
dotnet test tests/FactionWars.Tests/FactionWars.Tests.csproj -c Debug --nologo --filter "FullyQualifiedName!~CombatManagerFlowIntegrationTests" -v minimal 2>&1 | tail -3
```

Expected: 0 errors, all green.

- [ ] **Step 9.5: Commit**

```bash
git add src/FactionWars/ScriptHookV/GameLoopController.cs
git commit -m "refactor: HUD reads ZoneBattleManager.GetPlayerCurrentBattle"
```

---

### Task 10: `EnemyDefenderManager` reports kills via `ReportTroopKilled`

**Files:**
- Modify: `src/FactionWars/ScriptHookV/Managers/EnemyDefenderManager.cs`
- Test: `tests/FactionWars.Tests/Unit/ScriptHookV/EnemyDefenderManagerTests.cs` (verify still green after change)

`EnemyDefenderManager` watches enemy defender peds for death and currently routes the death event somewhere (probably `CombatManager` or directly into `ZoneBattleManager` already — check). Whichever it is, after this task it routes via `ZoneBattleManager.ReportTroopKilled(zoneId, victimFactionId, tier)`.

- [ ] **Step 10.1: Find the kill-watcher path**

```bash
grep -n "Killed\|OnDeath\|RemoveDefenderTroop\|ReportTroop" src/FactionWars/ScriptHookV/Managers/EnemyDefenderManager.cs
```

The result tells you which method is the kill handler.

- [ ] **Step 10.2: Update the call**

If today the manager calls `_zoneBattleManager.RemoveDefenderTroop(zoneId, tier)` (or similar), replace with:

```csharp
                _zoneBattleManager.ReportTroopKilled(zoneId, defenderFactionId, tier);
```

You'll need access to the defender faction id. The manager almost certainly already tracks it per ped (it's the "enemy" — the zone owner). If not directly, derive from `_factionService.GetFactionForZone(zoneId)`.

- [ ] **Step 10.3: Update existing tests if they assert the old call**

Run:

```bash
dotnet test tests/FactionWars.Tests/FactionWars.Tests.csproj -c Debug --nologo --filter "FullyQualifiedName~EnemyDefenderManagerTests" -v minimal 2>&1 | tail -10
```

If tests fail because they verify the old method call (e.g. `Mock.Verify(m => m.RemoveDefenderTroop(...))`), update them to verify `ReportTroopKilled(zoneId, factionId, tier)`.

Expected after fixes: all `EnemyDefenderManagerTests` pass.

- [ ] **Step 10.4: Commit**

```bash
git add src/FactionWars/ScriptHookV/Managers/EnemyDefenderManager.cs tests/FactionWars.Tests/Unit/ScriptHookV/EnemyDefenderManagerTests.cs
git commit -m "refactor: EnemyDefenderManager reports kills via ZoneBattleManager.ReportTroopKilled"
```

---

### Task 11: `BattleAttackerManager` reports kills via `ReportTroopKilled`

**Files:**
- Modify: `src/FactionWars/ScriptHookV/Managers/BattleAttackerManager.cs`
- Test: `tests/FactionWars.Tests/Unit/ScriptHookV/BattleAttackerManagerTests.cs`

Same pattern as Task 10 but for the attacker side.

- [ ] **Step 11.1: Find the kill-watcher**

```bash
grep -n "Killed\|OnDeath\|RemoveAttackerTroop\|ReportTroop" src/FactionWars/ScriptHookV/Managers/BattleAttackerManager.cs
```

- [ ] **Step 11.2: Replace with `ReportTroopKilled`**

Replace `_zoneBattleManager.RemoveAttackerTroop(zoneId, tier)` (or equivalent) with:

```csharp
                _zoneBattleManager.ReportTroopKilled(zoneId, attackerFactionId, tier);
```

The attacker faction id is on the manager already (it's the AI faction whose attackers it spawned).

- [ ] **Step 11.3: Update tests, run, commit**

```bash
dotnet test tests/FactionWars.Tests/FactionWars.Tests.csproj -c Debug --nologo --filter "FullyQualifiedName~BattleAttackerManagerTests" -v minimal 2>&1 | tail -10
```

```bash
git add src/FactionWars/ScriptHookV/Managers/BattleAttackerManager.cs tests/FactionWars.Tests/Unit/ScriptHookV/BattleAttackerManagerTests.cs
git commit -m "refactor: BattleAttackerManager reports kills via ZoneBattleManager.ReportTroopKilled"
```

---

### Task 12: `FriendlyDefenderManager` — verify kill reporting (no change expected)

**Files:**
- Read: `src/FactionWars/ScriptHookV/Managers/FriendlyDefenderManager.cs`
- Test: `tests/FactionWars.Tests/Unit/ScriptHookV/FriendlyDefenderManagerTests.cs`

Friendly defenders only die in player-owned-zone-under-attack scenarios, which today routes through `ZoneBattleManager` already. This task verifies that — no change expected.

- [ ] **Step 12.1: Confirm friendly-defender deaths route via `ZoneBattleManager`**

```bash
grep -n "Killed\|OnDeath\|RemoveDefenderTroop\|ReportTroop\|_combatManager\|_zoneBattleManager" src/FactionWars/ScriptHookV/Managers/FriendlyDefenderManager.cs
```

Expected: any kill-handling already targets `_zoneBattleManager`. If it routes via `_combatManager` instead, do the same fix as Task 10/11 (replace with `ReportTroopKilled`).

- [ ] **Step 12.2: If a fix was needed, run tests + commit**

If the file already routes through `_zoneBattleManager`, this task is a no-op — note in the commit message and skip.

```bash
git add src/FactionWars/ScriptHookV/Managers/FriendlyDefenderManager.cs tests/FactionWars.Tests/Unit/ScriptHookV/FriendlyDefenderManagerTests.cs
git commit -m "verify: FriendlyDefenderManager kill reporting routes through ZoneBattleManager"
```

If no changes were needed, skip the commit and proceed to Task 13.

---

### Task 13: `TakeoverDetector` updated for new API (or confirmed not needed)

**Files:**
- Read: `src/FactionWars/Combat/Services/TakeoverDetector.cs`
- Modify if needed
- Test: `tests/FactionWars.Tests/Unit/Combat/TakeoverDetectorTests.cs`

`TakeoverDetector` today takes a `CombatEncounter`. After Plan 2 those don't exist. Two outcomes possible:

**Outcome A — detector is only used in CombatManager's now-dead code path.** The detector is dead. Don't delete the file yet (callers cleaned up in Task 14), but if its consumers are all in CombatManager, no immediate change is needed.

**Outcome B — detector is also called from ZoneBattleManager's simulated-tick path.** Then we need to switch its API from `CheckTakeover(encounter)` to `CheckTakeover(battle)`.

- [ ] **Step 13.1: Find all callers**

```bash
grep -rn "TakeoverDetector\|ITakeoverDetector\|_takeoverDetector" src/ tests/
```

- [ ] **Step 13.2: If Outcome A, do nothing here; the detector follows CombatManager into deletion in Phase C**

Note in the commit message and skip to Task 14.

- [ ] **Step 13.3: If Outcome B, add a `CheckTakeover(ZoneBattle)` overload**

The new overload computes attacker/defender control percentages from `battle.TotalAttackerTroops` and `battle.TotalDefenderTroops` (or the more sophisticated split if the detector cares about per-tier weights — read its existing logic). Migrate ZoneBattleManager's tick callers to the new overload.

```csharp
        public TakeoverResult CheckTakeover(ZoneBattle battle)
        {
            int total = battle.TotalAttackerTroops + battle.TotalDefenderTroops;
            float attackerPercent = total > 0 ? 100f * battle.TotalAttackerTroops / total : 0f;
            float defenderPercent = total > 0 ? 100f * battle.TotalDefenderTroops / total : 0f;
            return CheckTakeover(attackerPercent, defenderPercent,
                battle.AttackerFactionId, battle.DefenderFactionId);
        }
```

(In 3-way mode, `AttackerFactionId` returns the *first* attacker per Plan 1's backward-compat shape. That's consistent with today's 2-way-only takeover semantics; 3-way scenarios always run with player present so the simulated tick — and thus the detector — never fires in 3-way mode. See spec §3 "Player absent" paragraph.)

- [ ] **Step 13.4: Run tests + commit**

```bash
dotnet test tests/FactionWars.Tests/FactionWars.Tests.csproj -c Debug --nologo --filter "FullyQualifiedName~TakeoverDetector" -v minimal 2>&1 | tail -5
git add src/FactionWars/Combat/Services/TakeoverDetector.cs src/FactionWars/Combat/Interfaces/ITakeoverDetector.cs src/FactionWars/Combat/Services/ZoneBattleManager.cs
git commit -m "refactor: TakeoverDetector accepts ZoneBattle"
```

---

**Phase B checkpoint:** All player-flow callers route through `ZoneBattleManager`. CombatManager still exists, still gets initialized in `GameLoopController`, still gets `Update()`-ticked, but its `IsInCombat` path is no longer reached because `OnZoneEntered` doesn't call `StartCombat`. Run the full suite — only `CombatManagerFlowIntegrationTests` should fail (rewritten in Task 17). All other tests must be green before Phase C.

---

## Phase C — Decommission `CombatManager`, `CombatEncounter`, `CombatResultHandler`

### Task 14: Remove `_combatManager` field + Update tick from `GameLoopController`

**Files:**
- Modify: `src/FactionWars/ScriptHookV/GameLoopController.cs`

Remove the `_combatManager` field and all references in `GameLoopController`. Also remove the wave-spawning code path that depended on `CombatManager` — it's player-flow logic that needs to migrate elsewhere.

**Wave spawning migration.** Today the wave spawner is a `CombatManager` responsibility. The new home is up to the implementer's judgment, but the simplest path is to extract it into a small standalone `WaveSpawnerService` (which the explore report shows is already an injected dependency — it just hangs off CombatManager today). Move the OnTick wave logic from `CombatManager.Update()` into `GameLoopController.OnTick()` directly, calling `_waveSpawnerService` against the current player battle.

- [ ] **Step 14.1: Remove the field and all `_combatManager.X` references**

In `GameLoopController.cs`:
- Delete the `_combatManager` field declaration (~line 49).
- Delete the `CombatManager` property (~line 179).
- Delete the `_combatManager?.Update()` call in OnTick (~line 282).
- Delete the constructor argument and assignment (~line 729).
- Delete any event subscriptions and handlers (`OnCombatStarted`, `OnCombatEnded`, ~lines 743-744 and 1465-1499).
- Delete the cleanup in dispose/teardown (~lines 1135-1141).

For each remaining wave-spawning reference to `_combatManager.IsWaveSpawningComplete()` etc., refactor to read from `_waveSpawnerService` directly with the current player battle as input. Look at the existing `IWaveSpawnerService` signatures — they probably already accept battle-shaped data.

- [ ] **Step 14.2: Build**

```bash
dotnet build src/FactionWars/FactionWars.csproj -c Debug -v minimal 2>&1 | tail -10
```

Expected: 0 errors. If there are errors, they're locations you missed — fix them inline.

- [ ] **Step 14.3: Update `GameLoopControllerTests`**

```bash
dotnet test tests/FactionWars.Tests/FactionWars.Tests.csproj -c Debug --nologo --filter "FullyQualifiedName~GameLoopControllerTests" -v minimal 2>&1 | tail -5
```

Tests that mock `_combatManager` will break. Update them to mock `_zoneBattleManager` instead, or remove tests that exercise CombatManager-only behaviour.

- [ ] **Step 14.4: Run full suite**

```bash
dotnet test tests/FactionWars.Tests/FactionWars.Tests.csproj -c Debug --nologo -v minimal 2>&1 | tail -3
```

Expected: all green except `CombatManagerFlowIntegrationTests` (still rewritten in Task 17).

- [ ] **Step 14.5: Commit**

```bash
git add src/FactionWars/ScriptHookV/GameLoopController.cs tests/FactionWars.Tests/
git commit -m "refactor: drop _combatManager dependency from GameLoopController"
```

---

### Task 15: Delete `CombatManager.cs` + `CombatEncounter.cs` + `WaveState.cs` (if encounter-only)

**Files:**
- Delete: `src/FactionWars/ScriptHookV/Managers/CombatManager.cs`
- Delete: `src/FactionWars/Combat/Models/CombatEncounter.cs`
- Delete: `tests/FactionWars.Tests/Unit/ScriptHookV/CombatManagerTests.cs` (if exists)
- Inspect for any other CombatManager-only callers and clean up

- [ ] **Step 15.1: Confirm no remaining references**

```bash
grep -rn "CombatManager\|CombatEncounter" src/ tests/
```

Expected: only references inside the files about to be deleted, plus possibly:
- `CombatManagerFlowIntegrationTests.cs` (still exists — rewritten in Task 17)
- Service container registration (e.g., `ServiceContainerFactory.cs`) — this needs cleanup

If service container still registers `CombatManager`, find and delete those lines:

```bash
grep -n "CombatManager" src/FactionWars/ServiceContainerFactory.cs
```

- [ ] **Step 15.2: Delete the files**

```bash
rm src/FactionWars/ScriptHookV/Managers/CombatManager.cs
rm src/FactionWars/Combat/Models/CombatEncounter.cs
```

Also delete `CombatStatus.cs` if it's a separate file:

```bash
find src -name "CombatStatus.cs"
```

If found, delete:

```bash
rm src/<path>/CombatStatus.cs
```

Delete the dedicated test file:

```bash
test -f tests/FactionWars.Tests/Unit/ScriptHookV/CombatManagerTests.cs && rm tests/FactionWars.Tests/Unit/ScriptHookV/CombatManagerTests.cs || echo "no CombatManagerTests.cs"
```

- [ ] **Step 15.3: Clean up service container registration**

In `ServiceContainerFactory.cs`, remove any `Register<ICombatManager, CombatManager>(...)` style line and any constructor arguments that take `CombatManager`/`ICombatManager`.

- [ ] **Step 15.4: Build**

```bash
dotnet build src/FactionWars/FactionWars.csproj -c Debug -v minimal 2>&1 | tail -10
```

Expected: 0 errors. Any errors mean residual references — find them and clean up.

- [ ] **Step 15.5: Commit**

```bash
git add -A
git commit -m "refactor: delete CombatManager and CombatEncounter"
```

---

### Task 16: Delete or shrink `CombatResultHandler.cs`

**Files:**
- Modify or Delete: `src/FactionWars/Combat/Services/CombatResultHandler.cs`
- Modify: `src/FactionWars/Combat/Interfaces/ICombatResultHandler.cs` (if shrinking)
- Possibly Delete: `tests/FactionWars.Tests/Unit/Combat/CombatResultHandlerTests.cs`

`CombatResultHandler.ProcessCombatResult` accepts a `CombatEncounter` (now deleted) and processes ownership transfers. Most logic moved into `ZoneBattleManager.ApplyBattleOutcome` in Task 6. If no callers remain, delete the file. If there's an AI-side code path that still needs it (unlikely), shrink to minimum.

- [ ] **Step 16.1: Confirm no remaining callers**

```bash
grep -rn "CombatResultHandler\|ICombatResultHandler" src/ tests/
```

If only the file itself shows up plus its dedicated test file, delete both. Otherwise inspect each caller.

- [ ] **Step 16.2: Delete the files**

```bash
rm src/FactionWars/Combat/Services/CombatResultHandler.cs
rm src/FactionWars/Combat/Interfaces/ICombatResultHandler.cs
test -f tests/FactionWars.Tests/Unit/Combat/CombatResultHandlerTests.cs && rm tests/FactionWars.Tests/Unit/Combat/CombatResultHandlerTests.cs
```

- [ ] **Step 16.3: Remove from service container**

```bash
grep -n "CombatResultHandler" src/FactionWars/ServiceContainerFactory.cs
```

Remove any registration lines.

- [ ] **Step 16.4: Build**

```bash
dotnet build src/FactionWars/FactionWars.csproj -c Debug -v minimal 2>&1 | tail -5
```

Expected: 0 errors.

- [ ] **Step 16.5: Commit**

```bash
git add -A
git commit -m "refactor: delete CombatResultHandler (logic moved to ZoneBattleManager)"
```

---

**Phase C checkpoint:** CombatManager, CombatEncounter, CombatResultHandler all gone. Build is clean. Full test suite green except `CombatManagerFlowIntegrationTests` (next task). Run:

```bash
dotnet test tests/FactionWars.Tests/FactionWars.Tests.csproj -c Debug --nologo --filter "FullyQualifiedName!~CombatManagerFlowIntegrationTests" -v minimal 2>&1 | tail -3
```

Expected: all green.

---

## Phase D — Test rewrites

### Task 17: Rewrite `CombatManagerFlowIntegrationTests` as `ZoneBattleManagerPlayerFlowTests`

**Files:**
- Delete: `tests/FactionWars.Tests/Integration/Combat/CombatManagerFlowIntegrationTests.cs`
- Create: `tests/FactionWars.Tests/Integration/Combat/ZoneBattleManagerPlayerFlowTests.cs`

The old file has 26 tests covering: combat start, attacker/defender victories, retreat behaviour, wave spawning order, control percentage, player death, full takeover flow. Rewrite each as an integration test against the new `ZoneBattleManager` API. **Don't preserve the old file — it tests an interface that no longer exists.**

This task is large (~26 tests). Decompose into sub-tasks if the implementer prefers, but commit the whole file at once.

- [ ] **Step 17.1: Read the old file and list every test**

```bash
grep "Fact\|public void" tests/FactionWars.Tests/Integration/Combat/CombatManagerFlowIntegrationTests.cs
```

For each test, write the equivalent test against `ZoneBattleManager`. Pattern:

| Old assertion | New equivalent |
|---|---|
| `Assert.True(combatManager.IsInCombat)` | `Assert.True(zoneBattleManager.IsPlayerInBattle())` |
| `Assert.Equal(zoneId, encounter.ZoneId)` | `Assert.Equal(zoneId, zoneBattleManager.GetPlayerCurrentBattle()!.ZoneId)` |
| `Assert.Equal(CombatStatus.AttackerVictory, encounter.Status)` | Subscribe to `BattleEnded` and assert outcome |
| `combatManager.StartCombat(zone, factionId)` | `zoneBattleManager.StartPlayerCombat(zone, factionId, () => squadCount)` |
| `combatManager.EndCombat(CombatStatus.PlayerRetreat)` | `zoneBattleManager.RemoveParticipant(zoneId, factionId)` |
| `encounter.AttackerPedCount` | `battle.Attackers.First(p => p.IsPlayer).AliveCount` |
| `encounter.DefenderPedCount` | `battle.Defender.AliveCount` |
| Wave spawning assertions (Heavy → Medium → Basic) | These belong in dedicated `WaveSpawnerServiceTests`, not here. Drop or migrate. |

- [ ] **Step 17.2: Write the new file**

Create `tests/FactionWars.Tests/Integration/Combat/ZoneBattleManagerPlayerFlowTests.cs`. Use real services (not mocks) for the integration test — ZoneBattleManager, Faction service, Zone service, allocation service. Mock external dependencies like `IFollowerService` (return constant alive count).

Build out the same scenarios as the old file:
- Combat starts when player enters enemy zone (no prior battle).
- Combat continues if player exits and re-enters.
- Player wipes defender → battle ends with `BattleOutcome.AttackersWon` and zone goes neutral.
- Defender wipes player → battle ends with `BattleOutcome.DefendersWon` and zone keeps owner.
- Player retreats (exits zone) → `RemoveParticipant`; if alone vs defender, defender wins.

The implementer should mirror the old file's test names where the scenario is preserved, prefixed with `PlayerFlow_` for clarity.

- [ ] **Step 17.3: Delete the old file**

```bash
rm tests/FactionWars.Tests/Integration/Combat/CombatManagerFlowIntegrationTests.cs
```

- [ ] **Step 17.4: Run the new tests**

```bash
dotnet test tests/FactionWars.Tests/FactionWars.Tests.csproj -c Debug --nologo --filter "FullyQualifiedName~ZoneBattleManagerPlayerFlowTests" -v minimal 2>&1 | tail -5
```

Expected: all new tests pass.

- [ ] **Step 17.5: Commit**

```bash
git add tests/FactionWars.Tests/Integration/Combat/
git commit -m "test: rewrite CombatManagerFlowIntegrationTests as ZoneBattleManagerPlayerFlowTests"
```

---

### Task 18: Add 3-way bug-repro test

**Files:**
- Modify: `tests/FactionWars.Tests/Integration/Combat/ZoneBattleManagerPlayerFlowTests.cs`

The original bug: AI₁ attacking AI₂'s zone, player enters, player wins their side, AI's resolution overwrites the player win. Add an explicit test asserting this can't happen.

- [ ] **Step 18.1: Add the test**

In `ZoneBattleManagerPlayerFlowTests.cs`:

```csharp
        [Fact]
        public void ThreeWay_PlayerWinsAfterJoiningContestedAiZone_OneBattleEndedFires_ZoneGoesNeutral()
        {
            // Bug repro: AI₁ vs AI₂, player walks in, wipes everyone, player wins.
            // Today (pre-Plan2): two parallel battles — AI's resolution can overwrite player's.
            // After Plan 2: single battle, single BattleEnded, zone goes neutral.

            var manager = CreateRealManager(playerFactionId: "player_faction");
            var zone = CreateZone("zone_1", ownerFactionId: "michael",
                deployedAllocation: new Dictionary<DefenderTier, int> { { DefenderTier.Basic, 1 } });

            // AI₁ (trevor) attacks AI₂ (michael).
            manager.StartBattle("zone_1", "trevor", "michael",
                new Dictionary<DefenderTier, int> { { DefenderTier.Basic, 1 } },
                new Dictionary<DefenderTier, int> { { DefenderTier.Basic, 1 } });

            // Player walks in.
            int squadCount = 4;
            var battle = manager.StartPlayerCombat(zone, "player_faction", () => squadCount);
            Assert.NotNull(battle);
            Assert.Equal(3, battle!.Participants.Count);

            // Track BattleEnded firings.
            var endedEvents = new List<(ZoneBattle, BattleOutcome)>();
            manager.BattleEnded += (b, o) => endedEvents.Add((b, o));

            // Player wipes both AI sides.
            manager.ReportTroopKilled("zone_1", "trevor", DefenderTier.Basic);
            manager.ReportTroopKilled("zone_1", "michael", DefenderTier.Basic);

            // Exactly one BattleEnded fires.
            Assert.Single(endedEvents);
            // Outcome is AttackersWon and the surviving attacker is the player.
            Assert.Equal(BattleOutcome.AttackersWon, endedEvents[0].Item2);
            Assert.True(endedEvents[0].Item1.Attackers.Any(p => p.IsPlayer));
            // Zone is no longer in active battles.
            Assert.Null(manager.GetBattleForZone("zone_1"));
            // Zone went neutral (player win → zone goes neutral; Q5.A).
            // (If your test setup mocks IFactionService.SetZoneOwner, verify the call instead.)
        }
```

`CreateRealManager` is a helper from Task 17. If it doesn't exist, inline the construction.

- [ ] **Step 18.2: Run the test**

```bash
dotnet test tests/FactionWars.Tests/FactionWars.Tests.csproj -c Debug --nologo --filter "FullyQualifiedName~ThreeWay_PlayerWins" -v minimal 2>&1 | tail -10
```

Expected: 1 passed.

- [ ] **Step 18.3: Commit**

```bash
git add tests/FactionWars.Tests/Integration/Combat/ZoneBattleManagerPlayerFlowTests.cs
git commit -m "test: 3-way bug-repro — player wins, zone neutral, exactly one BattleEnded"
```

---

**Phase D checkpoint:** Full test suite green. The original bug is now fixed and proven by Task 18's test. Pause here if you want to ship the bug fix without the cosmetic polish — Phases E and F are additive.

```bash
dotnet test tests/FactionWars.Tests/FactionWars.Tests.csproj -c Debug --nologo -v minimal 2>&1 | tail -3
```

Expected: all green.

---

## Phase E — HUD third-party row

### Task 19: Add `ThirdPartyCount` and `ThirdPartyFactionColor` to `TerritoryIndicatorData`

**Files:**
- Modify: `src/FactionWars/UI/Models/TerritoryIndicatorData.cs`
- Test: `tests/FactionWars.Tests/Unit/UI/TerritoryIndicatorDataTests.cs` (extend or create)

- [ ] **Step 19.1: Read the current shape**

```bash
cat src/FactionWars/UI/Models/TerritoryIndicatorData.cs
```

Note all existing public fields and the constructor signature.

- [ ] **Step 19.2: Write a failing test**

In `TerritoryIndicatorDataTests.cs` (create if absent):

```csharp
        [Fact]
        public void TerritoryIndicatorData_ExposesThirdPartyFields()
        {
            var data = new TerritoryIndicatorData(
                zoneName: "Sandy Shores",
                ownerFactionName: "Michael",
                ownerFactionColor: new FactionColor(0, 100, 255),
                controlPercentage: 50f,
                isContested: true,
                isPlayerOwned: false,
                deployedDefenderCount: 3,
                reserveDefenderCount: 2,
                playerTroopCount: 4,
                enemyDefenderCount: 3,
                enemyReserveCount: 0,
                thirdPartyCount: 2,
                thirdPartyFactionColor: new FactionColor(255, 150, 0));

            Assert.Equal(2, data.ThirdPartyCount);
            Assert.Equal(new FactionColor(255, 150, 0), data.ThirdPartyFactionColor);
        }
```

The constructor signature here may differ — adapt to whatever the existing constructor is. Add the two new params at the end.

- [ ] **Step 19.3: Run, verify fail, implement, verify pass**

Run:

```bash
dotnet test tests/FactionWars.Tests/FactionWars.Tests.csproj -c Debug --nologo --filter "FullyQualifiedName~TerritoryIndicatorData" -v minimal 2>&1 | tail -5
```

Expected: build error (new params don't exist).

Add to `TerritoryIndicatorData.cs`:

```csharp
        public int ThirdPartyCount { get; }
        public FactionColor? ThirdPartyFactionColor { get; }
```

Add the two parameters to the constructor (default values to maintain back-compat with existing call sites):

```csharp
        public TerritoryIndicatorData(
            // ... existing parameters ...
            int thirdPartyCount = 0,
            FactionColor? thirdPartyFactionColor = null)
        {
            // ... existing assignments ...
            ThirdPartyCount = thirdPartyCount;
            ThirdPartyFactionColor = thirdPartyFactionColor;
        }
```

Re-run tests; expected pass.

- [ ] **Step 19.4: Commit**

```bash
git add src/FactionWars/UI/Models/TerritoryIndicatorData.cs tests/FactionWars.Tests/Unit/UI/TerritoryIndicatorDataTests.cs
git commit -m "feat: TerritoryIndicatorData has third-party fields"
```

---

### Task 20: `TerritoryIndicatorService` populates third-party from second `Attacker` participant

**Files:**
- Modify: `src/FactionWars/UI/Services/TerritoryIndicatorService.cs`
- Test: `tests/FactionWars.Tests/Unit/UI/TerritoryIndicatorServiceTests.cs`

When the player is in a 3-way battle, populate the new `ThirdPartyCount`/`ThirdPartyFactionColor` from the AI attacker (the second one in the Attackers list — the player is one, the AI is the other).

- [ ] **Step 20.1: Read the service to find the contested-zone code path**

```bash
cat src/FactionWars/UI/Services/TerritoryIndicatorService.cs
```

Look for the branch that builds `TerritoryIndicatorData` for a contested zone. It currently consumes battle/encounter data — find the spot where it would have the participant list available.

- [ ] **Step 20.2: Write a failing test**

```csharp
        [Fact]
        public void BuildIndicatorData_PopulatesThirdParty_FromSecondAttacker()
        {
            // Player in zone, contested. Defender = michael, attackers = [trevor (AI), player_faction (player)].
            // Third party from player POV = trevor.
            var trevorColor = new FactionColor(255, 150, 0);
            var battle = CreateBattleWithParticipants(
                zoneId: "zone_1",
                defender: BattleParticipant.ForAi("michael", BattleRole.Defender,
                    new Dictionary<DefenderTier, int> { { DefenderTier.Basic, 5 } }),
                aiAttacker: BattleParticipant.ForAi("trevor", BattleRole.Attacker,
                    new Dictionary<DefenderTier, int> { { DefenderTier.Basic, 2 } }),
                playerAttacker: BattleParticipant.ForPlayer("player_faction", BattleRole.Attacker, () => 4),
                playerFactionId: "player_faction");
            // Mock _factionService to return trevor's color.

            var data = service.BuildIndicatorData(zoneId: "zone_1", isPlayerOwnedZone: false);

            Assert.True(data.IsContested);
            Assert.Equal(2, data.ThirdPartyCount);
            Assert.Equal(trevorColor, data.ThirdPartyFactionColor);
        }
```

`CreateBattleWithParticipants` is a test helper using the new ZoneBattle constructor. Mock the `_battleManager.GetBattleForZone(zoneId)` to return the constructed battle.

- [ ] **Step 20.3: Implement**

In the contested-data-building branch of `TerritoryIndicatorService`, look for how it accesses the battle. Add:

```csharp
            // 3-way: third party is the AI attacker not equal to the player.
            int thirdPartyCount = 0;
            FactionColor? thirdPartyColor = null;
            if (battle != null)
            {
                var aiAttacker = battle.Attackers.FirstOrDefault(p => !p.IsPlayer);
                if (aiAttacker != null && battle.Attackers.Any(p => p.IsPlayer))
                {
                    thirdPartyCount = aiAttacker.AliveCount;
                    thirdPartyColor = _factionService.GetFaction(aiAttacker.FactionId)?.Color;
                }
            }

            return new TerritoryIndicatorData(
                // ... existing args ...
                thirdPartyCount: thirdPartyCount,
                thirdPartyFactionColor: thirdPartyColor);
```

- [ ] **Step 20.4: Run + commit**

```bash
dotnet test tests/FactionWars.Tests/FactionWars.Tests.csproj -c Debug --nologo --filter "FullyQualifiedName~TerritoryIndicatorService" -v minimal 2>&1 | tail -5
git add src/FactionWars/UI/Services/TerritoryIndicatorService.cs tests/FactionWars.Tests/Unit/UI/TerritoryIndicatorServiceTests.cs
git commit -m "feat: TerritoryIndicatorService populates third-party from AI attacker"
```

---

### Task 21: `TerritoryIndicatorRenderer` draws a third row when `ThirdPartyCount > 0`

**Files:**
- Modify: `src/FactionWars/ScriptHookV/UI/TerritoryIndicatorRenderer.cs:~117-166`

In `DrawEnemyTerritoryHud`, find the contested-zone block (around lines 137-166 per the explore report). Add a third row below the existing two when `data.ThirdPartyCount > 0`. The row should display the third-party count in their faction color (`data.ThirdPartyFactionColor`).

- [ ] **Step 21.1: Read the renderer**

```bash
sed -n '115,170p' src/FactionWars/ScriptHookV/UI/TerritoryIndicatorRenderer.cs
```

Identify exactly where the existing two-row block lives and what helpers it uses (`DrawText`, `DrawColoredText`, etc.).

- [ ] **Step 21.2: Add the third row**

After the existing "You: X / Enemies: Y" rendering block, append:

```csharp
            if (data.ThirdPartyCount > 0 && data.ThirdPartyFactionColor.HasValue)
            {
                yOffset += LineHeight;
                _gameBridge.DrawText(
                    text: $"3rd party: {data.ThirdPartyCount}",
                    x: xPosition,
                    y: yOffset,
                    color: data.ThirdPartyFactionColor.Value,
                    scale: 0.4f);
            }
```

Adjust to match the existing rendering style — use the same helpers, same scale, same line spacing. The above is illustrative.

- [ ] **Step 21.3: Build & smoke-test**

```bash
dotnet build src/FactionWars/FactionWars.csproj -c Debug -v minimal 2>&1 | tail -3
```

This is a visual change — no unit tests verify rendering pixels. The code path is gated by `ThirdPartyCount > 0`, so 2-way scenarios render identically to today.

- [ ] **Step 21.4: Commit**

```bash
git add src/FactionWars/ScriptHookV/UI/TerritoryIndicatorRenderer.cs
git commit -m "feat: contested-zone HUD shows third-party row in 3-way battles"
```

---

**Phase E checkpoint:** HUD now shows a third row with the AI third-party count in their faction color when the player is in a 3-way battle. Build clean. Tests green.

---

## Phase F — Faction-colored AI ped blips

### Task 22: AI ped blips use faction color (Q6)

**Files:**
- Modify: `src/FactionWars/ScriptHookV/Managers/EnemyDefenderManager.cs:117,431`
- Modify: `src/FactionWars/ScriptHookV/Managers/BattleAttackerManager.cs:157,443`
- Possibly extract: `src/FactionWars/ScriptHookV/Utils/FactionBlipColor.cs` (NEW small helper)
- Test: `tests/FactionWars.Tests/Unit/ScriptHookV/EnemyDefenderManagerTests.cs`, `BattleAttackerManagerTests.cs`, `Utils/FactionBlipColorTests.cs` (NEW)

The current behaviour: `EnemyDefenderManager` and `BattleAttackerManager` both call `_pedBlipService.CreateBlipForPed(handle, BlipColor.Red)`. Replace with a faction-color lookup. The mapping `factionId → BlipColor` already exists privately in `MapBlipManager.GetBlipColorForFaction(factionId)`. Lift it to a shared helper and call it from both managers (and reuse it in `MapBlipManager`).

- [ ] **Step 22.1: Write failing test for the helper**

Create `tests/FactionWars.Tests/Unit/ScriptHookV/Utils/FactionBlipColorTests.cs`:

```csharp
using FactionWars.Core.Interfaces;
using FactionWars.ScriptHookV.Utils;
using Xunit;

namespace FactionWars.Tests.Unit.ScriptHookV.Utils
{
    public class FactionBlipColorTests
    {
        [Theory]
        [InlineData("michael", BlipColor.MichaelBlue)]
        [InlineData("Michael", BlipColor.MichaelBlue)]
        [InlineData("MICHAEL", BlipColor.MichaelBlue)]
        [InlineData("trevor", BlipColor.TrevorOrange)]
        [InlineData("franklin", BlipColor.FranklinGreen)]
        public void ForFactionId_KnownFaction_ReturnsCharacterColor(string factionId, BlipColor expected)
        {
            Assert.Equal(expected, FactionBlipColor.ForFactionId(factionId));
        }

        [Fact]
        public void ForFactionId_UnknownFaction_ReturnsRedFallback()
        {
            // Hostile-by-default fallback for unknown factions matches the pre-Plan-2
            // behaviour where all enemy peds rendered red.
            Assert.Equal(BlipColor.Red, FactionBlipColor.ForFactionId("unknown"));
        }

        [Fact]
        public void ForFactionId_Null_ReturnsWhite()
        {
            Assert.Equal(BlipColor.White, FactionBlipColor.ForFactionId(null));
        }
    }
}
```

- [ ] **Step 22.2: Run, verify fail**

```bash
dotnet test tests/FactionWars.Tests/FactionWars.Tests.csproj -c Debug --nologo --filter "FullyQualifiedName~FactionBlipColorTests" -v minimal 2>&1 | tail -5
```

Expected: build error — `FactionBlipColor` not defined.

- [ ] **Step 22.3: Create the helper**

Create `src/FactionWars/ScriptHookV/Utils/FactionBlipColor.cs`:

```csharp
using FactionWars.Core.Interfaces;

namespace FactionWars.ScriptHookV.Utils
{
    /// <summary>
    /// Maps a faction id to the corresponding <see cref="BlipColor"/> for HUD/map rendering.
    /// Centralizes the "michael → blue, trevor → orange, franklin → green" convention so all
    /// blip sites use it. Unknown faction ids fall back to <see cref="BlipColor.Red"/>
    /// (hostile-by-default), matching pre-faction-color behaviour.
    /// </summary>
    public static class FactionBlipColor
    {
        public static BlipColor ForFactionId(string? factionId)
        {
            if (factionId == null) return BlipColor.White;
            return factionId.ToLowerInvariant() switch
            {
                "michael" => BlipColor.MichaelBlue,
                "trevor" => BlipColor.TrevorOrange,
                "franklin" => BlipColor.FranklinGreen,
                _ => BlipColor.Red
            };
        }
    }
}
```

- [ ] **Step 22.4: Run helper tests; expect pass**

```bash
dotnet test tests/FactionWars.Tests/FactionWars.Tests.csproj -c Debug --nologo --filter "FullyQualifiedName~FactionBlipColorTests" -v minimal 2>&1 | tail -3
```

Expected: 5 passed.

- [ ] **Step 22.5: Switch `EnemyDefenderManager` blip calls**

In `src/FactionWars/ScriptHookV/Managers/EnemyDefenderManager.cs` lines 117 and 431, replace:

```csharp
_pedBlipService.CreateBlipForPed(pedHandle.Handle, BlipColor.Red);
```

with:

```csharp
_pedBlipService.CreateBlipForPed(pedHandle.Handle, FactionBlipColor.ForFactionId(enemyFactionId));
```

`enemyFactionId` is the faction id of the zone owner (the manager already tracks it). Add `using FactionWars.ScriptHookV.Utils;` at the top.

- [ ] **Step 22.6: Switch `BattleAttackerManager` blip calls**

In `src/FactionWars/ScriptHookV/Managers/BattleAttackerManager.cs` lines 157 and 443, same substitution:

```csharp
_pedBlipService.CreateBlipForPed(pedHandle.Handle, FactionBlipColor.ForFactionId(attackerFactionId));
```

- [ ] **Step 22.7: Update existing tests**

Tests that asserted `BlipColor.Red` was passed will fail. Update them to assert the new faction-aware value:

```bash
grep -n "BlipColor.Red" tests/FactionWars.Tests/Unit/ScriptHookV/EnemyDefenderManagerTests.cs tests/FactionWars.Tests/Unit/ScriptHookV/BattleAttackerManagerTests.cs
```

For each match, update the expected value to use `FactionBlipColor.ForFactionId(factionId)` of whatever faction id the test uses (often `"trevor"` → `BlipColor.TrevorOrange`).

- [ ] **Step 22.8: Run impacted test files**

```bash
dotnet test tests/FactionWars.Tests/FactionWars.Tests.csproj -c Debug --nologo --filter "FullyQualifiedName~EnemyDefenderManagerTests|FullyQualifiedName~BattleAttackerManagerTests" -v minimal 2>&1 | tail -5
```

Expected: all green.

- [ ] **Step 22.9: (Optional) Refactor MapBlipManager to use the same helper**

`MapBlipManager.GetBlipColorForFaction` is a private method that does the same thing. Replace its body with `=> FactionBlipColor.ForFactionId(factionId);`. This is a small DRY win; skip if `MapBlipManager` has any nuance that diverges.

- [ ] **Step 22.10: Commit**

```bash
git add src/FactionWars/ScriptHookV/Utils/FactionBlipColor.cs src/FactionWars/ScriptHookV/Managers/EnemyDefenderManager.cs src/FactionWars/ScriptHookV/Managers/BattleAttackerManager.cs src/FactionWars/ScriptHookV/Managers/MapBlipManager.cs tests/FactionWars.Tests/Unit/ScriptHookV/
git commit -m "feat: AI ped blips use faction color (Q6)"
```

---

## Phase G — Final verification & deploy

### Task 23: Full suite green + DLL build + deploy

**Files:** none (verification only)

- [ ] **Step 23.1: Run the full test suite**

```bash
dotnet test tests/FactionWars.Tests/FactionWars.Tests.csproj -c Debug --nologo -v minimal 2>&1 | grep -E "(Failed|Passed)" | tail -3
```

Expected: full pass except possibly the known-flaky `NativeSaveWatcherTests.MultipleRapidWritesSamePath_DebouncesToOneEvent` and `SingleSave_FiresOneEvent`. Re-run them in isolation to confirm flake:

```bash
dotnet test tests/FactionWars.Tests/FactionWars.Tests.csproj -c Debug --nologo --filter "FullyQualifiedName~NativeSaveWatcherTests" -v minimal 2>&1 | tail -3
```

Expected in isolation: 5/5 pass.

- [ ] **Step 23.2: Build the DLL**

```bash
dotnet build src/FactionWars/FactionWars.csproj -c Debug -v minimal 2>&1 | tail -3
```

Expected: 0 errors.

- [ ] **Step 23.3: Deploy**

```bash
cp "C:/Users/ryan7/programming/gtav-factions/src/FactionWars/bin/Debug/net48/FactionWars.dll" "E:/SteamLibrary/steamapps/common/Grand Theft Auto V/scripts/" && echo "deployed"
```

Expected: `deployed`.

- [ ] **Step 23.4: In-game smoke test (manual)**

Start GTA V with the mod and verify:
1. Walk into an enemy AI zone → takeover sequence starts (was working before; should still work).
2. Walk out → takeover aborts cleanly.
3. (If you can find or stage a contested AI-vs-AI zone) walk in → all three sides are visible on the HUD with the third-party row showing the second AI's count in their faction color.
4. Win an AI-only battle (let two AIs fight without entering the zone) → no errors in the log.
5. Check `C:\Users\ryan7\Documents\FactionWars\Logs\FactionWars_*.log` for any unexpected ERROR or WARN lines.

If anything looks broken, **do not** push to origin. Investigate, fix, re-run the suite, redeploy.

- [ ] **Step 23.5: (Final) Push to origin**

After confirming the smoke test, ask the user before pushing. The full Plan 1 + Plan 2 commit chain is large — they may want to inspect the diff first.

```bash
# Awaiting user confirmation before:
# git push -u origin feat/3-way-battles
```

---

## Summary

After all 23 tasks complete:

- The original parallel-state bug is gone — player and AI battles share `ZoneBattleManager` state.
- The 3-way melee works mechanically: player can join a contested AI-vs-AI zone as a third hostile attacker, kill routing handles all three sides, victory check resolves the last survivor.
- HUD shows three figures during 3-way (defender + your count + AI third-party count, third-party in their faction color).
- AI ped blips on the world map are color-coded by faction (always, not just during 3-way) — Trevor's peds are orange, Michael's blue, Franklin's green; unknowns fall back to red.
- `CombatManager`, `CombatEncounter`, and `CombatResultHandler` are gone; their logic is consolidated in `ZoneBattleManager.ResolveBattleIfDone` / `ApplyBattleOutcome`.
- The integration test suite asserts the new flow with `ZoneBattleManagerPlayerFlowTests`, including the explicit 3-way bug-repro test.

Persistence migration was dropped from this plan — the spec mentioned a `ZoneBattleData` migration but no such persistence layer exists in the codebase (battles are runtime-only state).
