# Ped-Control & Stability Roadmap

**Status:** Approved (2026-06-28). Sequencing/decomposition doc — each stage gets its own
spec → plan → execute cycle. This file fixes the ORDER, dependencies, and scope only.

**Owner direction:** Build all stages end-to-end, one PR per stage, autonomously. One in-game
test pass by the user at the very end (Stage 5). Do not gate stages on the user reproducing the
crash; the breadcrumb instrumentation (Stage 1) is the net that names the culprit if anything
still freezes during that final test.

---

## Background

A crash was investigated this session (systematic-debugging, Phase 1 complete):

- **Symptom:** SHVDN killed `FactionWarsScript` under `ScriptTimeoutThreshold=5000` — one tick ran
  > 5 s on the game's main thread during an active Search-and-Destroy zone battle. This is a
  blocking-script *freeze*, not a .NET exception.
- **Evidence:** Every per-tick path in our managed code is bounded over ≤12 items; there is no
  unbounded loop or recursion; spawn waits yield via `Script.Wait`. `FileLogger` flushes every line
  (`File.AppendAllText`), so the last log line genuinely is the last code reached. Therefore the
  > 5 s was spent **inside a GTA native our tick invoked** — an engine-side AI/pathfinding stall.
- **User observation:** combatants "flickering in and out of aiming, running really fast, staying in
  the same spot" — the visual signature of combat AI pathfinding to a target every frame and
  failing. The player moving ~10 m away just before the freeze lengthened/broke those paths.
- **Not yet confirmed to a specific subsystem/native** — the hanging call emitted no log before
  dying. Confirmation needs in-game evidence (Stage 1).

Contributing smells found:

- `GameBridge.VehicleSpawning.cs` static `ConfigureFollowerCombat` applies a **blanket**
  `ABILITY=2 / RANGE=2 / MOVEMENT=2 (Offensive)` to *every* follower on *every* `SetPedAsFollower`,
  overriding the per-role profile on Escort re-follow ("the similar bug").
- **Diffuse ped-control ownership:** followers, enemy defenders, battle attackers, and friendly
  defenders are each tasked/configured by their own manager, with no single owner of a ped's desired
  task+config. Conflicting per-tick intents are the structural driver of the thrash.
- `FileLogger.Write` does synchronous open/write/flush/close **per line** under a global lock on the
  script thread — a real perf liability during combat (independent of the freeze).

---

## Global Constraints (apply to every stage)

- `.cs` files: CRLF line endings, UTF-8 no BOM. Edit in place to preserve.
- Analyzer ERRORS: no tuple return types; ≤250 lines/class (CI0017); ≤40 effective method lines
  (CI0007); one public top-level type per file; ≤5 constructor params; no `#pragma warning disable`
  for `CA*`/`CI*`.
- Architecture: GTA/NativeUI references stay in `ScriptHookV`; `Core` does not reference
  `ScriptHookV`; production code does not reference test-only deps. New native behavior goes behind
  `IGameBridge` (or a renderer/menu interface) first, then domain behavior is tested against mocks.
- TDD: no production code without a failing test first. Pre-commit hook runs
  `dotnet build FactionWars.sln --no-incremental` + `--filter FactionWars.Tests.Unit`. Never bypass.
- All new `IGameBridge` functionality includes `FileLogger` debug logging (per CLAUDE.md).
- Workflow per stage: `gh issue create` → branch `<type>/<issue>-<slug>` off master → TDD →
  commit (hook gate) → push → `gh pr create` → `gh pr merge --squash --delete-branch` →
  `git checkout master && git pull`. Branch names MUST carry a numeric issue number (enforced by
  hook).
- Commit footer:
  `Co-Authored-By: Claude Opus 4.8 (1M context) <noreply@anthropic.com>` +
  `Claude-Session: https://claude.ai/code/session_01MGyei1N5nCAgmp5DmiECej`.
  PR footer: `🤖 Generated with [Claude Code](https://claude.com/claude-code)`.

---

## Staged sequence

### Stage 0 — Clean base (chore) — issue #67
Establish a clean, observable starting point.
- Gitignore the generated `reports/` directory (telemetry HTML artifacts; currently untracked,
  not ignored).
- Confirm master builds green and is in sync with origin (done: `02091e1` #34 on master; local
  master == origin/master; baseline build exit 0).
- Stale local branches (refactor ratchets, telemetry variants) are clutter but NOT blocking — leave
  them; deleting risks unmerged work.

**Deliverable:** chore PR (`.gitignore` + this roadmap doc). **Depends on:** nothing.

### Stage 1 — Crash instrumentation (safety net + measurement)
- **Phase breadcrumb:** a dedicated, always-flushed single-line writer *independent of* `FileLogger`.
  Before each subsystem call in `UpdateWorldSystems`/`UpdateCoreSystems`, record the phase name — but
  only once a tick has already crossed a "slow" threshold (~1 s), to keep overhead near zero on
  healthy ticks. After a freeze, this file names the subsystem that was executing.
- **Per-subsystem timing:** stopwatch each sub-call; emit a `WARN` for any single call over a
  threshold and a "SLOW TICK" summary line when the whole tick exceeds a threshold. Gives a
  before/after tick-cost measurement to prove later stages helped.
- Pure threshold/timing/aggregation logic is TDD'd in `Core`/an injectable seam; the wiring lives in
  `ScriptHookV`.

**Deliverable:** PR. **Depends on:** Stage 0. **Why first:** it is the evidence net and the ruler
for everything after.

### Stage 2 — Logger async/buffered writer
- Replace per-line `File.AppendAllText` with a buffered/asynchronous writer (background flush, flush
  on shutdown/abort). Keep the Stage 1 breadcrumb on its own guaranteed-flush channel so the logger
  rewrite cannot compromise crash survival.
- TDD the buffering/flush/ordering behavior behind an abstraction; the file sink stays in
  `ScriptHookV`.

**Deliverable:** PR. **Depends on:** Stage 1 (breadcrumb channel carved out first). **Why here:**
independent and cheap; reduces I/O-induced hitches during the final combat test.

### Stage 3 — Ped-control consolidation (all controllers)
The structural fix. Gets its **own** brainstorm/spec when reached (large, multi-PR). Target shape: a
single per-ped **intent reconciler** that owns each ped's desired task + combat config and applies it
idempotently (generalizes the existing `AlreadyApplied`/`Remember` dedup), so no two systems issue
conflicting tasks to the same ped in the same tick. Demote `SetPedAsFollower` to a dumb primitive;
collapse the two `ConfigureFollowerCombat` methods; remove the blanket profile (fixes "the similar
bug"). Migrate, one independently-testable PR each: followers → enemy defenders → battle attackers →
friendly defenders, on top of the reconciler core.

**Deliverable:** sub-spec + multiple PRs. **Depends on:** Stages 1–2 (observable base to measure the
fix). **Scope:** ALL four ped-controlling systems.

### Stage 4 — Telemetry / mock-calibration (feat/62)
Implement the already-approved behavior-telemetry spec
(`docs/superpowers/specs/2026-06-28-behavior-telemetry-mock-calibration-design.md`, branch
`feat/62-behavior-telemetry-foundation`): observability primitives, `CombatBehaviorSampler` over an
`ITrackedCombatantSource` seam, `CsvBehaviorTraceSink`, and the first mock correction. Goes straight
to writing-plans (spec exists).

**Deliverable:** plan + PR(s). **Depends on:** sequenced after Stage 3 so traces reflect the new
architecture; otherwise independent.

### Stage 5 — Verification (user, in-game)
Deploy the Release DLL. User runs an S&D zone battle and confirms: no freeze, no aim/run thrash.
Read the Stage 1 timing/breadcrumb logs (should show healthy ticks; if any freeze, the breadcrumb
names the subsystem) and the Stage 4 behavior traces.

**Depends on:** Stages 1–4.

---

## Dependency summary

```
S0 clean base
  └─ S1 instrumentation (net + ruler)
       └─ S2 logger (breadcrumb channel already isolated)
            └─ S3 ped-control consolidation (measured against S1)
                 └─ S4 telemetry (traces reflect new arch)
                      └─ S5 user in-game verification
```

## Out of scope (this roadmap)
- Deleting stale local branches.
- Unrelated analyzer/refactor ratchet work.
- Any gameplay tuning beyond what the consolidation requires.
