# Sniper Unit & Role Reframe — Design Spec

**Date:** 2026-06-27
**Issue:** #39
**Status:** Approved design — ready for implementation planning

## Goal

Add a **Sniper** specialist unit to FactionWars and reframe the existing troop
"tiers" into role-based **specialities**. The sniper behaves by posture: a
stationary high-ground emplacement when *defending* a zone, and a long-range
overwatch unit when *attacking*. It appears as a friendly zone defender, an
enemy zone defender, a battle attacker, and a personal player bodyguard.

## Background

Today every troop is a value of the `DefenderTier` enum
(`Basic`/`Medium`/`Heavy`/`Elite`), each carrying a `DefenderTierConfig`
(cost, health, armor, weapon, accuracy, combat modifier, ragdoll) and a
faction-specific ped model via `FactionPedModels`. The whole economy,
allocation, persistence, spawn, background-battle-sim, AI-purchasing,
telemetry, and UI stack is keyed on this enum. Notably `Elite` is already a
*specialist* (anti-vehicle / RPG), so the enum is in practice a set of **unit
types**, not a pure power ladder.

This makes the sniper a natural new enum value, and motivates renaming the set
from power-tiers to roles so the sniper sits beside the others as "what it
does", not "tier 5".

## Decisions (locked during brainstorming)

- **Unit model:** Sniper is a new value in the existing unit-type enum,
  presented as a specialist (not an apex tier). No orthogonal tier×role
  matrix (YAGNI).
- **Rename:** Full rename of the type and members — `DefenderTier` →
  `DefenderRole`, and the `*Tier*` config/service names too.
- **Role economy:** Keep the existing cost/strength ladder; only the identity
  is reframed. Numbers for the existing four are unchanged.
- **Perch (defence):** Computed high ground near the zone, resolved at spawn.
- **Rushed behavior (defence):** Switch to a pistol sidearm but hold the
  perch. Rushing the perch is the counter-play.
- **Lethality:** Dangerous but survivable — high per-shot damage, slow cycle,
  fragile body, breakable line of sight.
- **Scope:** All three phases designed in this single spec, implemented in
  ordered phases.

## Role rename mapping

Underlying integer values are preserved so enum *value* positions (e.g.
`SavedFollowerState.Tier`) remain stable.

| Old (tier) | int | New (role) | Weapon | Identity |
|---|---|---|---|---|
| `Basic`  | 0 | `Grunt`     | Pistol        | Cheap, fragile, expendable |
| `Medium` | 1 | `Gunner`    | SMG           | Close/mid spray |
| `Heavy`  | 2 | `Rifleman`  | Carbine       | Reliable line infantry |
| `Elite`  | 3 | `Rocketeer` | RPG           | Anti-vehicle specialist |
| *(new)*  | 4 | `Sniper`    | Sniper rifle + pistol sidearm | Long-range / perch specialist |

**Type/identifier renames:** `DefenderTier` → `DefenderRole`,
`DefenderTierConfig` → `DefenderRoleConfig`, `IDefenderTierService` /
`DefenderTierService` → `IDefenderRoleService` / `DefenderRoleService`, and the
`tier` parameters/locals/properties throughout (~168 referencing files, almost
entirely mechanical). The `DefenderTierConfig.Tier` property becomes `Role`.

## Persistence migration (hard requirement)

Saves use `Newtonsoft.Json` with default settings. Newtonsoft serializes
`Dictionary<TEnum,int>` **keys as enum member names**. The persisted
`GameState.Allocations[].Troops` and `FactionStateData.ReservePool` are both
`Dictionary<DefenderTier,int>`, so existing save files contain the string keys
`"Basic"`, `"Medium"`, `"Heavy"`, `"Elite"`. After the rename these member
names no longer exist and a naive load throws ("save corrupted").

**Mitigation:** a tolerant key converter that maps legacy names to the new
roles on read:

```
Basic → Grunt, Medium → Gunner, Heavy → Rifleman, Elite → Rocketeer
```

A Newtonsoft `JsonConverter` does **not** apply to dictionary keys, so the
converter is applied at the dictionary level on the two persisted properties
(`[JsonConverter(typeof(LegacyRoleDictionaryConverter))]` on
`ZoneDefenderAllocationData.Troops` and `FactionStateData.ReservePool`). The
converter:
- On **read**, accepts new role names, legacy tier names, and integer strings,
  mapping each to the correct `DefenderRole`.
- On **write**, emits the canonical new role name.

`SavedFollowerState.Tier` is an enum **value** (serialized as integer `0–3`),
so preserving the numeric values keeps it compatible without a converter; it is
renamed to `Role` as a property but the wire value is unchanged.

This migration is a Phase 0 deliverable with a test that loads a legacy-keyed
JSON document and asserts the roles resolve correctly.

## Sniper unit configuration

`DefenderRoleConfig` for `Sniper` (final numbers tuned in-game):

| Stat | Value | Rationale |
|---|---|---|
| Cost | 1500 | Premium specialist, below Rocketeer's 2000 (no anti-vehicle utility) |
| Health | 275 | Fragile — between Grunt (200) and Rifleman (500) |
| Armor | 50 | Low |
| Weapon | `WEAPON_SNIPERRIFLE` (primary) + `WEAPON_PISTOL` (sidearm) | Slow rifle cadence gives "dangerous but survivable" for free |
| Accuracy | 0.8 | High per-shot threat |
| CombatModifier | 2.2 | Specialist weight in background battle sim |
| Ragdoll | false | Stays posted, doesn't flop off the perch |

**Ped models:** one new `DefenderRole.Sniper` entry per faction
(`franklin`/`trevor`/`michael`) in `FactionPedModels`. Any human model works —
the weapon defines the role. Exact model IDs are chosen during planning and
verified in-game.

## Behaviors

The sniper composes three primitives, dispatched by **posture**.

### A. Perch (defence) — friendly + enemy zone defenders

1. At spawn, resolve a **computed high-ground perch** near the zone: sample
   candidate points around the zone center within a search radius, query a
   height for each, and pick the highest reachable. Fall back to the zone
   center if no usable high ground is found.
2. Task the sniper to **guard the perch** with a tight defensive sphere
   (reuses `IGameBridge.TaskGuardArea` from the squad-stance feature). It holds
   position and engages at range.
3. **Rushed → sidearm:** a per-tick check swaps the active weapon to the pistol
   when any attacker is within a short threshold (~15 m), and restores the
   sniper rifle when threats move back out. The sniper never abandons the
   perch; storming it is the intended counter-play.

### B. Overwatch (attack) — battle attackers

The sniper does **not** charge with the assault wave. It holds a **standoff
position** at the zone's outer edge or nearest high ground and engages targets
at long range (reuses the overwatch / hated-target combat tasking, with the
position held). It advances only if the standoff position is itself threatened.

### C. Bodyguard (Phase 3) — sniper follower dispatched by squad stance

Reuses primitives A and B via the existing squad-stance system:
- **Escort** → follows the player but hangs back further than melee units and
  fires at range.
- **Hold Area** → **perch** (primitive A) near the stance anchor.
- **Search & Destroy** → **overwatch** (primitive B): holds and picks assigned
  targets rather than charging.

This dependency order is why the phases are sequenced: Phase 1 builds the perch
primitive, Phase 2 builds overwatch, Phase 3 is mostly wiring those two into
stance dispatch.

## Economy, AI, UI, telemetry

- **Economy:** Sniper cost lives in `DefenderRoleConfig` and flows through the
  existing reserve/allocation/purchase paths unchanged.
- **AI:** `AIRecruitmentService` gains `Sniper` as a purchasable role with a
  **minority weighting** (cap of roughly one sniper per N defenders) so AI
  garrisons field some snipers without spamming them.
- **UI:** recruit / army / defenders menus already enumerate the enum values,
  so `Sniper` surfaces as a buyable option with its cost. The bodyguard recruit
  menu gains a sniper option in Phase 3.
- **Telemetry:** role counts are keyed by the enum, so `Sniper` is captured
  automatically.

## Architecture — new components

All domain logic stays portable (`Core` / `Combat`); native integration stays
behind `IGameBridge`.

- `DefenderRole.Sniper` + its `DefenderRoleConfig` entry. *(Phase 1)*
- `IPerchResolver` / `PerchResolver` — portable `Combat` logic:
  `Resolve(center, searchRadius, heightSampler) → perch position`. Pure and
  fully unit-testable; the height sampler is injected so tests need no natives.
  *(Phase 1)*
- `IStandoffResolver` / `StandoffResolver` — portable `Combat` logic computing
  an overwatch standoff position from the attacker origin, the target zone
  center, and the zone radius. Pure and unit-testable. *(Phase 2)*
- A perched-sniper close-defense behavior (sidearm swap) — a small per-tick
  check in the defender path, driven through `IGameBridge` weapon-select calls.
  *(Phase 1)*
- `LegacyRoleDictionaryConverter` — Newtonsoft converter for the two persisted
  role dictionaries. *(Phase 0)*
- **Possible new native** `IGameBridge.TryGetHighGroundZ(float x, float y, out
  float z)` — a downward height probe for rooftops, if the existing
  `GetGroundZ` is insufficient for perch quality. Added behind `IGameBridge`,
  recorded in `MockGameBridge`, native parameter order verified in-game per
  CLAUDE.md. The portable `PerchResolver` consumes this only via the injected
  height-sampler delegate, so it remains test-isolated.

## Implementation phases

- **Phase 0 — Rename + migration.** `DefenderTier` → `DefenderRole` (type +
  members + `*Tier*` services/configs), preserve integer values, add the
  `LegacyRoleDictionaryConverter`, and a legacy-save load test. Ship: identical
  behavior, new vocabulary, old saves still load.
- **Phase 1 — Sniper unit + perch defence.** Add the `Sniper` role + config +
  ped models + economy/UI exposure, `PerchResolver`, perch tasking on spawn for
  friendly + enemy defenders, and the rushed→sidearm close defense. Ship:
  recruit snipers into your zones; assaulting an enemy zone means clearing
  perched snipers.
- **Phase 2 — Overwatch attack.** `StandoffResolver` + sniper battle-attacker
  behavior. Ship: AI/player assault forces include hang-back snipers.
- **Phase 3 — Sniper bodyguard.** Recruit a sniper follower; wire perch /
  overwatch / follow-at-range into the Escort / Hold Area / Search & Destroy
  stances. Ship: a personal sniper that perches on Hold and overwatches on S&D.

## Testing strategy

- **Phase 0:** persistence value-stability + legacy-name load test (a legacy
  JSON with `"Basic"/"Elite"` keys loads into the correct roles); full unit
  suite green after the mechanical rename.
- **`PerchResolver`:** picks the highest candidate; respects the search radius;
  falls back to center when no high ground beats the center; deterministic
  given a fixed height sampler.
- **`StandoffResolver`:** standoff sits between the attacker origin and the zone
  at the expected range/side; bounded distances.
- **Close defense:** attacker within threshold → mock records a swap to the
  sidearm; attacker beyond threshold → rifle restored.
- **Spawn behavior:** sniper defender is perched and guard-tasked; sniper
  attacker is placed at standoff and overwatch-tasked — asserted via
  `MockGameBridge` recordings.
- **Bodyguard stance dispatch:** Hold → perch, Search & Destroy → overwatch,
  Escort → follow-at-range — controller tests against mocks.
- **Config:** `Sniper` cost / health / weapon / accuracy assertions.
- **In-game gate:** lethality tuning, perch quality, and any new native's
  parameter order are verified in-game (per CLAUDE.md); unit tests cannot cover
  real GTA native calls.

## Risks & open items

- GTA snipers can be oppressive; "dangerous but survivable" depends on accuracy,
  the rifle's slow cycle, fragile health, and the sidearm-when-rushed window.
  Final feel is an in-game tuning pass.
- Computed perch quality varies by zone; accepted (no authored perch data).
- If `GetGroundZ` proves enough for perches, the `TryGetHighGroundZ` native is
  dropped — decided during Phase 1.
- The rename is broad but mechanical; the migration converter is the only
  behaviorally risky part of Phase 0 and is covered by the legacy-load test.
