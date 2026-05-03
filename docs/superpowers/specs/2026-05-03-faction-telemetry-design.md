# Faction Telemetry System — Design

## Goal

Capture rich, structured per-match telemetry so a player or developer can reconstruct what every faction did during a single gamesave: faction state over time, every zone capture, every battle, every AI decision, every recruitment, every player kill. Output is per-save CSV files suitable for Excel pivot tables.

## Motivation

Right now telemetry is interleaved into the free-form `FileLogger` (`Documents\FactionWars\Logs\FactionWars_*.log`) and is partial:

- AI decision tick prints `Cash/Troops/Zones` per AI faction once per minute, but **skips the player faction** (the `michael state:` line never appears).
- Capture/loss events are mentioned in passing but not in a parseable form.
- Battles, recruitments, allocations, and player actions are scattered across log lines.

We can't graph anything without hand-parsing 1 MB of mixed text. A dedicated, structured telemetry pipeline solves this.

## Design philosophy

**Telemetry is a subscriber, not a producer.** Domain services keep raising the events they already raise. A single `TelemetryService` listens, transforms domain events into telemetry DTOs, and routes them to a swappable sink. Adding a new event type means: define a DTO + add one sink method + subscribe in one place. Nothing else in the codebase has to know telemetry exists.

This avoids two anti-patterns:

1. Sprinkling `_telemetry.WriteX(...)` calls through domain services (couples domain to telemetry).
2. Inventing a separate event bus when the codebase already uses standard `event EventHandler<T>` plumbing.

## Architecture

```
                 [Domain services — emit events as part of their normal job]
                        │
   ZoneBattleManager ───┤  (existing OnZoneBattleStarted/Ended)
   AIManager ───────────┤  (existing OnAIDecision)
   AIController ────────┤  (new event: OnTroopsRecruited)
   ZoneService ─────────┤  (new event: ZoneOwnershipChanged)
   ZoneDefenderAlloc ───┤  (existing TroopsAllocated)
   ResourceTickService ─┤  (existing OnResourceTick)
   GameStateManager ────┤  (existing OnGameLoaded)
   NativeSaveWatcher ───┤  (existing OnNativeSaveWritten)
   VictoryManager ──────┤  (existing OnVictory)
   DifficultyService ───┤  (existing DifficultyChanged)
   FollowerManager ─────┤  (existing FollowerDied)
   FriendlyDefenderMgr ─┤  (existing DefenderDied)
   BattleAttackerMgr ───┤  (new event: AttackerKilled)
                        ▼
                 ┌──────────────────────┐
                 │  TelemetryService    │  (orchestrator)
                 │   • 60s timer        │  → builds FactionSnapshot per faction
                 │   • event handlers   │  → maps domain events to telemetry DTOs
                 │   • save-file router │  → forwards SetSaveFile(...)
                 │   • player-kill calc │  → resolves killer == player from ped data
                 └──────────────────────┘
                        │ writes to
                        ▼
                 ┌──────────────────────┐
                 │  ITelemetrySink      │  (interface)
                 └──────────────────────┘
                        │
                ┌───────┴──────────┐
                ▼                  ▼
        CsvTelemetrySink     NullTelemetrySink (tests, opt-out)
                │
                ▼
   Documents\FactionWars\Telemetry\<saveFilename>\
       ├── snapshots.csv
       ├── zone_events.csv
       ├── battles.csv
       ├── decisions.csv
       ├── recruitments.csv
       ├── allocations.csv
       ├── resource_ticks.csv
       ├── match_meta.csv
       └── player_events.csv
```

**Per-save directory** (not 4–9 root files) keeps things tidy: one folder per gamesave keyed by native save filename (e.g., `SGTA0004\`).

## CSV schemas

All CSVs share a `Timestamp + PlayTimeSeconds` prefix as the cross-pivot join key.

### `snapshots.csv` — periodic state per faction (every 60s)

```
timestamp,play_time_seconds,faction_id,cash,total_troops,zones_owned,basic,medium,heavy,elite,reserve_troops,deployed_troops
```

One row per active faction per tick. Player faction included. "Active" = present in `IFactionService.GetAllFactions()` regardless of zone count (i.e., a faction with 0 zones still gets a row until eliminated).

### `zone_events.csv` — ownership changes

```
timestamp,play_time_seconds,event_type,zone_id,previous_owner,new_owner
```

`event_type` ∈ `Captured | Lost | Neutralized`. `previous_owner`/`new_owner` may be empty (neutral).

### `battles.csv` — combat lifecycle

```
timestamp,play_time_seconds,event_type,zone_id,attacker_faction,defender_faction,attacker_troops,defender_troops,outcome,attacker_casualties,defender_casualties
```

`event_type` ∈ `Started | Ended`. Casualties and outcome populate only on `Ended`.

### `decisions.csv` — AI strategic decisions

```
timestamp,play_time_seconds,faction_id,decision_type,target_zone,troops,priority,executed
```

`decision_type` ∈ `Attack | Defend | Reinforce | Idle`. Priority is the AI's own scoring float. `executed` distinguishes decisions that ran from decisions that were filtered out.

### `recruitments.csv` — troop recruitment cycles

```
timestamp,play_time_seconds,faction_id,troops_recruited,cost,cash_before,cash_after
```

One row per faction per recruitment cycle.

### `allocations.csv` — troop deployments

```
timestamp,play_time_seconds,faction_id,zone_id,tier,count,source
```

`source` ∈ `Player | AI | Initial`. Tier ∈ `Basic | Medium | Heavy | Elite`.

### `resource_ticks.csv` — income events

```
timestamp,play_time_seconds,faction_id,income,zones_contributing
```

### `match_meta.csv` — rare match-level events

```
timestamp,play_time_seconds,event_type,details
```

`event_type` ∈ `MatchStart | Victory | Defeat | DifficultyChanged | ModSessionStart | ModSessionEnd`. `details` is a free-form column for variant data (e.g., `winner=michael` or `difficulty=Hard`).

Trigger semantics:
- `MatchStart`: emitted on the first `OnGameLoaded` of a never-before-seen save filename (i.e., the per-save Telemetry folder did not exist yet) — written exactly once per save.
- `ModSessionStart`/`ModSessionEnd`: emitted on `TelemetryService` construction/disposal — one pair per mod run.
- `Victory`/`Defeat`: from `VictoryManager.OnVictory`. (`MatchEnd` is implicit — Victory or Defeat marks the end.)
- `DifficultyChanged`: from `DifficultyService.DifficultyChanged`.

### `player_events.csv` — player-personal actions

```
timestamp,play_time_seconds,event_type,zone_id,target_faction,target_tier,details
```

`event_type` ∈ `Kill | Death | FollowerRecruited | FollowerDied | ZoneEntered | ZoneExited | RespawnAtHospital`. `target_faction` and `target_tier` populate only on `Kill`.

## DTOs

Each CSV maps 1:1 to an immutable record:

```csharp
record FactionSnapshot(...);
record ZoneEvent(...);
record BattleEvent(...);
record DecisionEvent(...);
record RecruitmentEvent(...);
record AllocationEvent(...);
record ResourceTickEvent(...);
record MatchMetaEvent(...);
record PlayerEvent(...);
```

All in `FactionWars.Telemetry.Models`.

## Components

### `ITelemetrySink` (new interface, `FactionWars.Telemetry.Interfaces`)

```csharp
public interface ITelemetrySink : IDisposable
{
    void SetSaveFile(string saveFilename);
    void WriteSnapshot(IReadOnlyList<FactionSnapshot> rows);
    void WriteZoneEvent(ZoneEvent ev);
    void WriteBattle(BattleEvent ev);
    void WriteDecision(DecisionEvent ev);
    void WriteRecruitment(RecruitmentEvent ev);
    void WriteAllocation(AllocationEvent ev);
    void WriteResourceTick(ResourceTickEvent ev);
    void WriteMatchMeta(MatchMetaEvent ev);
    void WritePlayerEvent(PlayerEvent ev);
}
```

### `CsvTelemetrySink` (new, `FactionWars.Telemetry.Csv`)

- Owns the per-save directory and the 9 file handles (lazy-opened on first write).
- Lock-protected file appends, mirroring `FileLogger` pattern.
- Header row written exactly once when each file is first created.
- Buffers each event type in memory (per-type `List<...>`, capped at 10k each) until `SetSaveFile(...)` is called. On call, creates the per-save directory, flushes buffers, switches to direct-write mode for future events.
- If session ends with no save filename ever set, buffer is discarded (no save = no canonical home). This is logged once via `FileLogger.Info`.
- All file I/O wrapped in try/catch — logs error via `FileLogger.Error` and continues. Telemetry never throws into game code.

### `NullTelemetrySink` (new)

No-op implementation for tests and for an explicit opt-out. Default-injectable.

### `TelemetryService` (new, `FactionWars.Telemetry.Services`)

Orchestrator. Constructor takes `ITelemetrySink`, `IFactionService`, `IZoneService`, `IGameStateManager`, plus the various event-source interfaces (or a more granular subscription helper).

Responsibilities:

1. **Snapshot timer.** A `Stopwatch`-driven 60s tick (separate from AI tick) that builds one `FactionSnapshot` per active faction (player + AI) by reading `IFactionService` + `IZoneService`, and calls `_sink.WriteSnapshot(rows)`.
2. **Event subscriptions.** Subscribes to all sources listed in the architecture diagram. Each handler converts the domain event args to the corresponding telemetry DTO and forwards to the sink.
3. **Save-file routing.** Subscribes to `OnGameLoaded` and `OnNativeSaveWritten`; calls `_sink.SetSaveFile(saveFilename)`.
4. **Player kill detection.** Subscribes to `BattleAttackerManager.AttackerKilled` (new event). Reads the killer ped handle via `IGameBridge.GetPedKiller(deadPedHandle)`. If killer == player ped → emits a `PlayerEvent { Type=Kill, target_faction=..., target_tier=... }`.
5. **Lifecycle.** Subscribes/unsubscribes cleanly on `Dispose`. Owned by `FactionWarsScript` like other services.

Integration is one new line in `ServiceContainerFactory` to register `ITelemetrySink` (default `CsvTelemetrySink`) and `TelemetryService`, plus `FactionWarsScript` instantiates and disposes the service alongside its existing managers.

## New domain events

Three new events on existing services (small, well-scoped additions):

1. **`IZoneService.ZoneOwnershipChanged`** — raised inside `TransferZoneOwnership` when ownership actually changes. Args: `{ZoneId, PreviousOwner, NewOwner}`. Replaces the imperative `EventFeedService.AddZoneCaptured(...)` pattern long-term, but UI keeps its current callsites for now (refactor deferred).
2. **`AIController.OnTroopsRecruited`** — raised at end of recruitment cycle per faction. Args: `{FactionId, TroopsRecruited, Cost, CashBefore, CashAfter}`.
3. **`BattleAttackerManager.AttackerKilled`** — raised when a tracked attacker ped is detected dead. Args: `{ZoneId, FactionId, Tier, PedHandle, KillerPedHandle}`. Telemetry resolves "is this a player kill" downstream.

Each is a small, well-scoped event with a single producer; the producers stay responsible for raising them but don't know about telemetry.

## Save-filename handoff

```
Mod start (no save loaded yet)
  └─ TelemetryService starts buffering events in memory

LoadDetector matches sidecar     OR    First NativeSaveWatcher event
  └─ sink.SetSaveFile("SGTA0004") ──┘
        │
        ▼
   Open Documents\FactionWars\Telemetry\SGTA0004\
   Flush all buffered events to their CSVs
   Future events write directly
```

Edge cases:

- **Fresh new game, no save written**: rows buffer until first save. Cap = 10k rows per file (~28 hours of snapshots). Beyond cap, oldest dropped. Logged once.
- **Loading older save in same slot**: play_time_seconds jumps backward in `snapshots.csv`. Acceptable — graphs will show the discontinuity. No special handling.
- **New game in existing slot**: same as above; play_time goes backward to 0-ish. Acceptable.
- **Mod session ends before any save**: buffer discarded silently (logged).

## Error handling

- Sink wraps all file I/O in try/catch. Logs first error per CSV via `FileLogger.Error`, then suppresses subsequent ones for that file (avoid log spam if disk is full).
- Disposed sink ignores writes (no-throw).
- `TelemetryService` event handlers wrapped in try/catch — a bad event arg never crashes the game.
- `NullTelemetrySink` for tests means production code can never reach a null sink.

## Testing strategy

### Unit tests

- **`CsvTelemetrySinkTests`** — temp directory:
  - Header row written once per file
  - Multiple writes append correctly
  - `SetSaveFile` flushes pending buffers to the right directory
  - Buffer cap drops oldest
  - File I/O exception is swallowed and logged
  - Dispose is idempotent
- **`FactionSnapshotBuilderTests`** — given mock services, verifies snapshot DTO fields including tier breakdown and reserve/deployed split for all factions including player.
- **`TelemetryServiceTests`** — given a mock `ITelemetrySink`:
  - Snapshot timer tick → `WriteSnapshot` called with one row per active faction
  - `OnZoneBattleStarted` → `WriteBattle(Started)` with correct fields
  - `OnZoneBattleEnded` → `WriteBattle(Ended)` with outcome and casualties
  - `ZoneOwnershipChanged(prev=trevor, new=michael)` → `WriteZoneEvent(Captured)` + corresponding `Lost` row
  - `OnAIDecision` → `WriteDecision`
  - `AttackerKilled` with killer == player → `WritePlayerEvent(Kill)`
  - `AttackerKilled` with killer != player → no `WritePlayerEvent`
  - `OnGameLoaded` → `SetSaveFile` forwarded
  - `OnVictory` → `WriteMatchMeta(Victory)`

### Integration tests

- **`TelemetryEndToEndTests`** — wire real `TelemetryService` + `CsvTelemetrySink` (temp dir) + mock domain services. Drive a scripted scenario (start mod → load save → simulate battle → verify CSVs on disk have the expected rows).

### Manual verification

- Run mod in-game for a session. Open `Documents\FactionWars\Telemetry\SGTA000X\snapshots.csv` in Excel; build a chart. Confirm player rows present and reasonable.

## In scope vs deferred

**In scope:**
- All 9 CSVs and DTOs
- `ITelemetrySink`, `CsvTelemetrySink`, `NullTelemetrySink`
- `TelemetryService` orchestrator + 60s snapshot timer
- 3 new domain events (`ZoneOwnershipChanged`, `OnTroopsRecruited`, `AttackerKilled`)
- Buffer-then-flush save-filename handoff
- Unit + integration tests
- Manual in-game verification

**Deferred:**
- Refactoring `EventFeedService`/`EventAlertService` callsites onto `ZoneOwnershipChanged` (architectural cleanup, separate work).
- JSON sink, network sink, in-game stats UI.
- Cumulative metrics (kills_total, recruits_total) — derivable from event streams in post-processing.
- Player-civilian-kill tracking (Tier-3-broader). Current scope is "kills of tracked combatants" only.
- Cross-session "master CSV" with `session_id` column. Per-save folder is enough for the foreseeable future.
- Telemetry settings UI (enable/disable per category). Default-on is fine; `NullTelemetrySink` provides explicit opt-out via DI override.

## Open questions

None — all design decisions resolved during brainstorming.
