# Recruitment Menu Restructure Implementation Plan

> **For Claude:** REQUIRED SUB-SKILL: Use superpowers:executing-plans to implement this plan task-by-task.

**Goal:** Split the Recruitment menu into "Defenders" and "Squad" submenus, and add a new Elite/RPG defender tier to both.

**Architecture:** Replace ArmyMenuController with a RecruitmentMenuController that has nested DefendersMenuController and SquadMenuController. Each submenu handles its own purchases. The Elite tier uses RPG weapon for anti-vehicle capability.

**Tech Stack:** C# .NET 4.8, NativeUI menu system, TDD with xUnit

---

## Task 1: Add Elite Tier to DefenderTier Enum

**Files:**
- Modify: `src/FactionWars/Core/Models/DefenderTier.cs`
- Modify: `tests/FactionWars.Tests/Unit/Core/DefenderTierConfigProviderTests.cs`

**Step 1: Update DefenderTier enum**

Add Elite tier with value 3:

```csharp
/// <summary>
/// Elite tier defenders - anti-vehicle specialists with RPG.
/// Cost: $2000, Health: 250, Armor: Full, Weapon: RPG, Accuracy: 0.8
/// </summary>
Elite = 3
```

**Step 2: Run existing tests**

Run: `dotnet test tests/FactionWars.Tests --filter "DefenderTier"`
Expected: Tests should still pass (enum extension doesn't break existing)

**Step 3: Commit**

```bash
git add src/FactionWars/Core/Models/DefenderTier.cs
git commit -m "feat: add Elite defender tier enum value"
```

---

## Task 2: Add Elite Tier Configuration

**Files:**
- Modify: `src/FactionWars/Core/Services/DefenderTierConfigProvider.cs`
- Modify: `tests/FactionWars.Tests/Unit/Core/DefenderTierConfigProviderTests.cs`

**Step 1: Write failing test for Elite config**

```csharp
[Fact]
public void GetConfig_ForEliteTier_ShouldReturnCorrectConfiguration()
{
    // Arrange
    var provider = new DefenderTierConfigProvider();

    // Act
    var config = provider.GetConfig(DefenderTier.Elite);

    // Assert
    Assert.Equal(DefenderTier.Elite, config.Tier);
    Assert.Equal(2000, config.Cost);
    Assert.Equal(250, config.Health);
    Assert.Equal(100, config.Armor);
    Assert.Equal("weapon_rpg", config.Weapon);
    Assert.Equal(0.8f, config.Accuracy);
    Assert.Equal(2.5f, config.CombatModifier);
}
```

**Step 2: Run test to verify it fails**

Run: `dotnet test tests/FactionWars.Tests --filter "GetConfig_ForEliteTier"`
Expected: FAIL (KeyNotFoundException or similar)

**Step 3: Add Elite configuration**

In DefenderTierConfigProvider constructor, add:

```csharp
{ DefenderTier.Elite, new DefenderTierConfig(
    DefenderTier.Elite,
    cost: 2000,
    health: 250,
    armor: 100,
    weapon: "weapon_rpg",
    accuracy: 0.8f,
    combatModifier: 2.5f) }
```

**Step 4: Run test to verify it passes**

Run: `dotnet test tests/FactionWars.Tests --filter "DefenderTierConfigProvider"`
Expected: All PASS

**Step 5: Commit**

```bash
git add src/FactionWars/Core/Services/DefenderTierConfigProvider.cs tests/FactionWars.Tests/Unit/Core/DefenderTierConfigProviderTests.cs
git commit -m "feat: add Elite tier configuration with RPG weapon"
```

---

## Task 3: Create DefendersMenuController

**Files:**
- Create: `src/FactionWars/ScriptHookV/UI/DefendersMenuController.cs`
- Create: `tests/FactionWars.Tests/Unit/ScriptHookV/UI/DefendersMenuControllerTests.cs`

**Step 1: Write failing test for menu structure**

```csharp
[Fact]
public void Show_ShouldDisplayDefendersMenu_WithAllTiers()
{
    // Arrange
    var menuProvider = new MockMenuProvider();
    var gameBridge = new MockGameBridge();
    var reservePool = new TroopReservePool();
    var configProvider = new DefenderTierConfigProvider();
    var controller = new DefendersMenuController(menuProvider, gameBridge, reservePool, configProvider);

    // Act
    controller.Show();

    // Assert
    Assert.True(menuProvider.IsMenuVisible);
    Assert.Equal(DefendersMenuController.MenuId, menuProvider.CurrentMenuId);

    var menu = menuProvider.GetCurrentMenuDefinition();
    Assert.NotNull(menu);
    Assert.Equal("Defenders", menu!.Title);

    // Should have: reserve display, 4 buy options (Basic/Medium/Heavy/Elite), back
    Assert.Equal(6, menu.Items.Count);
}
```

**Step 2: Run test to verify it fails**

Run: `dotnet test tests/FactionWars.Tests --filter "DefendersMenuController"`
Expected: FAIL (class doesn't exist)

**Step 3: Implement DefendersMenuController**

Create the controller with:
- MenuId = "defenders_menu"
- Shows reserve troop count
- Buy options for all 4 tiers with costs
- Back button
- Purchases add to TroopReservePool
- BackRequested event

**Step 4: Run tests to verify pass**

Run: `dotnet test tests/FactionWars.Tests --filter "DefendersMenuController"`
Expected: PASS

**Step 5: Add purchase tests**

Write tests for:
- Purchasing each tier adds to reserve pool
- Purchasing deducts money
- Can't purchase without enough money
- Back button raises BackRequested event

**Step 6: Implement purchase logic**

**Step 7: Run all tests**

Run: `dotnet test tests/FactionWars.Tests --filter "DefendersMenuController"`
Expected: All PASS

**Step 8: Commit**

```bash
git add src/FactionWars/ScriptHookV/UI/DefendersMenuController.cs tests/FactionWars.Tests/Unit/ScriptHookV/UI/DefendersMenuControllerTests.cs
git commit -m "feat: add DefendersMenuController for zone troop purchases"
```

---

## Task 4: Create SquadMenuController

**Files:**
- Create: `src/FactionWars/ScriptHookV/UI/SquadMenuController.cs`
- Create: `tests/FactionWars.Tests/Unit/ScriptHookV/UI/SquadMenuControllerTests.cs`

**Step 1: Write failing test for menu structure**

```csharp
[Fact]
public void Show_ShouldDisplaySquadMenu_WithAllTiers()
{
    // Arrange
    var menuProvider = new MockMenuProvider();
    var gameBridge = new MockGameBridge();
    var configProvider = new DefenderTierConfigProvider();
    var followerManager = new FollowerManager(gameBridge, configProvider);
    var controller = new SquadMenuController(menuProvider, gameBridge, configProvider, followerManager);

    // Act
    controller.Show();

    // Assert
    Assert.True(menuProvider.IsMenuVisible);
    Assert.Equal(SquadMenuController.MenuId, menuProvider.CurrentMenuId);

    var menu = menuProvider.GetCurrentMenuDefinition();
    Assert.NotNull(menu);
    Assert.Equal("Squad", menu!.Title);

    // Should have: squad count, 4 recruit options, manage followers, back
    Assert.Equal(7, menu.Items.Count);
}
```

**Step 2: Run test to verify it fails**

Run: `dotnet test tests/FactionWars.Tests --filter "SquadMenuController"`
Expected: FAIL (class doesn't exist)

**Step 3: Implement SquadMenuController**

Create the controller with:
- MenuId = "squad_menu"
- Shows current follower count
- Recruit options for all 4 tiers with costs
- Manage Followers option
- Back button
- Recruitment spawns bodyguard and makes them follow player
- ManageFollowersRequested event
- BackRequested event

**Step 4: Add recruitment and management tests**

Write tests for:
- Recruiting each tier spawns ped and makes follower
- Recruiting deducts money
- Can't recruit without enough money
- Manage Followers raises ManageFollowersRequested event
- Back button raises BackRequested event

**Step 5: Implement all logic**

**Step 6: Run all tests**

Run: `dotnet test tests/FactionWars.Tests --filter "SquadMenuController"`
Expected: All PASS

**Step 7: Commit**

```bash
git add src/FactionWars/ScriptHookV/UI/SquadMenuController.cs tests/FactionWars.Tests/Unit/ScriptHookV/UI/SquadMenuControllerTests.cs
git commit -m "feat: add SquadMenuController for bodyguard recruitment"
```

---

## Task 5: Create RecruitmentMenuController (Parent Menu)

**Files:**
- Create: `src/FactionWars/ScriptHookV/UI/RecruitmentMenuController.cs`
- Create: `tests/FactionWars.Tests/Unit/ScriptHookV/UI/RecruitmentMenuControllerTests.cs`

**Step 1: Write failing test for menu structure**

```csharp
[Fact]
public void Show_ShouldDisplayRecruitmentMenu_WithSubmenus()
{
    // Arrange
    var menuProvider = new MockMenuProvider();
    var gameBridge = new MockGameBridge();
    var controller = new RecruitmentMenuController(menuProvider, gameBridge);

    // Act
    controller.Show();

    // Assert
    Assert.True(menuProvider.IsMenuVisible);
    Assert.Equal(RecruitmentMenuController.MenuId, menuProvider.CurrentMenuId);

    var menu = menuProvider.GetCurrentMenuDefinition();
    Assert.NotNull(menu);
    Assert.Equal("Recruitment", menu!.Title);

    // Should have: cash display, Defenders option, Squad option, back
    Assert.Equal(4, menu.Items.Count);
}
```

**Step 2: Run test to verify it fails**

Run: `dotnet test tests/FactionWars.Tests --filter "RecruitmentMenuController"`
Expected: FAIL (class doesn't exist)

**Step 3: Implement RecruitmentMenuController**

Create the controller with:
- MenuId = "recruitment_menu"
- Shows cash display (disabled)
- Defenders option → raises DefendersRequested event
- Squad option → raises SquadRequested event
- Back button → raises BackRequested event

**Step 4: Add navigation tests**

Write tests for:
- Selecting Defenders raises DefendersRequested
- Selecting Squad raises SquadRequested
- Selecting Back raises BackRequested

**Step 5: Implement event handling**

**Step 6: Run all tests**

Run: `dotnet test tests/FactionWars.Tests --filter "RecruitmentMenuController"`
Expected: All PASS

**Step 7: Commit**

```bash
git add src/FactionWars/ScriptHookV/UI/RecruitmentMenuController.cs tests/FactionWars.Tests/Unit/ScriptHookV/UI/RecruitmentMenuControllerTests.cs
git commit -m "feat: add RecruitmentMenuController as parent menu for Defenders and Squad"
```

---

## Task 6: Wire Up Menu Navigation in GameLoopController

**Files:**
- Modify: `src/FactionWars/ScriptHookV/GameLoopController.cs`
- Modify: `tests/FactionWars.Tests/Unit/ScriptHookV/GameLoopControllerTests.cs` (if exists)

**Step 1: Replace ArmyMenuController with new controllers**

In GameLoopController:
- Remove ArmyMenuController field
- Add RecruitmentMenuController, DefendersMenuController, SquadMenuController fields
- Initialize all three in constructor
- Wire up navigation events:
  - MainMenu "Recruitment" → RecruitmentMenuController.Show()
  - Recruitment "Defenders" → DefendersMenuController.Show()
  - Recruitment "Squad" → SquadMenuController.Show()
  - DefendersMenu "Back" → RecruitmentMenuController.Show()
  - SquadMenu "Back" → RecruitmentMenuController.Show()
  - SquadMenu "ManageFollowers" → show follower list (existing logic)
  - RecruitmentMenu "Back" → MainMenuController.Show()

**Step 2: Update MainMenuController if needed**

Rename item from "Army" to "Recruitment" if not already.

**Step 3: Run all tests**

Run: `dotnet test tests/FactionWars.Tests`
Expected: All PASS

**Step 4: Commit**

```bash
git add src/FactionWars/ScriptHookV/GameLoopController.cs
git commit -m "feat: wire up new recruitment menu hierarchy"
```

---

## Task 7: Remove Old ArmyMenuController

**Files:**
- Delete: `src/FactionWars/ScriptHookV/UI/ArmyMenuController.cs`
- Delete: `tests/FactionWars.Tests/Unit/ScriptHookV/UI/ArmyMenuControllerTests.cs`

**Step 1: Verify new menus work**

Run: `dotnet test tests/FactionWars.Tests`
Expected: All PASS

**Step 2: Delete old files**

Remove ArmyMenuController.cs and its tests.

**Step 3: Run tests to ensure no references remain**

Run: `dotnet build`
Expected: Build succeeds with no ArmyMenuController references

**Step 4: Run all tests**

Run: `dotnet test tests/FactionWars.Tests`
Expected: All PASS

**Step 5: Commit**

```bash
git add -A
git commit -m "refactor: remove old ArmyMenuController replaced by new menu hierarchy"
```

---

## Task 8: Deploy and Test In-Game

**Step 1: Build**

Run: `dotnet build src/FactionWars`
Expected: Build succeeds

**Step 2: Deploy**

```bash
cp "C:/Users/ryan7/programming/gtav-factions/src/FactionWars/bin/Debug/net48/FactionWars.dll" "E:/SteamLibrary/steamapps/common/Grand Theft Auto V/scripts/"
```

**Step 3: Test in-game**

Verify:
- F7 opens main menu
- Recruitment shows submenu with Defenders and Squad
- Defenders shows all 4 tiers with correct prices
- Squad shows all 4 tiers with correct prices
- Purchasing works correctly
- Elite troops spawn with RPG weapon
- Back navigation works throughout

---

## Elite Tier Configuration Summary

| Property | Value |
|----------|-------|
| Cost | $2,000 |
| Health | 250 |
| Armor | 100 (full) |
| Weapon | weapon_rpg |
| Accuracy | 0.8 |
| Combat Modifier | 2.5 |
