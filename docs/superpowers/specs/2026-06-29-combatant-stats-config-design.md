# Per-Category Combatant Stats via config.json — Design

**Issue:** #130
**Status:** Approved (design), pending spec review

## Goal

Move per-combatant combat stats out of the hardcoded, faction-shared `DefenderRoleService`
into `config.json`, with **independent full per-role tables per category**: `Player`, `Squad`,
`Friendlies`, `Enemies`. This lets the player hand-tune balance (the accuracy / health / sniper-damage
knobs we have been iterating on) without a code change, and — critically — set *different* stats for
an enemy Rifleman vs the player's own squad Rifleman, which is impossible today.

Load-on-startup only (edit file, restart GTA to apply). Defaults reproduce today's exact values, so an
existing install behaves identically until the file is edited.

## Background / Current State

- A working JSON config system already exists: `ConfigLoader.Load()` reads
  `<scripts>/FactionWars/config.json` into a `GameConfig` (sections: `AI`, `Combat`, `Economy`,
  `Initialization`, `Persistence`), writes a default file if missing, caches, and falls back to
  `GameConfig.Default` on any error. `GameConfig` is registered in the DI container
  (`ServiceContainerFactory.Create`) and resolved widely.
- Per-combatant stats currently live in `DefenderRoleService : IDefenderRoleService`
  (`Core/Services`). `GetRoleConfig(DefenderRole)` returns a `DefenderRoleConfig`
  (role, cost, health, armor, weapon, accuracy, combatModifier, ragdollEnabled). **All factions share
  this one table.**
- Spawn sites read the combat-stat fields off `DefenderRoleConfig` and apply them via `IGameBridge`:
  - `FollowerManager.Combat` (the player's squad)
  - `FriendlyDefenderManager.Replacements` (player-owned zone defenders)
  - `EnemyDefenderManager.Spawning` (enemy zone defenders)
  - `BattleAttackerManager.Spawning` (battle attackers — either side)
  - `GameLoopController.BattlePeds` (battle defender peds)
- `DefenderRoleConfig` is *also* used for non-combat purposes that are **not** per-faction:
  cost/recruitment (`TroopPurchaseService`, `AIRecruitmentService`, `ArmyMenuController`,
  zone-claim cost), troop strength, and `combatModifier` (simulated battles). These stay in
  `DefenderRoleService` unchanged.

### Current default stat values (post-#128 accuracy nerf) — defaults MUST reproduce these

| Role      | Health | Armor | Accuracy | Weapon              |
|-----------|--------|-------|----------|---------------------|
| Grunt     | 200    | 50    | 0.25     | WEAPON_PISTOL       |
| Gunner    | 350    | 100   | 0.45     | WEAPON_SMG          |
| Rifleman  | 500    | 200   | 0.60     | WEAPON_CARBINERIFLE |
| Rocketeer | 650    | 200   | 0.70     | WEAPON_RPG          |
| Sniper    | 275    | 50    | 0.70     | WEAPON_SNIPERRIFLE  |

`DamageMultiplier` default = `1.0` for every role. Player defaults: `MaxHealth = 200`,
`SpawnArmor = 0`, `OutgoingDamageMultiplier = 1.0`, `IncomingDamageMultiplier = 1.0`.

## Schema

New top-level `Combatants` section on `GameConfig` (sibling of `Combat`):

```jsonc
"Combatants": {
  "Player": {
    "MaxHealth": 200,
    "SpawnArmor": 0,
    "OutgoingDamageMultiplier": 1.0,
    "IncomingDamageMultiplier": 1.0
  },
  "Enemies": {
    "Grunt":     { "Health": 200, "Armor": 50,  "Accuracy": 0.25, "Weapon": "WEAPON_PISTOL",       "DamageMultiplier": 1.0 },
    "Gunner":    { "Health": 350, "Armor": 100, "Accuracy": 0.45, "Weapon": "WEAPON_SMG",          "DamageMultiplier": 1.0 },
    "Rifleman":  { "Health": 500, "Armor": 200, "Accuracy": 0.60, "Weapon": "WEAPON_CARBINERIFLE", "DamageMultiplier": 1.0 },
    "Rocketeer": { "Health": 650, "Armor": 200, "Accuracy": 0.70, "Weapon": "WEAPON_RPG",          "DamageMultiplier": 1.0 },
    "Sniper":    { "Health": 275, "Armor": 50,  "Accuracy": 0.70, "Weapon": "WEAPON_SNIPERRIFLE",  "DamageMultiplier": 1.0 }
  },
  "Squad":      { /* same five roles, same default values */ },
  "Friendlies": { /* same five roles, same default values */ }
}
```

### Config classes (namespace `FactionWars.Configuration`)

- `CombatantsConfig` — `Player`, `Enemies`, `Squad`, `Friendlies`.
- `PlayerStatsConfig` — `MaxHealth:int`, `SpawnArmor:int`, `OutgoingDamageMultiplier:float`,
  `IncomingDamageMultiplier:float`.
- `CategoryStatsConfig` — explicit per-role properties `Grunt`, `Gunner`, `Rifleman`, `Rocketeer`,
  `Sniper`, each a `RoleStatsConfig`. (Explicit properties, not an enum-keyed dictionary, so the JSON
  reads cleanly and avoids Newtonsoft enum-key quirks.)
- `RoleStatsConfig` — `Health:int`, `Armor:int`, `Accuracy:float`, `Weapon:string`,
  `DamageMultiplier:float`.

All classes expose default-valued auto-properties so `new CombatantsConfig()` yields the table above
and `GameConfig.Default` is unchanged-behavior.

## Provider

New `ICombatantStatsProvider` (in `Core`, portable, no GTA refs):

```csharp
public enum CombatantCategory { Player, Squad, Friendlies, Enemies }

public interface ICombatantStatsProvider
{
    RoleStats GetRoleStats(CombatantCategory category, DefenderRole role);
    PlayerStats GetPlayerStats();
}
```

- `RoleStats` / `PlayerStats` are immutable Core value types (mirror the config fields). Keeping them
  distinct from the `Configuration.*Config` DTOs keeps the spawn/domain code independent of the config
  layer's mutable JSON DTOs.
- `CombatantStatsProvider` is constructed from `CombatantsConfig` (resolved from the `GameConfig` in
  DI) and maps category+role → `RoleStats`. `Player` category on `GetRoleStats` is unsupported
  (throws / not called); player uses `GetPlayerStats`.
- Registered in `ServiceContainerFactory` alongside the existing `GameConfig` registration.

## Category routing at spawn sites

Each spawn site resolves combat stats through the provider with its category, replacing the
`DefenderRoleConfig` reads for **health, armor, accuracy, weapon, and (new) damage multiplier**.
`DefenderRoleService` continues to supply cost, `combatModifier`, `ragdollEnabled`, and troop strength.

| Spawn site                          | Category                                                        |
|-------------------------------------|-----------------------------------------------------------------|
| `FollowerManager.Combat`            | `Squad`                                                         |
| `FriendlyDefenderManager`           | `Friendlies`                                                    |
| `EnemyDefenderManager.Spawning`     | `Enemies`                                                       |
| `GameLoopController.BattlePeds`     | `Friendlies` if defender faction == player faction, else `Enemies` |
| `BattleAttackerManager.Spawning`    | `Friendlies` if attacker faction == player faction, else `Enemies` |

`BattleAttackerManager` and `GameLoopController.BattlePeds` already know the relevant faction ids and
the player faction, so the friendly-vs-enemy decision is a local comparison — no new plumbing.
(Implementation note: confirm `GameLoopController.BattlePeds` is still a live spawn path and which
faction it configures; if it only ever spawns enemy defenders in practice, the comparison simply
always yields `Enemies` — the faction-based rule is correct either way.)

## New game-bridge methods

Added to `IGameBridge` (+ real `GameBridge`, + `MockGameBridge` with test getters):

- `SetPedWeaponDamageModifier(int pedHandle, float multiplier)` → `SET_PED_WEAPON_DAMAGE_MODIFIER`.
  Applied at every combatant spawn from the role's `DamageMultiplier`. A `>1` value on the
  `Friendlies`/`Squad` Sniper is what makes friendly snipers one-shot NPCs **without** touching enemy
  snipers — folding the earlier sniper request into the config rather than special-casing it.
- Player application (init + respawn edge):
  - `SetPlayerMaxHealth(int maxHealth)` → `SET_PED_MAX_HEALTH` on the player ped + heal to full.
  - reuse `SetPedArmor` for `SpawnArmor`.
  - `SetPlayerWeaponDamageModifier(float)` → `SET_PLAYER_WEAPON_DAMAGE_MODIFIER`.
  - `SetPlayerWeaponDefenseModifier(float)` → `SET_PLAYER_WEAPON_DEFENSE_MODIFIER` (incoming-damage).

Player stats are applied in `ConfigurePlayerSettings` (init) **and** re-applied on the existing
respawn edge in `GameLoopController.TerritoryFlow` / player-respawn detection, because GTA resets
max health and modifiers on death.

## Error handling / back-compat

- Missing `Combatants` section in an existing `config.json`: Newtonsoft leaves the property at its
  default (`new CombatantsConfig()`), which equals today's values — identical behavior.
- Malformed file: existing `ConfigLoader` catch already falls back to `GameConfig.Default`.
- Unknown weapon string: passed through to `GivePedWeapon`, which already guards invalid weapons
  (same as today).
- Out-of-range numbers (e.g. negative health): clamped at the bridge boundary the same way current
  code does (`SetPedArmor` clamps `>= 0`, accuracy clamps 0–100). No new validation layer.

## Testing

- **Defaults regression guard:** `CombatantStatsProvider` built from `GameConfig.Default` returns,
  for every role, stats equal to the current `DefenderRoleService` values (the table above) for all of
  `Enemies`/`Squad`/`Friendlies`; player at 200/0/1.0/1.0. This locks "no behavior change until edited."
- **Config round-trip:** a JSON document with distinct per-category values deserializes and the
  provider returns the right value per (category, role).
- **Category routing:** `BattleAttackerManager` resolves `Friendlies` when attacker == player faction,
  `Enemies` otherwise; other managers always use their fixed category.
- **Spawn application:** via `MockGameBridge`, each manager applies its category's health/armor/
  accuracy/weapon/damage-multiplier to spawned peds.
- **Bridge methods:** new mock methods tracked with test getters; reconciler/manager tests assert the
  multiplier is set per role.
- **Player application:** init and respawn both apply max-health/armor/damage/defense from config.

## Out of scope (YAGNI)

- In-game hot reload (chosen: load-on-startup).
- Per-category cost / troop-strength / `combatModifier` (those remain global in `DefenderRoleService`).
- Re-statting already-alive peds.
- A validation/clamping layer beyond what the bridge already does.
