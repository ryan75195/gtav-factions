# Territory HUD Redesign

## Goal

Replace the centered territory indicator with a compact, polished HUD box in the top-right corner that shows territory status with deployed/reserve troops for friendly zones and capture progress for enemy zones.

## Design

### Position & Style

- **Position:** Top-right corner (x=0.92, y=0.02)
- **Width:** ~15% of screen (0.15)
- **Style:** GTA V native - semi-transparent black background (40% opacity) with thin left accent bar in faction color
- **Text:** Small scale (0.35 main, 0.28 details) with shadow/outline

### Friendly Territory Display

```
┌──────────────────┐
│ VINEWOOD HILLS   │  ← Zone name, faction colored
│ Your Territory   │  ← Status subtitle
│ 8 deployed · 14  │  ← Spawned count · Reserve count
└──────────────────┘
```

- Green left accent bar
- Shows currently spawned defenders and reserve troops waiting

### Enemy Territory Display (During Combat)

```
┌──────────────────┐
│ DAVIS            │  ← Zone name, red
│ Enemy Territory  │  ← Status subtitle
│ ████░░░░ 45%     │  ← Capture progress bar
│ 3 vs 5           │  ← Your troops vs enemy defenders
└──────────────────┘
```

- Red left accent bar
- Shows capture progress and troop counts

### Neutral Zones

No HUD box displayed - keep screen clean.

## Performance Strategy

1. **Cache data** - Store current display values, only refresh on events
2. **Event-driven updates** - Subscribe to:
   - `DefenderDied` / `TerritoryLost` events
   - Zone enter/exit events
   - Combat state changes
   - Troop allocation changes
3. **Throttled refresh** - Max 1 data query per 500ms
4. **Cheap draw** - DRAW_RECT and TextElement are GPU-accelerated natives

## Additional Fix

**Menu scroll position retention:** Pass `selectedItemId` to `ShowMenu()` in `ZoneManagementMenuController` when refreshing after allocate/withdraw actions.

## Files to Modify

- `src/FactionWars/ScriptHookV/UI/TerritoryIndicatorRenderer.cs` - New box drawing
- `src/FactionWars/UI/Models/TerritoryIndicatorData.cs` - Add troop counts
- `src/FactionWars/UI/Services/TerritoryIndicatorService.cs` - Provide troop data
- `src/FactionWars/ScriptHookV/UI/ZoneManagementMenuController.cs` - Fix scroll retention
