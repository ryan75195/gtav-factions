# Zone Combatant Allegiance — Design

**Date:** 2026-06-27
**Status:** Approved (brainstorming) — pending implementation plan

## Problem

"What allegiance does this combatant ped have" and "what colour is its minimap blip" are
decided independently in three separate places:

- `FriendlyDefenderManager` — hardcoded `BlipColor.LightBlue` + `SetPedAsFriendly`
- `EnemyDefenderManager` — `FactionBlipColor.ForFactionId(enemyFactionId)` + `SetPedAsHostileWanderer`
- `BattleAttackerManager` — `FactionBlipColor.ForFactionId(attackerFactionId)` + `SetPedAsHostileWanderer`

Nothing guarantees blip colour and relationship agree, and the "is this the player's own
faction?" guard exists in one attacker spawn path (`OnPlayerZoneEntered` → `GetHostileAttackerForPlayer`)
but **not** the replacement path (`TrySpawnReplacement` → `SpawnSingleAttacker`, which uses
`battle.AttackerFactionId` directly). This produced observed bugs:

1. **Blue-but-hostile peds** — when the player attacks a zone, the battle's attacker faction is
   the player's own faction (michael). The unguarded replacement path can spawn michael-faction
   reinforcements: blip coloured as the player's faction (blue) **and** made hostile to the player
   via `SetPedAsHostileWanderer`.
2. **Guard asymmetry** — initial attacker spawn is guarded against the player faction; the
   replacement path is not.
3. **Friendly defenders that never despawn** — `FriendlyDefenderManager` despawns on death and on
   global teardown, but has no despawn when a zone's ownership leaves the player. Orphaned friendly
   peds keep their blips and fall out of death-cleanup tracking.
4. **Per-spawn relationship corruption** — `SetPedAsFriendly` / `SetPedAsHostileWanderer` /
   `ConfigureBattleRelationships` mutate **global, persistent, bidirectional** GTA relationship-group
   pairings on every spawn. `SetPedAsHostileWanderer` trusts whatever group the ped happens to be in.

Root cause: there is no single authority for combatant allegiance, and relationship-group wiring is
scattered across per-spawn code instead of established once.

## Scope

In scope: the three **zone combatant** spawn paths (friendly defenders, enemy defenders, attackers).

Out of scope: the player's **followers/bodyguards**, which keep their existing `PLAYER`-group
companion path. The relationship matrix still protects the `PLAYER` group so followers behave
correctly, but their spawn path is not rerouted.

## Approach (chosen)

**Pure faction groups + relationship matrix wired once.** Every zone combatant lives in its faction
group (`MICHAEL` / `FRANKLIN` / `TREVOR`), friendly defenders included. "Friendly to the player"
becomes "same faction as the player." The special `FRIENDLY_DEFENDERS` / `DEFENDER_ENEMIES` groups
are removed for zone combatants. All group-pair relationships are established once at init and on
character-switch. Per-spawn code never mutates relationships again.

Rejected alternatives:
- *Keep special groups, centralize only the decision* — lower risk but retains the confusing group
  taxonomy that is half the problem.
- *Resolver + spawner only, leave per-spawn relationship mutation* — does not fix the global-mutation
  footgun.

## Components

### Domain (portable — `FactionWars.Combat`)

- `enum Allegiance { Friendly, Hostile }`
- `CombatantProfile` — value object: `{ string RelationshipGroup; BlipColor BlipColor; Allegiance Allegiance; }`
- `IAllegianceResolver` / `AllegianceResolver` — the single source of truth:

  ```
  CombatantProfile Resolve(string combatantFactionId, string playerFactionId):
      group      = combatantFactionId.ToUpperInvariant()
      blipColor  = factionColorService.ColorFor(combatantFactionId)
      allegiance = combatantFactionId == playerFactionId ? Friendly : Hostile
  ```

  Pure, no native calls. Colour and allegiance derive from the same faction identity, so
  "blue-but-hostile" is structurally impossible. Reuses the existing `IFactionColorService`
  (extended to ped blip colours if it does not already cover them) so there is one colour map.

### Native (`FactionWars.ScriptHookV`)

- `IRelationshipMatrixInitializer` / `RelationshipMatrixInitializer` — wires GTA relationship groups
  once at init and on character-switch:

  ```
  Initialize(playerFactionId, allFactionIds):
      for each distinct pair (A,B) of faction groups:  A <-> B = Hate
      playerFactionGroup <-> PLAYER = Companion
      every other factionGroup <-> PLAYER = Hate
  ```

- `IZoneCombatantSpawner` / `ZoneCombatantSpawner` — the one place zone combatants are created:

  ```
  PedHandle Spawn(factionId, tier, position, zoneId):
      profile = allegianceResolver.Resolve(factionId, playerFactionId)
      model   = FactionPedModels.GetModel(factionId, tier)
      handle  = pedSpawningService.SpawnPed(model, position, factionId, zoneId)
      combatConfigurer.Configure(handle, profile.Allegiance)   // attributes only, no relationship mutation
      pedBlipService.CreateBlipForPed(handle, profile.BlipColor)
      return handle
  ```

  Role-specific tasking (defenders wander-defend; attackers seek hated targets) stays in each
  manager after spawn.

- `ZoneOwnershipReconciler` — single subscriber to `ZoneOwnershipChanged`; owns the ped lifecycle
  policy: on a zone leaving the player, despawn that zone's friendly defenders; on a zone leaving a
  faction, despawn that faction's enemy defenders there. Delegates to each manager's
  `DespawnForZone(zoneId)`. Managers still own their per-zone ped tracking.

### Bridge cleanup

- `SetPedAsFriendly` / `SetPedAsHostileWanderer` lose their `SetRelationshipBetweenGroups` calls and
  collapse into the spawner's `combatConfigurer` (combat attributes by allegiance).
- `ConfigureBattleRelationships`'s per-battle mutation is removed — the matrix covers faction-vs-faction.

### Manager slim-down

- `FriendlyDefenderManager` — spawn via spawner; drop hardcoded `LightBlue` + friendly relationship
  setup; add `DespawnForZone`.
- `EnemyDefenderManager` — spawn via spawner; drop `ForFactionId` blip + hostile relationship; add
  `DespawnForZone`.
- `BattleAttackerManager` — spawn via spawner; route **both** initial and replacement attacker-faction
  selection through the one guarded method (`GetHostileAttackerForPlayer`), removing the asymmetry.

## Why each bug cannot recur

| Bug | Prevention |
|---|---|
| Blue-but-hostile attackers | Allegiance + colour derive from the same faction identity in one resolver; a player-faction ped is always Friendly. |
| Guard-here-not-there | One guarded attacker-selection method feeds both spawn paths. |
| Friendly defenders that do not despawn | One reconciler despawns on ownership loss; no orphans. |
| Per-spawn relationship corruption | Relationships wired once at init; spawn code never mutates them. |

## Testing

- `AllegianceResolver`: same faction → Friendly + own colour; different faction → Hostile; pure
  unit tests.
- `RelationshipMatrixInitializer`: asserts the full pairing set (faction-vs-faction Hate; player
  faction ↔ PLAYER Companion; others ↔ PLAYER Hate) against `MockGameBridge`.
- `ZoneCombatantSpawner`: spawns with the resolved group, colour, and allegiance against mocks.
- `ZoneOwnershipReconciler`: ownership transitions trigger the correct `DespawnForZone` calls.
- `BattleAttackerManager`: player-faction attacker never selected by either spawn path.
- Existing manager tests retained; add ownership-loss despawn tests.

## Character-switch semantics

On character switch the player faction changes; `RelationshipMatrixInitializer.Initialize` re-runs
with the new player faction, and the reconciler despawns combatants that no longer match. Managers
already track `_playerFactionId` and update it on switch.

## Migration / risk notes

- Removing `FRIENDLY_DEFENDERS` / `DEFENDER_ENEMIES` changes friendly-defender AI from GTA "companion"
  to "allied faction member." Defenders wander-defend rather than tight-formation companion, so the
  behavioural loss is expected to be negligible; verify in-game per CLAUDE.md.
- All native relationship behaviour must be validated in-game and reflected back into `MockGameBridge`
  per the CLAUDE.md "Updating Mocks from In-Game Behavior" guideline.
