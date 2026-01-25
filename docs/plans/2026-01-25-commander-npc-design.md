# Commander NPC Design

## Overview

A "Commander" NPC that spawns in each player-owned zone, providing an immersive way to access the mod menu by aiming at them and pressing E.

## Requirements

- One Commander per player-owned zone
- Spawns at random navmesh-safe position (like defenders)
- Blue blip on minimap (distinct from white/light-blue defender blips)
- Military/tactical ped model (`s_m_y_armymech_01`)
- Wanders the zone like defenders
- Interaction: aim at Commander + press E to open main menu
- If killed: respawns immediately at new random location
- Despawns when territory is lost (all defenders die)
- F7 still works as alternative menu access

## Architecture

### Separation from Defender System

The Commander is completely separate from the defender/troop allocation system:

- Has its own manager (`CommanderManager`) - not part of `FriendlyDefenderManager`
- NOT tracked in `ZoneDefenderAllocation` - doesn't use troop counts
- Death does NOT affect territory loss logic
- No reserve pool - just spawns/respawns independently

However, the Commander IS affected by territory loss:
- When `FriendlyDefenderManager` raises `TerritoryLost` event, Commander despawns
- No Commander spawns in neutral/enemy zones

### New Components

```
CommanderManager (new class)
├── _spawnedCommanderByZone: Dictionary<string, int>  // zoneId → pedHandle
├── OnZoneEntered(zone) → spawn Commander if player-owned
├── OnZoneExited(zone) → despawn Commander
├── OnTerritoryLost(zoneId) → despawn Commander
├── Update() → check death (respawn), check interaction (open menu)
└── IsCommander(pedHandle) → for interaction detection
```

### Event Flow

```
Player enters owned zone
    ↓
TerritoryManager.ZoneEntered event
    ↓
CommanderManager.OnZoneEntered(zone)
    ↓
Spawn Commander at random navmesh position
    ↓
Create blue blip for Commander
```

```
All defenders die
    ↓
FriendlyDefenderManager.TerritoryLost event
    ↓
ZoneService.TransferZoneOwnership(zoneId, null)  // zone becomes neutral
    ↓
CommanderManager.OnTerritoryLost(zoneId)  // despawn Commander
```

### Interaction Detection

GTA V natives for aiming detection:

```csharp
// IGameBridge additions
int GetEntityPlayerIsFreeAiming();  // Returns ped handle or 0
bool IsPlayerFreeAiming();          // True if aiming weapon/fists
void ShowHelpText(string text);     // Display help text on screen
```

Interaction flow (every frame):

1. Is player free-aiming?
2. Get entity player is aiming at
3. Is that entity a Commander? (check against `_spawnedCommanderByZone` values)
4. If yes and NOT pressing E: show help text "Press ~INPUT_CONTEXT~ to talk to Commander"
5. If yes and pressing E: open main menu

## Ped Configuration

| Property | Value |
|----------|-------|
| Model | `s_m_y_armymech_01` |
| Blip color | `BlipColor.Blue` |
| Weapon | `weapon_carbinerifle` |
| Health | 300 |
| Armor | 100 |
| Accuracy | 75 |
| Relationship | `FRIENDLY_DEFENDERS` (same as friendly defenders) |

Combat attributes:
- Can use cover
- Will fight armed peds

Behavior:
- Wanders zone using `TaskPedWanderInArea()` with zone radius
- During battle: switches to `TaskPedWanderInAreaSprinting()`

## File Changes

### New Files

- `src/FactionWars/ScriptHookV/Managers/CommanderManager.cs`

### Modified Files

- `src/FactionWars/Core/Interfaces/IGameBridge.cs` - add targeting natives
- `src/FactionWars/ScriptHookV/GameBridge.cs` - implement targeting natives
- `src/FactionWars/Core/Utils/MockGameBridge.cs` - mock implementations
- `src/FactionWars/ScriptHookV/GameLoopController.cs` - create and wire CommanderManager

## Testing

Unit tests for CommanderManager:
- Commander spawns when entering player-owned zone
- Commander does NOT spawn in enemy/neutral zones
- Commander despawns when exiting zone
- Commander despawns on territory loss
- Commander respawns immediately on death
- Interaction detection identifies Commander correctly
- Menu opens when aiming at Commander and pressing E
