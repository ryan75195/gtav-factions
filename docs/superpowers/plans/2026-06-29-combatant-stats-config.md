# Per-Category Combatant Stats via config.json — Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Move per-combatant combat stats (health, armor, accuracy, weapon, weapon-damage multiplier) out of the hardcoded faction-shared `DefenderRoleService` into `config.json`, with independent full per-role tables per category (Player, Squad, Friendlies, Enemies), plus player health/armor and damage in/out multipliers.

**Architecture:** Extend the existing `GameConfig`/`ConfigLoader` JSON system with a new top-level `Combatants` section. A new Core `ICombatantStatsProvider` maps (category, role) → stats and is injected into each spawn site, replacing the combat-stat fields previously read off `DefenderRoleConfig`. `DefenderRoleService` keeps cost/troop-strength/combatModifier/ragdoll. Four new `IGameBridge` methods apply the weapon-damage multiplier and player stats. Load-on-startup; defaults reproduce current values exactly.

**Tech Stack:** C# / .NET Framework 4.8, ScriptHookVDotNet3, Newtonsoft.Json, xUnit + Moq.

## Global Constraints

- Spec: `docs/superpowers/specs/2026-06-29-combatant-stats-config-design.md`.
- All work on branch `feat/130-combatant-stats-config`. Commits blocked on master.
- Pre-commit hook runs `dotnet build FactionWars.sln --no-incremental` + unit tests; allow ≥5 min.
- Run tests with `--filter "FullyQualifiedName~FactionWars.Tests.Unit"` (the hook's filter); integration tests run unfiltered.
- Analyzer rules (errors): files ≤250 lines (CI0017), methods ≤40 lines (CI0007), ≤10 public methods/class (CI0004), no tuple returns, no concrete disposable creation in ctors, one public top-level type per file. New public methods need a matching test fixture.
- GTA/native (`GTA.*`, `Function.Call`, `Hash.*`) references stay in `ScriptHookV`. `Core` must not reference `ScriptHookV` or GTA.
- New `GameBridge` methods MUST include `FileLogger` debug logging and a `MockGameBridge` implementation with a test getter.
- **Defaults reproduce these exact current values** (post-#128):
  - Grunt 200hp/50ar/0.25acc/WEAPON_PISTOL; Gunner 350/100/0.45/WEAPON_SMG; Rifleman 500/200/0.60/WEAPON_CARBINERIFLE; Rocketeer 650/200/0.70/WEAPON_RPG; Sniper 275/50/0.70/WEAPON_SNIPERRIFLE. `DamageMultiplier` 1.0 all.
  - Player MaxHealth 200, SpawnArmor 0, OutgoingDamageMultiplier 1.0, IncomingDamageMultiplier 1.0.
- `DefenderRole` enum: `Grunt=0, Gunner=1, Rifleman=2, Rocketeer=3, Sniper=4` (`Core/Models/DefenderRole.cs`).

---

## File Structure

- `src/FactionWars/Configuration/RoleStatsConfig.cs` (new) — JSON DTO for one role's stats.
- `src/FactionWars/Configuration/PlayerStatsConfig.cs` (new) — JSON DTO for the player block.
- `src/FactionWars/Configuration/CategoryStatsConfig.cs` (new) — five `RoleStatsConfig` per-role properties.
- `src/FactionWars/Configuration/CombatantsConfig.cs` (new) — Player/Enemies/Squad/Friendlies.
- `src/FactionWars/Configuration/GameConfig.cs` (modify) — add `Combatants` property.
- `src/FactionWars/Core/Models/CombatantCategory.cs` (new) — enum.
- `src/FactionWars/Core/Models/RoleStats.cs` (new) — immutable Core value type.
- `src/FactionWars/Core/Models/PlayerStats.cs` (new) — immutable Core value type.
- `src/FactionWars/Core/Interfaces/ICombatantStatsProvider.cs` (new).
- `src/FactionWars/Core/Services/CombatantStatsProvider.cs` (new) — maps config → stats.
- `src/FactionWars/Core/Interfaces/IGameBridge.cs` (modify) — 4 new methods.
- `src/FactionWars/ScriptHookV/GameBridge.PedWeapons.cs` (modify) — `SetPedWeaponDamageModifier`.
- `src/FactionWars/ScriptHookV/GameBridge.PlayerState.cs` (modify) — 3 player methods.
- `src/FactionWars/Core/Utils/MockGameBridge.cs` (modify) — 4 mock impls + getters.
- `src/FactionWars/ScriptHookV/ServiceContainerFactory.cs` (modify) — register provider.
- Manager dependency classes + spawn sites (Tasks 5–6).
- `src/FactionWars/ScriptHookV/GameBridge.PlayerState.cs` + respawn flow (Task 7).

---

### Task 1: Config DTOs + `GameConfig.Combatants` with current-value defaults

**Files:**
- Create: `src/FactionWars/Configuration/RoleStatsConfig.cs`, `PlayerStatsConfig.cs`, `CategoryStatsConfig.cs`, `CombatantsConfig.cs`
- Modify: `src/FactionWars/Configuration/GameConfig.cs`
- Test: `tests/FactionWars.Tests/Unit/Configuration/CombatantsConfigTests.cs` (new)

**Interfaces:**
- Produces: `CombatantsConfig { PlayerStatsConfig Player; CategoryStatsConfig Enemies, Squad, Friendlies }`; `CategoryStatsConfig { RoleStatsConfig Grunt, Gunner, Rifleman, Rocketeer, Sniper }`; `RoleStatsConfig { int Health; int Armor; float Accuracy; string Weapon; float DamageMultiplier }`; `PlayerStatsConfig { int MaxHealth; int SpawnArmor; float OutgoingDamageMultiplier; float IncomingDamageMultiplier }`; `GameConfig.Combatants`.

- [ ] **Step 1: Write the failing test**

`tests/FactionWars.Tests/Unit/Configuration/CombatantsConfigTests.cs`:
```csharp
using FactionWars.Configuration;
using Newtonsoft.Json;
using Xunit;

namespace FactionWars.Tests.Unit.Configuration
{
    public class CombatantsConfigTests
    {
        [Fact]
        public void Default_Enemies_ReproduceCurrentRoleValues()
        {
            var c = new GameConfig().Combatants.Enemies;
            Assert.Equal(500, c.Rifleman.Health);
            Assert.Equal(200, c.Rifleman.Armor);
            Assert.Equal(0.60f, c.Rifleman.Accuracy, 2);
            Assert.Equal("WEAPON_CARBINERIFLE", c.Rifleman.Weapon);
            Assert.Equal(1.0f, c.Rifleman.DamageMultiplier, 2);
            Assert.Equal(0.25f, c.Grunt.Accuracy, 2);
            Assert.Equal(0.70f, c.Sniper.Accuracy, 2);
        }

        [Fact]
        public void Default_AllCategories_StartIdentical()
        {
            var cfg = new GameConfig().Combatants;
            Assert.Equal(cfg.Enemies.Gunner.Health, cfg.Squad.Gunner.Health);
            Assert.Equal(cfg.Enemies.Gunner.Health, cfg.Friendlies.Gunner.Health);
        }

        [Fact]
        public void Default_Player_IsVanilla()
        {
            var p = new GameConfig().Combatants.Player;
            Assert.Equal(200, p.MaxHealth);
            Assert.Equal(0, p.SpawnArmor);
            Assert.Equal(1.0f, p.OutgoingDamageMultiplier, 2);
            Assert.Equal(1.0f, p.IncomingDamageMultiplier, 2);
        }

        [Fact]
        public void RoundTrip_PreservesPerCategoryOverride()
        {
            var cfg = new GameConfig();
            cfg.Combatants.Enemies.Rifleman.Accuracy = 0.4f;
            var json = JsonConvert.SerializeObject(cfg);
            var back = JsonConvert.DeserializeObject<GameConfig>(json)!;
            Assert.Equal(0.4f, back.Combatants.Enemies.Rifleman.Accuracy, 2);
            Assert.Equal(0.6f, back.Combatants.Squad.Rifleman.Accuracy, 2);
        }
    }
}
```

- [ ] **Step 2: Run test to verify it fails**

Run: `dotnet test tests/FactionWars.Tests/FactionWars.Tests.csproj --filter "FullyQualifiedName~CombatantsConfigTests"`
Expected: FAIL to compile — `GameConfig` has no `Combatants`.

- [ ] **Step 3: Create the DTOs**

`RoleStatsConfig.cs`:
```csharp
namespace FactionWars.Configuration
{
    /// <summary>Combat stats for one defender role within a category.</summary>
    public class RoleStatsConfig
    {
        public int Health { get; set; }
        public int Armor { get; set; }
        public float Accuracy { get; set; }
        public string Weapon { get; set; } = "WEAPON_PISTOL";
        public float DamageMultiplier { get; set; } = 1.0f;
    }
}
```

`PlayerStatsConfig.cs`:
```csharp
namespace FactionWars.Configuration
{
    /// <summary>Player-specific combat tunables.</summary>
    public class PlayerStatsConfig
    {
        public int MaxHealth { get; set; } = 200;
        public int SpawnArmor { get; set; } = 0;
        public float OutgoingDamageMultiplier { get; set; } = 1.0f;
        public float IncomingDamageMultiplier { get; set; } = 1.0f;
    }
}
```

`CategoryStatsConfig.cs` (defaults reproduce current values; the same factory is reused by all three NPC categories):
```csharp
namespace FactionWars.Configuration
{
    /// <summary>Full per-role stat table for one combatant category.</summary>
    public class CategoryStatsConfig
    {
        public RoleStatsConfig Grunt { get; set; } = new RoleStatsConfig
        { Health = 200, Armor = 50, Accuracy = 0.25f, Weapon = "WEAPON_PISTOL", DamageMultiplier = 1.0f };

        public RoleStatsConfig Gunner { get; set; } = new RoleStatsConfig
        { Health = 350, Armor = 100, Accuracy = 0.45f, Weapon = "WEAPON_SMG", DamageMultiplier = 1.0f };

        public RoleStatsConfig Rifleman { get; set; } = new RoleStatsConfig
        { Health = 500, Armor = 200, Accuracy = 0.60f, Weapon = "WEAPON_CARBINERIFLE", DamageMultiplier = 1.0f };

        public RoleStatsConfig Rocketeer { get; set; } = new RoleStatsConfig
        { Health = 650, Armor = 200, Accuracy = 0.70f, Weapon = "WEAPON_RPG", DamageMultiplier = 1.0f };

        public RoleStatsConfig Sniper { get; set; } = new RoleStatsConfig
        { Health = 275, Armor = 50, Accuracy = 0.70f, Weapon = "WEAPON_SNIPERRIFLE", DamageMultiplier = 1.0f };
    }
}
```

`CombatantsConfig.cs`:
```csharp
namespace FactionWars.Configuration
{
    /// <summary>Per-category combatant stats. Defaults reproduce the pre-config hardcoded values.</summary>
    public class CombatantsConfig
    {
        public PlayerStatsConfig Player { get; set; } = new PlayerStatsConfig();
        public CategoryStatsConfig Enemies { get; set; } = new CategoryStatsConfig();
        public CategoryStatsConfig Squad { get; set; } = new CategoryStatsConfig();
        public CategoryStatsConfig Friendlies { get; set; } = new CategoryStatsConfig();
    }
}
```

- [ ] **Step 4: Add `Combatants` to `GameConfig`**

In `GameConfig.cs`, add alongside the existing sections:
```csharp
        public CombatantsConfig Combatants { get; set; } = new CombatantsConfig();
```

- [ ] **Step 5: Run tests to verify they pass**

Run: `dotnet test tests/FactionWars.Tests/FactionWars.Tests.csproj --filter "FullyQualifiedName~CombatantsConfigTests"`
Expected: PASS (4 tests).

- [ ] **Step 6: Commit**

```bash
git add src/FactionWars/Configuration tests/FactionWars.Tests/Unit/Configuration/CombatantsConfigTests.cs
git commit -m "feat: add Combatants config section with current-value defaults (#130)"
```

---

### Task 2: Core stats provider

**Files:**
- Create: `src/FactionWars/Core/Models/CombatantCategory.cs`, `RoleStats.cs`, `PlayerStats.cs`; `src/FactionWars/Core/Interfaces/ICombatantStatsProvider.cs`; `src/FactionWars/Core/Services/CombatantStatsProvider.cs`
- Test: `tests/FactionWars.Tests/Unit/Core/CombatantStatsProviderTests.cs` (new)

**Interfaces:**
- Consumes: `CombatantsConfig` (Task 1), `DefenderRole`.
- Produces: `enum CombatantCategory { Player, Squad, Friendlies, Enemies }`; `RoleStats { int Health; int Armor; float Accuracy; string Weapon; float DamageMultiplier }` (ctor in that order); `PlayerStats { int MaxHealth; int SpawnArmor; float OutgoingDamageMultiplier; float IncomingDamageMultiplier }`; `ICombatantStatsProvider.GetRoleStats(CombatantCategory, DefenderRole)`, `.GetPlayerStats()`; `CombatantStatsProvider(CombatantsConfig)`.

- [ ] **Step 1: Write the failing test**

`tests/FactionWars.Tests/Unit/Core/CombatantStatsProviderTests.cs`:
```csharp
using FactionWars.Configuration;
using FactionWars.Core.Models;
using FactionWars.Core.Services;
using Xunit;

namespace FactionWars.Tests.Unit.Core
{
    public class CombatantStatsProviderTests
    {
        private static CombatantStatsProvider Default()
            => new CombatantStatsProvider(new CombatantsConfig());

        [Fact]
        public void GetRoleStats_DefaultEnemyRifleman_MatchesCurrentValues()
        {
            var s = Default().GetRoleStats(CombatantCategory.Enemies, DefenderRole.Rifleman);
            Assert.Equal(500, s.Health);
            Assert.Equal(200, s.Armor);
            Assert.Equal(0.60f, s.Accuracy, 2);
            Assert.Equal("WEAPON_CARBINERIFLE", s.Weapon);
            Assert.Equal(1.0f, s.DamageMultiplier, 2);
        }

        [Fact]
        public void GetRoleStats_ReadsPerCategoryOverride()
        {
            var cfg = new CombatantsConfig();
            cfg.Friendlies.Sniper.DamageMultiplier = 8.0f;
            var s = new CombatantStatsProvider(cfg).GetRoleStats(CombatantCategory.Friendlies, DefenderRole.Sniper);
            Assert.Equal(8.0f, s.DamageMultiplier, 2);
            Assert.Equal(1.0f, Default().GetRoleStats(CombatantCategory.Enemies, DefenderRole.Sniper).DamageMultiplier, 2);
        }

        [Fact]
        public void GetPlayerStats_DefaultIsVanilla()
        {
            var p = Default().GetPlayerStats();
            Assert.Equal(200, p.MaxHealth);
            Assert.Equal(0, p.SpawnArmor);
            Assert.Equal(1.0f, p.OutgoingDamageMultiplier, 2);
            Assert.Equal(1.0f, p.IncomingDamageMultiplier, 2);
        }
    }
}
```

- [ ] **Step 2: Run test to verify it fails**

Run: `dotnet test tests/FactionWars.Tests/FactionWars.Tests.csproj --filter "FullyQualifiedName~CombatantStatsProviderTests"`
Expected: FAIL to compile — types do not exist.

- [ ] **Step 3: Create the Core types**

`CombatantCategory.cs`:
```csharp
namespace FactionWars.Core.Models
{
    /// <summary>Which side a combatant is on, relative to the player.</summary>
    public enum CombatantCategory { Player, Squad, Friendlies, Enemies }
}
```

`RoleStats.cs`:
```csharp
namespace FactionWars.Core.Models
{
    /// <summary>Immutable per-role combat stats resolved for a category.</summary>
    public sealed class RoleStats
    {
        public RoleStats(int health, int armor, float accuracy, string weapon, float damageMultiplier)
        {
            Health = health; Armor = armor; Accuracy = accuracy;
            Weapon = weapon; DamageMultiplier = damageMultiplier;
        }

        public int Health { get; }
        public int Armor { get; }
        public float Accuracy { get; }
        public string Weapon { get; }
        public float DamageMultiplier { get; }
    }
}
```

`PlayerStats.cs`:
```csharp
namespace FactionWars.Core.Models
{
    /// <summary>Immutable player combat tunables.</summary>
    public sealed class PlayerStats
    {
        public PlayerStats(int maxHealth, int spawnArmor, float outgoingDamageMultiplier, float incomingDamageMultiplier)
        {
            MaxHealth = maxHealth; SpawnArmor = spawnArmor;
            OutgoingDamageMultiplier = outgoingDamageMultiplier;
            IncomingDamageMultiplier = incomingDamageMultiplier;
        }

        public int MaxHealth { get; }
        public int SpawnArmor { get; }
        public float OutgoingDamageMultiplier { get; }
        public float IncomingDamageMultiplier { get; }
    }
}
```

`ICombatantStatsProvider.cs`:
```csharp
using FactionWars.Core.Models;

namespace FactionWars.Core.Interfaces
{
    /// <summary>Supplies per-category combat stats, backed by config.json.</summary>
    public interface ICombatantStatsProvider
    {
        RoleStats GetRoleStats(CombatantCategory category, DefenderRole role);
        PlayerStats GetPlayerStats();
    }
}
```

- [ ] **Step 4: Create the provider**

`CombatantStatsProvider.cs`:
```csharp
using System;
using FactionWars.Configuration;
using FactionWars.Core.Interfaces;
using FactionWars.Core.Models;

namespace FactionWars.Core.Services
{
    /// <inheritdoc />
    public sealed class CombatantStatsProvider : ICombatantStatsProvider
    {
        private readonly CombatantsConfig _config;

        public CombatantStatsProvider(CombatantsConfig config)
        {
            _config = config ?? throw new ArgumentNullException(nameof(config));
        }

        public RoleStats GetRoleStats(CombatantCategory category, DefenderRole role)
        {
            var table = TableFor(category);
            var r = RoleConfigFor(table, role);
            return new RoleStats(r.Health, r.Armor, r.Accuracy, r.Weapon, r.DamageMultiplier);
        }

        public PlayerStats GetPlayerStats()
        {
            var p = _config.Player;
            return new PlayerStats(p.MaxHealth, p.SpawnArmor, p.OutgoingDamageMultiplier, p.IncomingDamageMultiplier);
        }

        private CategoryStatsConfig TableFor(CombatantCategory category)
        {
            switch (category)
            {
                case CombatantCategory.Squad: return _config.Squad;
                case CombatantCategory.Friendlies: return _config.Friendlies;
                case CombatantCategory.Enemies: return _config.Enemies;
                default: throw new ArgumentOutOfRangeException(nameof(category), category, "Player has no per-role stats; use GetPlayerStats().");
            }
        }

        private static RoleStatsConfig RoleConfigFor(CategoryStatsConfig table, DefenderRole role)
        {
            switch (role)
            {
                case DefenderRole.Grunt: return table.Grunt;
                case DefenderRole.Gunner: return table.Gunner;
                case DefenderRole.Rifleman: return table.Rifleman;
                case DefenderRole.Rocketeer: return table.Rocketeer;
                case DefenderRole.Sniper: return table.Sniper;
                default: throw new ArgumentOutOfRangeException(nameof(role), role, null);
            }
        }
    }
}
```

- [ ] **Step 5: Run tests to verify they pass**

Run: `dotnet test tests/FactionWars.Tests/FactionWars.Tests.csproj --filter "FullyQualifiedName~CombatantStatsProviderTests"`
Expected: PASS (3 tests).

- [ ] **Step 6: Commit**

```bash
git add src/FactionWars/Core tests/FactionWars.Tests/Unit/Core/CombatantStatsProviderTests.cs
git commit -m "feat: add ICombatantStatsProvider over CombatantsConfig (#130)"
```

---

### Task 3: GameBridge weapon-damage + player-stat methods

**Files:**
- Modify: `src/FactionWars/Core/Interfaces/IGameBridge.cs`; `src/FactionWars/ScriptHookV/GameBridge.PedWeapons.cs`; `src/FactionWars/ScriptHookV/GameBridge.PlayerState.cs`; `src/FactionWars/Core/Utils/MockGameBridge.cs`
- Test: `tests/FactionWars.Tests/Unit/Core/MockGameBridgeCombatStatsTests.cs` (new)

**Interfaces:**
- Produces on `IGameBridge`: `void SetPedWeaponDamageModifier(int pedHandle, float multiplier)`; `void SetPlayerMaxHealth(int maxHealth)`; `void SetPlayerWeaponDamageModifier(float multiplier)`; `void SetPlayerWeaponDefenseModifier(float multiplier)`. On `MockGameBridge` (test getters): `float GetPedWeaponDamageModifierForTest(int)`, `int GetPlayerMaxHealthForTest()`, `float GetPlayerWeaponDamageModifierForTest()`, `float GetPlayerWeaponDefenseModifierForTest()`.

- [ ] **Step 1: Write the failing test**

`tests/FactionWars.Tests/Unit/Core/MockGameBridgeCombatStatsTests.cs`:
```csharp
using FactionWars.Core.Utils;
using Xunit;

namespace FactionWars.Tests.Unit.Core
{
    public class MockGameBridgeCombatStatsTests
    {
        [Fact]
        public void SetPedWeaponDamageModifier_IsTracked()
        {
            var b = new MockGameBridge();
            b.SetPedWeaponDamageModifier(42, 8f);
            Assert.Equal(8f, b.GetPedWeaponDamageModifierForTest(42), 2);
        }

        [Fact]
        public void PlayerStatSetters_AreTracked()
        {
            var b = new MockGameBridge();
            b.SetPlayerMaxHealth(600);
            b.SetPlayerWeaponDamageModifier(1.5f);
            b.SetPlayerWeaponDefenseModifier(0.5f);
            Assert.Equal(600, b.GetPlayerMaxHealthForTest());
            Assert.Equal(1.5f, b.GetPlayerWeaponDamageModifierForTest(), 2);
            Assert.Equal(0.5f, b.GetPlayerWeaponDefenseModifierForTest(), 2);
        }
    }
}
```

- [ ] **Step 2: Run test to verify it fails**

Run: `dotnet test tests/FactionWars.Tests/FactionWars.Tests.csproj --filter "FullyQualifiedName~MockGameBridgeCombatStatsTests"`
Expected: FAIL to compile — methods missing.

- [ ] **Step 3: Add to `IGameBridge.cs`** (near `SetPedAccuracy`/`SetPedArmor`)

```csharp
        /// <summary>Multiplies the damage this ped's weapons deal (SET_PED_WEAPON_DAMAGE_MODIFIER).</summary>
        void SetPedWeaponDamageModifier(int pedHandle, float multiplier);

        /// <summary>Sets the player ped's max health and heals to full (SET_PED_MAX_HEALTH).</summary>
        void SetPlayerMaxHealth(int maxHealth);

        /// <summary>Multiplies damage the player's weapons deal (SET_PLAYER_WEAPON_DAMAGE_MODIFIER).</summary>
        void SetPlayerWeaponDamageModifier(float multiplier);

        /// <summary>Multiplies damage the player TAKES; &lt;1 = tougher (SET_PLAYER_WEAPON_DEFENSE_MODIFIER).</summary>
        void SetPlayerWeaponDefenseModifier(float multiplier);
```

- [ ] **Step 4: Implement in `GameBridge.PedWeapons.cs`** (append to the partial class)

```csharp
        public void SetPedWeaponDamageModifier(int pedHandle, float multiplier)
        {
            try
            {
                var ped = Entity.FromHandle(pedHandle) as Ped;
                if (ped == null || !ped.Exists()) return;
                Function.Call(Hash.SET_PED_WEAPON_DAMAGE_MODIFIER, ped.Handle, multiplier);
                FileLogger.Combat($"SetPedWeaponDamageModifier: ped {pedHandle} x{multiplier:F2}");
            }
            catch (Exception ex)
            {
                FileLogger.Error($"SetPedWeaponDamageModifier exception for ped {pedHandle}", ex);
            }
        }
```
(Confirm `GameBridge.PedWeapons.cs` already has `using System; using GTA; using GTA.Native; using FactionWars.ScriptHookV.Logging;` — add any missing.)

- [ ] **Step 5: Implement in `GameBridge.PlayerState.cs`** (append to the partial class)

```csharp
        public void SetPlayerMaxHealth(int maxHealth)
        {
            try
            {
                var player = Game.Player.Character;
                if (player == null || !player.Exists()) return;
                Function.Call(Hash.SET_PED_MAX_HEALTH, player.Handle, maxHealth);
                Function.Call(Hash.SET_ENTITY_HEALTH, player.Handle, maxHealth);
                FileLogger.Info($"SetPlayerMaxHealth: max={maxHealth}");
            }
            catch (Exception ex) { FileLogger.Error("SetPlayerMaxHealth exception", ex); }
        }

        public void SetPlayerWeaponDamageModifier(float multiplier)
        {
            try
            {
                Function.Call(Hash.SET_PLAYER_WEAPON_DAMAGE_MODIFIER, Game.Player.Handle, multiplier);
                FileLogger.Info($"SetPlayerWeaponDamageModifier: x{multiplier:F2}");
            }
            catch (Exception ex) { FileLogger.Error("SetPlayerWeaponDamageModifier exception", ex); }
        }

        public void SetPlayerWeaponDefenseModifier(float multiplier)
        {
            try
            {
                Function.Call(Hash.SET_PLAYER_WEAPON_DEFENSE_MODIFIER, Game.Player.Handle, multiplier);
                FileLogger.Info($"SetPlayerWeaponDefenseModifier: x{multiplier:F2}");
            }
            catch (Exception ex) { FileLogger.Error("SetPlayerWeaponDefenseModifier exception", ex); }
        }
```
(Confirm `GameBridge.PlayerState.cs` has `using GTA; using GTA.Native;` and `FileLogger` using — it already calls `Function.Call`/`FileLogger`.)

- [ ] **Step 6: Implement in `MockGameBridge.cs`** (add near `SetPedArmor`)

```csharp
        private readonly System.Collections.Generic.Dictionary<int, float> _pedWeaponDamageMods
            = new System.Collections.Generic.Dictionary<int, float>();
        private int _playerMaxHealth = 200;
        private float _playerWeaponDamageMod = 1f;
        private float _playerWeaponDefenseMod = 1f;

        public void SetPedWeaponDamageModifier(int pedHandle, float multiplier)
            => _pedWeaponDamageMods[pedHandle] = multiplier;
        public float GetPedWeaponDamageModifierForTest(int pedHandle)
            => _pedWeaponDamageMods.TryGetValue(pedHandle, out var m) ? m : 1f;

        public void SetPlayerMaxHealth(int maxHealth) => _playerMaxHealth = maxHealth;
        public int GetPlayerMaxHealthForTest() => _playerMaxHealth;

        public void SetPlayerWeaponDamageModifier(float multiplier) => _playerWeaponDamageMod = multiplier;
        public float GetPlayerWeaponDamageModifierForTest() => _playerWeaponDamageMod;

        public void SetPlayerWeaponDefenseModifier(float multiplier) => _playerWeaponDefenseMod = multiplier;
        public float GetPlayerWeaponDefenseModifierForTest() => _playerWeaponDefenseMod;
```

- [ ] **Step 7: Run tests + full build**

Run: `dotnet build FactionWars.sln --no-incremental` then `dotnet test tests/FactionWars.Tests/FactionWars.Tests.csproj --filter "FullyQualifiedName~MockGameBridgeCombatStatsTests"`
Expected: build succeeds (all `IGameBridge` implementers compile), 2 tests PASS.

- [ ] **Step 8: Commit**

```bash
git add src/FactionWars tests/FactionWars.Tests/Unit/Core/MockGameBridgeCombatStatsTests.cs
git commit -m "feat: add ped/player weapon-damage + player max-health bridge methods (#130)"
```

---

### Task 4: Register provider + thread into manager dependencies

**Files:**
- Modify: `src/FactionWars/ScriptHookV/ServiceContainerFactory.cs` (register provider)
- Modify: manager dependency classes that need stats — `FollowerManagerDependencies.cs`, `FriendlyDefenderManagerDependencies.cs`, `EnemyDefenderManagerDependencies.cs`, `BattleAttackerManagerDependencies.cs` — add `ICombatantStatsProvider StatsProvider`.
- Modify: the same managers' constructors to accept/store it (initialize field to `null`-checked).
- Test: `tests/FactionWars.Tests/Unit/ScriptHookV/ServiceContainerFactoryCombatantStatsTests.cs` (new) — resolve `ICombatantStatsProvider`.

**Interfaces:**
- Consumes: `CombatantStatsProvider(CombatantsConfig)` (Task 2), `GameConfig.Combatants` (Task 1).
- Produces: `container.Resolve<ICombatantStatsProvider>()`; each affected manager exposes a private `_statsProvider` field.

- [ ] **Step 1: Write the failing test**

`tests/FactionWars.Tests/Unit/ScriptHookV/ServiceContainerFactoryCombatantStatsTests.cs`:
```csharp
using FactionWars.Core.Interfaces;
using FactionWars.Core.Models;
using FactionWars.Core.Utils;
using FactionWars.ScriptHookV;
using Xunit;

namespace FactionWars.Tests.Unit.ScriptHookV
{
    public class ServiceContainerFactoryCombatantStatsTests
    {
        [Fact]
        public void Create_RegistersCombatantStatsProvider_WithDefaultEnemyValues()
        {
            var container = ServiceContainerFactory.Create(new MockGameBridge());
            var provider = container.Resolve<ICombatantStatsProvider>();
            var s = provider.GetRoleStats(CombatantCategory.Enemies, DefenderRole.Rifleman);
            Assert.Equal(500, s.Health);
            Assert.Equal(0.60f, s.Accuracy, 2);
        }
    }
}
```
(If an equivalent `ServiceContainerFactory.Create` smoke test already exists, add the resolve assertion there instead of a new file.)

- [ ] **Step 2: Run test to verify it fails**

Run: `dotnet test tests/FactionWars.Tests/FactionWars.Tests.csproj --filter "FullyQualifiedName~ServiceContainerFactoryCombatantStatsTests"`
Expected: FAIL — `ICombatantStatsProvider` not registered (resolve throws).

- [ ] **Step 3: Register the provider in `ServiceContainerFactory.cs`**

Immediately after `container.Register(config);` (line ~66):
```csharp
            container.RegisterSingleton<ICombatantStatsProvider>(() =>
                new CombatantStatsProvider(container.Resolve<GameConfig>().Combatants));
```
Add `using FactionWars.Core.Services;` / `using FactionWars.Core.Interfaces;` if not present.

- [ ] **Step 4: Run test to verify it passes**

Run: `dotnet test tests/FactionWars.Tests/FactionWars.Tests.csproj --filter "FullyQualifiedName~ServiceContainerFactoryCombatantStatsTests"`
Expected: PASS.

- [ ] **Step 5: Add `StatsProvider` to the four manager dependency classes**

For each of `FollowerManagerDependencies`, `FriendlyDefenderManagerDependencies`, `EnemyDefenderManagerDependencies`, `BattleAttackerManagerDependencies`, add:
```csharp
        public ICombatantStatsProvider? StatsProvider { get; set; }
```
(Use the file's existing nullability style; add `using FactionWars.Core.Interfaces;` if missing.)

- [ ] **Step 6: Store it in each manager constructor**

In each manager's primary constructor (where other dependencies are assigned), add a field and assignment:
```csharp
        private readonly ICombatantStatsProvider _statsProvider;
        // in ctor:
        _statsProvider = dependencies.StatsProvider ?? throw new ArgumentNullException(nameof(dependencies.StatsProvider));
```
Then update each manager's construction in `ServiceContainerFactory` (and any `params object?[]` convenience ctor) to pass `StatsProvider = container.Resolve<ICombatantStatsProvider>()`. Update existing manager unit tests that build the dependencies object to set `StatsProvider = <mock or real provider>` — construct a real `new CombatantStatsProvider(new CombatantsConfig())` in test setup (it's pure, no GTA refs).

- [ ] **Step 7: Build + full unit suite**

Run: `dotnet build FactionWars.sln --no-incremental` then `dotnet test tests/FactionWars.Tests/FactionWars.Tests.csproj --filter "FullyQualifiedName~FactionWars.Tests.Unit"`
Expected: build clean, all tests PASS (existing manager tests now construct with a provider).

- [ ] **Step 8: Commit**

```bash
git add src/FactionWars tests/FactionWars.Tests
git commit -m "feat: register CombatantStatsProvider and inject into combat managers (#130)"
```

---

### Task 5: Route fixed-category spawn sites through the provider

Squad, Friendlies, and Enemy-defender spawns always have a fixed category.

**Files:**
- Modify: `src/FactionWars/ScriptHookV/Managers/FollowerManager.Combat.cs` (Squad)
- Modify: `src/FactionWars/ScriptHookV/Managers/FriendlyDefenderManager.Replacements.cs` (Friendlies)
- Modify: `src/FactionWars/ScriptHookV/Managers/EnemyDefenderManager.Spawning.cs` (Enemies)
- Test: extend existing manager test fixtures (`FollowerManagerTests`, `FriendlyDefenderManager*Tests`, `EnemyDefenderManager*Tests`) — assert applied stats + damage modifier via `MockGameBridge`.

**Interfaces:**
- Consumes: `_statsProvider.GetRoleStats(category, role)` (Task 4), `IGameBridge.SetPedWeaponDamageModifier` (Task 3), existing `SetPedHealth/SetPedArmor/SetPedAccuracy/GivePedWeapon`.

- [ ] **Step 1: Write the failing test (Squad example; mirror for Friendlies/Enemies)**

In `FollowerManagerTests`, add a test asserting a recruited Rifleman follower gets the **Squad** Rifleman stats from a provider whose Squad table was overridden, and that the damage modifier is applied. Build the manager with a `CombatantStatsProvider` over a `CombatantsConfig` where `Squad.Rifleman.DamageMultiplier = 3f` and `Squad.Rifleman.Health = 999`. After recruiting/configuring, assert:
```csharp
Assert.Equal(999, _gameBridge.GetPedHealthForTest(pedHandle)); // existing getter
Assert.Equal(3f, _gameBridge.GetPedWeaponDamageModifierForTest(pedHandle), 2);
```
(Use the fixture's existing recruit/configure entry point and ped-handle accessor; if no health getter exists on `MockGameBridge`, assert accuracy via the existing `GetPedAccuracy` getter instead.)

- [ ] **Step 2: Run test to verify it fails**

Run: `dotnet test tests/FactionWars.Tests/FactionWars.Tests.csproj --filter "FullyQualifiedName~FollowerManagerTests"`
Expected: FAIL — stats still come from `DefenderRoleConfig` (old values), no damage modifier set.

- [ ] **Step 3: Edit `FollowerManager.Combat.cs` `ConfigureFollowerCombat`**

Resolve the role from the existing `roleConfig.Role` and read combat stats from the provider; keep using `DefenderRoleConfig` only for non-stat behavior (none here). Replace the stat lines:
```csharp
        private void ConfigureFollowerCombat(int pedHandle, DefenderRoleConfig roleConfig)
        {
            var stats = _statsProvider.GetRoleStats(CombatantCategory.Squad, roleConfig.Role);

            _gameBridge.GivePedWeapon(pedHandle, "weapon_pistol");
            _gameBridge.GivePedWeapon(pedHandle, stats.Weapon);
            _gameBridge.SetPedAccuracy(pedHandle, stats.Accuracy);
            _gameBridge.SetPedArmor(pedHandle, stats.Armor);
            _gameBridge.SetPedHealth(pedHandle, stats.Health);
            _gameBridge.SetPedWeaponDamageModifier(pedHandle, stats.DamageMultiplier);
            _gameBridge.SetPedCriticalHitsEnabled(pedHandle, false);
            _gameBridge.SetPedRagdollEnabled(pedHandle, false);
            // ... rest of method (combat attributes, sniper profile) unchanged ...
        }
```
Add `using FactionWars.Core.Models;` if `CombatantCategory` is unresolved.

- [ ] **Step 4: Edit `EnemyDefenderManager.Spawning.cs` `ConfigureEnemyDefender`**

Same pattern with `CombatantCategory.Enemies`; preserve the existing pistol-give, `SetPedCriticalHitsEnabled(true)`, `SetPedRagdollEnabled(roleConfig.RagdollEnabled)` (ragdoll stays sourced from `DefenderRoleConfig`), and the Rocketeer `SetPedCanSwitchWeapons(false)` branch:
```csharp
            var stats = _statsProvider.GetRoleStats(CombatantCategory.Enemies, roleConfig.Role);
            _gameBridge.GivePedWeapon(pedHandle, "weapon_pistol");
            _gameBridge.GivePedWeapon(pedHandle, stats.Weapon);
            _gameBridge.SetPedAccuracy(pedHandle, stats.Accuracy);
            _gameBridge.SetPedArmor(pedHandle, stats.Armor);
            _gameBridge.SetPedHealth(pedHandle, stats.Health);
            _gameBridge.SetPedWeaponDamageModifier(pedHandle, stats.DamageMultiplier);
            _gameBridge.SetPedCriticalHitsEnabled(pedHandle, true);
            _gameBridge.SetPedRagdollEnabled(pedHandle, roleConfig.RagdollEnabled);
```

- [ ] **Step 5: Edit `FriendlyDefenderManager.Replacements.cs`**

Find the friendly-defender stat application (lines applying `roleConfig.Accuracy/Armor/Health/Weapon`, ~170–175) and route through `_statsProvider.GetRoleStats(CombatantCategory.Friendlies, roleConfig.Role)` identically, adding `SetPedWeaponDamageModifier(pedHandle, stats.DamageMultiplier)`.

- [ ] **Step 6: Run the manager test suites**

Run: `dotnet test tests/FactionWars.Tests/FactionWars.Tests.csproj --filter "FullyQualifiedName~FollowerManagerTests|FullyQualifiedName~FriendlyDefenderManager|FullyQualifiedName~EnemyDefenderManager"`
Expected: PASS (new assertions + existing behavior).

- [ ] **Step 7: Commit**

```bash
git add src/FactionWars tests/FactionWars.Tests
git commit -m "feat: route squad/friendly/enemy spawns through CombatantStatsProvider (#130)"
```

---

### Task 6: Route faction-decided spawn sites (battle attackers + battle defenders)

**Files:**
- Modify: `src/FactionWars/ScriptHookV/Managers/BattleAttackerManager.Spawning.cs`
- Modify: `src/FactionWars/ScriptHookV/GameLoopController.BattlePeds.cs`
- Test: `BattleAttackerManager` test fixture — friendly vs enemy category by faction.

**Interfaces:**
- Consumes: `_statsProvider.GetRoleStats(category, role)`; the manager's known attacker faction + player faction.

- [ ] **Step 1: Confirm available faction context**

Read `BattleAttackerManager.cs` / `.Spawning.cs` for the fields holding the attacker faction id and the player faction id (the manager already logs `Attacker=michael, Defender=franklin` and has `SetPlayerFaction`). Note the exact field names for the comparison. Do the same for `GameLoopController.BattlePeds.cs` (it resolves a `tier` and operates on `spawnedPeds`; identify the defender faction id in scope and `CurrentPlayerFactionId`).

- [ ] **Step 2: Write the failing test**

In the `BattleAttackerManager` test fixture, drive a spawn where attacker faction == player faction and assert the spawned ped received **Friendlies** stats (override `Friendlies.<role>.Health` distinctly from `Enemies.<role>.Health` in the provider's config), then a second case where attacker != player asserts **Enemies** stats. Assert via the `MockGameBridge` health/accuracy getter on the spawned handle.

- [ ] **Step 3: Run test to verify it fails**

Run: `dotnet test tests/FactionWars.Tests/FactionWars.Tests.csproj --filter "FullyQualifiedName~BattleAttackerManager"`
Expected: FAIL — attacker stats still from `DefenderRoleConfig`.

- [ ] **Step 4: Edit `ConfigureAttacker` in `BattleAttackerManager.Spawning.cs`**

Add a category decision and route stats:
```csharp
            var category = _attackerFactionId == _playerFactionId
                ? CombatantCategory.Friendlies
                : CombatantCategory.Enemies;
            var stats = _statsProvider.GetRoleStats(category, roleConfig.Role);
            _gameBridge.GivePedWeapon(pedHandle, "weapon_pistol");
            _gameBridge.GivePedWeapon(pedHandle, stats.Weapon);
            _gameBridge.SetPedAccuracy(pedHandle, stats.Accuracy);
            _gameBridge.SetPedArmor(pedHandle, stats.Armor);
            _gameBridge.SetPedHealth(pedHandle, stats.Health);
            _gameBridge.SetPedWeaponDamageModifier(pedHandle, stats.DamageMultiplier);
            _gameBridge.SetPedCriticalHitsEnabled(pedHandle, true);
            _gameBridge.SetPedRagdollEnabled(pedHandle, roleConfig.RagdollEnabled);
```
(Substitute the real field names found in Step 1 for `_attackerFactionId` / `_playerFactionId`.)

- [ ] **Step 5: Edit `GameLoopController.BattlePeds.cs`**

Replace the local `weaponName` switch and `config.Health/Armor/Accuracy` applications with provider-sourced stats using a faction-decided category (`Friendlies` if the battle's defender faction == `CurrentPlayerFactionId`, else `Enemies`). Resolve the provider via `_container.Resolve<ICombatantStatsProvider>()` (this file already does `_container.Resolve<IDefenderRoleService>()`). Apply `SetPedWeaponDamageModifier(ped.Handle, stats.DamageMultiplier)` and drop the now-redundant local `weaponName` switch in favour of `stats.Weapon`.

- [ ] **Step 6: Run tests + full unit suite**

Run: `dotnet test tests/FactionWars.Tests/FactionWars.Tests.csproj --filter "FullyQualifiedName~FactionWars.Tests.Unit"`
Expected: build clean, all PASS.

- [ ] **Step 7: Commit**

```bash
git add src/FactionWars tests/FactionWars.Tests
git commit -m "feat: route battle attackers/defenders through stats provider by faction (#130)"
```

---

### Task 7: Apply player stats at init and on respawn

**Files:**
- Modify: `src/FactionWars/ScriptHookV/GameBridge.PlayerState.cs` (`ConfigurePlayerSettings` — but it has no config access; see approach) OR a new small applier in `GameLoopController`.
- Modify: `src/FactionWars/ScriptHookV/GameLoopController.TerritoryFlow.cs` (respawn edge) and the init path that currently calls `ConfigurePlayerSettings`.
- Test: `tests/FactionWars.Tests/Unit/ScriptHookV/PlayerStatsApplierTests.cs` (new).

**Approach:** `ConfigurePlayerSettings` (in `GameBridge`) has no config access and must stay GTA-only. Add a small domain helper `PlayerStatsApplier` (Core or ScriptHookV-Managers) that takes `IGameBridge` + `ICombatantStatsProvider` and exposes `Apply()`:
```csharp
public sealed class PlayerStatsApplier
{
    private readonly IGameBridge _bridge;
    private readonly ICombatantStatsProvider _stats;
    public PlayerStatsApplier(IGameBridge bridge, ICombatantStatsProvider stats) { _bridge = bridge; _stats = stats; }
    public void Apply()
    {
        var p = _stats.GetPlayerStats();
        _bridge.SetPlayerMaxHealth(p.MaxHealth);
        _bridge.SetPlayerArmorToValue(p.SpawnArmor);            // see note
        _bridge.SetPlayerWeaponDamageModifier(p.OutgoingDamageMultiplier);
        _bridge.SetPlayerWeaponDefenseModifier(p.IncomingDamageMultiplier);
    }
}
```

**Interfaces:**
- Consumes: `ICombatantStatsProvider.GetPlayerStats()`, the four `IGameBridge` player methods (Task 3). For armor, add a small `SetPlayerArmor(int)` bridge method (player wrapper over `SetPedArmor` on the player handle) — or reuse `SetPedArmor(GetPlayerPedHandle(), value)` if a player-ped-handle getter exists (`GetPlayerPedHandle()` is referenced elsewhere in the codebase). Prefer the existing handle getter to avoid a new method.

- [ ] **Step 1: Write the failing test**

`PlayerStatsApplierTests.cs`:
```csharp
using FactionWars.Configuration;
using FactionWars.Core.Services;
using FactionWars.Core.Utils;
using FactionWars.ScriptHookV.Managers; // or wherever PlayerStatsApplier lands
using Xunit;

namespace FactionWars.Tests.Unit.ScriptHookV
{
    public class PlayerStatsApplierTests
    {
        [Fact]
        public void Apply_PushesConfiguredPlayerStatsToBridge()
        {
            var cfg = new CombatantsConfig();
            cfg.Player.MaxHealth = 600;
            cfg.Player.IncomingDamageMultiplier = 0.5f;
            var bridge = new MockGameBridge();
            new PlayerStatsApplier(bridge, new CombatantStatsProvider(cfg)).Apply();
            Assert.Equal(600, bridge.GetPlayerMaxHealthForTest());
            Assert.Equal(0.5f, bridge.GetPlayerWeaponDefenseModifierForTest(), 2);
        }
    }
}
```

- [ ] **Step 2: Run test to verify it fails**

Run: `dotnet test tests/FactionWars.Tests/FactionWars.Tests.csproj --filter "FullyQualifiedName~PlayerStatsApplierTests"`
Expected: FAIL to compile — `PlayerStatsApplier` missing.

- [ ] **Step 3: Create `PlayerStatsApplier`** (in `src/FactionWars/ScriptHookV/Managers/PlayerStatsApplier.cs`) per the Approach block. For armor use `_bridge.SetPedArmor(_bridge.GetPlayerPedHandle(), p.SpawnArmor)` (confirm `GetPlayerPedHandle()` exists on `IGameBridge`; it is used in `MovePlayerToOwnedTerritory`).

- [ ] **Step 4: Run test to verify it passes**

Run: `dotnet test tests/FactionWars.Tests/FactionWars.Tests.csproj --filter "FullyQualifiedName~PlayerStatsApplierTests"`
Expected: PASS.

- [ ] **Step 5: Wire it in**

Construct a `PlayerStatsApplier` in `GameLoopController` init (where `ConfigurePlayerSettings()` is invoked — `GameLoopController.InitializationTelemetry.cs:98/126`) using the resolved `ICombatantStatsProvider`, and call `.Apply()` right after `ConfigurePlayerSettings()`. Also call `.Apply()` on the respawn-success edge in `GameLoopController.TerritoryFlow.cs` `ProcessPendingOwnedTerritoryPlacement` (same place `PlaceCommanderNearPlayerInCurrentZone` is called, since respawn resets max health). Store the applier as a field initialized in the init flow.

- [ ] **Step 6: Build + full unit suite**

Run: `dotnet build FactionWars.sln --no-incremental` then `dotnet test tests/FactionWars.Tests/FactionWars.Tests.csproj --filter "FullyQualifiedName~FactionWars.Tests.Unit"`
Expected: clean build, all PASS.

- [ ] **Step 7: Commit**

```bash
git add src/FactionWars tests/FactionWars.Tests
git commit -m "feat: apply player config stats at init and on respawn (#130)"
```

---

### Task 8: Regenerate default config.json + integration sanity + docs

**Files:**
- Modify: `CLAUDE.md` (document the `Combatants` config section + that deleting `config.json` regenerates defaults).
- Test: `tests/FactionWars.Tests/Integration/...` — a round-trip that writes defaults via `ConfigLoader` to a temp path and reloads, asserting `Combatants.Enemies.Rifleman.Accuracy == 0.6`.

- [ ] **Step 1: Write the integration test**

Add (or extend an existing `ConfigLoader` test) writing to a temp file path, calling `Load()` twice, asserting the regenerated file contains the `Combatants` block and reloads to the default Rifleman values.

- [ ] **Step 2: Run it (red), implement nothing new (loader already serializes the new section), confirm green**

Run: `dotnet test tests/FactionWars.Tests/FactionWars.Tests.csproj --filter "FullyQualifiedName~ConfigLoader"`
Expected: PASS (no code change needed — `Combatants` serializes automatically; this is a guard test). If it fails because the section is absent, that indicates a missing `[JsonProperty]`/getter — fix the DTO.

- [ ] **Step 3: Document in `CLAUDE.md`**

Add a "Combat balance config" subsection: file at `<scripts>/FactionWars/config.json`, `Combatants` block with `Player`/`Enemies`/`Squad`/`Friendlies`, load-on-startup (restart GTA to apply), delete the file to regenerate defaults, and that DamageMultiplier on `Friendlies.Sniper` is how to make friendly snipers one-shot NPCs.

- [ ] **Step 4: Commit**

```bash
git add tests/FactionWars.Tests CLAUDE.md
git commit -m "test+docs: config.json default regeneration guard + Combatants docs (#130)"
```

---

## Self-Review

**Spec coverage:** schema (Task 1) ✓; provider (Task 2) ✓; bridge methods incl. weapon-damage + player (Task 3) ✓; DI registration (Task 4) ✓; category routing all 5 spawn sites incl. faction decision (Tasks 5–6) ✓; player apply at init+respawn (Task 7) ✓; defaults reproduce current values (Tasks 1–2 regression tests) ✓; back-compat/missing-section + regeneration (Task 8) ✓. `DefenderRoleService` retains cost/troop-strength/combatModifier/ragdoll (Tasks 5–6 keep ragdoll from `DefenderRoleConfig`) ✓.

**Type consistency:** `RoleStats(health, armor, accuracy, weapon, damageMultiplier)` and `PlayerStats(maxHealth, spawnArmor, outgoingDamageMultiplier, incomingDamageMultiplier)` ctor orders are used consistently. `CombatantCategory { Player, Squad, Friendlies, Enemies }` used in provider + all spawn sites. `ICombatantStatsProvider.GetRoleStats/GetPlayerStats` consistent across Tasks 2/4/5/6/7. Bridge method names (`SetPedWeaponDamageModifier`, `SetPlayerMaxHealth`, `SetPlayerWeaponDamageModifier`, `SetPlayerWeaponDefenseModifier`) consistent across Tasks 3/7.

**Open implementation notes (resolve during the task, not blockers):** exact private field names for attacker/player faction in `BattleAttackerManager` (Task 6 Step 1); confirm `GetPlayerPedHandle()` is on `IGameBridge` for armor (Task 7 Step 3); confirm `GameBridge.PedWeapons.cs`/`PlayerState.cs` usings.
