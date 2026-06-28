# Sniper Unit — Phase 0 + Phase 1 Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Reframe troop tiers into roles (`DefenderTier`→`DefenderRole`: Grunt/Gunner/Rifleman/Rocketeer) with a save-compatible migration, then add a **Sniper** specialist that perches on computed high ground when defending and switches to a sidearm when rushed.

**Architecture:** The existing unit-type enum gains a `Sniper` value and a config entry. A pure `PerchResolver` (portable `Combat` layer) picks high ground from sampled heights; a thin `SniperDeploymentService` (`ScriptHookV`) positions and guard-tasks snipers via `IGameBridge`, reusing the existing `TaskGuardArea`/`GetGroundZ` natives plus one new `SetPedActiveWeapon` native for the sidearm swap. Enemy + friendly defender spawn paths call the deployment service; menus and AI purchasing gain the Sniper option.

**Tech Stack:** C# / .NET Framework 4.8, ScriptHookVDotNet3, NativeUI, xUnit + Moq, Newtonsoft.Json.

## Global Constraints

- All `.cs` files: CRLF line endings, UTF-8 **without BOM**, trailing newline. Edit existing files with the Edit tool to preserve encoding; for new files verify encoding before commit.
- Build gate (pre-commit hook): `dotnet build FactionWars.sln --no-incremental` + `dotnet test … --filter "FullyQualifiedName~FactionWars.Tests.Unit"` must be **0 warnings / 0 errors**, all tests green.
- Custom analyzers are **errors**: no tuple return types; ≤10 public methods/class (interface-implementing members excluded); ≤5 ctor params; ≤40 effective method lines; ≤250 lines/class; one public top-level type per file; interfaces must live in an `Interfaces` namespace segment; constructors take interfaces, not concrete production types; no `?.`+`??` chains in method arguments.
- Layering: portable domain logic (`Core`, `Combat`) must **not** reference `GTA`/NativeUI or `ScriptHookV`. Native calls go behind `IGameBridge`.
- Test project has `<Nullable>enable</Nullable>` — use `null!` for null literals passed to non-nullable reference params.
- Enum integer values are a persistence contract: `Grunt=0, Gunner=1, Rifleman=2, Rocketeer=3, Sniper=4`. Never reorder.
- New GameBridge methods MUST include `FileLogger` logging and null/exists guards (per CLAUDE.md). Native parameter order is verified in-game, not by tests.
- Commit messages end with the two trailer lines used in this repo (`Co-Authored-By:` and `Claude-Session:`).

## File Structure

**Phase 0 — rename + migration**
- Modify (rename): `src/FactionWars/Core/Models/DefenderTier.cs` → `DefenderRole.cs` (type `DefenderRole`, members Grunt/Gunner/Rifleman/Rocketeer).
- Modify (rename): `DefenderTierConfig.cs`→`DefenderRoleConfig.cs` (`.Tier`→`.Role`), `IDefenderTierService.cs`/`DefenderTierService.cs`→`IDefenderRoleService.cs`/`DefenderRoleService.cs` (`GetTierConfig`→`GetRoleConfig`, `GetAllTierConfigs`→`GetAllRoleConfigs`).
- Modify: all ~93 production files + their tests referencing the renamed identifiers (mechanical).
- Create: `src/FactionWars/Persistence/Converters/LegacyRoleDictionaryConverter.cs` — tolerant Newtonsoft converter for the two persisted role dictionaries.
- Modify: `src/FactionWars/Persistence/Models/ZoneDefenderAllocationData.cs`, `FactionStateData.cs` — annotate the dictionaries with the converter.
- Test: `tests/FactionWars.Tests/Unit/Persistence/LegacyRoleDictionaryConverterTests.cs`, `tests/FactionWars.Tests/Unit/Core/DefenderRoleValuesTests.cs`.

**Phase 1 — sniper unit + perch defence**
- Modify: `DefenderRole.cs` (add `Sniper = 4`), `DefenderRoleService.cs` (add config), `src/FactionWars/Combat/Models/FactionPedModels.cs` (sniper models).
- Create: `src/FactionWars/Combat/Interfaces/IPerchResolver.cs`, `src/FactionWars/Combat/Services/PerchResolver.cs`, `src/FactionWars/Combat/Models/PerchSampling.cs` (constants).
- Create: `src/FactionWars/ScriptHookV/Combat/Interfaces/ISniperDeploymentService.cs`, `src/FactionWars/ScriptHookV/Combat/SniperDeploymentService.cs`.
- Modify: `src/FactionWars/Core/Interfaces/IGameBridge.cs`, new `src/FactionWars/ScriptHookV/GameBridge.SniperWeapon.cs`, `src/FactionWars/Core/Utils/MockGameBridge.cs` (add `SetPedActiveWeapon`).
- Modify: `EnemyDefenderManager.Spawning.cs`, `EnemyDefenderManagerDependencies.cs`, `FriendlyDefenderManager.UpdateAndSpawning.cs` (+ its config-combat method) and `FriendlyDefenderManagerDependencies.cs`, `ServiceContainerFactory*.cs` (wiring).
- Modify: `src/FactionWars/ScriptHookV/UI/DefendersMenuController.cs` (Sniper buy item), `src/FactionWars/AI/Services/AIRecruitmentService.Purchasing.cs` (sniper weighting).
- Test: matching test files under `tests/FactionWars.Tests/Unit/…`.

---

## Phase 0 — Rename + migration

### Task 1: Rename `DefenderTier` → `DefenderRole` (atomic)

A pure refactor. The existing test suite is the safety net — it must stay green. This must be a single commit so the solution never compiles in a half-renamed state.

**Files:** the renamed type/config/service files plus every referencing file (~93 production + tests). Use the analyzer build to find stragglers.

**Identifier mapping (apply globally, case-sensitive):**

| Old | New |
|---|---|
| `DefenderTier` (type) | `DefenderRole` |
| `DefenderTier.Basic` | `DefenderRole.Grunt` |
| `DefenderTier.Medium` | `DefenderRole.Gunner` |
| `DefenderTier.Heavy` | `DefenderRole.Rifleman` |
| `DefenderTier.Elite` | `DefenderRole.Rocketeer` |
| `DefenderTierConfig` | `DefenderRoleConfig` |
| `.Tier` (on a config) | `.Role` |
| `IDefenderTierService` | `IDefenderRoleService` |
| `DefenderTierService` | `DefenderRoleService` |
| `GetTierConfig` | `GetRoleConfig` |
| `GetAllTierConfigs` | `GetAllRoleConfigs` |

Keep `GetCost`, `GetCombatModifier`, `CalculateTotalCost`, `CalculateTotalStrength` names (their parameters are renamed `tier`→`role` for clarity but that is non-breaking). Local variables/parameters named `tier` should become `role`; this is optional for compilation but expected for a clean rename.

- [ ] **Step 1: Rename the enum file and members**

Rename `src/FactionWars/Core/Models/DefenderTier.cs` to `DefenderRole.cs` with this content (values preserved):

```csharp
namespace FactionWars.Core.Models
{
    /// <summary>
    /// The combat role of a defender troop. Each role has a distinct weapon,
    /// cost, and survivability profile. Integer values are a persistence
    /// contract and must never be reordered.
    /// </summary>
    public enum DefenderRole
    {
        /// <summary>Pistol, cheap and fragile. (Formerly Basic.)</summary>
        Grunt = 0,

        /// <summary>SMG, close/mid spray. (Formerly Medium.)</summary>
        Gunner = 1,

        /// <summary>Carbine, reliable line infantry. (Formerly Heavy.)</summary>
        Rifleman = 2,

        /// <summary>RPG anti-vehicle specialist. (Formerly Elite.)</summary>
        Rocketeer = 3
    }
}
```

- [ ] **Step 2: Rename config + service types and members**

In `DefenderTierConfig.cs` → `DefenderRoleConfig.cs`: rename the class to `DefenderRoleConfig` and the `Tier` property/ctor param to `Role` (type `DefenderRole`). In `IDefenderTierService.cs`/`DefenderTierService.cs` → `IDefenderRoleService.cs`/`DefenderRoleService.cs`: rename the interface/class, `GetTierConfig`→`GetRoleConfig`, `GetAllTierConfigs`→`GetAllRoleConfigs`, and the backing dictionary type to `Dictionary<DefenderRole, DefenderRoleConfig>`. The four config entries keep identical numbers; only the `tier:`→`role:` argument label and enum members change (e.g. `role: DefenderRole.Grunt`).

- [ ] **Step 3: Apply the mapping across the codebase**

Replace every remaining reference (production + tests) per the mapping table. Known hot spot: `src/FactionWars/ScriptHookV/Managers/EnemyDefenderManager.Spawning.cs` line 26 `if (tierConfig.Tier == DefenderTier.Elite)` → `if (roleConfig.Role == DefenderRole.Rocketeer)` (rename the `tierConfig` parameter to `roleConfig` in `ConfigureEnemyDefender`). `FriendlyDefenderManager.cs` line ~170 `Enum.GetValues(typeof(DefenderTier))` → `typeof(DefenderRole)`. Persisted DTOs (`ZoneDefenderAllocationData.Troops`, `FactionStateData.ReservePool`, `SavedFollowerState.Tier`→`Role`) change their dictionary/property enum type to `DefenderRole`.

- [ ] **Step 4: Build and run the full suite (this is the rename's test)**

Run: `dotnet build FactionWars.sln --no-incremental`
Expected: 0 warnings / 0 errors (fix every unresolved old identifier the compiler reports).

Run: `dotnet test tests/FactionWars.Tests/FactionWars.Tests.csproj --filter "FullyQualifiedName~FactionWars.Tests.Unit"`
Expected: all tests pass (same count as before the rename). Behavior is unchanged, so no test logic changes beyond the identifier rename.

- [ ] **Step 5: Commit**

```bash
git add -A
git commit -F <message-file>
```
Message subject: `Rename defender tiers to roles (#39)`.

---

### Task 2: Legacy-save migration + value-stability guard

Saves serialize `Dictionary<DefenderRole,int>` **keys as member names** (Newtonsoft default). Existing saves contain `"Basic"/"Medium"/"Heavy"/"Elite"`; after Task 1 those names are gone, so loading throws. Add a tolerant converter that maps legacy names (and ints and the new names) on read, and writes canonical new names.

**Files:**
- Create: `src/FactionWars/Persistence/Converters/LegacyRoleDictionaryConverter.cs`
- Modify: `src/FactionWars/Persistence/Models/ZoneDefenderAllocationData.cs`, `src/FactionWars/Persistence/Models/FactionStateData.cs`
- Test: `tests/FactionWars.Tests/Unit/Persistence/LegacyRoleDictionaryConverterTests.cs`, `tests/FactionWars.Tests/Unit/Core/DefenderRoleValuesTests.cs`

**Interfaces:**
- Produces: `LegacyRoleDictionaryConverter : JsonConverter<Dictionary<DefenderRole,int>>` with public parameterless ctor.

- [ ] **Step 1: Write the failing converter test**

Create `tests/FactionWars.Tests/Unit/Persistence/LegacyRoleDictionaryConverterTests.cs`:

```csharp
using System.Collections.Generic;
using FactionWars.Core.Models;
using FactionWars.Persistence.Converters;
using Newtonsoft.Json;
using Xunit;

namespace FactionWars.Tests.Unit.Persistence
{
    public class LegacyRoleDictionaryConverterTests
    {
        private static readonly JsonSerializerSettings Settings = new JsonSerializerSettings
        {
            Converters = { new LegacyRoleDictionaryConverter() }
        };

        [Fact]
        public void Read_LegacyTierNames_MapsToRoles()
        {
            var json = "{\"Basic\":3,\"Medium\":2,\"Heavy\":1,\"Elite\":4}";

            var result = JsonConvert.DeserializeObject<Dictionary<DefenderRole, int>>(json, Settings);

            Assert.Equal(3, result![DefenderRole.Grunt]);
            Assert.Equal(2, result[DefenderRole.Gunner]);
            Assert.Equal(1, result[DefenderRole.Rifleman]);
            Assert.Equal(4, result[DefenderRole.Rocketeer]);
        }

        [Fact]
        public void Read_NewRoleNames_RoundTrips()
        {
            var json = "{\"Grunt\":5,\"Sniper\":2}";

            var result = JsonConvert.DeserializeObject<Dictionary<DefenderRole, int>>(json, Settings);

            Assert.Equal(5, result![DefenderRole.Grunt]);
            Assert.Equal(2, result[DefenderRole.Sniper]);
        }

        [Fact]
        public void Write_EmitsNewRoleNames()
        {
            var dict = new Dictionary<DefenderRole, int> { { DefenderRole.Grunt, 1 } };

            var json = JsonConvert.SerializeObject(dict, Settings);

            Assert.Contains("\"Grunt\"", json);
            Assert.DoesNotContain("Basic", json);
        }
    }
}
```

> Note: `DefenderRole.Sniper` is added in Task 3. If executing strictly in order, write the `Read_NewRoleNames_RoundTrips` assertion against `DefenderRole.Rifleman` instead and update it in Task 3. The other two tests are valid immediately.

- [ ] **Step 2: Run the test to verify it fails**

Run: `dotnet test … --filter "FullyQualifiedName~LegacyRoleDictionaryConverterTests"`
Expected: FAIL — `LegacyRoleDictionaryConverter` does not exist (compile error).

- [ ] **Step 3: Implement the converter**

Create `src/FactionWars/Persistence/Converters/LegacyRoleDictionaryConverter.cs`:

```csharp
using System;
using System.Collections.Generic;
using FactionWars.Core.Models;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace FactionWars.Persistence.Converters
{
    /// <summary>
    /// Deserializes a role-keyed troop dictionary while tolerating legacy
    /// tier key names (Basic/Medium/Heavy/Elite) from saves written before the
    /// role rename. Serializes using canonical role names.
    /// </summary>
    public sealed class LegacyRoleDictionaryConverter : JsonConverter<Dictionary<DefenderRole, int>>
    {
        private static readonly Dictionary<string, DefenderRole> LegacyNames =
            new Dictionary<string, DefenderRole>(StringComparer.OrdinalIgnoreCase)
            {
                { "Basic", DefenderRole.Grunt },
                { "Medium", DefenderRole.Gunner },
                { "Heavy", DefenderRole.Rifleman },
                { "Elite", DefenderRole.Rocketeer }
            };

        public override Dictionary<DefenderRole, int> ReadJson(
            JsonReader reader,
            Type objectType,
            Dictionary<DefenderRole, int>? existingValue,
            bool hasExistingValue,
            JsonSerializer serializer)
        {
            var result = new Dictionary<DefenderRole, int>();
            var obj = JObject.Load(reader);
            foreach (var property in obj.Properties())
            {
                var role = ResolveRole(property.Name);
                result[role] = property.Value.Value<int>();
            }

            return result;
        }

        public override void WriteJson(
            JsonWriter writer,
            Dictionary<DefenderRole, int> value,
            JsonSerializer serializer)
        {
            writer.WriteStartObject();
            foreach (var kvp in value)
            {
                writer.WritePropertyName(kvp.Key.ToString());
                writer.WriteValue(kvp.Value);
            }

            writer.WriteEndObject();
        }

        private static DefenderRole ResolveRole(string key)
        {
            if (LegacyNames.TryGetValue(key, out var legacy))
                return legacy;
            if (Enum.TryParse<DefenderRole>(key, ignoreCase: true, out var role))
                return role;
            if (int.TryParse(key, out var numeric) && Enum.IsDefined(typeof(DefenderRole), numeric))
                return (DefenderRole)numeric;

            throw new JsonSerializationException($"Unknown defender role key '{key}'.");
        }
    }
}
```

- [ ] **Step 4: Annotate the persisted dictionaries**

In `ZoneDefenderAllocationData.cs`, change the `Troops` property to:

```csharp
[JsonConverter(typeof(LegacyRoleDictionaryConverter))]
public Dictionary<DefenderRole, int> Troops { get; set; } = new Dictionary<DefenderRole, int>();
```

Do the same for `FactionStateData.ReservePool`. Add `using FactionWars.Persistence.Converters;` and `using Newtonsoft.Json;` to both files.

- [ ] **Step 5: Add the value-stability guard test**

Create `tests/FactionWars.Tests/Unit/Core/DefenderRoleValuesTests.cs`:

```csharp
using FactionWars.Core.Models;
using Xunit;

namespace FactionWars.Tests.Unit.Core
{
    public class DefenderRoleValuesTests
    {
        [Theory]
        [InlineData(DefenderRole.Grunt, 0)]
        [InlineData(DefenderRole.Gunner, 1)]
        [InlineData(DefenderRole.Rifleman, 2)]
        [InlineData(DefenderRole.Rocketeer, 3)]
        public void Role_HasStablePersistedValue(DefenderRole role, int expected)
        {
            Assert.Equal(expected, (int)role);
        }
    }
}
```

- [ ] **Step 6: Run tests to verify they pass**

Run: `dotnet test … --filter "FullyQualifiedName~LegacyRoleDictionaryConverterTests|FullyQualifiedName~DefenderRoleValuesTests"`
Expected: PASS.

- [ ] **Step 7: Commit**

Message subject: `Migrate legacy tier keys on save load (#39)`.

---

## Phase 1 — Sniper unit + perch defence

### Task 3: Add the `Sniper` role + config

**Files:** Modify `src/FactionWars/Core/Models/DefenderRole.cs`, `src/FactionWars/Core/Services/DefenderRoleService.cs`; update `tests/FactionWars.Tests/Unit/Core/DefenderRoleValuesTests.cs` and add `tests/FactionWars.Tests/Unit/Core/DefenderRoleServiceSniperTests.cs`.

**Interfaces:**
- Produces: `DefenderRole.Sniper = 4`; `DefenderRoleService.GetRoleConfig(DefenderRole.Sniper)` returns a config with Cost=1500, Health=275, Armor=50, Weapon="WEAPON_SNIPERRIFLE", Accuracy=0.8f, CombatModifier=2.2f, RagdollEnabled=false.

- [ ] **Step 1: Write the failing config test**

Create `tests/FactionWars.Tests/Unit/Core/DefenderRoleServiceSniperTests.cs`:

```csharp
using FactionWars.Core.Models;
using FactionWars.Core.Services;
using Xunit;

namespace FactionWars.Tests.Unit.Core
{
    public class DefenderRoleServiceSniperTests
    {
        private readonly DefenderRoleService _service = new DefenderRoleService();

        [Fact]
        public void GetRoleConfig_Sniper_HasSpecialistStats()
        {
            var config = _service.GetRoleConfig(DefenderRole.Sniper);

            Assert.Equal(DefenderRole.Sniper, config.Role);
            Assert.Equal(1500, config.Cost);
            Assert.Equal(275, config.Health);
            Assert.Equal(50, config.Armor);
            Assert.Equal("WEAPON_SNIPERRIFLE", config.Weapon);
            Assert.Equal(0.8f, config.Accuracy);
            Assert.False(config.RagdollEnabled);
        }

        [Fact]
        public void GetAllRoleConfigs_IncludesFiveRoles()
        {
            Assert.Equal(5, _service.GetAllRoleConfigs().Count);
        }
    }
}
```

- [ ] **Step 2: Run to verify it fails**

Run: `dotnet test … --filter "FullyQualifiedName~DefenderRoleServiceSniperTests"`
Expected: FAIL — `DefenderRole.Sniper` undefined (compile error).

- [ ] **Step 3: Add the enum member**

In `DefenderRole.cs`, add after `Rocketeer = 3`:

```csharp
,

        /// <summary>Sniper rifle, long-range / perch specialist. Fragile up close.</summary>
        Sniper = 4
```

- [ ] **Step 4: Add the config entry**

In `DefenderRoleService.cs`, add to the `_configs` dictionary initializer after the `Rocketeer` entry:

```csharp
,
                {
                    DefenderRole.Sniper,
                    new DefenderRoleConfig(
                        role: DefenderRole.Sniper,
                        cost: 1500,
                        health: 275,
                        armor: 50,
                        weapon: "WEAPON_SNIPERRIFLE",
                        accuracy: 0.8f,
                        combatModifier: 2.2f,
                        ragdollEnabled: false)
                }
```

- [ ] **Step 5: Extend the value-stability test**

Add to `DefenderRoleValuesTests`:

```csharp
        [Fact]
        public void Sniper_HasValueFour()
        {
            Assert.Equal(4, (int)DefenderRole.Sniper);
        }
```

If Task 2's `Read_NewRoleNames_RoundTrips` was written against `Rifleman`, switch it back to `DefenderRole.Sniper` now.

- [ ] **Step 6: Run to verify pass**

Run: `dotnet test … --filter "FullyQualifiedName~DefenderRoleServiceSniperTests|FullyQualifiedName~DefenderRoleValuesTests"`
Expected: PASS.

- [ ] **Step 7: Commit** — `Add Sniper role and config (#39)`.

---

### Task 4: Sniper ped models per faction

**Files:** Modify `src/FactionWars/Combat/Models/FactionPedModels.cs`; add `tests/FactionWars.Tests/Unit/Combat/FactionPedModelsSniperTests.cs`.

**Interfaces:**
- Produces: `FactionPedModels.GetModel(faction, DefenderRole.Sniper)` returns a faction-specific model; unknown faction returns `FallbackModel`.

- [ ] **Step 1: Write the failing test**

Create `tests/FactionWars.Tests/Unit/Combat/FactionPedModelsSniperTests.cs`:

```csharp
using FactionWars.Combat.Models;
using FactionWars.Core.Models;
using Xunit;

namespace FactionWars.Tests.Unit.Combat
{
    public class FactionPedModelsSniperTests
    {
        [Theory]
        [InlineData("franklin")]
        [InlineData("trevor")]
        [InlineData("michael")]
        public void GetModel_Sniper_ReturnsFactionSpecificModel(string faction)
        {
            var model = FactionPedModels.GetModel(faction, DefenderRole.Sniper);

            Assert.False(string.IsNullOrEmpty(model));
            Assert.NotEqual(FactionPedModels.FallbackModel, model);
        }

        [Fact]
        public void GetModel_Sniper_UnknownFaction_ReturnsFallback()
        {
            Assert.Equal(FactionPedModels.FallbackModel, FactionPedModels.GetModel("nobody", DefenderRole.Sniper));
        }
    }
}
```

- [ ] **Step 2: Run to verify it fails**

Run: `dotnet test … --filter "FullyQualifiedName~FactionPedModelsSniperTests"`
Expected: FAIL — sniper key missing, `GetModel` returns `FallbackModel` for known factions.

- [ ] **Step 3: Add sniper models**

In each faction dictionary in `FactionPedModels.cs`, add a `Sniper` entry (any human model; weapon defines the role — exact IDs verified in-game):

```csharp
                { DefenderRole.Sniper, "s_m_y_blackops_03" }   // franklin: add this line (use a faction-fitting model)
```

Use distinct, faction-themed models, e.g. franklin `"g_m_y_famfor_01"`, trevor `"g_m_y_lost_02"`, michael `"s_m_y_blackops_03"`. (These are placeholders to be confirmed in-game; any valid ped model satisfies the test.)

- [ ] **Step 4: Run to verify pass**

Run: `dotnet test … --filter "FullyQualifiedName~FactionPedModelsSniperTests"`
Expected: PASS.

- [ ] **Step 5: Commit** — `Add faction sniper ped models (#39)`.

---

### Task 5: `PerchResolver` — pick high ground (pure)

**Files:** Create `src/FactionWars/Combat/Interfaces/IPerchResolver.cs`, `src/FactionWars/Combat/Services/PerchResolver.cs`, `src/FactionWars/Combat/Models/PerchSampling.cs`; Test `tests/FactionWars.Tests/Unit/Combat/PerchResolverTests.cs`.

**Interfaces:**
- Produces: `IPerchResolver.Resolve(Vector3 center, float searchRadius, int sampleCount, Func<float,float,float> heightAt) → Vector3`. Returns the sampled point (one ring of `sampleCount` points at `searchRadius`, plus the center) with the greatest height; ties keep the earliest; returns the center when no sample beats it. `PerchSampling.DefaultSampleCount = 8`, `PerchSampling.DefaultSearchRadius = 25f`, `PerchSampling.GuardRadius = 6f`, `PerchSampling.ProbeHeight = 50f`.

- [ ] **Step 1: Write the failing test**

Create `tests/FactionWars.Tests/Unit/Combat/PerchResolverTests.cs`:

```csharp
using System;
using FactionWars.Combat.Services;
using FactionWars.Core.Interfaces;
using Xunit;

namespace FactionWars.Tests.Unit.Combat
{
    public class PerchResolverTests
    {
        private readonly PerchResolver _resolver = new PerchResolver();

        [Fact]
        public void Resolve_PicksHighestSampledPoint()
        {
            var center = new Vector3(100f, 100f, 0f);
            // Height is high only near x = 125 (east sample at radius 25).
            float HeightAt(float x, float y) => Math.Abs(x - 125f) < 0.5f ? 40f : 5f;

            var perch = _resolver.Resolve(center, 25f, 8, HeightAt);

            Assert.Equal(125f, perch.X, 3);
            Assert.Equal(100f, perch.Y, 3);
            Assert.Equal(40f, perch.Z, 3);
        }

        [Fact]
        public void Resolve_NoHighGround_FallsBackToCenter()
        {
            var center = new Vector3(0f, 0f, 10f);
            float HeightAt(float x, float y) => 3f; // everything lower than center's own sample

            var perch = _resolver.Resolve(center, 25f, 8, HeightAt);

            Assert.Equal(0f, perch.X, 3);
            Assert.Equal(0f, perch.Y, 3);
            Assert.Equal(3f, perch.Z, 3); // center sampled height
        }
    }
}
```

- [ ] **Step 2: Run to verify it fails**

Run: `dotnet test … --filter "FullyQualifiedName~PerchResolverTests"`
Expected: FAIL — `PerchResolver` undefined.

- [ ] **Step 3: Implement the constants, interface, and resolver**

`src/FactionWars/Combat/Models/PerchSampling.cs`:

```csharp
namespace FactionWars.Combat.Models
{
    /// <summary>Tuning constants for sniper perch resolution and guarding.</summary>
    public static class PerchSampling
    {
        public const int DefaultSampleCount = 8;
        public const float DefaultSearchRadius = 25f;
        public const float GuardRadius = 6f;
        public const float ProbeHeight = 50f;
    }
}
```

`src/FactionWars/Combat/Interfaces/IPerchResolver.cs`:

```csharp
using System;
using FactionWars.Core.Interfaces;

namespace FactionWars.Combat.Interfaces
{
    /// <summary>Picks a high-ground perch position near a center point.</summary>
    public interface IPerchResolver
    {
        /// <summary>
        /// Samples a ring of <paramref name="sampleCount"/> points at
        /// <paramref name="searchRadius"/> around <paramref name="center"/>
        /// (plus the center itself) and returns the one with the greatest
        /// height as reported by <paramref name="heightAt"/>. Returns the
        /// center when no sample is higher.
        /// </summary>
        Vector3 Resolve(Vector3 center, float searchRadius, int sampleCount, Func<float, float, float> heightAt);
    }
}
```

`src/FactionWars/Combat/Services/PerchResolver.cs`:

```csharp
using System;
using FactionWars.Combat.Interfaces;
using FactionWars.Core.Interfaces;

namespace FactionWars.Combat.Services
{
    /// <inheritdoc />
    public sealed class PerchResolver : IPerchResolver
    {
        public Vector3 Resolve(Vector3 center, float searchRadius, int sampleCount, Func<float, float, float> heightAt)
        {
            if (heightAt == null)
                throw new ArgumentNullException(nameof(heightAt));

            float bestX = center.X;
            float bestY = center.Y;
            float bestZ = heightAt(center.X, center.Y);

            for (int i = 0; i < sampleCount; i++)
            {
                double angle = 2.0 * Math.PI * i / sampleCount;
                float x = center.X + (float)Math.Cos(angle) * searchRadius;
                float y = center.Y + (float)Math.Sin(angle) * searchRadius;
                float z = heightAt(x, y);
                if (z > bestZ)
                {
                    bestX = x;
                    bestY = y;
                    bestZ = z;
                }
            }

            return new Vector3(bestX, bestY, bestZ);
        }
    }
}
```

- [ ] **Step 4: Run to verify pass**

Run: `dotnet test … --filter "FullyQualifiedName~PerchResolverTests"`
Expected: PASS.

- [ ] **Step 5: Commit** — `Add PerchResolver high-ground picker (#39)`.

---

### Task 6: `SetPedActiveWeapon` native

**Files:** Modify `src/FactionWars/Core/Interfaces/IGameBridge.cs`; create `src/FactionWars/ScriptHookV/GameBridge.SniperWeapon.cs`; modify `src/FactionWars/Core/Utils/MockGameBridge.cs`; test `tests/FactionWars.Tests/Unit/Core/MockGameBridgeSniperWeaponTests.cs`.

**Interfaces:**
- Produces: `IGameBridge.SetPedActiveWeapon(int pedHandle, string weaponName)`; `MockGameBridge.GetPedActiveWeapon(int pedHandle)` returns the last weapon set (or empty string).

- [ ] **Step 1: Write the failing mock test**

Create `tests/FactionWars.Tests/Unit/Core/MockGameBridgeSniperWeaponTests.cs`:

```csharp
using FactionWars.Core.Interfaces;
using FactionWars.Core.Utils;
using Xunit;

namespace FactionWars.Tests.Unit.Core
{
    public class MockGameBridgeSniperWeaponTests
    {
        [Fact]
        public void SetPedActiveWeapon_RecordsLastWeapon()
        {
            var bridge = new MockGameBridge();
            int ped = bridge.CreatePed("s_m_y_blackops_03", new Vector3(0f, 0f, 0f), "michael", "zone1");

            bridge.SetPedActiveWeapon(ped, "weapon_pistol");
            Assert.Equal("weapon_pistol", bridge.GetPedActiveWeapon(ped));

            bridge.SetPedActiveWeapon(ped, "WEAPON_SNIPERRIFLE");
            Assert.Equal("WEAPON_SNIPERRIFLE", bridge.GetPedActiveWeapon(ped));
        }
    }
}
```

> Confirm the exact `CreatePed` signature in `MockGameBridge` before running (Task 7's recon shows peds are created with model/position/faction/zone). Adjust the call to match.

- [ ] **Step 2: Run to verify it fails**

Run: `dotnet test … --filter "FullyQualifiedName~MockGameBridgeSniperWeaponTests"`
Expected: FAIL — `SetPedActiveWeapon`/`GetPedActiveWeapon` undefined.

- [ ] **Step 3: Add the interface method**

In `IGameBridge.cs`, near the other ped-weapon methods, add:

```csharp
        /// <summary>
        /// Forces the ped's currently-equipped weapon to the named weapon
        /// (the ped must already own it). Used to swap a sniper between rifle
        /// and sidearm.
        /// </summary>
        void SetPedActiveWeapon(int pedHandle, string weaponName);
```

- [ ] **Step 4: Implement the real native**

Create `src/FactionWars/ScriptHookV/GameBridge.SniperWeapon.cs`:

```csharp
using System;
using FactionWars.ScriptHookV.Logging;
using GTA;
using GTA.Native;

namespace FactionWars.ScriptHookV
{
    public partial class GameBridge
    {
        /// <inheritdoc />
        public void SetPedActiveWeapon(int pedHandle, string weaponName)
        {
            try
            {
                var ped = Entity.FromHandle(pedHandle) as Ped;
                if (ped == null || !ped.Exists())
                {
                    FileLogger.Warn($"SetPedActiveWeapon: ped {pedHandle} missing");
                    return;
                }

                var weaponHash = GetWeaponHash(weaponName);
                // SET_CURRENT_PED_WEAPON(ped, weaponHash, bForceInHand)
                Function.Call(Hash.SET_CURRENT_PED_WEAPON, ped.Handle, (uint)weaponHash, true);
                FileLogger.AI($"SetPedActiveWeapon: ped {pedHandle} -> {weaponName}");
            }
            catch (Exception ex)
            {
                FileLogger.Error($"SetPedActiveWeapon exception for ped {pedHandle}, weapon {weaponName}", ex);
            }
        }
    }
}
```

> `GetWeaponHash` is the existing private helper used by `GivePedWeapon` (see `GameBridge.PedWeapons.cs`). If it returns a `WeaponHash` enum, cast as that code does; match its existing usage exactly. Native parameter order verified in-game.

- [ ] **Step 5: Implement the mock**

In `MockGameBridge.cs`, add a recording field and the method/getter near the other weapon methods (the `PedState` inner class already exists; add an `ActiveWeapon` field to it or use a side dictionary):

```csharp
        private readonly Dictionary<int, string> _activeWeapon = new Dictionary<int, string>();

        public void SetPedActiveWeapon(int pedHandle, string weaponName)
        {
            _activeWeapon[pedHandle] = weaponName;
        }

        public string GetPedActiveWeapon(int pedHandle) =>
            _activeWeapon.TryGetValue(pedHandle, out var w) ? w : string.Empty;
```

- [ ] **Step 6: Run to verify pass**

Run: `dotnet test … --filter "FullyQualifiedName~MockGameBridgeSniperWeaponTests"`
Expected: PASS.

- [ ] **Step 7: Commit** — `Add SetPedActiveWeapon native for sidearm swap (#39)`.

---

### Task 7: `SniperDeploymentService` — perch + close-defense decision

**Files:** Create `src/FactionWars/ScriptHookV/Combat/Interfaces/ISniperDeploymentService.cs`, `src/FactionWars/ScriptHookV/Combat/SniperDeploymentService.cs`; test `tests/FactionWars.Tests/Unit/ScriptHookV/Combat/SniperDeploymentServiceTests.cs`.

**Interfaces:**
- Consumes: `IPerchResolver`, `IGameBridge` (`GetGroundZ`, `SetPedPosition`, `TaskGuardArea`, `GetPedPosition`, `SetPedActiveWeapon`), `DefenderRoleConfig`, `PerchSampling`.
- Produces:
  - `void DeployIfSniper(int pedHandle, DefenderRoleConfig roleConfig, Vector3 zoneCenter)` — no-op unless `roleConfig.Role == Sniper`; otherwise resolves a perch and guard-tasks the ped there.
  - `void UpdateCloseDefense(int sniperHandle, IReadOnlyList<Vector3> threatPositions)` — swaps the ped to `"weapon_pistol"` when the nearest threat is within `SidearmThresholdMeters` (15f), else restores `"WEAPON_SNIPERRIFLE"`; only calls the native when the chosen weapon changes.
  - `const float SidearmThresholdMeters = 15f`.

**Note on size:** keep this class ≤250 lines and ≤10 public methods (2 public methods here is fine). The change-tracking dictionary for last-applied weapon lives in the service.

- [ ] **Step 1: Write the failing test**

Create `tests/FactionWars.Tests/Unit/ScriptHookV/Combat/SniperDeploymentServiceTests.cs`:

```csharp
using System.Collections.Generic;
using FactionWars.Combat.Models;
using FactionWars.Combat.Services;
using FactionWars.Core.Interfaces;
using FactionWars.Core.Models;
using FactionWars.Core.Utils;
using FactionWars.ScriptHookV.Combat;
using Xunit;

namespace FactionWars.Tests.Unit.ScriptHookV.Combat
{
    public class SniperDeploymentServiceTests
    {
        private readonly MockGameBridge _bridge = new MockGameBridge();
        private readonly SniperDeploymentService _service;

        public SniperDeploymentServiceTests()
        {
            _service = new SniperDeploymentService(new PerchResolver(), _bridge);
        }

        private DefenderRoleConfig SniperConfig() =>
            new DefenderRoleConfig(DefenderRole.Sniper, 1500, 275, 50, "WEAPON_SNIPERRIFLE", 0.8f, 2.2f, false);

        private DefenderRoleConfig GruntConfig() =>
            new DefenderRoleConfig(DefenderRole.Grunt, 200, 200, 50, "weapon_pistol", 0.3f, 1.0f, true);

        [Fact]
        public void DeployIfSniper_Sniper_GuardsResolvedPerch()
        {
            int ped = _bridge.CreatePed("s_m_y_blackops_03", new Vector3(0f, 0f, 0f), "michael", "zone1");
            var center = new Vector3(100f, 100f, 20f);
            // East sample (x≈125) is highest.
            _bridge.GroundZResolver = (x, y, z) => System.Math.Abs(x - 125f) < 1f ? 60f : 21f;

            _service.DeployIfSniper(ped, SniperConfig(), center);

            Assert.True(_bridge.IsPedGuardingArea(ped));
            Assert.Equal(125f, _bridge.GetGuardAreaCenter(ped).X, 0);
        }

        [Fact]
        public void DeployIfSniper_NonSniper_DoesNothing()
        {
            int ped = _bridge.CreatePed("a_m_m_business_01", new Vector3(0f, 0f, 0f), "michael", "zone1");

            _service.DeployIfSniper(ped, GruntConfig(), new Vector3(100f, 100f, 20f));

            Assert.False(_bridge.IsPedGuardingArea(ped));
        }

        [Fact]
        public void UpdateCloseDefense_ThreatClose_SwitchesToSidearm()
        {
            int ped = _bridge.CreatePed("s_m_y_blackops_03", new Vector3(0f, 0f, 0f), "michael", "zone1");

            _service.UpdateCloseDefense(ped, new List<Vector3> { new Vector3(5f, 0f, 0f) });

            Assert.Equal("weapon_pistol", _bridge.GetPedActiveWeapon(ped));
        }

        [Fact]
        public void UpdateCloseDefense_ThreatFar_UsesRifle()
        {
            int ped = _bridge.CreatePed("s_m_y_blackops_03", new Vector3(0f, 0f, 0f), "michael", "zone1");

            _service.UpdateCloseDefense(ped, new List<Vector3> { new Vector3(40f, 0f, 0f) });

            Assert.Equal("WEAPON_SNIPERRIFLE", _bridge.GetPedActiveWeapon(ped));
        }
    }
}
```

> Confirm `MockGameBridge.CreatePed` and `GroundZResolver` signatures (recon: `GetGroundZ(x,y,z)` is driven by a `GroundZResolver` delegate; `GetPedPosition` returns the create position). Adjust calls if needed.

- [ ] **Step 2: Run to verify it fails**

Run: `dotnet test … --filter "FullyQualifiedName~SniperDeploymentServiceTests"`
Expected: FAIL — `SniperDeploymentService` undefined.

- [ ] **Step 3: Implement the interface and service**

`src/FactionWars/ScriptHookV/Combat/Interfaces/ISniperDeploymentService.cs`:

```csharp
using System.Collections.Generic;
using FactionWars.Core.Interfaces;
using FactionWars.Core.Models;

namespace FactionWars.ScriptHookV.Combat.Interfaces
{
    /// <summary>Positions snipers on high ground and manages their close-range sidearm swap.</summary>
    public interface ISniperDeploymentService
    {
        /// <summary>Perches and guard-tasks the ped if its role is Sniper; otherwise no-op.</summary>
        void DeployIfSniper(int pedHandle, DefenderRoleConfig roleConfig, Vector3 zoneCenter);

        /// <summary>Swaps the sniper to a sidearm when a threat is within range, else the rifle.</summary>
        void UpdateCloseDefense(int sniperHandle, IReadOnlyList<Vector3> threatPositions);
    }
}
```

`src/FactionWars/ScriptHookV/Combat/SniperDeploymentService.cs`:

```csharp
using System.Collections.Generic;
using FactionWars.Combat.Interfaces;
using FactionWars.Combat.Models;
using FactionWars.Core.Interfaces;
using FactionWars.Core.Models;
using FactionWars.ScriptHookV.Combat.Interfaces;
using FactionWars.ScriptHookV.Logging;

namespace FactionWars.ScriptHookV.Combat
{
    /// <inheritdoc />
    public sealed class SniperDeploymentService : ISniperDeploymentService
    {
        public const float SidearmThresholdMeters = 15f;
        private const string Rifle = "WEAPON_SNIPERRIFLE";
        private const string Sidearm = "weapon_pistol";

        private readonly IPerchResolver _perchResolver;
        private readonly IGameBridge _gameBridge;
        private readonly Dictionary<int, string> _lastWeapon = new Dictionary<int, string>();

        public SniperDeploymentService(IPerchResolver perchResolver, IGameBridge gameBridge)
        {
            _perchResolver = perchResolver;
            _gameBridge = gameBridge;
        }

        public void DeployIfSniper(int pedHandle, DefenderRoleConfig roleConfig, Vector3 zoneCenter)
        {
            if (roleConfig.Role != DefenderRole.Sniper)
                return;

            var perch = _perchResolver.Resolve(
                zoneCenter,
                PerchSampling.DefaultSearchRadius,
                PerchSampling.DefaultSampleCount,
                (x, y) => _gameBridge.GetGroundZ(x, y, zoneCenter.Z + PerchSampling.ProbeHeight));

            _gameBridge.SetPedPosition(pedHandle, perch);
            _gameBridge.TaskGuardArea(pedHandle, perch, PerchSampling.GuardRadius);
            _lastWeapon[pedHandle] = Rifle;
            FileLogger.AI($"SniperDeployment: ped {pedHandle} perched at ({perch.X:F1},{perch.Y:F1},{perch.Z:F1})");
        }

        public void UpdateCloseDefense(int sniperHandle, IReadOnlyList<Vector3> threatPositions)
        {
            var sniperPos = _gameBridge.GetPedPosition(sniperHandle);
            bool threatClose = false;
            foreach (var threat in threatPositions)
            {
                if (sniperPos.DistanceTo(threat) <= SidearmThresholdMeters)
                {
                    threatClose = true;
                    break;
                }
            }

            var desired = threatClose ? Sidearm : Rifle;
            if (_lastWeapon.TryGetValue(sniperHandle, out var current) && current == desired)
                return;

            _lastWeapon[sniperHandle] = desired;
            _gameBridge.SetPedActiveWeapon(sniperHandle, desired);
        }
    }
}
```

- [ ] **Step 4: Run to verify pass**

Run: `dotnet test … --filter "FullyQualifiedName~SniperDeploymentServiceTests"`
Expected: PASS.

- [ ] **Step 5: Commit** — `Add SniperDeploymentService (perch + sidearm) (#39)`.

---

### Task 8: Wire perch into enemy defenders

**Files:** Modify `src/FactionWars/ScriptHookV/Managers/EnemyDefenderManager.Spawning.cs`, `src/FactionWars/ScriptHookV/Models/EnemyDefenderManagerDependencies.cs`, `EnemyDefenderManager.cs` (ctor field), `src/FactionWars/ScriptHookV/ServiceContainerFactory*.cs` (registration). Test: extend an existing enemy-defender manager test fixture or add `tests/FactionWars.Tests/Unit/ScriptHookV/Managers/EnemyDefenderManagerSniperTests.cs`.

**Interfaces:**
- Consumes: `ISniperDeploymentService.DeployIfSniper`.

- [ ] **Step 1: Write the failing test**

Add `tests/FactionWars.Tests/Unit/ScriptHookV/Managers/EnemyDefenderManagerSniperTests.cs` that constructs an `EnemyDefenderManager` with a real `SniperDeploymentService(new PerchResolver(), mockBridge)`, spawns a zone defender allocation containing a Sniper, ticks the spawn, and asserts the spawned sniper ped `IsPedGuardingArea`. Mirror the construction used by the existing `EnemyDefenderManagerTests` (read that file for the exact dependency wiring and spawn-trigger helper).

```csharp
// Skeleton — match existing EnemyDefenderManagerTests setup for dependencies and spawn trigger.
[Fact]
public void Spawn_SniperAllocation_PerchesAndGuards()
{
    // Arrange: allocation with one DefenderRole.Sniper in a zone the manager spawns for.
    // Act: trigger the manager's spawn path.
    // Assert: the spawned sniper handle reports _bridge.IsPedGuardingArea(handle) == true.
}
```

- [ ] **Step 2: Run to verify it fails**

Expected: FAIL — no guard task issued (deployment not wired).

- [ ] **Step 3: Add the dependency**

In `EnemyDefenderManagerDependencies.cs` add:

```csharp
        public ISniperDeploymentService? SniperDeployment { get; set; }
```

(add `using FactionWars.ScriptHookV.Combat.Interfaces;`). In `EnemyDefenderManager.cs`, store it in a field `private readonly ISniperDeploymentService _sniperDeployment;` from the dependencies (match the existing null-check/assignment pattern used for the other dependencies).

- [ ] **Step 4: Call the deployment in `ConfigureEnemyDefender`**

At the end of `ConfigureEnemyDefender` (after `TaskCombatHatedTargetsAroundPed`), add:

```csharp
            _sniperDeployment.DeployIfSniper(pedHandle, roleConfig, zoneCenter);
```

(`roleConfig` is the renamed `tierConfig` parameter; `zoneCenter` is already a parameter.)

- [ ] **Step 5: Register in the container**

In the factory that builds `EnemyDefenderManagerDependencies` (`ServiceContainerFactory*.cs`), construct one `SniperDeploymentService` (shared `IPerchResolver` + `IGameBridge`) and assign it to `SniperDeployment`. Register `PerchResolver` as `IPerchResolver` and `SniperDeploymentService` as `ISniperDeploymentService` if the container resolves them by interface.

- [ ] **Step 6: Run to verify pass + full suite**

Run: `dotnet test … --filter "FullyQualifiedName~EnemyDefenderManagerSniperTests"` then the full unit suite.
Expected: PASS, no regressions.

- [ ] **Step 7: Commit** — `Perch enemy snipers on spawn (#39)`.

---

### Task 9: Wire perch into friendly defenders

**Files:** Modify `src/FactionWars/ScriptHookV/Managers/FriendlyDefenderManager.*.cs` (the method that applies combat config at spawn — recon: `ConfigureDefenderCombat(pedHandle, roleConfig)`), `FriendlyDefenderManagerDependencies.cs`, `FriendlyDefenderManager.cs` (field), `ServiceContainerFactory*.cs`. Test: `tests/FactionWars.Tests/Unit/ScriptHookV/FriendlyDefenderManagerSniperTests.cs`.

**Interfaces:**
- Consumes: `ISniperDeploymentService.DeployIfSniper`.

- [ ] **Step 1: Write the failing test**

Mirror Task 8's test against `FriendlyDefenderManager` (read `FriendlyDefenderManagerTests` for setup). Assert a spawned friendly sniper `IsPedGuardingArea`.

- [ ] **Step 2: Run to verify it fails** — FAIL (not wired).

- [ ] **Step 3: Add the dependency + field**

Add `ISniperDeploymentService? SniperDeployment { get; set; }` to `FriendlyDefenderManagerDependencies.cs` and a `_sniperDeployment` field in `FriendlyDefenderManager.cs`, matching existing patterns.

- [ ] **Step 4: Call deployment after stat application**

Locate `ConfigureDefenderCombat(int pedHandle, DefenderRoleConfig roleConfig)` (the method that calls `SetPedHealth`/`SetPedAccuracy` for friendly defenders). It needs the zone center; fetch it where the method is invoked (the caller knows `zoneId` → `_zoneService.GetZone(zoneId).Center`). Add, after the stat calls:

```csharp
            _sniperDeployment.DeployIfSniper(pedHandle, roleConfig, zoneCenter);
```

If `ConfigureDefenderCombat` lacks a `zoneCenter` parameter, add one and thread `zone.Center` from the spawn call site.

- [ ] **Step 5: Register in the container** (reuse the same `SniperDeploymentService` instance as Task 8 if the factory builds both managers; otherwise construct one).

- [ ] **Step 6: Run to verify pass + full suite** — PASS, no regressions.

- [ ] **Step 7: Commit** — `Perch friendly snipers on spawn (#39)`.

---

### Task 10: Sniper buy option in the defenders menu

**Files:** Modify `src/FactionWars/ScriptHookV/UI/DefendersMenuController.cs`; extend `tests/FactionWars.Tests/Unit/ScriptHookV/UI/DefendersMenuControllerTests.cs`.

**Interfaces:**
- Consumes: existing `AddPurchaseItem(menu, itemId, DefenderRole role, string description, factionId)` and a new `PurchaseSniperItemId` constant.

- [ ] **Step 1: Write the failing test**

Read `DefendersMenuControllerTests` for its build-menu assertion style, then add a test asserting the built menu contains a Sniper buy item (an item whose title contains "Sniper") when the player can afford it. Match the existing mock-purchase-service setup.

- [ ] **Step 2: Run to verify it fails** — FAIL (no sniper item).

- [ ] **Step 3: Add the buy item**

Add a constant alongside the other `Purchase*ItemId` constants:

```csharp
        public const string PurchaseSniperItemId = "purchase_sniper";
```

After the `Elite`/`Rocketeer` purchase line in the menu builder, add:

```csharp
            AddPurchaseItem(menu, PurchaseSniperItemId, DefenderRole.Sniper, "Sniper rifle, long-range specialist", factionId);
```

Ensure the selection handler that maps item IDs to a purchase (read the file's `OnItemSelected`/equivalent) handles `PurchaseSniperItemId` → buy `DefenderRole.Sniper`, mirroring the existing roles.

- [ ] **Step 4: Run to verify pass** — PASS.

- [ ] **Step 5: Commit** — `Add sniper purchase option to defenders menu (#39)`.

---

### Task 11: AI buys snipers as a capped minority

**Files:** Modify `src/FactionWars/AI/Services/AIRecruitmentService.Purchasing.cs` (and `AIRecruitmentService.cs` if the `recruited` dict initializer lives there); extend `tests/FactionWars.Tests/Unit/AI/Services/AIRecruitmentServiceTests.cs`.

**Interfaces:**
- Consumes: `IDefenderRoleService.GetCost`, `_factionService.AddReserveTroops`.
- Produces: AI garrisons include at most `ceil(totalTroops / SniperPerNDefenders)` snipers (use `SniperPerNDefenders = 6`), bought only when wealth ≥ `MidWealthThreshold`, before standard troops, within budget/slots.

- [ ] **Step 1: Write the failing test**

Add to `AIRecruitmentServiceTests` (match its existing arrange/act for a recruitment run):

```csharp
[Fact]
public void Recruit_WealthyFaction_BuysCappedSnipers()
{
    // Arrange a faction with high cash and a recruitment run of N troops.
    // Act: run recruitment.
    // Assert: reserve received >= 1 Sniper but <= ceil(N / 6).
}

[Fact]
public void Recruit_PoorFaction_BuysNoSnipers()
{
    // Arrange cash below MidWealthThreshold.
    // Assert: zero snipers recruited.
}
```

Read the test file for the exact faction/service mocks and the method that triggers a recruitment pass.

- [ ] **Step 2: Run to verify it fails** — FAIL (no snipers bought).

- [ ] **Step 3: Implement the sniper buy step**

Add a constant `private const int SniperPerNDefenders = 6;` and the dict entry `{ DefenderRole.Sniper, 0 }` to the `recruited` initializer. Add a method:

```csharp
        private int BuySnipers(
            int cash,
            int maxTroops,
            Dictionary<DefenderRole, int> recruited,
            out int remainingSlots)
        {
            int remainingBudget = cash;
            remainingSlots = maxTroops;
            if (cash < MidWealthThreshold)
                return remainingBudget;

            int snipersWanted = Math.Min(remainingSlots, (maxTroops + SniperPerNDefenders - 1) / SniperPerNDefenders);
            int sniperCost = RoleService.GetCost(DefenderRole.Sniper);
            for (int i = 0; i < snipersWanted && remainingSlots > 0 && remainingBudget >= sniperCost; i++)
            {
                recruited[DefenderRole.Sniper]++;
                remainingBudget -= sniperCost;
                remainingSlots--;
            }

            return remainingBudget;
        }
```

Call `BuySnipers` in the recruitment sequence before `BuyEliteTroops`/`BuyStandardTroops`, threading `remainingBudget`/`remainingSlots` through (the existing methods already chain budget and slots — insert sniper buying at the front of that chain). `RoleService` is the renamed `TierService`.

- [ ] **Step 4: Run to verify pass + full suite** — PASS, no regressions.

- [ ] **Step 5: Commit** — `AI recruits snipers as a capped minority (#39)`.

---

## Self-Review

**Spec coverage:**
- Rename tiers→roles (full type + members, values preserved) → Task 1. ✅
- Persistence migration (legacy names, two dicts) → Task 2. ✅
- Sniper unit + config (cost/health/weapon/accuracy/ragdoll) → Task 3. ✅
- Faction ped models → Task 4. ✅
- Perch (computed high ground) → Task 5 (`PerchResolver`) + Tasks 8–9 (defender wiring). ✅
- Rushed→sidearm → Task 6 (native) + Task 7 (`UpdateCloseDefense`). ⚠️ See gap below.
- Economy/UI exposure → Task 10. ✅
- AI minority weighting → Task 11. ✅
- **Phase 2 (overwatch) + Phase 3 (bodyguard):** intentionally deferred to follow-up plans (documented in the spec's phase list). ✅

**Gap to close during execution:** `UpdateCloseDefense` (Task 7) is built and unit-tested, but this Phase-0/1 plan does not yet wire its per-tick call into the defender managers' `Update` loops (the managers would need to supply threat positions — player position for enemy snipers, hostile handles via the existing `IHostilePedHandleSource` for friendly snipers). Add a **Task 12** during execution that calls `UpdateCloseDefense` each tick for tracked snipers, OR fold it into Tasks 8–9. Flagged here rather than hidden: the sidearm swap is inert until wired.

**Placeholder scan:** ped model IDs (Task 4) and the manager-test skeletons (Tasks 8, 9, 11) are explicitly marked "match the existing fixture" because the exact dependency wiring must be read from the current test files — these are instructions to read real code, not unfilled blanks. All new components carry complete code.

**Type consistency:** `DefenderRole`, `DefenderRoleConfig`, `.Role`, `GetRoleConfig`, `GetAllRoleConfigs`, `IDefenderRoleService`/`DefenderRoleService`, `RoleService` (AI field), `ISniperDeploymentService.DeployIfSniper`/`UpdateCloseDefense`, `IPerchResolver.Resolve`, `SetPedActiveWeapon`/`GetPedActiveWeapon`, `PerchSampling.*` are used consistently across tasks.
