# Difficulty Settings Implementation Plan

> **For Claude:** REQUIRED SUB-SKILL: Use superpowers:executing-plans to implement this plan task-by-task.

**Goal:** Implement Easy/Normal/Hard difficulty modes with AI income multiplier and tick rate adjustments, accessible from the settings menu.

**Architecture:** A `DifficultyService` manages presets and notifies subscribers. The `ResourceTickService` applies the AI income multiplier, and tick interval adjusts via existing `SetTickInterval` method.

**Tech Stack:** C#, xUnit, Moq, existing menu system

---

## Task 1: Create Difficulty Enum and DifficultySettings Model

**Files:**
- Create: `src/FactionWars/Core/Models/Difficulty.cs`
- Create: `src/FactionWars/Core/Models/DifficultySettings.cs`
- Test: `tests/FactionWars.Tests/Unit/Core/Models/DifficultySettingsTests.cs`

**Step 1: Write the failing test**

```csharp
// tests/FactionWars.Tests/Unit/Core/Models/DifficultySettingsTests.cs
using FactionWars.Core.Models;
using Xunit;

namespace FactionWars.Tests.Unit.Core.Models
{
    public class DifficultySettingsTests
    {
        [Fact]
        public void Easy_HasCorrectValues()
        {
            var settings = DifficultySettings.Easy;

            Assert.Equal(Difficulty.Easy, settings.Level);
            Assert.Equal(0.75f, settings.AiIncomeMultiplier);
            Assert.Equal(7, settings.TickIntervalMinutes);
        }

        [Fact]
        public void Normal_HasCorrectValues()
        {
            var settings = DifficultySettings.Normal;

            Assert.Equal(Difficulty.Normal, settings.Level);
            Assert.Equal(1.0f, settings.AiIncomeMultiplier);
            Assert.Equal(5, settings.TickIntervalMinutes);
        }

        [Fact]
        public void Hard_HasCorrectValues()
        {
            var settings = DifficultySettings.Hard;

            Assert.Equal(Difficulty.Hard, settings.Level);
            Assert.Equal(1.25f, settings.AiIncomeMultiplier);
            Assert.Equal(3, settings.TickIntervalMinutes);
        }

        [Theory]
        [InlineData(Difficulty.Easy, 0.75f, 7)]
        [InlineData(Difficulty.Normal, 1.0f, 5)]
        [InlineData(Difficulty.Hard, 1.25f, 3)]
        public void FromLevel_ReturnsCorrectSettings(Difficulty level, float expectedMultiplier, int expectedMinutes)
        {
            var settings = DifficultySettings.FromLevel(level);

            Assert.Equal(level, settings.Level);
            Assert.Equal(expectedMultiplier, settings.AiIncomeMultiplier);
            Assert.Equal(expectedMinutes, settings.TickIntervalMinutes);
        }

        [Fact]
        public void TickIntervalSeconds_ConvertsMinutesToSeconds()
        {
            var settings = DifficultySettings.Normal;

            Assert.Equal(300, settings.TickIntervalSeconds);
        }
    }
}
```

**Step 2: Run test to verify it fails**

Run: `dotnet test tests/FactionWars.Tests --filter "FullyQualifiedName~DifficultySettingsTests" --no-build`
Expected: FAIL with "Difficulty does not exist" or similar

**Step 3: Write minimal implementation**

```csharp
// src/FactionWars/Core/Models/Difficulty.cs
namespace FactionWars.Core.Models
{
    /// <summary>
    /// Game difficulty levels affecting AI income and tick rate.
    /// </summary>
    public enum Difficulty
    {
        Easy,
        Normal,
        Hard
    }
}
```

```csharp
// src/FactionWars/Core/Models/DifficultySettings.cs
namespace FactionWars.Core.Models
{
    /// <summary>
    /// Preset settings for a difficulty level.
    /// </summary>
    public class DifficultySettings
    {
        /// <summary>
        /// The difficulty level.
        /// </summary>
        public Difficulty Level { get; }

        /// <summary>
        /// Multiplier applied to AI faction income (player always gets 1.0x).
        /// </summary>
        public float AiIncomeMultiplier { get; }

        /// <summary>
        /// Minutes between resource ticks.
        /// </summary>
        public int TickIntervalMinutes { get; }

        /// <summary>
        /// Seconds between resource ticks.
        /// </summary>
        public int TickIntervalSeconds => TickIntervalMinutes * 60;

        private DifficultySettings(Difficulty level, float aiIncomeMultiplier, int tickIntervalMinutes)
        {
            Level = level;
            AiIncomeMultiplier = aiIncomeMultiplier;
            TickIntervalMinutes = tickIntervalMinutes;
        }

        /// <summary>
        /// Easy difficulty: 0.75x AI income, 7 minute ticks.
        /// </summary>
        public static DifficultySettings Easy => new(Difficulty.Easy, 0.75f, 7);

        /// <summary>
        /// Normal difficulty: 1.0x AI income, 5 minute ticks.
        /// </summary>
        public static DifficultySettings Normal => new(Difficulty.Normal, 1.0f, 5);

        /// <summary>
        /// Hard difficulty: 1.25x AI income, 3 minute ticks.
        /// </summary>
        public static DifficultySettings Hard => new(Difficulty.Hard, 1.25f, 3);

        /// <summary>
        /// Gets the settings for a difficulty level.
        /// </summary>
        public static DifficultySettings FromLevel(Difficulty level) => level switch
        {
            Difficulty.Easy => Easy,
            Difficulty.Normal => Normal,
            Difficulty.Hard => Hard,
            _ => Normal
        };
    }
}
```

**Step 4: Run test to verify it passes**

Run: `dotnet test tests/FactionWars.Tests --filter "FullyQualifiedName~DifficultySettingsTests"`
Expected: PASS

**Step 5: Commit**

```bash
git add src/FactionWars/Core/Models/Difficulty.cs src/FactionWars/Core/Models/DifficultySettings.cs tests/FactionWars.Tests/Unit/Core/Models/DifficultySettingsTests.cs
git commit -m "feat: add Difficulty enum and DifficultySettings model"
```

---

## Task 2: Create IDifficultyService Interface and Implementation

**Files:**
- Create: `src/FactionWars/Core/Interfaces/IDifficultyService.cs`
- Create: `src/FactionWars/Core/Services/DifficultyService.cs`
- Test: `tests/FactionWars.Tests/Unit/Core/Services/DifficultyServiceTests.cs`

**Step 1: Write the failing test**

```csharp
// tests/FactionWars.Tests/Unit/Core/Services/DifficultyServiceTests.cs
using System;
using FactionWars.Core.Interfaces;
using FactionWars.Core.Models;
using FactionWars.Core.Services;
using Xunit;

namespace FactionWars.Tests.Unit.Core.Services
{
    public class DifficultyServiceTests
    {
        [Fact]
        public void Constructor_DefaultsToNormal()
        {
            var service = new DifficultyService();

            Assert.Equal(Difficulty.Normal, service.Current.Level);
        }

        [Fact]
        public void Constructor_WithDifficulty_SetsInitialValue()
        {
            var service = new DifficultyService(Difficulty.Hard);

            Assert.Equal(Difficulty.Hard, service.Current.Level);
        }

        [Fact]
        public void SetDifficulty_UpdatesCurrent()
        {
            var service = new DifficultyService();

            service.SetDifficulty(Difficulty.Easy);

            Assert.Equal(Difficulty.Easy, service.Current.Level);
            Assert.Equal(0.75f, service.Current.AiIncomeMultiplier);
        }

        [Fact]
        public void SetDifficulty_RaisesEvent()
        {
            var service = new DifficultyService();
            DifficultySettings? eventSettings = null;
            service.DifficultyChanged += (s, e) => eventSettings = e;

            service.SetDifficulty(Difficulty.Hard);

            Assert.NotNull(eventSettings);
            Assert.Equal(Difficulty.Hard, eventSettings!.Level);
        }

        [Fact]
        public void SetDifficulty_SameLevel_DoesNotRaiseEvent()
        {
            var service = new DifficultyService(Difficulty.Normal);
            int eventCount = 0;
            service.DifficultyChanged += (s, e) => eventCount++;

            service.SetDifficulty(Difficulty.Normal);

            Assert.Equal(0, eventCount);
        }
    }
}
```

**Step 2: Run test to verify it fails**

Run: `dotnet test tests/FactionWars.Tests --filter "FullyQualifiedName~DifficultyServiceTests" --no-build`
Expected: FAIL with "DifficultyService does not exist"

**Step 3: Write minimal implementation**

```csharp
// src/FactionWars/Core/Interfaces/IDifficultyService.cs
using System;
using FactionWars.Core.Models;

namespace FactionWars.Core.Interfaces
{
    /// <summary>
    /// Service for managing game difficulty settings.
    /// </summary>
    public interface IDifficultyService
    {
        /// <summary>
        /// Gets the current difficulty settings.
        /// </summary>
        DifficultySettings Current { get; }

        /// <summary>
        /// Sets the difficulty level.
        /// </summary>
        /// <param name="level">The difficulty level to set.</param>
        void SetDifficulty(Difficulty level);

        /// <summary>
        /// Event raised when difficulty changes.
        /// </summary>
        event EventHandler<DifficultySettings>? DifficultyChanged;
    }
}
```

```csharp
// src/FactionWars/Core/Services/DifficultyService.cs
using System;
using FactionWars.Core.Interfaces;
using FactionWars.Core.Models;

namespace FactionWars.Core.Services
{
    /// <summary>
    /// Service for managing game difficulty settings.
    /// </summary>
    public class DifficultyService : IDifficultyService
    {
        private DifficultySettings _current;

        /// <inheritdoc />
        public DifficultySettings Current => _current;

        /// <inheritdoc />
        public event EventHandler<DifficultySettings>? DifficultyChanged;

        /// <summary>
        /// Creates a new DifficultyService with the specified initial difficulty.
        /// </summary>
        /// <param name="initialDifficulty">The initial difficulty level (defaults to Normal).</param>
        public DifficultyService(Difficulty initialDifficulty = Difficulty.Normal)
        {
            _current = DifficultySettings.FromLevel(initialDifficulty);
        }

        /// <inheritdoc />
        public void SetDifficulty(Difficulty level)
        {
            if (_current.Level == level)
                return;

            _current = DifficultySettings.FromLevel(level);
            DifficultyChanged?.Invoke(this, _current);
        }
    }
}
```

**Step 4: Run test to verify it passes**

Run: `dotnet test tests/FactionWars.Tests --filter "FullyQualifiedName~DifficultyServiceTests"`
Expected: PASS

**Step 5: Commit**

```bash
git add src/FactionWars/Core/Interfaces/IDifficultyService.cs src/FactionWars/Core/Services/DifficultyService.cs tests/FactionWars.Tests/Unit/Core/Services/DifficultyServiceTests.cs
git commit -m "feat: add IDifficultyService interface and implementation"
```

---

## Task 3: Integrate AI Income Multiplier into ResourceTickService

**Files:**
- Modify: `src/FactionWars/Economy/Services/ResourceTickService.cs`
- Modify: `src/FactionWars/Economy/Interfaces/IResourceTickService.cs`
- Test: `tests/FactionWars.Tests/Unit/Economy/ResourceTickServiceTests.cs`

**Step 1: Write the failing test**

Add to existing `ResourceTickServiceTests.cs`:

```csharp
[Fact]
public void ExecuteTick_WithAiMultiplier_AppliesMultiplierToAiFactions()
{
    // Arrange - AI faction should get 0.75x income
    var aiFaction = new Faction("ai_faction", "AI", FactionType.Michael, FactionColor.Blue);
    _mockFactionService.Setup(f => f.GetActiveFactions()).Returns(new[] { aiFaction });
    _mockZoneService.Setup(z => z.GetZonesByOwner("ai_faction")).Returns(new[] { _testZone });

    int capturedCash = 0;
    _mockFactionService.Setup(f => f.AddCash("ai_faction", It.IsAny<int>()))
        .Callback<string, int>((id, amount) => capturedCash = amount)
        .Returns(true);

    // Act
    _service.SetAiIncomeMultiplier(0.75f);
    _service.SetPlayerFactionId("player_faction"); // Different from ai_faction
    _service.ForceTick();

    // Assert - cash should be multiplied by 0.75
    Assert.True(capturedCash > 0);
    // Base cash would be 100 * strategic value, multiplied by 0.75
}

[Fact]
public void ExecuteTick_PlayerFaction_DoesNotApplyMultiplier()
{
    // Arrange - Player faction should get 1.0x income regardless of multiplier
    var playerFaction = new Faction("player_faction", "Player", FactionType.Franklin, FactionColor.Green);
    _mockFactionService.Setup(f => f.GetActiveFactions()).Returns(new[] { playerFaction });
    _mockZoneService.Setup(z => z.GetZonesByOwner("player_faction")).Returns(new[] { _testZone });

    int capturedCash = 0;
    _mockFactionService.Setup(f => f.AddCash("player_faction", It.IsAny<int>()))
        .Callback<string, int>((id, amount) => capturedCash = amount)
        .Returns(true);

    // Act
    _service.SetAiIncomeMultiplier(0.5f); // Even with 0.5x, player should get full
    _service.SetPlayerFactionId("player_faction");
    _service.ForceTick();

    // Assert - player faction should NOT have multiplier applied
    // (compare against baseline without multiplier)
}
```

**Step 2: Run test to verify it fails**

Run: `dotnet test tests/FactionWars.Tests --filter "FullyQualifiedName~ResourceTickServiceTests.ExecuteTick_WithAiMultiplier"`
Expected: FAIL with "SetAiIncomeMultiplier does not exist"

**Step 3: Write minimal implementation**

Add to `IResourceTickService.cs`:
```csharp
/// <summary>
/// Sets the AI income multiplier for resource generation.
/// </summary>
/// <param name="multiplier">The multiplier (1.0 = normal, 0.75 = easy, 1.25 = hard).</param>
void SetAiIncomeMultiplier(float multiplier);

/// <summary>
/// Sets the player faction ID. Player faction does not get AI multiplier applied.
/// </summary>
/// <param name="factionId">The player's faction ID, or null to clear.</param>
void SetPlayerFactionId(string? factionId);
```

Add to `ResourceTickService.cs`:
```csharp
private float _aiIncomeMultiplier = 1.0f;
private string? _playerFactionId;

/// <inheritdoc />
public void SetAiIncomeMultiplier(float multiplier)
{
    _aiIncomeMultiplier = multiplier;
}

/// <inheritdoc />
public void SetPlayerFactionId(string? factionId)
{
    _playerFactionId = factionId;
}
```

Modify `ExecuteTick()` method to apply multiplier:
```csharp
private void ExecuteTick()
{
    var activeFactions = _factionService.GetActiveFactions();

    foreach (var faction in activeFactions)
    {
        var resources = CalculateFactionResources(faction.Id);

        // Apply AI income multiplier (player faction excluded)
        bool isPlayerFaction = _playerFactionId != null && faction.Id == _playerFactionId;
        float incomeMultiplier = isPlayerFaction ? 1.0f : _aiIncomeMultiplier;

        int finalCash = (int)(resources.cash * incomeMultiplier);
        int finalRecruitment = (int)(resources.recruitment * incomeMultiplier);
        int finalWeapons = (int)(resources.weapons * incomeMultiplier);

        // Add resources to faction
        if (finalCash > 0)
            _factionService.AddCash(faction.Id, finalCash);

        if (finalRecruitment > 0)
            _factionService.AddRecruitmentPoints(faction.Id, finalRecruitment);

        if (finalWeapons > 0)
            _factionService.AddWeapons(faction.Id, finalWeapons);

        // Raise event with actual values added
        var args = new ResourceTickEventArgs(
            faction.Id,
            finalCash,
            finalRecruitment,
            finalWeapons);

        OnResourceTick?.Invoke(this, args);
    }
}
```

**Step 4: Run test to verify it passes**

Run: `dotnet test tests/FactionWars.Tests --filter "FullyQualifiedName~ResourceTickServiceTests"`
Expected: PASS

**Step 5: Commit**

```bash
git add src/FactionWars/Economy/Interfaces/IResourceTickService.cs src/FactionWars/Economy/Services/ResourceTickService.cs tests/FactionWars.Tests/Unit/Economy/ResourceTickServiceTests.cs
git commit -m "feat: add AI income multiplier to ResourceTickService"
```

---

## Task 4: Add Difficulty to GameState for Persistence

**Files:**
- Modify: `src/FactionWars/Persistence/Models/GameState.cs`
- Test: `tests/FactionWars.Tests/Unit/Persistence/GameStateTests.cs`

**Step 1: Write the failing test**

Add to existing `GameStateTests.cs`:

```csharp
[Fact]
public void Difficulty_DefaultsToNormal()
{
    var state = new GameState();

    Assert.Equal(Difficulty.Normal, state.Difficulty);
}

[Fact]
public void Difficulty_CanBeSetAndRetrieved()
{
    var state = new GameState { Difficulty = Difficulty.Hard };

    Assert.Equal(Difficulty.Hard, state.Difficulty);
}
```

**Step 2: Run test to verify it fails**

Run: `dotnet test tests/FactionWars.Tests --filter "FullyQualifiedName~GameStateTests.Difficulty"`
Expected: FAIL with "Difficulty property does not exist"

**Step 3: Write minimal implementation**

Add to `GameState.cs`:
```csharp
using FactionWars.Core.Models;

// Add property:
/// <summary>
/// Current difficulty level.
/// </summary>
public Difficulty Difficulty { get; set; } = Difficulty.Normal;
```

**Step 4: Run test to verify it passes**

Run: `dotnet test tests/FactionWars.Tests --filter "FullyQualifiedName~GameStateTests"`
Expected: PASS

**Step 5: Commit**

```bash
git add src/FactionWars/Persistence/Models/GameState.cs tests/FactionWars.Tests/Unit/Persistence/GameStateTests.cs
git commit -m "feat: add Difficulty property to GameState"
```

---

## Task 5: Add Difficulty Menu to SettingsMenuController

**Files:**
- Modify: `src/FactionWars/ScriptHookV/UI/SettingsMenuController.cs`
- Test: `tests/FactionWars.Tests/Unit/ScriptHookV/UI/SettingsMenuControllerTests.cs`

**Step 1: Write the failing test**

Add to existing `SettingsMenuControllerTests.cs`:

```csharp
[Fact]
public void Show_IncludesDifficultyMenuItem()
{
    _controller.Show();

    _mockMenuProvider.Verify(m => m.ShowMenu(
        It.Is<MenuDefinition>(menu =>
            menu.Items.Any(i => i.Id == SettingsMenuController.DifficultyItemId))),
        Times.Once);
}

[Fact]
public void ShowDifficultyMenu_ShowsThreeOptions()
{
    _controller.ShowDifficultyMenu();

    _mockMenuProvider.Verify(m => m.ShowMenu(
        It.Is<MenuDefinition>(menu =>
            menu.Id == SettingsMenuController.DifficultyMenuId &&
            menu.Items.Count(i => i.Id.StartsWith("difficulty_")) == 3)),
        Times.Once);
}

[Fact]
public void SelectDifficulty_ShowsConfirmation_WhenDifferent()
{
    // Setup current difficulty as Normal
    _mockDifficultyService.Setup(d => d.Current).Returns(DifficultySettings.Normal);

    // Select Easy (different from current)
    _controller.HandleDifficultySelection(Difficulty.Easy);

    _mockMenuProvider.Verify(m => m.ShowMenu(
        It.Is<MenuDefinition>(menu =>
            menu.Id == SettingsMenuController.DifficultyConfirmMenuId)),
        Times.Once);
}

[Fact]
public void ConfirmDifficulty_SetsDifficultyAndClosesMenu()
{
    _controller.ConfirmDifficultyChange(Difficulty.Hard);

    _mockDifficultyService.Verify(d => d.SetDifficulty(Difficulty.Hard), Times.Once);
    _mockMenuProvider.Verify(m => m.CloseMenu(), Times.Once);
}
```

**Step 2: Run test to verify it fails**

Run: `dotnet test tests/FactionWars.Tests --filter "FullyQualifiedName~SettingsMenuControllerTests.Show_IncludesDifficultyMenuItem"`
Expected: FAIL

**Step 3: Write minimal implementation**

Add to `SettingsMenuController.cs`:

```csharp
// Add constants
public const string DifficultyItemId = "difficulty";
public const string DifficultyMenuId = "difficulty_menu";
public const string DifficultyConfirmMenuId = "difficulty_confirm_menu";

// Add field
private readonly IDifficultyService _difficultyService;
private Difficulty _pendingDifficulty;

// Update constructor to accept IDifficultyService

// Add to Show() method - new menu item:
var currentDifficulty = _difficultyService.Current.Level;
menu.AddItem(new MenuItem(
    DifficultyItemId,
    $"Difficulty: {currentDifficulty}",
    "Change game difficulty"));

// Add ShowDifficultyMenu() method
public void ShowDifficultyMenu()
{
    var menu = new MenuDefinition(DifficultyMenuId, "Difficulty", "Select Difficulty");
    var current = _difficultyService.Current.Level;

    menu.AddItem(new MenuItem(
        "difficulty_easy",
        current == Difficulty.Easy ? "Easy [Current]" : "Easy",
        "AI earns 0.75x income, 7 min ticks"));

    menu.AddItem(new MenuItem(
        "difficulty_normal",
        current == Difficulty.Normal ? "Normal [Current]" : "Normal",
        "Balanced experience, 5 min ticks"));

    menu.AddItem(new MenuItem(
        "difficulty_hard",
        current == Difficulty.Hard ? "Hard [Current]" : "Hard",
        "AI earns 1.25x income, 3 min ticks"));

    menu.AddItem(new MenuItem(BackItemId, "Back", "Return to settings"));

    _menuProvider.ShowMenu(menu);
}

// Add confirmation methods
public void HandleDifficultySelection(Difficulty level)
{
    if (_difficultyService.Current.Level == level)
    {
        // Same as current, just go back
        Show();
        return;
    }

    _pendingDifficulty = level;
    ShowDifficultyConfirmation(level);
}

private void ShowDifficultyConfirmation(Difficulty level)
{
    var menu = new MenuDefinition(DifficultyConfirmMenuId, "Confirm Change",
        $"Change difficulty to {level}?");

    menu.AddItem(new MenuItem("confirm_yes", "Confirm", "Apply new difficulty"));
    menu.AddItem(new MenuItem("confirm_no", "Cancel", "Keep current difficulty"));

    _menuProvider.ShowMenu(menu);
}

public void ConfirmDifficultyChange(Difficulty level)
{
    _difficultyService.SetDifficulty(level);
    _menuProvider.CloseMenu();
}

// Update OnItemSelected to handle new menus
```

**Step 4: Run test to verify it passes**

Run: `dotnet test tests/FactionWars.Tests --filter "FullyQualifiedName~SettingsMenuControllerTests"`
Expected: PASS

**Step 5: Commit**

```bash
git add src/FactionWars/ScriptHookV/UI/SettingsMenuController.cs tests/FactionWars.Tests/Unit/ScriptHookV/UI/SettingsMenuControllerTests.cs
git commit -m "feat: add difficulty menu to settings"
```

---

## Task 6: Wire Up DifficultyService in GameLoopController

**Files:**
- Modify: `src/FactionWars/ScriptHookV/GameLoopController.cs`
- Modify: `src/FactionWars/ScriptHookV/ServiceContainerFactory.cs`

**Step 1: Review existing wiring patterns**

Check `GameLoopController.cs` and `ServiceContainerFactory.cs` to understand the DI pattern.

**Step 2: Add DifficultyService registration**

In `ServiceContainerFactory.cs`:
```csharp
container.RegisterSingleton<IDifficultyService, DifficultyService>();
```

**Step 3: Wire up in GameLoopController**

In initialization:
```csharp
var difficultyService = _container.Resolve<IDifficultyService>();

// Connect to ResourceTickService
_resourceTickService.SetAiIncomeMultiplier(difficultyService.Current.AiIncomeMultiplier);
_resourceTickService.SetTickInterval(difficultyService.Current.TickIntervalSeconds);
_resourceTickService.SetPlayerFactionId(_playerFactionId);

// Subscribe to difficulty changes
difficultyService.DifficultyChanged += OnDifficultyChanged;

// Pass to SettingsMenuController
_settingsMenuController = new SettingsMenuController(menuProvider, saveSlotManager, gameStateCoordinator, difficultyService);
```

Add handler:
```csharp
private void OnDifficultyChanged(object? sender, DifficultySettings settings)
{
    _resourceTickService.SetAiIncomeMultiplier(settings.AiIncomeMultiplier);
    _resourceTickService.SetTickInterval(settings.TickIntervalSeconds);
    FileLogger.Info($"Difficulty changed to {settings.Level}: AI={settings.AiIncomeMultiplier}x, Tick={settings.TickIntervalMinutes}min");
}
```

**Step 4: Update save/load to include difficulty**

In save logic:
```csharp
gameState.Difficulty = _difficultyService.Current.Level;
```

In load logic:
```csharp
_difficultyService.SetDifficulty(gameState.Difficulty);
```

**Step 5: Commit**

```bash
git add src/FactionWars/ScriptHookV/GameLoopController.cs src/FactionWars/ScriptHookV/ServiceContainerFactory.cs
git commit -m "feat: wire up DifficultyService in game loop"
```

---

## Task 7: Update Config to Support Default Difficulty

**Files:**
- Modify: `bin/FactionWars/config.json`
- Any config loading code if needed

**Step 1: Update config.json**

```json
"gameplay": {
    "defaultDifficulty": "Normal",
    "resourceTickIntervalMinutes": 5,
    ...
}
```

**Step 2: Commit**

```bash
git add bin/FactionWars/config.json
git commit -m "feat: add defaultDifficulty to config"
```

---

## Task 8: Final Integration Test

**Files:**
- Create: `tests/FactionWars.Tests/Integration/DifficultyIntegrationTests.cs`

**Step 1: Write integration test**

```csharp
[Fact]
public void DifficultyChange_UpdatesTickServiceAndPersists()
{
    // Arrange
    var difficultyService = new DifficultyService(Difficulty.Normal);
    var resourceTickService = CreateResourceTickService();

    difficultyService.DifficultyChanged += (s, settings) =>
    {
        resourceTickService.SetAiIncomeMultiplier(settings.AiIncomeMultiplier);
        resourceTickService.SetTickInterval(settings.TickIntervalSeconds);
    };

    // Act
    difficultyService.SetDifficulty(Difficulty.Easy);

    // Assert
    Assert.Equal(420, resourceTickService.TickIntervalSeconds); // 7 * 60
}
```

**Step 2: Run all tests**

Run: `dotnet test tests/FactionWars.Tests`
Expected: All PASS

**Step 3: Final commit**

```bash
git add tests/FactionWars.Tests/Integration/DifficultyIntegrationTests.cs
git commit -m "test: add difficulty integration tests"
```

---

## Summary

| Task | Description | Files |
|------|-------------|-------|
| 1 | Difficulty enum and settings model | Core/Models |
| 2 | IDifficultyService interface and implementation | Core/Interfaces, Core/Services |
| 3 | AI income multiplier in ResourceTickService | Economy/Services |
| 4 | Difficulty in GameState for persistence | Persistence/Models |
| 5 | Difficulty menu in SettingsMenuController | ScriptHookV/UI |
| 6 | Wire up DifficultyService in GameLoopController | ScriptHookV |
| 7 | Config default difficulty | bin/FactionWars |
| 8 | Integration tests | Tests/Integration |
