# Zone Held While the Player Is Present — Design

**Issue:** #141
**Status:** Approved (design decision), pending spec review

## Goal

When the last defender in a player-owned zone dies, the zone is no longer lost immediately.
If the player is **physically in that zone and alive**, the zone enters a short **grace
state** so the player can keep fighting and redeploy a defender to save it. The zone is only
lost when the player **dies** or **leaves the zone while it still has no defenders**.

## Background / Current State

`FriendlyDefenderManager.UpdateAndSpawning.cs`: when a friendly defender dies, it fires
`DefenderDied`, tries a reserve replacement, then:

```csharp
if (IsAllDefendersDead(zoneId, allocation))   // no spawned defenders AND no reserve
    HandleTerritoryLost(zoneId);
```

`HandleTerritoryLost(zoneId)` currently **immediately** transfers the zone to neutral:

```csharp
private void HandleTerritoryLost(string zoneId)
{
    _zoneService.TransferZoneOwnership(zoneId, null);
    TerritoryLost?.Invoke(this, new TerritoryLostEventArgs(zoneId));
}
```

`FriendlyDefenderManager` already holds `_gameBridge`, `_zoneService`, `_playerFactionId`,
a per-tick `Update()`, and `GetSpawnedDefenderCount(zoneId)`. `IZoneService.GetZoneAtPosition(Vector3)`,
`IGameBridge.GetPlayerPosition()`, and `IGameBridge.IsPlayerDead()` all exist.

## Design Decision (locked)

Grace ends and the zone is lost when the player **dies** OR **leaves the zone while it is
still undefended**. Redeploying any defender **saves** it. (Chosen over "only lost if I
die" and over adding a countdown timer.)

## Behavior

**"Player is in the zone" (helper):** `_zoneService.GetZoneAtPosition(_gameBridge.GetPlayerPosition())?.Id == zoneId`.
**"Player alive":** `!_gameBridge.IsPlayerDead()`. **"Zone has a defender again":**
`GetSpawnedDefenderCount(zoneId) > 0`.

### 1. Defer the loss (in `HandleTerritoryLost`)

Replace the unconditional transfer with:

- If the player **is in this zone and alive** → do NOT transfer. Add `zoneId` to a new
  `HashSet<string> _undefendedGraceZones`, and show a one-time warning notification
  (e.g. `~y~<ZoneName> is undefended — hold it or redeploy!`). Do NOT fire `TerritoryLost`
  yet (the commander/despawn side-effects should only run on real loss).
- Else (player not present, or dead) → transfer to neutral and fire `TerritoryLost` exactly
  as today (immediate loss — unchanged behavior when the player isn't holding the zone).

A zone already in `_undefendedGraceZones` is not re-added or re-warned.

### 2. Monitor grace zones each tick (new `MonitorGraceZones()` called from `Update()`)

For each `zoneId` in a snapshot of `_undefendedGraceZones`:

- **Saved:** `GetSpawnedDefenderCount(zoneId) > 0` → remove from grace; notify
  `~g~<ZoneName> secured.`. (The player redeployed; the owned zone spawned a defender.)
- **Lost — player died:** `_gameBridge.IsPlayerDead()` → `FinalizeZoneLoss(zoneId)`; remove
  from grace.
- **Lost — player left undefended:** the player's current zone id `!= zoneId` (they walked/
  drove out while it still has zero defenders) → `FinalizeZoneLoss(zoneId)`; remove from grace.
- **Otherwise** (player present, alive, still zero defenders) → stay in grace.

`FinalizeZoneLoss(zoneId)` runs the original loss path: `_zoneService.TransferZoneOwnership(zoneId, null)`
+ `TerritoryLost?.Invoke(...)`.

Order the checks **Saved → died → left** so a redeploy on the same tick the player is
leaving still counts as saved.

### 3. Ownership-change safety

If the zone's owner changes out from under the grace state (e.g. an AI battle transfers it,
or it's re-claimed), the grace entry is stale. In `MonitorGraceZones`, before the checks,
drop any grace `zoneId` whose `_zoneService.GetZone(zoneId)?.OwnerFactionId != _playerFactionId`
(no longer the player's — nothing to hold), without a "lost" notification (it already changed
hands through another path).

## Components

- **Modify `FriendlyDefenderManager.UpdateAndSpawning.cs`:** add `_undefendedGraceZones`;
  rewrite `HandleTerritoryLost` to defer when the player holds the zone; add
  `MonitorGraceZones()` + `FinalizeZoneLoss()`; call `MonitorGraceZones()` from `Update()`.
- **Helper** `PlayerIsInZoneAndAlive(string zoneId)` and `PlayerCurrentZoneId()` (private).
- No new interfaces or bridge methods — all dependencies already exist.

## Error handling / edge cases

- **Redeploy timing:** deploying via the zone menu increases the allocation; because the
  zone is still owned (loss deferred), `FriendlyDefenderManager` spawns the defender on its
  normal path, so `GetSpawnedDefenderCount > 0` becomes true within a tick or two → saved.
- **Player dies and respawns:** `IsPlayerDead()` is true during death; the monitor finalizes
  the loss on that tick, so respawning into the (now-neutral) zone doesn't resurrect the grace.
- **Zone re-secured then attacked again:** once removed from grace and defenders die again
  with the player present, `HandleTerritoryLost` re-enters grace normally.
- **Save/load:** `_undefendedGraceZones` is in-memory only; a zone mid-grace at save time
  loads as simply owned-but-undefended (its next defender-death re-evaluates). No persistence
  change.

## Battle-path interaction (verify during planning)

The primary loss path for a player-held zone under attack is friendly defenders dying →
`HandleTerritoryLost`, which this covers. Confirm during planning whether
`ZoneBattleManager`/`GameLoopController.Battles` can independently `TransferZoneOwnership`
away from the player while defenders are being wiped; if it can and it bypasses the grace,
note it as a follow-up (out of scope here) rather than expanding this change.

## Testing

`FriendlyDefenderManager` is unit-testable (existing fixtures). With mocked `IGameBridge` +
`IZoneService`:

- **Defer:** all defenders dead + player in the zone (`GetZoneAtPosition(playerPos).Id == zoneId`)
  + alive → `TransferZoneOwnership` is **not** called; zone is in grace.
- **Immediate loss when not present:** all defenders dead + player NOT in the zone → transfer
  to neutral immediately (unchanged behavior).
- **Saved:** zone in grace, then `GetSpawnedDefenderCount(zoneId) > 0` → no transfer; grace cleared.
- **Lost on death:** zone in grace, `IsPlayerDead()` true → transfer to neutral; grace cleared.
- **Lost on leaving:** zone in grace, player's current zone != this zone, still 0 defenders →
  transfer to neutral; grace cleared.
- **Ownership-change safety:** zone in grace but `GetZone(zoneId).OwnerFactionId` != player →
  grace entry dropped, no extra transfer.

## Out of scope (YAGNI)

- A visible countdown/timer (rejected in favor of die-or-leave).
- Persisting grace state across save/load.
- Changing the AI/battle transfer paths (only the friendly-defender-death path is covered).
- Any change to how defenders are bought/deployed (that flow already exists, #134).
