# 3-Way Zone Battles — Design

## Goal

When two AI factions are fighting over a zone and the player walks
in, the three sides should fight as a single coherent battle —
defender, AI attacker, and player, all hostile to each other — with
one source-of-truth state and one ownership outcome. Today's
behaviour creates two parallel battles (`CombatManager` for the
player, `ZoneBattleManager` for the AIs) that race to write zone
ownership, so an AI win can overwrite a player win when both resolve
in overlapping windows.

This redesign unifies the two systems behind `ZoneBattleManager`,
adds a participant model to `ZoneBattle`, and supports 3-way melee
when the player joins a contested zone.

## Scope

In scope:
- `ZoneBattle` model becomes participant-list based.
- `ZoneBattleManager` gains `StartPlayerCombat`, `JoinAsAttacker`,
  and `RemoveParticipant` lifecycle entry points.
- `CombatManager` and `CombatEncounter` are eliminated; their
  responsibilities migrate into `ZoneBattleManager` and a couple of
  thin helpers.
- 3-way kill routing and victory conditions in `ZoneBattleManager`.
- HUD shows three troop counts when a 3-way is active.
- AI ped blips on the world map use the AI faction's color (always —
  not just during a 3-way; see Section 6).
- Persistence migration: existing 2-way savegames load into the new
  participant model.

Out of scope (locked out by decisions Q1–Q5 made during brainstorming):
- AI-AI-AI 3-ways. The data model is generic enough to allow them
  later, but `ZoneBattleManager.JoinAsAttacker` rejects any non-player
  third party in v1 (Q2.A).
- Player-allocated expedition troops. Player participant's count
  comes from "self + squad followers" only (Q4.A).
- Direct player capture. When the player wins a 3-way, the zone goes
  neutral, exactly as today's player-vs-AI takeover flow (Q5.A).
- Temporary alliances / friendship machinery. All three sides are
  hostile to all others, full stop (Q1.A).

## Decisions (recap)

| # | Decision |
|---|---|
| Q1 | True 3-way melee — every side hostile to every other side. |
| Q2 | Only the player can be a third party. AI factions can't pile on a contested zone in v1. |
| Q3 | Eliminate `CombatManager`; player combat lives inside `ZoneBattleManager`. |
| Q4 | Player's troop count = self + squad followers. No reserve concept on the player side. |
| Q5 | Player win → zone goes neutral, two-step capture preserved. |
| Q6 | AI peds are always blip-colored by faction (covers the 3-way distinguishability ask, no dynamic re-coloring). |

## 1. Data model

`ZoneBattle` switches from named attacker/defender fields to a
participant list. The new shapes:

```csharp
public class BattleParticipant
{
    public string FactionId { get; }
    public BattleRole Role { get; }      // Defender | Attacker
    public bool IsPlayer { get; }
    public Dictionary<DefenderTier, int> Troops;  // empty for player
    // Player participant: alive count comes from a Func<int> the manager
    // hands in at join time, delegating to the squad/follower service.
    public int AliveCount { get; }       // sum of Troops, OR squad-callback
}

public enum BattleRole { Defender, Attacker }
```

`ZoneBattle.Participants` is `IReadOnlyList<BattleParticipant>`.
Convenience computed accessors keep the diff manageable for the rest
of the codebase:

- `Defender` → the single `Defender`-role participant.
- `Attackers` → the list of `Attacker`-role participants (length 1 in
  2-way, 2 in 3-way).
- `TotalDefenderTroops`, `TotalAttackerTroops` — backwards-compatible
  computed properties.

**Cap is enforced in the manager, not the model.** `ZoneBattleManager`
will reject `JoinAsAttacker` if there are already 2 attackers. The
`BattleParticipant` list itself is unbounded so a future extension to
N-way (Q2.B) doesn't require another model change.

## 2. Lifecycle (start / join / leave)

`ZoneBattleManager` exposes three lifecycle entry points:

- **`StartBattle(zoneId, attackerFactionId, defenderFactionId,
  attackerTroops, defenderTroops)`** — existing signature, unchanged.
  Used by `AIController` for AI-vs-AI battles. Creates a 2-way battle
  with one `Defender` participant and one `Attacker` participant.

- **`StartPlayerCombat(zone, playerFactionId)`** — replaces today's
  `CombatManager.StartCombat`. Called from
  `GameLoopController.OnZoneEntered` when the player walks into an
  enemy zone. Two cases:
  - No existing battle → create a new battle, defender = zone owner,
    attacker = player.
  - Existing battle → call `JoinAsAttacker` instead.

- **`JoinAsAttacker(zoneId, factionId, isPlayer)`** — adds a new
  `Attacker`-role participant to an existing battle. Returns false if
  2 attackers are already present. v1 rejects `isPlayer == false`
  (locking out AI third parties; Q2.A).

- **`RemoveParticipant(zoneId, factionId)`** — fires when:
  - Player exits the zone (`OnZoneExited` calls this for the player
    participant).
  - A participant is wiped (`AliveCount == 0`); called internally by
    the tick.

  Removal is followed by a victory check; if exactly one participant
  remains, `BattleEnded` fires for that survivor.

**Player-exit-while-3-way (the heart of the bug fix):**
- Player participant removed.
- Battle continues as 2-way (AI₁ vs defender), troop counts unchanged.
- Off-screen sim resumes via existing 2-way `Tick()`.

## 3. Kill routing & victory conditions

Two regimes, mirroring today: "player present" (real ped deaths) and
"player absent" (simulated kills).

**Player present.** `FriendlyDefenderManager`, `EnemyDefenderManager`,
`BattleAttackerManager`, plus a new player-side death watcher
(formerly inside `CombatManager`), each call into
`ZoneBattleManager.ReportTroopKilled(zoneId, victimFactionId, tier)`.
The manager finds the victim's participant and decrements
`Troops[tier]`. Kills are attributed by victim only — no killer
tracking — same as today. This works in 3-way unchanged because every
ped already knows its own faction.

**Player absent.** Existing `ProcessKill` simulator. In v1 this only
ever runs in 2-way mode because the only 3-way is "player + AI₁ + AI₂"
and the player must be physically present to be a participant. When
the player exits, the participant is removed and the sim resumes from
its existing 2-way path. **No 3-way simulator is needed.**

**Victory check** (runs after every troop change):
1. Count participants with `AliveCount > 0`.
2. If exactly 1 → that participant wins. Emit `BattleEnded`.
3. If 0 (everyone wiped same tick — vanishingly rare) → defender
   keeps zone, no transfer.
4. If 2+ → battle continues.

**Outcome routing on `BattleEnded`:**
- Winner is **defender** → no ownership change. Refund attackers'
  reserve troops (existing AI-side behaviour preserved).
- Winner is **AI attacker** → zone ownership transferred to that
  faction (existing behaviour preserved).
- Winner is **player** → zone goes neutral
  (`CombatResultHandler`-equivalent logic, now inlined). Player's
  squad stays with the player, no troops to refund.

## 4. `CombatManager` migration

| Today | After migration |
|---|---|
| `CombatManager._currentEncounter` | Query `ZoneBattleManager.GetBattleForZone(playerCurrentZone)` and find player participant. |
| `CombatManager.StartCombat(zone, factionId)` | `ZoneBattleManager.StartPlayerCombat(zone, factionId)`. |
| `CombatManager.EndCombat(status)` | Driven by `BattleEnded` events. Player retreating triggers `RemoveParticipant`, which may resolve victory naturally. |
| `CombatEncounter.AttackerPedCount` / `DefenderPedCount` | `BattleParticipant.AliveCount`. |
| `CombatStatus` enum | Folded into `BattleOutcome` plus an `IsPlayerWinner` bit on `BattleEnded` event args. |
| `CombatResultHandler.ProcessCombatResult()` | Inlined into `ZoneBattleManager.OnBattleEnded` handler — one if-statement for the player-win-neutral branch. |
| `TakeoverDetector` | Stays. Reports up to `ZoneBattleManager.ReportTroopKilled` instead of `CombatManager`. |
| `CombatManager.IsInCombat` (callers throughout) | `ZoneBattleManager.IsPlayerInBattle()`. |
| `CombatManager.CurrentEncounter` (HUD reads) | `ZoneBattleManager.GetPlayerCurrentBattle()`. |

**Files affected:**
- Deleted: `Combat/Services/CombatManager.cs`,
  `Combat/Models/CombatEncounter.cs`.
- Reduced: `Combat/Services/CombatResultHandler.cs` (most logic
  inlined into `ZoneBattleManager.OnBattleEnded`; possibly removed
  entirely).
- Modified: `Combat/Services/TakeoverDetector.cs`,
  `Combat/Services/ZoneBattleManager.cs`,
  `Combat/Models/ZoneBattle.cs`,
  `ScriptHookV/GameLoopController.cs` (combat hooks, HUD update,
  zone-entry/exit flow), and the three defender/attacker managers
  (kill reporting routes through `ZoneBattleManager` rather than
  branching by "is player here?").

This is the largest mechanical piece. Plan it as a sequence of small,
testable steps — not a single big-bang commit.

## 5. UI & persistence

**HUD.** `TerritoryIndicatorRenderer.DrawEnemyTerritoryHud` and the
contested branch in `GameLoopController.UpdateAndDrawHud` (around
lines 423-438) currently surface two figures (defender + attacker).
Add `ThirdPartyCount` (and the third-party faction name/color) to
`TerritoryIndicatorData`, populated from the second `Attacker`
participant when present. The HUD draws a third row only when
nonzero. 2-way battles render identically to today.

**Persistence.** `ZoneBattleData` (savegame model) currently
serializes `AttackerFactionId`, `DefenderFactionId`, two troop dicts.
Refactor to a participant list with a `Version` field:
- `Version == 1` (legacy) loaders translate the two old fields into a
  2-participant list on read. Lazy migration — no separate script.
- `Version == 2` is the new participant-list format. New saves write
  v2.

## 6. AI ped blip faction coloring

Today, `EnemyDefenderManager` and `BattleAttackerManager` create
ped blips with a fixed red/hostile color (matching
"hostile = red" convention). In a 3-way, the defender and the AI
attacker would both render red and become indistinguishable on the
mini-map and pause-menu map.

**Resolution.** AI ped blips always use their faction's color, not
just during 3-way. Implementation:
- Each faction has a `Color` property (`FactionColor` enum) already.
- `IPedBlipService.CreateBlipForPed` is already called with a
  `BlipColor` argument by each manager.
- Each call site that today passes a hardcoded red is changed to look
  up the faction's color via `IFactionService.GetFaction(factionId)`
  and pass the resulting `BlipColor`.
- The player's friendly defenders (`FriendlyDefenderManager`) keep
  the existing `BlipColor.LightBlue` (the player faction's defining
  color today).

This delivers the 3-way distinguishability requirement without
introducing dynamic per-battle blip recoloring; the rule is uniform
regardless of whether a battle is active in the zone.

## Test plan (overview, plan-doc spells out individual cases)

- **Model:** existing `ZoneBattleTests` adapt to participant-list
  shape. New tests for join/remove/cap-of-3.
- **Manager:** `ZoneBattleManagerTests` extends with 3-way scenarios
  — player joins active AI battle, player wipes, player exits, AI
  wins after player exit, simultaneous-wipe edge case. All existing
  2-way tests stay green.
- **Integration:** today's `CombatManagerFlowIntegrationTests` is
  rewritten as `ZoneBattleManagerPlayerFlowTests`, exercising the
  same player-takeover scenarios through the unified API.
- **Bug repro (new test):** AI₁ attacking AI₂'s zone, player enters,
  player wins their side; assert that `BattleEnded` fires exactly
  once, the outcome is "zone neutral", and no later AI win event
  fires for that zone (proves the parallel-state race is dead).
- **Persistence:** `ZoneBattleDataTests` cover v1→v2 migration plus
  v2 round-trip.
- **Blip color (Section 6):** existing manager tests get assertions
  that `CreateBlipForPed` is called with the faction's color, not a
  hardcoded value. New unit test for the
  faction-color-lookup helper.

## Risks

- **Big refactor surface.** ~10-12 production files touched plus
  their tests. Mitigate by phasing the implementation plan: model
  refactor first (no behaviour change), then manager API additions,
  then `CombatManager` deletion in a series of small commits, then
  3-way wiring, then UI/blip changes last.
- **Save migration regressions.** Cover with explicit v1→v2 round-
  trip tests before deleting any v1 code.
- **Existing test churn.** A lot of `CombatManager`-specific tests
  will need to be retired or rewritten. Don't try to preserve them
  verbatim; rewrite to assert behaviour through the unified API.
