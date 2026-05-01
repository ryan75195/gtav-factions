# Savegame Integration Design

**Date:** 2026-05-01
**Status:** Draft — pending review

## Goal

Tie FactionWars mod state to the GTA V single-player savegame so that when the player saves or loads through GTA's native pause menu, mod state is mirrored automatically. Replaces the current standalone 10-slot mod-managed save system.

## Non-goals

- Writing into Rockstar's encrypted SGTA binary format directly (impossible)
- Cross-machine save sync (the player would need to manually copy the mod's `sidecars\` folder; documented limitation)
- Switching between multiple Rockstar profiles mid-session
- A new mod-driven save UI (it goes away — the GTA pause menu is the single entry point)

## Decisions

| Question | Decision |
|---|---|
| Who drives save/load? | GTA's pause menu only. Mod menu's save/load UI is removed. |
| GTA save loaded with no sidecar? | Treat as fresh mod state — mod runs `NewGame()` silently. |
| Mirror autosaves? | Yes — every save event (autosave + manual) writes a sidecar. |
| Existing 10 mod slots? | Abandoned. On first launch of new system, moved to `legacy_backup_<UTC>\` for safety. |
| Sidecar location? | Mod's own directory: `Documents\FactionWars\sidecars\`. Rockstar's profile dir is left untouched. |
| Player money/weapons mirroring? | Removed. GTA already persists these natively; the mirroring code in `GameStateManager` is dead weight under sidecar. |
| Load-detection mechanism? | **State fingerprint matching.** Primary key: total-playing-time stat (exact stat name to be confirmed during implementation — likely `SP0_TOTAL_PLAYING_TIME` for character 0, with character index resolved via `GET_PED_INT_STAT_INDEX` or character-aware `STAT_GET_INT`). Tiebreakers: money, completed-mission count, in-game clock. Position is *recorded* in the sidecar payload (for a future "restore position" feature) but is *not* part of the matching fingerprint. |
| Orphan sidecar cleanup? | No auto-delete. Sidecars are kilobytes; orphans accumulate harmlessly. A manual "clean up" mod-menu option may be added later. |

### Why state-fingerprint matching (not the alternatives)

- **Querying the active save's name via a native** — verified to not exist in SHVDN. No clean API returns "currently loaded save name/slot."
- **Marker stat (`STAT_SET_INT` with a custom hash) inside the GTA save** — research found no working modder examples of arbitrary custom hashes persisting per-save; all known usage is with predefined GTA stat names.
- **mtime heuristic ("most recently modified SGTA = active slot")** — simple, but breaks when the player loads an *older* save than their most-recent write. Misbehavior on a feature checkpointing players actively use.
- **State fingerprint** — uses `TOTAL_PLAYING_TIME` (monotonically increasing seconds-played, persisted in the save, restored exactly on load). Two saves at different points in a campaign always have different values. Works without any unverified native.

## Architecture

```
+----------------------+      +----------------------+
| FileSystemWatcher    |      | LoadDetector         |
| (NativeSaveWatcher)  |      | (Game.IsLoading edge)|
+----------+-----------+      +----------+-----------+
           |                             |
           | OnNativeSaveWritten         | OnLoadDetected(sidecar?)
           v                             v
+----------------------+      +----------------------+
| GameStateManager     |<-----+ SidecarStore         |
| .WriteCurrentSidecar |      | .TryFindByFingerprint|
| .HydrateFromSidecar  |      | .WriteSidecar        |
| .NewGame             |      | (filesystem-backed)  |
+----------+-----------+      +----------------------+
           |
           v
+----------------------+
| Domain repositories  |
| (zones, factions,    |
|  allocations)        |
+----------------------+
```

## Components

### `SaveFingerprint` (matching primitive)

```csharp
public sealed class SaveFingerprint
{
    public long TotalPlayTimeSeconds { get; init; }   // primary key
    public int  Money { get; init; }                   // tiebreaker
    public int  CompletedMissionCount { get; init; }   // tiebreaker
    public int  InGameClockMinutes { get; init; }      // tiebreaker (HH*60+MM)

    public bool ExactMatch(SaveFingerprint other);     // all fields equal
    public bool PrimaryMatch(SaveFingerprint other);   // TotalPlayTimeSeconds only
}
```

Captured by `IGameBridge.GetCurrentFingerprint()`.

### Sidecar JSON payload

File path: `Documents\FactionWars\sidecars\sidecar_<totalPlayTimeSeconds>.json`

```jsonc
{
  "fingerprint": {
    "totalPlayTimeSeconds": 12340,
    "money": 50000,
    "completedMissionCount": 23,
    "inGameClockMinutes": 854
  },
  "writtenAtUtc": "2026-05-01T12:34:56Z",
  "nativeSaveFilename": "SGTA00003",
  "playerPosition": {
    "x": 245.1, "y": -1100.7, "z": 29.3, "heading": 87.0
  },
  "gameState": { /* existing GameState minus PlayerMoney/PlayerWeapons */ }
}
```

`playerPosition` is recorded but not consumed by load logic in v1 — reserved for the future "restore position" feature.

### `NativeSaveWatcher` (new)

- `FileSystemWatcher` on `Documents\Rockstar Games\GTA V\Profiles\<id>\` with filter `SGTA*`.
- Debounces FS events with a 200ms window per path (saves can fire multiple events).
- Public event: `OnNativeSaveWritten(string path, DateTime mtime)`. Deletion events are dropped at the watcher level — orphan policy means we don't react to them.
- Profile dir is detected at startup by scanning `Profiles\` and picking the subfolder whose `SGTA*` files have the most recent collective mtime. Logs the choice.
- If the profile dir doesn't exist yet (clean install), enters "deferred mode" and polls every 30s until it appears.

### `LoadDetector` (new)

- Detects loading-screen edges. Primary signal: SHVDN's loading-state property (e.g., `Game.IsLoading` if available in this SHVDN version). If that's not exposed, fallback signal: a multi-frame gap in script ticks combined with a player-position teleport (>500m delta) — a load reliably produces both. The implementation picks whichever signal is exposed and tests against the manual verification checklist.
- On loading-edge transition (true → false):
  1. Waits a short settle window (~500ms) for stats to stabilize.
  2. Captures current `SaveFingerprint` via `IGameBridge`.
  3. Compares to the *last-known* fingerprint. If `totalPlayTimeSeconds` is unchanged, treats it as a non-load event (mission cutscene, fast-travel, etc.) and skips hydration.
  4. Otherwise calls `SidecarStore.TryFindByFingerprint(fp)`.
  5. Found → emits `OnLoadDetected(sidecar)`.
  6. Not found → emits `OnLoadDetected(null)` (signals `NewGame`).

### `SidecarStore` (replaces `SaveSlotManager`)

```csharp
public interface ISidecarStore
{
    void WriteSidecar(SaveFingerprint fp, GameState state, Vector3WithHeading pos, string nativeFilename);
    bool TryFindByFingerprint(SaveFingerprint fp, out Sidecar sidecar);
    IReadOnlyList<Sidecar> ListAll();
}
```

- `WriteSidecar` writes `sidecar_<totalPlayTimeSeconds>.json` atomically (write to `.tmp`, `Move` overwrite).
- `TryFindByFingerprint`: O(1) lookup by computed filename, then `ExactMatch` sanity check on tiebreakers. Returns false if the JSON is corrupt or unreadable (logs and continues).
- No deletion API — orphans accumulate. (A power-user cleanup option may be added later as a separate feature.)

### `GameStateManager` surgery

**Removed:**
- `SaveToSlot`, `SaveToSlotAsync`, `LoadFromSlot`, `LoadFromSlotAsync`, `_currentSaveName` field.
- `PlayerMoney` / `PlayerWeapons` capture in `GetCurrentGameState` and restore in `ApplyGameState`. The corresponding `IGameBridge.GetPlayerMoney`/`GetPlayerWeapons`/`SetPlayerMoney`/`RemoveAllPlayerWeapons`/`GivePlayerWeapon` calls in the save/load codepath go away (the bridge methods themselves stay since they may be used elsewhere).
- The mod-menu-driven save/load surface in `MainMenuController` and `SettingsMenuController`.

**Added:**
- `WriteCurrentSidecar(SaveFingerprint, Vector3WithHeading, string nativeFilename)` — packages the current `GameState` snapshot and delegates to `SidecarStore.WriteSidecar`.
- `HydrateFromSidecar(Sidecar)` — wraps existing `ApplyGameState`.

**Kept:**
- `NewGame()` — now triggered automatically by `LoadDetector` on no-match instead of by mod menu.
- `UpdatePlayTime`, `ApplyGameState`, `OnGameSaved` / `OnGameLoaded` events (still useful for HUD/menu reactions).

### `IGameBridge` additions

```csharp
SaveFingerprint GetCurrentFingerprint();
Vector3WithHeading GetPlayerPositionWithHeading();
```

`MockGameBridge` gets matching implementations driven by seeded values (per CLAUDE.md mock alignment guidance).

### `LegacyBackupTask` (new, one-shot at startup)

If `Documents\FactionWars\save_slot_*.json` files exist on first launch of the new system:
1. Move them all to `Documents\FactionWars\legacy_backup_<UTC-timestamp>\`.
2. Log the action.
3. Create `Documents\FactionWars\sidecars\` if missing.

### Wiring in `FactionWarsScript` / `ServiceContainerFactory`

Constructs `NativeSaveWatcher`, `LoadDetector`, `SidecarStore`, `LegacyBackupTask`. Subscribes:

- `NativeSaveWatcher.OnNativeSaveWritten` → `GameStateManager.WriteCurrentSidecar`
- `LoadDetector.OnLoadDetected(sidecar)` → `GameStateManager.HydrateFromSidecar` if non-null, else `GameStateManager.NewGame()`

`LegacyBackupTask.Run()` invoked once at startup before any other persistence component.

## Data flow

### First launch
1. `LegacyBackupTask` moves any `save_slot_*.json` files to `legacy_backup_<UTC>\`.
2. `sidecars\` dir created if missing.

### Player saves via GTA pause menu (incl. autosave)
1. GTA writes `SGTA0000X`.
2. `FileSystemWatcher` fires; debounce window holds.
3. After debounce, `OnNativeSaveWritten` emitted.
4. `GameStateManager.WriteCurrentSidecar`:
   - Gets fingerprint and position from `IGameBridge`.
   - Snapshots current `GameState`.
   - `SidecarStore.WriteSidecar` writes `sidecar_<totalPlayTimeSeconds>.json`.

### Player loads via GTA pause menu
1. GTA reads SGTA file; loading screen appears.
2. `LoadDetector` observes `IsLoading: true → false`.
3. After ~500ms settle, captures current fingerprint.
4. If `totalPlayTimeSeconds` unchanged from last known → skip (false-positive guard).
5. Otherwise `SidecarStore.TryFindByFingerprint`.
6. Found → `HydrateFromSidecar`.
7. Not found → `NewGame()`.

### Player starts a new GTA campaign
- Equivalent to "load with no matching sidecar." `NewGame()` runs once the prologue's first loading screen completes. Subsequent autosaves create sidecars normally.

### Player deletes a GTA save
- Watcher swallows the deletion at its own level (no public event raised). Sidecar persists as orphan per the no-auto-delete policy.

## Error handling

| Failure | Behavior |
|---|---|
| Sidecar write IO error | Caught, logged via `FileLogger.Error`, not rethrown. Mod state in memory unaffected; next save retries. |
| Sidecar JSON corrupt on read | `TryFindByFingerprint` returns false, logs the bad path. Load falls through to `NewGame()`. Bad file left on disk for inspection. |
| Profile dir not present at startup | Watcher enters deferred mode, polls every 30s, attaches when dir appears. |
| Multiple Rockstar profile dirs | Pick the one with most-recent collective `SGTA*` mtime. Switching profiles mid-session unsupported (documented). |
| Watcher event on partial write | Debounce + post-debounce 50ms quiet check, retry up to 5 times before giving up. |
| `IGameBridge` returns null fingerprint (game not ready) | `WriteCurrentSidecar` aborts silently with log; shouldn't happen in practice since saves only fire post-world-load. |
| Loading-screen false positive (cutscene, fast-travel) | `LoadDetector` skips hydration when `totalPlayTimeSeconds` is unchanged. |
| Watcher misses an event under buffer pressure | On every `OnLoadDetected`, also re-scan SGTA dir for any file with mtime newer than our newest sidecar — defense-in-depth. |
| Fingerprint collision (essentially impossible) | If primary match exists but tiebreakers fail → log warning, fall through to `NewGame`. Better fresh than wrong. |
| SHVDN script reload mid-session | Watcher and detector are reconstructed cleanly. No persistent in-memory state needed beyond sidecar files. |
| Cloud save sync from another machine | New SGTA appears; we treat as a normal save event. Likely no matching sidecar → `NewGame`. Documented limitation. |

## Testing

### Unit tests
- **`SaveFingerprint`**: `ExactMatch`, `PrimaryMatch`, equality/hashcode behaviors.
- **`SidecarStore`** (with temp-dir or in-memory FS): write/read round-trip, no-match returns false, primary-match-but-tiebreaker-mismatch returns false, corrupt JSON swallowed.
- **`LoadDetector`** (mocked `IGameBridge`): fires on transition with match, no-match dispatches null, false-positive guard skips when fingerprint unchanged.
- **`GameStateManager`**: `WriteCurrentSidecar` payload assembly, `HydrateFromSidecar` populates repositories, no `GetPlayerMoney`/`GetPlayerWeapons` calls remain in save/load codepath.
- **`LegacyBackupTask`**: legacy files moved to dated subfolder, no-op when none present, name-collision suffix increment.

### Integration tests (real filesystem)
- **`NativeSaveWatcher` debouncing**: triple-touch within 100ms → one event; spaced touches → multiple events; 10-file burst → no events lost.

### Mock alignment (per CLAUDE.md)
- `MockGameBridge.GetCurrentFingerprint` and `GetPlayerPositionWithHeading` controllable per test.
- After first in-game runs, log lines for fingerprint capture cross-checked against `MockGameBridge` and updated where they diverge.

### Manual in-game verification checklist
1. Save slot 1 → exit → relaunch → load slot 1 → mod state restored.
2. Save slot 1 → save slot 2 → load slot 1 (older) → mod state matches slot 1.
3. Trigger autosave (mission complete) → sidecar written within ~1s of GTA save.
4. New GTA campaign on fresh profile → mod runs `NewGame`.
5. Load a GTA save predating mod install → mod runs `NewGame` silently.
6. Delete a GTA save → orphan sidecar persists (no auto-delete).
7. Mid-mission cutscene with loading screen → mod state unchanged (false-positive guard works).

Each item logged with a `SAVE_INTEGRATION` tag for diff against logs.

### Out of scope for v1
- Cross-machine save sync (cloud).
- Multiple Rockstar profiles switching mid-session.
- Property-based fingerprint collision testing (collisions are theoretical).
- A "restore position on load" feature — `playerPosition` is recorded for it but not consumed yet.
