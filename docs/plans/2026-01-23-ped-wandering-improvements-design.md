# Ped Wandering & Spawn Improvements

## Overview

Improve friendly and enemy troop spawning and wandering behavior:
- Random spawn positions across zones (not deterministic circles)
- Zone-wide wandering using actual zone radius
- Sprint mode during battles, walk mode in peaceful zones
- Combat engagement via relationship system

## Behavior Matrix

| Scenario | Peds | Movement |
|----------|------|----------|
| Friendly zone, no battle | Friendly defenders | Walk |
| Friendly zone, under attack | Friendly defenders + enemy attackers | Sprint |
| Enemy zone | Enemy defenders | Sprint |
| Enemy zone, attacking | Enemy defenders + friendly attackers | Sprint |

## Changes

### 1. IGameBridge / GameBridge

Add new method:
```csharp
void TaskPedWanderInAreaSprinting(int pedHandle, Vector3 center, float radius);
```

### 2. FriendlyDefenderManager

- Replace `CalculateSpawnPosition` with `CalculateRandomSpawnPosition` (matching enemy logic)
- Remove fixed `WanderRadius = 40f` constant
- Use zone radius for wandering
- Add `SetPedAsHostileWanderer` call for combat engagement
- Add battle awareness:
  - `OnBattleStarted(string zoneId)` - switch friendlies to sprint
  - `OnBattleEnded(string zoneId)` - switch friendlies back to walk
- Store zone info for spawned peds to support re-tasking

### 3. EnemyDefenderManager

- Add `TaskPedWanderInAreaSprinting` call with zone radius
- Already has random spawn positions

### 4. BattleAttackerManager

- Add `TaskPedWanderInAreaSprinting` call with zone radius
- Already has random spawn positions

## Wire-up

FriendlyDefenderManager needs to subscribe to battle events from ZoneBattleManager to toggle sprint/walk modes.
