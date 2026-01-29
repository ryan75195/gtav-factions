# Difficulty Settings Design

> **For Claude:** REQUIRED SUB-SKILL: Use superpowers:executing-plans to implement this plan task-by-task.

**Goal:** Add Easy/Normal/Hard difficulty modes that adjust AI income multiplier and tick rate, accessible from the settings menu.

**Architecture:** A `DifficultyService` manages current difficulty settings and notifies subscribers when changed. The resource tick service applies the AI income multiplier, and the tick interval adjusts dynamically.

**Tech Stack:** C#, existing menu system, existing save/load infrastructure.

---

## Difficulty Presets

| Difficulty | AI Income Multiplier | Tick Interval |
|------------|---------------------|---------------|
| Easy       | 0.75x               | 7 minutes     |
| Normal     | 1.0x                | 5 minutes     |
| Hard       | 1.25x               | 3 minutes     |

- AI income multiplier affects AI factions only (player income stays 1.0x)
- Tick interval affects all factions equally (economy pacing)

---

## Data Model

### Difficulty Enum
```csharp
public enum Difficulty
{
    Easy,
    Normal,
    Hard
}
```

### DifficultySettings Class
```csharp
public class DifficultySettings
{
    public Difficulty Level { get; }
    public float AiIncomeMultiplier { get; }
    public int TickIntervalMinutes { get; }

    public static DifficultySettings Easy => new(Difficulty.Easy, 0.75f, 7);
    public static DifficultySettings Normal => new(Difficulty.Normal, 1.0f, 5);
    public static DifficultySettings Hard => new(Difficulty.Hard, 1.25f, 3);

    public static DifficultySettings FromLevel(Difficulty level) => level switch
    {
        Difficulty.Easy => Easy,
        Difficulty.Normal => Normal,
        Difficulty.Hard => Hard,
        _ => Normal
    };
}
```

---

## IDifficultyService Interface

```csharp
public interface IDifficultyService
{
    DifficultySettings Current { get; }
    void SetDifficulty(Difficulty level);
    event EventHandler<DifficultySettings>? DifficultyChanged;
}
```

---

## Integration Points

### Resource Tick Service
- When calculating AI faction income, multiply by `IDifficultyService.Current.AiIncomeMultiplier`
- Player faction income is NOT multiplied (always 1.0x)

### Economy Manager / Tick Timer
- Subscribe to `DifficultyChanged` event
- Update tick interval to `Current.TickIntervalMinutes * 60` seconds
- Takes effect on next tick (no mid-tick disruption)

---

## Settings Menu UI

### Menu Structure
```
Settings Menu
├── Save Game
├── Load Game
├── Difficulty: [Current]  ← NEW
├── Debug Mode: Off
└── Back
```

### Difficulty Submenu
```
Difficulty Menu
├── Easy - AI earns less, slower ticks
├── Normal - Balanced experience  [Current]
├── Hard - AI earns more, faster ticks
└── Back
```

### Confirmation Dialog
When selecting a different difficulty:
- Show confirmation: "Change difficulty to {level}? This will affect game balance."
- Options: Confirm / Cancel
- Only shown when changing, not when selecting current difficulty

---

## Persistence

### Save File
- `GameState` includes `Difficulty` property
- Saved/loaded with game state
- Old saves without difficulty default to `Normal`

### Config File
- `config.json` stores `defaultDifficulty` for new games only
- Save files override config when loading

```json
"gameplay": {
    "defaultDifficulty": "Normal",
    ...
}
```

---

## Backwards Compatibility

- Existing saves load as `Normal` difficulty
- Existing config `difficultyMultiplier` field deprecated (can be removed later)
- Existing `resourceTickIntervalMinutes` becomes the Normal preset value

---

## Files to Create/Modify

### Create
- `src/FactionWars/Core/Models/Difficulty.cs` - Enum
- `src/FactionWars/Core/Models/DifficultySettings.cs` - Settings class
- `src/FactionWars/Core/Interfaces/IDifficultyService.cs` - Interface
- `src/FactionWars/Core/Services/DifficultyService.cs` - Implementation

### Modify
- `src/FactionWars/Economy/Services/ResourceTickService.cs` - Apply AI multiplier
- `src/FactionWars/ScriptHookV/UI/SettingsMenuController.cs` - Add difficulty menu
- `src/FactionWars/ScriptHookV/GameLoopController.cs` - Wire up service, handle tick interval changes
- Save/load models - Add difficulty field
- `bin/FactionWars/config.json` - Add defaultDifficulty
