# Neutral Zone Mechanics Design

**Date:** 2026-01-20
**Status:** Approved

## Overview

Redesign territory control to use neutral zones as the primary expansion mechanic. Zones with 0 troops become neutral, and neutral zones can be claimed by paying for a single guard troop.

## Starting Conditions

### Faction Starting Zones (3 each, 5 Basic troops per zone)

| Faction | Zones | Rationale |
|---------|-------|-----------|
| **Michael** | Rockford Hills, Vinewood, Del Perro | Wealthy west side near his mansion |
| **Trevor** | Sandy Shores, Harmony, Grapeseed | Blaine County around his trailer |
| **Franklin** | Davis, Strawberry, Rancho | South LS around Forum Drive |

### Starting Resources (Normalized)

- **Cash:** $5,000 each
- **Zones:** 3 each (with 5 Basic troops per zone)
- **Reserve troops:** 0 (all troops start deployed)
- **Neutral zones:** 22 (remaining zones have no owner)

## Zone States

### State Definitions

1. **Owned** - Has owner faction + at least 1 troop
2. **Neutral** - No owner, no troops (gray on map)

### State Transitions

| Event | Result |
|-------|--------|
| Troops in owned zone drop to 0 | Zone becomes **neutral** |
| Win combat (kill all defenders) | Zone becomes **neutral** |
| Buy neutral zone (pay 1 Basic troop cost) | Zone becomes **owned** with 1 troop |

**Key Rule:** A zone cannot be owned with 0 troops. The moment the last troop dies, ownership is lost.

## Claiming Neutral Zones

### Player Interaction

1. Player enters neutral zone
2. Notification appears: "Unclaimed territory. Press [E] to claim for $X"
3. Player presses E to claim (if they have funds)
4. Cost deducted (Basic troop cost, ~$150)
5. Zone ownership set to player's faction
6. 1 Basic troop spawned as defender
7. Notification: "You now control [Zone Name]"

### Insufficient Funds

- Prompt shows but pressing E displays: "Not enough cash ($X needed)"

## Combat Results

### Player Wins Combat (kills all defenders)

1. Zone becomes **neutral** (owner set to null)
2. Defenders are gone, zone is empty
3. Prompt appears: "Zone cleared! Press [E] to claim for $X"
4. Player can claim immediately or leave

### Player Leaves Without Claiming

- Zone stays neutral
- Any faction (including AI) can claim it later
- Risk/reward: clear a zone but forget to claim = wasted effort

### Player Dies During Combat

- Combat ends (existing behavior)
- Zone ownership unchanged (defenders still there)

## Implementation Changes

### Files to Modify

| File | Change |
|------|--------|
| `FactionInitializer.cs` | 3 zones per faction, 5 troops each, $5k cash, 0 reserve |
| `CombatResultHandler.cs` | Victory → zone becomes neutral (not captured) |
| `GameLoopController.cs` | Add neutral zone detection + E key claim prompt |
| `Zone.cs` / `ZoneService.cs` | Add logic: 0 troops = neutral |
| `TerritoryManager.cs` | Detect neutral zone entry, show claim UI |
| `ZoneDefenderAllocationService.cs` | Initialize 5 troops per starting zone |

### New Functionality

- Claim prompt UI when in neutral zone
- E key handler to purchase claim
- Notification system for claim success/failure

### Removed/Changed

- Remove asymmetric starting conditions (8/10/5 zones → 3/3/3)
- Remove asymmetric starting cash ($10k/$8k/$5k → $5k/$5k/$5k)
- Combat victory no longer transfers ownership directly
