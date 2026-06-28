# Zone Combatant Allegiance Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Make a single authority decide a zone combatant's relationship group, blip colour, and friend/foe, with GTA relationship groups wired once at init — removing the bug class where blip colour and relationship diverge or guards exist in one spawn path but not another.

**Architecture:** A pure-domain `AllegianceResolver` maps `(combatantFactionId, playerFactionId) → CombatantProfile { group, blipColor, allegiance }`. Every zone combatant lives in its faction group (`MICHAEL`/`FRANKLIN`/`TREVOR`); "friendly to player" means same faction as the player. A `RelationshipMatrixInitializer` establishes all group-pair relationships once at init and on character-switch. A `ZoneCombatantSpawner` is the single spawn site the three managers call. A `ZoneOwnershipReconciler` owns despawn-on-ownership-change. Per-spawn relationship mutation is removed.

**Tech Stack:** C# / .NET Framework 4.8, xUnit + Moq, ScriptHookVDotNet3, NativeUI. Layered namespaces (`Core`/`Combat`/`Territory` portable; `ScriptHookV` native).

## Global Constraints

- Pre-commit gate (run before every commit): `dotnet build FactionWars.sln --no-incremental` then `dotnet test tests/FactionWars.Tests/FactionWars.Tests.csproj --filter "FullyQualifiedName~FactionWars.Tests.Unit"`. Both must pass with **0 warnings / 0 errors** (custom analyzers run as errors).
- Analyzer limits: max 10 public methods per class (CI0004); max 250 lines per class (CI0017); one public top-level type per file; no tuple return types; service-like classes need a first-party interface; constructors must not exceed the parameter limit or create disposable deps; source files use **CRLF** line endings (ENDOFLINE analyzer).
- Layering: `Core`/`Combat`/`Territory` must NOT reference `ScriptHookV`, GTA, or NativeUI. Native calls stay behind `IGameBridge`.
- Do NOT deploy during this refactor (a play build is live). Verify in-game only when the user is back; reflect any native-behaviour findings into `MockGameBridge` per CLAUDE.md.
- Temporary `[ALLEGIANCE]`/`[REL-DIAG]` diagnostics and the temp `IGameBridge` probes stay until the hostility bug is confirmed fixed in-game; their removal is the final task, gated on user confirmation.

---

## File Structure

- `src/FactionWars/Combat/Models/Allegiance.cs` — `enum Allegiance { Friendly, Hostile }` (new)
- `src/FactionWars/Combat/Models/CombatantProfile.cs` — value object (new)
- `src/FactionWars/Core/Utils/FactionBlipColor.cs` — moved from `ScriptHookV/Utils` (portable; pure faction→BlipColor)
- `src/FactionWars/Combat/Interfaces/IAllegianceResolver.cs` — resolver interface (new)
- `src/FactionWars/Combat/Services/AllegianceResolver.cs` — resolver impl (new)
- `src/FactionWars/ScriptHookV/Combat/IRelationshipMatrixInitializer.cs` + `RelationshipMatrixInitializer.cs` — wires GTA relationship groups once (new)
- `src/FactionWars/ScriptHookV/Combat/IZoneCombatantSpawner.cs` + `ZoneCombatantSpawner.cs` — single spawn site (new)
- `src/FactionWars/ScriptHookV/Managers/ZoneOwnershipReconciler.cs` — despawn-on-ownership-change (new)
- Modify `EnemyDefenderManager`, `BattleAttackerManager`, `FriendlyDefenderManager` — spawn via `ZoneCombatantSpawner`, expose `DespawnForZone`
- Modify `GameBridge.FollowTasks.cs` (`SetPedAsFriendly`), `GameBridge.HostilePeds.cs` (`SetPedAsHostileWanderer`) — drop relationship mutation; `BattleAttackerManager.Spawning.cs` — drop `ConfigureBattleRelationships` mutation
- Modify `GameLoopController.Initialization.cs` — construct/wire matrix initializer, spawner, reconciler; re-init matrix on character switch

---

## Task 1: Allegiance enum + CombatantProfile value object

**Files:**
- Create: `src/FactionWars/Combat/Models/Allegiance.cs`
- Create: `src/FactionWars/Combat/Models/CombatantProfile.cs`
- Test: `tests/FactionWars.Tests/Unit/Combat/CombatantProfileTests.cs`

**Interfaces:**
- Produces: `enum FactionWars.Combat.Models.Allegiance { Friendly, Hostile }`; `sealed class CombatantProfile` with ctor `(string relationshipGroup, FactionWars.Core.Interfaces.BlipColor blipColor, Allegiance allegiance)` and read-only properties `RelationshipGroup`, `BlipColor`, `Allegiance`.

- [ ] **Step 1: Write the failing test**

```csharp
using FactionWars.Combat.Models;
using FactionWars.Core.Interfaces;
using Xunit;

namespace FactionWars.Tests.Unit.Combat
{
    public class CombatantProfileTests
    {
        [Fact]
        public void Constructor_ExposesAllProperties()
        {
            var profile = new CombatantProfile("MICHAEL", BlipColor.MichaelBlue, Allegiance.Friendly);

            Assert.Equal("MICHAEL", profile.RelationshipGroup);
            Assert.Equal(BlipColor.MichaelBlue, profile.BlipColor);
            Assert.Equal(Allegiance.Friendly, profile.Allegiance);
        }
    }
}
```

- [ ] **Step 2: Run test — expect FAIL** (`Allegiance`/`CombatantProfile` not defined).

Run: `dotnet test tests/FactionWars.Tests/FactionWars.Tests.csproj --filter "FullyQualifiedName~CombatantProfileTests"`

- [ ] **Step 3: Implement**

`Allegiance.cs`:
```csharp
namespace FactionWars.Combat.Models
{
    public enum Allegiance
    {
        Friendly,
        Hostile
    }
}
```

`CombatantProfile.cs`:
```csharp
using FactionWars.Core.Interfaces;

namespace FactionWars.Combat.Models
{
    public sealed class CombatantProfile
    {
        public CombatantProfile(string relationshipGroup, BlipColor blipColor, Allegiance allegiance)
        {
            RelationshipGroup = relationshipGroup;
            BlipColor = blipColor;
            Allegiance = allegiance;
        }

        public string RelationshipGroup { get; }
        public BlipColor BlipColor { get; }
        public Allegiance Allegiance { get; }
    }
}
```

- [ ] **Step 4: Run test — expect PASS.**
- [ ] **Step 5: Commit** `git add` the three files; `git commit -m "feat(combat): add Allegiance enum and CombatantProfile"`.

---

## Task 2: Move FactionBlipColor into the portable domain

`FactionBlipColor` is pure logic (faction id → `BlipColor`) but lives in `ScriptHookV/Utils`, so the portable resolver can't use it. Move it to `Core/Utils` and update the two callers' `using`.

**Files:**
- Create: `src/FactionWars/Core/Utils/FactionBlipColor.cs` (namespace `FactionWars.Core.Utils`)
- Delete: `src/FactionWars/ScriptHookV/Utils/FactionBlipColor.cs`
- Modify: `src/FactionWars/ScriptHookV/Managers/EnemyDefenderManager.cs`, `EnemyDefenderManager.Replacements.cs`, `BattleAttackerManager.Spawning.cs` — change `using FactionWars.ScriptHookV.Utils;` reference so `FactionBlipColor` resolves (add `using FactionWars.Core.Utils;`).

**Interfaces:**
- Produces: `static class FactionWars.Core.Utils.FactionBlipColor { static BlipColor ForFactionId(string? factionId); }`

- [ ] **Step 1:** Create `Core/Utils/FactionBlipColor.cs` with the exact body of the existing file but `namespace FactionWars.Core.Utils`.
- [ ] **Step 2:** Delete `ScriptHookV/Utils/FactionBlipColor.cs`.
- [ ] **Step 3:** Add `using FactionWars.Core.Utils;` to the three caller files (grep `FactionBlipColor` to find them).
- [ ] **Step 4: Run gate** (build + unit tests). Expected: PASS, 0 warnings. This is a pure move — existing tests cover behaviour.
- [ ] **Step 5: Commit** `git commit -m "refactor(combat): move FactionBlipColor to Core.Utils for portability"`.

---

## Task 3: IAllegianceResolver + AllegianceResolver

**Files:**
- Create: `src/FactionWars/Combat/Interfaces/IAllegianceResolver.cs`
- Create: `src/FactionWars/Combat/Services/AllegianceResolver.cs`
- Test: `tests/FactionWars.Tests/Unit/Combat/AllegianceResolverTests.cs`

**Interfaces:**
- Consumes: `FactionBlipColor.ForFactionId` (Task 2); `CombatantProfile`, `Allegiance` (Task 1).
- Produces: `interface IAllegianceResolver { CombatantProfile Resolve(string combatantFactionId, string playerFactionId); }`; `class AllegianceResolver : IAllegianceResolver`.

- [ ] **Step 1: Write the failing tests**

```csharp
using FactionWars.Combat.Models;
using FactionWars.Combat.Services;
using FactionWars.Core.Interfaces;
using Xunit;

namespace FactionWars.Tests.Unit.Combat
{
    public class AllegianceResolverTests
    {
        private readonly AllegianceResolver _resolver = new AllegianceResolver();

        [Fact]
        public void Resolve_SameFactionAsPlayer_IsFriendlyWithOwnColourAndFactionGroup()
        {
            var profile = _resolver.Resolve("michael", "michael");

            Assert.Equal(Allegiance.Friendly, profile.Allegiance);
            Assert.Equal("MICHAEL", profile.RelationshipGroup);
            Assert.Equal(BlipColor.MichaelBlue, profile.BlipColor);
        }

        [Fact]
        public void Resolve_DifferentFactionFromPlayer_IsHostileWithItsOwnColour()
        {
            var profile = _resolver.Resolve("franklin", "michael");

            Assert.Equal(Allegiance.Hostile, profile.Allegiance);
            Assert.Equal("FRANKLIN", profile.RelationshipGroup);
            Assert.Equal(BlipColor.FranklinGreen, profile.BlipColor);
        }

        [Fact]
        public void Resolve_IsCaseInsensitiveOnFactionMatch()
        {
            var profile = _resolver.Resolve("Michael", "michael");
            Assert.Equal(Allegiance.Friendly, profile.Allegiance);
        }
    }
}
```

- [ ] **Step 2: Run tests — expect FAIL** (resolver not defined).
- [ ] **Step 3: Implement**

`IAllegianceResolver.cs`:
```csharp
using FactionWars.Combat.Models;

namespace FactionWars.Combat.Interfaces
{
    public interface IAllegianceResolver
    {
        CombatantProfile Resolve(string combatantFactionId, string playerFactionId);
    }
}
```

`AllegianceResolver.cs`:
```csharp
using System;
using FactionWars.Combat.Interfaces;
using FactionWars.Combat.Models;
using FactionWars.Core.Utils;

namespace FactionWars.Combat.Services
{
    public class AllegianceResolver : IAllegianceResolver
    {
        public CombatantProfile Resolve(string combatantFactionId, string playerFactionId)
        {
            if (combatantFactionId == null) throw new ArgumentNullException(nameof(combatantFactionId));

            var group = combatantFactionId.ToUpperInvariant();
            var blipColor = FactionBlipColor.ForFactionId(combatantFactionId);
            var allegiance = string.Equals(combatantFactionId, playerFactionId, StringComparison.OrdinalIgnoreCase)
                ? Allegiance.Friendly
                : Allegiance.Hostile;

            return new CombatantProfile(group, blipColor, allegiance);
        }
    }
}
```

- [ ] **Step 4: Run tests — expect PASS.**
- [ ] **Step 5: Commit** `git commit -m "feat(combat): add AllegianceResolver as single allegiance authority"`.

---

## Task 4: RelationshipMatrixInitializer

Establish all GTA relationship-group pairings once. Uses the existing bridge method `SetRelationshipBetweenGroups(string group1, string group2, int relationship, bool bidirectional)` (relationship 5 = Hate, 0 = Companion — confirm enum values against `GameBridge`). Needs the set of faction ids (from `IFactionRepository.GetAll()` → `Faction.Id`).

**Files:**
- Create: `src/FactionWars/ScriptHookV/Combat/IRelationshipMatrixInitializer.cs`
- Create: `src/FactionWars/ScriptHookV/Combat/RelationshipMatrixInitializer.cs`
- Test: `tests/FactionWars.Tests/Unit/ScriptHookV/Combat/RelationshipMatrixInitializerTests.cs`

**Interfaces:**
- Consumes: `IGameBridge.SetRelationshipBetweenGroups(string, string, int, bool)`.
- Produces: `interface IRelationshipMatrixInitializer { void Initialize(string playerFactionId, IReadOnlyList<string> allFactionIds); }`; class impl taking `IGameBridge` in ctor. Constants: `RelHate = 5`, `RelCompanion = 0`, `PlayerGroup = "PLAYER"`.

- [ ] **Step 1: Write the failing test** (uses `Mock<IGameBridge>`; asserts the pairings)

```csharp
using System.Collections.Generic;
using FactionWars.Core.Interfaces;
using FactionWars.ScriptHookV.Combat;
using Moq;
using Xunit;

namespace FactionWars.Tests.Unit.ScriptHookV.Combat
{
    public class RelationshipMatrixInitializerTests
    {
        private const int Hate = 5;
        private const int Companion = 0;

        [Fact]
        public void Initialize_HatesBetweenFactionsAndProtectsPlayerFaction()
        {
            var bridge = new Mock<IGameBridge>();
            var sut = new RelationshipMatrixInitializer(bridge.Object);

            sut.Initialize("michael", new List<string> { "michael", "franklin", "trevor" });

            // Faction-vs-faction hate (group names are uppercased)
            bridge.Verify(b => b.SetRelationshipBetweenGroups("MICHAEL", "FRANKLIN", Hate, true), Times.Once);
            bridge.Verify(b => b.SetRelationshipBetweenGroups("MICHAEL", "TREVOR", Hate, true), Times.Once);
            bridge.Verify(b => b.SetRelationshipBetweenGroups("FRANKLIN", "TREVOR", Hate, true), Times.Once);

            // Player's faction allied to PLAYER group; others hate PLAYER
            bridge.Verify(b => b.SetRelationshipBetweenGroups("MICHAEL", "PLAYER", Companion, true), Times.Once);
            bridge.Verify(b => b.SetRelationshipBetweenGroups("FRANKLIN", "PLAYER", Hate, true), Times.Once);
            bridge.Verify(b => b.SetRelationshipBetweenGroups("TREVOR", "PLAYER", Hate, true), Times.Once);
        }
    }
}
```

- [ ] **Step 2: Run test — expect FAIL.**
- [ ] **Step 3: Implement**

`IRelationshipMatrixInitializer.cs`:
```csharp
using System.Collections.Generic;

namespace FactionWars.ScriptHookV.Combat
{
    public interface IRelationshipMatrixInitializer
    {
        void Initialize(string playerFactionId, IReadOnlyList<string> allFactionIds);
    }
}
```

`RelationshipMatrixInitializer.cs`:
```csharp
using System;
using System.Collections.Generic;
using FactionWars.Core.Interfaces;

namespace FactionWars.ScriptHookV.Combat
{
    public class RelationshipMatrixInitializer : IRelationshipMatrixInitializer
    {
        private const int RelHate = 5;
        private const int RelCompanion = 0;
        private const string PlayerGroup = "PLAYER";

        private readonly IGameBridge _gameBridge;

        public RelationshipMatrixInitializer(IGameBridge gameBridge)
        {
            _gameBridge = gameBridge ?? throw new ArgumentNullException(nameof(gameBridge));
        }

        public void Initialize(string playerFactionId, IReadOnlyList<string> allFactionIds)
        {
            if (allFactionIds == null) throw new ArgumentNullException(nameof(allFactionIds));

            var groups = new List<string>(allFactionIds.Count);
            foreach (var id in allFactionIds) groups.Add(id.ToUpperInvariant());

            for (int i = 0; i < groups.Count; i++)
                for (int j = i + 1; j < groups.Count; j++)
                    _gameBridge.SetRelationshipBetweenGroups(groups[i], groups[j], RelHate, true);

            var playerGroupName = playerFactionId?.ToUpperInvariant();
            foreach (var group in groups)
            {
                var rel = group == playerGroupName ? RelCompanion : RelHate;
                _gameBridge.SetRelationshipBetweenGroups(group, PlayerGroup, rel, true);
            }
        }
    }
}
```

- [ ] **Step 4: Run test — expect PASS.** Confirm `IGameBridge.SetRelationshipBetweenGroups` signature matches (it is used by `ConfigureBattleRelationships` with `relationship: 5, bidirectional: true`).
- [ ] **Step 5: Commit** `git commit -m "feat(combat): add RelationshipMatrixInitializer (wire groups once)"`.

---

## Task 5: ZoneCombatantSpawner

Single spawn site. Resolves a profile, spawns the ped (group assigned by `PedSpawningService` from faction id — already `factionId.ToUpperInvariant()`), configures combat attributes by allegiance, blips with the profile colour. NO relationship mutation here.

**Files:**
- Create: `src/FactionWars/ScriptHookV/Combat/IZoneCombatantSpawner.cs`
- Create: `src/FactionWars/ScriptHookV/Combat/ZoneCombatantSpawner.cs`
- Test: `tests/FactionWars.Tests/Unit/ScriptHookV/Combat/ZoneCombatantSpawnerTests.cs`

**Interfaces:**
- Consumes: `IAllegianceResolver` (Task 3); `IPedSpawningService.SpawnPed(string model, Vector3 pos, string factionId, string? zoneId)`; `IPedBlipService.CreateBlipForPed(int, BlipColor)`; `IGameBridge` combat-config methods.
- Produces: `interface IZoneCombatantSpawner { PedHandle Spawn(string factionId, string playerFactionId, string model, Vector3 position, string zoneId); }` returning the spawned `PedHandle` (or `PedHandle.Invalid`). The caller supplies `model` (managers already resolve models via `FactionPedModels`) and `playerFactionId`.

- [ ] **Step 1: Write the failing test**

```csharp
using FactionWars.Combat.Interfaces;
using FactionWars.Combat.Models;
using FactionWars.Combat.Services;
using FactionWars.Core.Interfaces;
using FactionWars.ScriptHookV.Combat;
using FactionWars.UI.Interfaces;
using Moq;
using Xunit;

namespace FactionWars.Tests.Unit.ScriptHookV.Combat
{
    public class ZoneCombatantSpawnerTests
    {
        [Fact]
        public void Spawn_FriendlyCombatant_AssignsFactionGroupColourAndBlip()
        {
            var bridge = new Mock<IGameBridge>();
            var spawning = new Mock<IPedSpawningService>();
            var blips = new Mock<IPedBlipService>();
            spawning.Setup(s => s.SpawnPed("model", It.IsAny<Vector3>(), "michael", "z1"))
                    .Returns(new PedHandle(50));

            var sut = new ZoneCombatantSpawner(new AllegianceResolver(), spawning.Object, blips.Object, bridge.Object);

            var handle = sut.Spawn("michael", "michael", "model", new Vector3(1, 2, 3), "z1");

            Assert.Equal(50, handle.Handle);
            blips.Verify(b => b.CreateBlipForPed(50, BlipColor.MichaelBlue), Times.Once);
        }
    }
}
```

- [ ] **Step 2: Run test — expect FAIL.**
- [ ] **Step 3: Implement** `IZoneCombatantSpawner` + `ZoneCombatantSpawner`. The spawner calls `_resolver.Resolve(factionId, playerFactionId)`, `_pedSpawningService.SpawnPed(model, position, factionId, zoneId)`; if `!handle.IsValid` return `PedHandle.Invalid`; configure combat by `profile.Allegiance` (extract a private `ConfigureCombat(int handle, Allegiance allegiance)` calling the same `SET_PED_COMBAT_ATTRIBUTES`-style bridge methods the managers used — friendly vs hostile variants); `_pedBlipService.CreateBlipForPed(handle.Handle, profile.BlipColor)`; return handle. Keep public methods ≤ 1 (just `Spawn`) to respect CI0004.

- [ ] **Step 4: Run test — expect PASS.**
- [ ] **Step 5: Commit** `git commit -m "feat(combat): add ZoneCombatantSpawner single spawn site"`.

---

## Task 6: Route EnemyDefenderManager through the spawner

**Files:** Modify `EnemyDefenderManager.cs` / `.Spawning.cs` / `.Replacements.cs`; their dependency object; test `EnemyDefenderManagerTests`.

- [ ] **Step 1:** Add a failing test asserting an enemy defender is spawned with its faction colour blip via the spawner (or that `SetPedAsHostileWanderer`'s relationship mutation is no longer called per spawn).
- [ ] **Step 2:** Run — expect FAIL.
- [ ] **Step 3:** Inject `IZoneCombatantSpawner` + `IAllegianceResolver` (or pass `playerFactionId`); replace the direct `SpawnPed` + `FactionBlipColor` + `SetPedAsHostileWanderer` calls with `_spawner.Spawn(enemyFactionId, playerFactionId, model, pos, zoneId)`. Keep enemy-specific tasking (wander/seek) after spawn.
- [ ] **Step 4:** Run gate — expect PASS.
- [ ] **Step 5: Commit** `git commit -m "refactor(combat): EnemyDefenderManager spawns via ZoneCombatantSpawner"`.

---

## Task 7: Route BattleAttackerManager through the spawner

**Files:** Modify `BattleAttackerManager.Spawning.cs` (`SpawnSingleAttacker`, `SpawnAttackersForTier`); test `BattleAttackerManagerTests`.

- [ ] **Step 1:** Failing test: spawning an attacker uses the spawner and never spawns the player's own faction as hostile (route both initial and replacement selection through `GetHostileAttackerForPlayer`).
- [ ] **Step 2:** Run — expect FAIL.
- [ ] **Step 3:** Replace `SpawnPed`+blip+`SetPedAsHostileWanderer` in `SpawnSingleAttacker` with `_spawner.Spawn(attackerFactionId, playerFactionId, model, pos, zoneId)`; make `TrySpawnReplacement` resolve the attacker faction via `GetHostileAttackerForPlayer(battle)?.FactionId` and return false when null. Keep `ConfigureAttacker`'s tasking (combat range/ability/`TaskCombatHatedTargetsAroundPed`) but drop its `SetPedAsHostileWanderer` relationship mutation.
- [ ] **Step 4:** Run gate — expect PASS (existing attacker tests + new guard test).
- [ ] **Step 5: Commit** `git commit -m "refactor(combat): BattleAttackerManager spawns via spawner; unify player-faction guard"`.

---

## Task 8: Route FriendlyDefenderManager through the spawner

**Files:** Modify `FriendlyDefenderManager.cs` / `.Replacements.cs` (spawn sites at the `BlipColor.LightBlue` + `SetPedAsFriendly` lines); test `FriendlyDefenderManagerTests`.

- [ ] **Step 1:** Failing test: a friendly defender is spawned with the player faction's colour (not hardcoded `LightBlue`) via the spawner. NOTE: existing test `OnFriendlyZoneEntered_CreatesLightBlueBlips` asserts `LightBlue` — update it to assert `FactionBlipColor.ForFactionId(playerFactionId)` (e.g. `MichaelBlue` for the `"michael"`/`"player"` test faction; adjust the test's player faction to a real faction id if it currently uses `"player"`).
- [ ] **Step 2:** Run — expect FAIL.
- [ ] **Step 3:** Replace the three spawn sites' `SpawnPed`+`SetPedAsFriendly`+`CreateBlipForPed(LightBlue)` with `_spawner.Spawn(_playerFactionId, _playerFactionId, model, pos, zoneId)`. Keep wander/defend tasking.
- [ ] **Step 4:** Run gate — expect PASS.
- [ ] **Step 5: Commit** `git commit -m "refactor(combat): FriendlyDefenderManager spawns via spawner"`.

---

## Task 9: Strip per-spawn relationship mutation from the bridge

**Files:** Modify `GameBridge.FollowTasks.cs` (`SetPedAsFriendly`), `GameBridge.HostilePeds.cs` (`SetPedAsHostileWanderer`), `BattleAttackerManager.Spawning.cs` (`ConfigureBattleRelationships`).

- [ ] **Step 1:** Failing/guard test — assert `SetPedAsFriendly`/`SetPedAsHostileWanderer` no longer call `SetRelationshipBetweenGroups` (if behaviourally observable via mock; otherwise rely on the matrix test + build). For `ConfigureBattleRelationships`, assert it is no longer invoked (or removed).
- [ ] **Step 2:** Run — expect FAIL/red where observable.
- [ ] **Step 3:** Remove the `RelationshipGroup`/`SetRelationshipBetweenGroups` lines from both bridge methods (leave combat-attribute config, which now lives in the spawner — delete the methods entirely if the spawner fully replaces them and nothing else calls them; grep first). Remove `ConfigureBattleRelationships` and its call site.
- [ ] **Step 4:** Run gate — expect PASS.
- [ ] **Step 5: Commit** `git commit -m "refactor(combat): relationships wired once at init, not per spawn"`.

---

## Task 10: ZoneOwnershipReconciler

Replace the interim `OnZoneExited`-on-ownership-change hack with a dedicated reconciler that despawns the right combatants per transition.

**Files:**
- Create: `src/FactionWars/ScriptHookV/Managers/ZoneOwnershipReconciler.cs`
- Test: `tests/FactionWars.Tests/Unit/ScriptHookV/Managers/ZoneOwnershipReconcilerTests.cs`
- Modify: `GameLoopController.Initialization.cs` (replace the `ZoneOwnershipChanged` lambda that calls `OnZoneExited` with a `ZoneOwnershipReconciler` subscription); add `DespawnForZone(zoneId)` to `EnemyDefenderManager` (and use Friendly manager's existing despawn).

**Interfaces:**
- Produces: `class ZoneOwnershipReconciler` ctor `(IFriendlyDefenderManager friendly, IEnemyDefenderManager enemy, Func<string?> getPlayerFactionId)`; method `void OnOwnershipChanged(string zoneId, string? previousOwner, string? newOwner)`.

- [ ] **Step 1:** Failing tests: (a) zone leaves player (`newOwner != playerFaction`) → friendly `DespawnForZone(zoneId)` called once; (b) zone leaves a faction → that faction's enemy defenders despawned.
- [ ] **Step 2:** Run — expect FAIL.
- [ ] **Step 3:** Implement the reconciler; wire it in `GameLoopController` to `_zoneService.ZoneOwnershipChanged`; add per-zone despawn methods where missing (respect CI0004 — if `FriendlyDefenderManager` is at the public-method cap, route despawn through the existing `OnZoneExited` zone overload or split a partial).
- [ ] **Step 4:** Run gate — expect PASS.
- [ ] **Step 5: Commit** `git commit -m "feat(combat): ZoneOwnershipReconciler centralises ownership-loss despawn"`.

---

## Task 11: Wire RelationshipMatrixInitializer at init + character switch

**Files:** Modify `GameLoopController.Initialization.cs` (construct initializer, call `Initialize(CurrentPlayerFactionId, factionIds)` after factions load) and the character-switch handler (re-call `Initialize` with the new player faction).

- [ ] **Step 1:** (Integration-style) Failing test if a seam exists, else verify via build + a focused unit test that the character-switch handler calls `Initialize`.
- [ ] **Step 2:** Run — expect FAIL where testable.
- [ ] **Step 3:** Construct `new RelationshipMatrixInitializer(_gameBridge)`; gather faction ids from `_factionRepository.GetAll()`; call `Initialize` once game data is loaded and again on character switch (where `_playerFactionId` updates are propagated to managers today).
- [ ] **Step 4:** Run gate — expect PASS.
- [ ] **Step 5: Commit** `git commit -m "feat(combat): initialise relationship matrix at load and on character switch"`.

---

## Task 12 (gated on in-game verification): remove temporary diagnostics

Only after the user confirms in-game that friendly defenders are never hostile and blips match allegiance:

- [ ] Delete `FriendlyDefenderManager.Diagnostics.cs` + `GameBridge.Diagnostics.cs`, the `LogRelationshipDiagnostics` call in `FriendlyDefenderManager.UpdateAndSpawning.cs`, and the temp `IGameBridge` probes (`GetGroupRelationship`, `IsPedInCombatWithPlayer`, `GetPedRelationshipGroupHash`, `GetPlayerRelationshipGroupHash`) plus their `MockGameBridge` impls. Keep `IsPedInCombat` (used by leash logic) and `DisableControlThisFrame` (used by nav throttle).
- [ ] Run gate; commit `git commit -m "chore(combat): remove temporary allegiance diagnostics"`.

---

## Self-Review Notes

- **Spec coverage:** resolver (T3), matrix-once (T4, T11), spawner (T6-T8), reconciler (T10), bridge cleanup (T9), faction-group model (T2-T3) — all covered.
- **Risk:** Tasks 6-11 change in-game combat relationships; unit tests prove wiring, but behaviour (peds don't attack player; friendlies fight enemies) must be verified in-game before deploy. Do not deploy mid-refactor.
- **Analyzer watch:** new service classes need first-party interfaces (provided); keep one public top-level type per file; CRLF endings; watch CI0004 on `FriendlyDefenderManager`/`BattleAttackerManager` when adding `DespawnForZone` — prefer reusing existing methods or splitting a partial over exceeding 10 public methods.
