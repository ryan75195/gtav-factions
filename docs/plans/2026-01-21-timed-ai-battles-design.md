# Timed AI Territory Battles Design

## Overview

Replace instant AI battle calculations with timed battles that play out over 1-5 minutes. Includes a kill feed showing individual eliminations, a HUD displaying troop counts, and physical NPC combat when the player is present.

## Battle State Model

New `ActiveBattle` class to track ongoing battles:

```csharp
public class ActiveBattle
{
    public string AttackerFactionId { get; set; }
    public string DefenderFactionId { get; set; }
    public string ZoneId { get; set; }

    // Troop counts by tier (Basic, Medium, Heavy)
    public Dictionary<TroopTier, int> AttackerTroops { get; set; }
    public Dictionary<TroopTier, int> DefenderTroops { get; set; }

    public DateTime StartTime { get; set; }
    public float Duration { get; set; }  // in seconds
    public float NextKillTime { get; set; }  // seconds since start
    public float KillInterval { get; set; }

    public bool IsPlayerPresent { get; set; }
}
```

## Timing Mechanics

### Duration Calculation

```
totalTroops = attackerCount + defenderCount
duration = clamp(totalTroops * 6 seconds, 60s, 300s)
killInterval = duration / (totalTroops - 1)
```

Examples:
- 10 troops: 60s duration (hits floor), kills every ~6.7s
- 20 troops: 120s duration, kills every ~6.3s
- 50+ troops: 300s duration (hits cap), kills every ~6.1s

## Combat Tick Mechanics

### Kill Determination

Each tick when `NextKillTime` is reached:

1. Calculate weighted strength for each side:
   - Tier modifiers: Basic = 1.0, Medium = 1.5, Heavy = 2.0
   - Defender bonus: 1.5x multiplier

2. Roll weighted random to determine which side gets the kill:
   - Attacker chance = attackerStrength / (attackerStrength + defenderStrength)

### Victim Selection

When a side takes a casualty:
- Random selection weighted inversely by tier
- Death weights: Basic = 3, Medium = 2, Heavy = 1
- Elite troops survive longer on average

### Example

- Attackers: 5 Basic, 2 Medium = 5x1.0 + 2x1.5 = 8.0 strength
- Defenders: 3 Basic, 1 Heavy = (3x1.0 + 1x2.0) x 1.5 = 7.5 strength
- Attacker kill chance: 8.0 / (8.0 + 7.5) = 51.6%

## UI Components

### Kill Feed

Format: `"[AttackerFaction] KillerTier killed [DefenderFaction] VictimTier in ZoneName"`

Examples:
- `"[Ballas] Heavy killed [Grove St] Basic in Davis"`
- `"[Vagos] Medium killed [Ballas] Medium in Rancho"`

Uses existing notification/feed system. Each kill event generates one entry.

### Battle HUD

Persistent display showing active battle:
- Format: `"Davis: Ballas 12 vs Grove St 8"`
- Shows indicator when multiple battles: `"Battle 1/3"`
- Press configurable key (default: B) to cycle through active battles
- Defaults to battle closest to player or most recently started
- Updates in real-time as kills happen

### Battle Outcome Notification

When battle ends:
- Victory: `"[Ballas] captured Davis from [Grove St]"`
- Defense: `"[Grove St] defended Davis against [Ballas]"`

## Player Interaction

### Entering Contested Zone

When player enters a zone with an active battle:
1. Set `IsPlayerPresent = true`
2. Pause tick-based simulation
3. Spawn physical NPCs for both factions proportional to current troop counts
4. Attacker NPCs hostile to defenders (and player if enemy)
5. NPCs fight each other autonomously

### Player Kills

- Player killing a battle NPC decrements that faction's troop count
- Directly affects battle outcome
- If player eliminates all attackers, defenders win immediately

### Leaving Contested Zone

When player exits:
1. Snapshot current troop counts into `ActiveBattle`
2. Despawn remaining battle NPCs
3. Set `IsPlayerPresent = false`
4. Resume tick-based simulation with remaining troops

## Service Architecture

### New `ActiveBattleManager` Service

```csharp
public class ActiveBattleManager
{
    private List<ActiveBattle> _activeBattles;

    // Lifecycle
    public void StartBattle(string attackerId, string defenderId, string zoneId, TroopAllocation attackerTroops, TroopAllocation defenderTroops);
    public void Tick(float deltaTime);

    // Queries
    public ActiveBattle GetBattleForZone(string zoneId);
    public IReadOnlyList<ActiveBattle> GetAllActiveBattles();

    // Player presence
    public void OnPlayerEnterZone(string zoneId);
    public void OnPlayerExitZone(string zoneId);

    // Events
    public event Action<BattleKillEvent> OnKill;
    public event Action<BattleEndedEvent> OnBattleEnded;
}
```

### Integration Points

1. **AIController / BackgroundBattleSimulator**: Call `ActiveBattleManager.StartBattle()` instead of instant `BattleSimulationService.SimulateBattle()`

2. **GameLoopController**: Call `ActiveBattleManager.Tick()` each frame

3. **EnemyDefenderManager / FriendlyDefenderManager**: When battle active in zone, spawn battle NPCs. Track deaths and report to `ActiveBattleManager`

4. **Feed/Notification system**: Subscribe to `OnKill` and `OnBattleEnded` events

5. **HUD system**: Query `GetAllActiveBattles()` for display

## Edge Cases

### Multiple Fronts

- Factions can attack multiple zones simultaneously
- Only reserve troops can be committed to new battles
- Committed troops are locked to their battle

### Second Attacker on Same Zone

- If zone already has active battle, second attack is queued/rejected
- No 3-way battles; keeps logic simple

### Faction Depleted Globally

- If faction loses all reserves from other battles, committed troops in active battles remain
- Those battles continue to resolution with committed troops
- Faction just can't start new attacks

### Player Joins Mid-Battle

- NPCs spawn proportional to current (not original) troop counts
- Battle may be partially resolved already

## Configuration

| Setting | Default | Description |
|---------|---------|-------------|
| `MinBattleDuration` | 60 | Minimum battle length in seconds |
| `MaxBattleDuration` | 300 | Maximum battle length in seconds |
| `SecondsPerTroop` | 6 | Duration scaling factor |
| `DefenderAdvantage` | 1.5 | Defender strength multiplier |
| `CycleKey` | B | Key to cycle battle HUD |
