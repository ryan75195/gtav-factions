# Territory HUD Redesign Implementation Plan

> **For Claude:** REQUIRED SUB-SKILL: Use superpowers:executing-plans to implement this plan task-by-task.

**Goal:** Replace centered territory indicator with compact top-right HUD box showing deployed/reserve troops for friendly zones and capture progress for enemy zones, plus fix menu scroll retention.

**Architecture:** Extend `TerritoryIndicatorData` with troop counts, update `TerritoryIndicatorRenderer` to draw GTA V native style boxes at top-right, add event subscriptions for real-time updates with throttling.

**Tech Stack:** C#, ScriptHookVDotNet (DRAW_RECT, TextElement), existing service layer

---

### Task 1: Fix Menu Scroll Position Retention

**Files:**
- Modify: `src/FactionWars/ScriptHookV/UI/ZoneManagementMenuController.cs`

**Step 1: Update allocate/withdraw handlers to pass selected item ID**

In `HandleZoneDetailSelection`, change each `ShowZoneDetailMenu(_selectedZoneId)` call to pass the current item ID for cursor retention:

```csharp
case AllocateBasicItemId:
    _allocationService.AllocateTroops(factionState, _selectedZoneId, DefenderTier.Basic, 1);
    ShowZoneDetailMenu(_selectedZoneId, AllocateBasicItemId);
    break;

case AllocateMediumItemId:
    _allocationService.AllocateTroops(factionState, _selectedZoneId, DefenderTier.Medium, 1);
    ShowZoneDetailMenu(_selectedZoneId, AllocateMediumItemId);
    break;

case AllocateHeavyItemId:
    _allocationService.AllocateTroops(factionState, _selectedZoneId, DefenderTier.Heavy, 1);
    ShowZoneDetailMenu(_selectedZoneId, AllocateHeavyItemId);
    break;

case WithdrawBasicItemId:
    _allocationService.WithdrawTroops(factionState, _selectedZoneId, DefenderTier.Basic, 1);
    ShowZoneDetailMenu(_selectedZoneId, WithdrawBasicItemId);
    break;

case WithdrawMediumItemId:
    _allocationService.WithdrawTroops(factionState, _selectedZoneId, DefenderTier.Medium, 1);
    ShowZoneDetailMenu(_selectedZoneId, WithdrawMediumItemId);
    break;

case WithdrawHeavyItemId:
    _allocationService.WithdrawTroops(factionState, _selectedZoneId, DefenderTier.Heavy, 1);
    ShowZoneDetailMenu(_selectedZoneId, WithdrawHeavyItemId);
    break;
```

**Step 2: Update ShowZoneDetailMenu signature**

Change the method signature to accept optional selectedItemId:

```csharp
private void ShowZoneDetailMenu(string zoneId, string? selectedItemId = null)
```

And update the ShowMenu call at the end:

```csharp
_menuProvider.ShowMenu(menu, selectedItemId);
```

**Step 3: Run existing tests**

Run: `dotnet test tests/FactionWars.Tests --filter "FullyQualifiedName~ZoneManagementMenuController" --no-restore`
Expected: All tests pass

**Step 4: Commit**

```bash
git add src/FactionWars/ScriptHookV/UI/ZoneManagementMenuController.cs
git commit -m "fix: Retain menu cursor position when allocating/withdrawing troops"
```

---

### Task 2: Extend TerritoryIndicatorData with Troop Counts

**Files:**
- Modify: `src/FactionWars/UI/Models/TerritoryIndicatorData.cs`
- Modify: `tests/FactionWars.Tests/Unit/UI/TerritoryIndicatorTests.cs`

**Step 1: Add new properties to TerritoryIndicatorData**

Add these properties after existing ones:

```csharp
/// <summary>
/// Number of defenders currently spawned in the zone (0-12).
/// Only relevant for player-owned zones.
/// </summary>
public int DeployedDefenderCount { get; }

/// <summary>
/// Number of defenders in reserve waiting to spawn.
/// Only relevant for player-owned zones.
/// </summary>
public int ReserveDefenderCount { get; }

/// <summary>
/// Number of player's troops in combat (for enemy zones during takeover).
/// </summary>
public int PlayerTroopCount { get; }

/// <summary>
/// Number of enemy defenders in combat (for enemy zones during takeover).
/// </summary>
public int EnemyDefenderCount { get; }
```

**Step 2: Update constructor**

```csharp
public TerritoryIndicatorData(
    string zoneName,
    string? ownerFactionName,
    FactionColor? ownerFactionColor,
    float controlPercentage,
    bool isContested,
    bool isPlayerOwned,
    int deployedDefenderCount = 0,
    int reserveDefenderCount = 0,
    int playerTroopCount = 0,
    int enemyDefenderCount = 0)
{
    // ... existing validation ...

    DeployedDefenderCount = Math.Max(0, deployedDefenderCount);
    ReserveDefenderCount = Math.Max(0, reserveDefenderCount);
    PlayerTroopCount = Math.Max(0, playerTroopCount);
    EnemyDefenderCount = Math.Max(0, enemyDefenderCount);
}
```

**Step 3: Run tests to verify no regressions**

Run: `dotnet test tests/FactionWars.Tests --filter "FullyQualifiedName~TerritoryIndicator" --no-restore`
Expected: All tests pass (default parameters maintain compatibility)

**Step 4: Commit**

```bash
git add src/FactionWars/UI/Models/TerritoryIndicatorData.cs
git commit -m "feat: Add troop count properties to TerritoryIndicatorData"
```

---

### Task 3: Rewrite TerritoryIndicatorRenderer with Box Drawing

**Files:**
- Modify: `src/FactionWars/ScriptHookV/UI/TerritoryIndicatorRenderer.cs`

**Step 1: Update constants for top-right positioning**

Replace existing constants:

```csharp
// Position constants - top right corner
private const float BoxX = 0.92f;       // Right side of screen
private const float BoxY = 0.02f;       // Near top
private const float BoxWidth = 0.14f;   // Compact width
private const float BoxPadding = 0.005f;
private const float AccentBarWidth = 0.003f;

// Text scales - compact
private const float TitleScale = 0.35f;
private const float SubtitleScale = 0.28f;
private const float DetailScale = 0.26f;

// Colors
private static readonly Color BackgroundColor = Color.FromArgb(100, 0, 0, 0);
private static readonly Color FriendlyAccent = Color.FromArgb(255, 100, 200, 100);
private static readonly Color EnemyAccent = Color.FromArgb(255, 255, 100, 100);
private static readonly Color NeutralAccent = Color.FromArgb(255, 180, 180, 180);
```

**Step 2: Add DrawBox helper method**

```csharp
/// <summary>
/// Draws a styled box with accent bar.
/// </summary>
private void DrawBox(float x, float y, float width, float height, Color accentColor)
{
    // Background
    DrawRect(x, y, width, height, BackgroundColor);

    // Left accent bar
    float accentX = x - (width / 2) + (AccentBarWidth / 2);
    DrawRect(accentX, y, AccentBarWidth, height, accentColor);
}

/// <summary>
/// Draws a filled rectangle.
/// </summary>
private void DrawRect(float x, float y, float width, float height, Color color)
{
    GTA.Native.Function.Call(
        GTA.Native.Hash.DRAW_RECT,
        x, y, width, height,
        color.R, color.G, color.B, color.A);
}

/// <summary>
/// Draws a progress bar.
/// </summary>
private void DrawProgressBar(float x, float y, float width, float height, float percent, Color fillColor)
{
    // Background
    DrawRect(x, y, width, height, Color.FromArgb(80, 50, 50, 50));

    // Fill
    float fillWidth = (percent / 100f) * width;
    float fillX = x - (width / 2) + (fillWidth / 2);
    if (fillWidth > 0.001f)
    {
        DrawRect(fillX, y, fillWidth, height, fillColor);
    }
}
```

**Step 3: Rewrite Draw method**

```csharp
public void Draw()
{
    if (!_isVisible || _currentData == null)
        return;

    var data = _currentData;

    // Don't show for neutral zones
    if (data.IsNeutral)
        return;

    if (data.IsPlayerOwned)
    {
        DrawFriendlyTerritoryHud(data);
    }
    else
    {
        DrawEnemyTerritoryHud(data);
    }
}

private void DrawFriendlyTerritoryHud(TerritoryIndicatorData data)
{
    float boxHeight = 0.055f;
    float centerY = BoxY + (boxHeight / 2);

    // Draw box
    DrawBox(BoxX, centerY, BoxWidth, boxHeight, FriendlyAccent);

    // Zone name
    float textX = BoxX - (BoxWidth / 2) + BoxPadding + AccentBarWidth + 0.005f;
    DrawTextLeft(data.ZoneName.ToUpper(), textX, BoxY + 0.005f, TitleScale, FriendlyAccent);

    // Status
    DrawTextLeft("Your Territory", textX, BoxY + 0.022f, SubtitleScale, Color.LightGray);

    // Troop counts: "8 deployed · 14 reserve"
    string troopText = $"{data.DeployedDefenderCount} deployed · {data.ReserveDefenderCount} reserve";
    DrawTextLeft(troopText, textX, BoxY + 0.038f, DetailScale, Color.White);
}

private void DrawEnemyTerritoryHud(TerritoryIndicatorData data)
{
    float boxHeight = data.IsContested ? 0.072f : 0.055f;
    float centerY = BoxY + (boxHeight / 2);

    // Draw box
    DrawBox(BoxX, centerY, BoxWidth, boxHeight, EnemyAccent);

    // Zone name
    float textX = BoxX - (BoxWidth / 2) + BoxPadding + AccentBarWidth + 0.005f;
    DrawTextLeft(data.ZoneName.ToUpper(), textX, BoxY + 0.005f, TitleScale, EnemyAccent);

    // Status
    string status = data.IsContested ? "Capturing..." : $"{data.OwnerFactionName}";
    DrawTextLeft(status, textX, BoxY + 0.022f, SubtitleScale, Color.LightGray);

    if (data.IsContested)
    {
        // Progress bar
        float barY = BoxY + 0.042f;
        float barWidth = BoxWidth - 0.02f;
        DrawProgressBar(BoxX, barY, barWidth, 0.008f, data.ControlPercentage, FriendlyAccent);

        // Percentage text
        DrawTextLeft($"{data.ControlPercentage:F0}%", textX, BoxY + 0.052f, DetailScale, Color.White);

        // Troop counts
        string vsText = $"{data.PlayerTroopCount} vs {data.EnemyDefenderCount}";
        float rightX = BoxX + (BoxWidth / 2) - BoxPadding - 0.005f;
        DrawTextRight(vsText, rightX, BoxY + 0.052f, DetailScale, Color.White);
    }
}

private void DrawTextLeft(string text, float x, float y, float scale, Color color)
{
    var textElement = new TextElement(text, new PointF(x * 1920f, y * 1080f), scale, color)
    {
        Alignment = Alignment.Left,
        Shadow = true
    };
    textElement.ScaledDraw();
}

private void DrawTextRight(string text, float x, float y, float scale, Color color)
{
    var textElement = new TextElement(text, new PointF(x * 1920f, y * 1080f), scale, color)
    {
        Alignment = Alignment.Right,
        Shadow = true
    };
    textElement.ScaledDraw();
}
```

**Step 4: Build and verify**

Run: `dotnet build src/FactionWars --no-restore`
Expected: Build succeeds

**Step 5: Commit**

```bash
git add src/FactionWars/ScriptHookV/UI/TerritoryIndicatorRenderer.cs
git commit -m "feat: Redesign territory HUD with compact top-right boxes"
```

---

### Task 4: Update GameLoopController to Provide Troop Data

**Files:**
- Modify: `src/FactionWars/ScriptHookV/GameLoopController.cs`

**Step 1: Find the territory indicator update section (around line 346-380)**

Update the code that creates `TerritoryIndicatorData` to include troop counts:

```csharp
// Get deployed and reserve counts for player-owned zones
int deployedCount = 0;
int reserveCount = 0;
int playerTroopCount = 0;
int enemyDefenderCount = 0;

if (isPlayerOwned && _friendlyDefenderManager != null)
{
    deployedCount = _friendlyDefenderManager.GetSpawnedDefenderCount(currentZone.Id);

    // Get total allocation as reserve
    var allocation = _allocationService?.GetAllocation(playerFactionId, currentZone.Id);
    if (allocation != null)
    {
        int totalAllocated = allocation.GetTroopCount(DefenderTier.Basic)
                          + allocation.GetTroopCount(DefenderTier.Medium)
                          + allocation.GetTroopCount(DefenderTier.Heavy);
        reserveCount = Math.Max(0, totalAllocated - deployedCount);
    }
}
else if (currentZone.IsContested && _combatManager != null)
{
    // Get combat troop counts for enemy zone takeover
    var encounter = _combatManager.GetActiveEncounter(currentZone.Id);
    if (encounter != null)
    {
        playerTroopCount = encounter.AttackerPedCount;
        enemyDefenderCount = encounter.DefenderPedCount;
    }
}

var territoryData = new TerritoryIndicatorData(
    zoneName: currentZone.Name,
    ownerFactionName: ownerFactionName,
    ownerFactionColor: ownerFactionColor,
    controlPercentage: currentZone.ControlPercentage,
    isContested: currentZone.IsContested,
    isPlayerOwned: isPlayerOwned,
    deployedDefenderCount: deployedCount,
    reserveDefenderCount: reserveCount,
    playerTroopCount: playerTroopCount,
    enemyDefenderCount: enemyDefenderCount);
```

**Step 2: Build and verify**

Run: `dotnet build src/FactionWars --no-restore`
Expected: Build succeeds

**Step 3: Commit**

```bash
git add src/FactionWars/ScriptHookV/GameLoopController.cs
git commit -m "feat: Provide troop counts to territory indicator"
```

---

### Task 5: Add Throttled Update for Performance

**Files:**
- Modify: `src/FactionWars/ScriptHookV/UI/TerritoryIndicatorRenderer.cs`

**Step 1: Add throttle fields**

```csharp
private DateTime _lastDataUpdate = DateTime.MinValue;
private static readonly TimeSpan UpdateThrottle = TimeSpan.FromMilliseconds(500);
private TerritoryIndicatorData? _cachedData;
```

**Step 2: Update Render method with throttling**

```csharp
public void Render(TerritoryIndicatorData data)
{
    if (data == null) throw new ArgumentNullException(nameof(data));

    // Always accept new data but throttle how often we update cached display
    var now = DateTime.UtcNow;
    if (_cachedData == null ||
        now - _lastDataUpdate >= UpdateThrottle ||
        DataChangedSignificantly(_cachedData, data))
    {
        _cachedData = data;
        _lastDataUpdate = now;
    }

    _currentData = _cachedData;
    _isVisible = true;
}

private static bool DataChangedSignificantly(TerritoryIndicatorData old, TerritoryIndicatorData current)
{
    // Always update if zone changed
    if (old.ZoneName != current.ZoneName) return true;

    // Always update if contest state changed
    if (old.IsContested != current.IsContested) return true;

    // Update if troop counts changed
    if (old.DeployedDefenderCount != current.DeployedDefenderCount) return true;
    if (old.ReserveDefenderCount != current.ReserveDefenderCount) return true;
    if (old.PlayerTroopCount != current.PlayerTroopCount) return true;
    if (old.EnemyDefenderCount != current.EnemyDefenderCount) return true;

    // Update if control percentage changed by more than 1%
    if (Math.Abs(old.ControlPercentage - current.ControlPercentage) >= 1f) return true;

    return false;
}
```

**Step 3: Build and verify**

Run: `dotnet build src/FactionWars --no-restore`
Expected: Build succeeds

**Step 4: Commit**

```bash
git add src/FactionWars/ScriptHookV/UI/TerritoryIndicatorRenderer.cs
git commit -m "perf: Add throttled updates to territory indicator"
```

---

### Task 6: Deploy and Test

**Step 1: Build release**

Run: `dotnet build src/FactionWars -c Debug --no-restore`

**Step 2: Deploy to GTA V**

```bash
cp "C:/Users/ryan7/programming/gtav-factions/src/FactionWars/bin/Debug/net48/FactionWars.dll" "E:/SteamLibrary/steamapps/common/Grand Theft Auto V/scripts/"
```

**Step 3: Test in game**

Verify:
- [ ] Menu scroll position stays when adding/removing troops
- [ ] Friendly territory shows compact box at top-right with deployed/reserve
- [ ] Enemy territory shows progress bar during capture
- [ ] Neutral territory shows no box
- [ ] HUD updates when defenders die
- [ ] No visible lag or performance issues

**Step 4: Final commit**

```bash
git add -A
git commit -m "feat: Complete territory HUD redesign with troop status"
```

---

## Summary

| Task | Description |
|------|-------------|
| 1 | Fix menu scroll position retention |
| 2 | Extend TerritoryIndicatorData with troop counts |
| 3 | Rewrite renderer with box drawing |
| 4 | Update GameLoopController to provide data |
| 5 | Add throttled updates for performance |
| 6 | Deploy and test |
