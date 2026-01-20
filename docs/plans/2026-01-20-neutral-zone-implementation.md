# Neutral Zone Mechanics Implementation Plan

> **For Claude:** REQUIRED SUB-SKILL: Use superpowers:executing-plans to implement this plan task-by-task.

**Goal:** Implement neutral zone mechanics where zones with 0 troops become neutral and can be claimed by paying for a guard troop.

**Architecture:** Modify FactionInitializer for balanced starts (3 zones each, 5 troops, $5k). Change CombatResultHandler to make zones neutral on victory. Add claim prompt system to GameLoopController for E key interaction.

**Tech Stack:** C#, ScriptHookVDotNet, existing service layer

---

## Task 1: Update FactionInitializer Starting Conditions

**Files:**
- Modify: `src/FactionWars/ScriptHookV/FactionInitializer.cs`

**Step 1: Update constants for normalized start**

Change the starting constants:

```csharp
// Starting resources per faction - NORMALIZED
private const int StartingCash = 5000;
private const int StartingTroopsPerZone = 5;
private const int StartingZonesPerFaction = 3;
```

**Step 2: Update InitializeFactionStates method**

```csharp
private void InitializeFactionStates()
{
    // All factions start equal: $5k cash, 0 reserve troops (all deployed)
    var michaelState = new FactionState(
        CharacterModelFactionDetector.MichaelFactionId,
        StartingCash,
        0  // No reserve troops - all deployed to zones
    );
    _factionRepository.SetState(michaelState);

    var trevorState = new FactionState(
        CharacterModelFactionDetector.TrevorFactionId,
        StartingCash,
        0
    );
    _factionRepository.SetState(trevorState);

    var franklinState = new FactionState(
        CharacterModelFactionDetector.FranklinFactionId,
        StartingCash,
        0
    );
    _factionRepository.SetState(franklinState);
}
```

**Step 3: Update AssignStartingZones with new zone lists**

```csharp
private void AssignStartingZones()
{
    // Michael's zones - wealthy west side near his mansion (3 zones)
    var michaelZones = new[]
    {
        "rockford_hills",
        "vinewood",
        "del_perro"
    };

    // Trevor's zones - Blaine County around his trailer (3 zones)
    var trevorZones = new[]
    {
        "sandy_shores",
        "harmony",
        "grapeseed"
    };

    // Franklin's zones - South LS around Forum Drive (3 zones)
    var franklinZones = new[]
    {
        "davis",
        "strawberry",
        "rancho"
    };

    AssignZonesToFaction(CharacterModelFactionDetector.MichaelFactionId, michaelZones);
    AssignZonesToFaction(CharacterModelFactionDetector.TrevorFactionId, trevorZones);
    AssignZonesToFaction(CharacterModelFactionDetector.FranklinFactionId, franklinZones);
}
```

**Step 4: Commit**

```bash
git add src/FactionWars/ScriptHookV/FactionInitializer.cs
git commit -m "feat: normalize faction starting conditions (3 zones, $5k each)"
```

---

## Task 2: Add Initial Troop Allocation to Starting Zones

**Files:**
- Modify: `src/FactionWars/ScriptHookV/FactionInitializer.cs`
- Modify: Constructor to accept IZoneDefenderAllocationService

**Step 1: Add allocation service dependency**

```csharp
private readonly IZoneDefenderAllocationService _allocationService;

public FactionInitializer(
    IFactionRepository factionRepository,
    IZoneRepository zoneRepository,
    IZoneDefenderAllocationService allocationService)
{
    _factionRepository = factionRepository ?? throw new ArgumentNullException(nameof(factionRepository));
    _zoneRepository = zoneRepository ?? throw new ArgumentNullException(nameof(zoneRepository));
    _allocationService = allocationService ?? throw new ArgumentNullException(nameof(allocationService));
}
```

**Step 2: Update AssignZonesToFaction to allocate troops**

```csharp
private void AssignZonesToFaction(string factionId, IEnumerable<string> zoneIds)
{
    var state = _factionRepository.GetState(factionId);
    if (state == null)
    {
        throw new InvalidOperationException($"Faction state not found for faction: {factionId}");
    }

    foreach (var zoneId in zoneIds)
    {
        var zone = _zoneRepository.GetById(zoneId);
        if (zone != null)
        {
            // Set the zone owner
            zone.OwnerFactionId = factionId;
            _zoneRepository.Update(zone);

            // Add zone to faction state
            state.AddZone(zoneId);

            // Allocate 5 Basic troops to each starting zone
            _allocationService.SetAllocation(factionId, zoneId, DefenderTier.Basic, StartingTroopsPerZone);
        }
    }

    _factionRepository.SetState(state);
}
```

**Step 3: Update ServiceContainerFactory to pass allocation service**

In `ServiceContainerFactory.cs`, find where FactionInitializer is created and add the allocation service:

```csharp
container.RegisterSingleton<FactionInitializer>(() =>
    new FactionInitializer(
        container.Resolve<IFactionRepository>(),
        container.Resolve<IZoneRepository>(),
        container.Resolve<IZoneDefenderAllocationService>()));
```

**Step 4: Commit**

```bash
git add src/FactionWars/ScriptHookV/FactionInitializer.cs src/FactionWars/ScriptHookV/ServiceContainerFactory.cs
git commit -m "feat: allocate 5 Basic troops to each starting zone"
```

---

## Task 3: Modify CombatResultHandler - Victory Makes Zone Neutral

**Files:**
- Modify: `src/FactionWars/Combat/Services/CombatResultHandler.cs`

**Step 1: Update ProcessAttackerVictory to make zone neutral**

```csharp
private CombatProcessingResult ProcessAttackerVictory(CombatEncounter encounter)
{
    // NEW: Victory makes zone neutral, not captured
    // Player must then claim it by paying for a troop
    var transferSuccess = _zoneService.TransferZoneOwnership(
        encounter.ZoneId,
        null);  // null = neutral

    if (!transferSuccess)
    {
        return CombatProcessingResult.Failure(
            CombatResultOutcome.ZoneNotFound,
            encounter.ZoneId);
    }

    // Set control to 0% (neutral)
    _zoneService.UpdateZoneControl(encounter.ZoneId, 0f);

    // Clear contested state
    _zoneService.SetZoneContested(encounter.ZoneId, false);

    return CombatProcessingResult.Success(
        CombatResultOutcome.ZoneNeutralized,  // New outcome type
        encounter.ZoneId,
        null,  // No new owner yet
        encounter.DefendingFactionId);
}
```

**Step 2: Add ZoneNeutralized to CombatResultOutcome enum**

In `src/FactionWars/Combat/Models/CombatResultOutcome.cs`, add:

```csharp
/// <summary>
/// Zone was cleared and is now neutral (needs to be claimed).
/// </summary>
ZoneNeutralized
```

**Step 3: Commit**

```bash
git add src/FactionWars/Combat/Services/CombatResultHandler.cs src/FactionWars/Combat/Models/CombatResultOutcome.cs
git commit -m "feat: combat victory makes zone neutral instead of captured"
```

---

## Task 4: Add Neutral Zone Detection to TerritoryManager

**Files:**
- Modify: `src/FactionWars/ScriptHookV/Managers/TerritoryManager.cs`

**Step 1: Add IsNeutralZone check and event**

```csharp
public event EventHandler<Zone>? NeutralZoneEntered;

private void CheckZoneChange()
{
    // ... existing zone change detection ...

    if (currentZone != null && currentZone.OwnerFactionId == null)
    {
        // Player entered a neutral zone
        NeutralZoneEntered?.Invoke(this, currentZone);
    }
}
```

**Step 2: Commit**

```bash
git add src/FactionWars/ScriptHookV/Managers/TerritoryManager.cs
git commit -m "feat: add neutral zone detection event"
```

---

## Task 5: Add Zone Claim System to GameLoopController

**Files:**
- Modify: `src/FactionWars/ScriptHookV/GameLoopController.cs`

**Step 1: Add claim state tracking fields**

```csharp
private Zone? _currentNeutralZone;
private bool _showingClaimPrompt;
private const int ClaimKeyCode = 0x45; // E key
```

**Step 2: Subscribe to NeutralZoneEntered event in InitializeGameData**

```csharp
_territoryManager.NeutralZoneEntered += OnNeutralZoneEntered;
_territoryManager.ZoneExited += OnZoneExitedForClaim;
```

**Step 3: Add event handlers**

```csharp
private void OnNeutralZoneEntered(object? sender, Zone zone)
{
    _currentNeutralZone = zone;
    _showingClaimPrompt = true;

    var cost = GetBasicTroopCost();
    _gameBridge.ShowNotification($"~y~Unclaimed territory: {zone.Name}~n~Press ~g~E~w~ to claim for ~g~${cost}");
}

private void OnZoneExitedForClaim(object? sender, Zone zone)
{
    if (_currentNeutralZone?.Id == zone.Id)
    {
        _currentNeutralZone = null;
        _showingClaimPrompt = false;
    }
}

private int GetBasicTroopCost()
{
    var tierService = _container.Resolve<IDefenderTierService>();
    return tierService.GetTierConfig(DefenderTier.Basic).Cost;
}
```

**Step 4: Handle E key in OnKeyDown**

```csharp
public void OnKeyDown(int keyCode)
{
    if (!_isInitialized)
        return;

    // Handle claim key when in neutral zone
    if (keyCode == ClaimKeyCode && _showingClaimPrompt && _currentNeutralZone != null)
    {
        TryClaimNeutralZone();
        return;
    }

    _mainMenuController?.OnKeyDown(keyCode);
}

private void TryClaimNeutralZone()
{
    if (_currentNeutralZone == null) return;

    var cost = GetBasicTroopCost();
    var playerMoney = _gameBridge.GetPlayerMoney();
    var playerFaction = CurrentPlayerFactionId;

    if (playerMoney < cost)
    {
        _gameBridge.ShowNotification($"~r~Not enough cash! Need ${cost}");
        return;
    }

    // Deduct cost
    _gameBridge.AddPlayerMoney(-cost);

    // Transfer ownership
    _zoneService.TransferZoneOwnership(_currentNeutralZone.Id, playerFaction);

    // Allocate 1 Basic troop
    var allocationService = _container.Resolve<IZoneDefenderAllocationService>();
    var factionState = _container.Resolve<IFactionRepository>().GetState(playerFaction);
    if (factionState != null)
    {
        allocationService.SetAllocation(playerFaction, _currentNeutralZone.Id, DefenderTier.Basic, 1);
    }

    _gameBridge.ShowNotification($"~g~You now control {_currentNeutralZone.Name}!");

    // Clear prompt state
    _currentNeutralZone = null;
    _showingClaimPrompt = false;
}
```

**Step 5: Commit**

```bash
git add src/FactionWars/ScriptHookV/GameLoopController.cs
git commit -m "feat: add E key to claim neutral zones"
```

---

## Task 6: Show Claim Prompt After Combat Victory

**Files:**
- Modify: `src/FactionWars/ScriptHookV/GameLoopController.cs`

**Step 1: Update combat end handling to show claim prompt**

After combat ends with AttackerVictory, trigger the neutral zone prompt:

```csharp
// In the combat ended handler or after EndCombat is called
private void OnCombatEnded(CombatEncounter encounter, CombatStatus status)
{
    if (status == CombatStatus.AttackerVictory)
    {
        // Zone is now neutral, show claim prompt
        var zone = _zoneService.GetZone(encounter.ZoneId);
        if (zone != null)
        {
            OnNeutralZoneEntered(this, zone);
        }
    }
}
```

**Step 2: Commit**

```bash
git add src/FactionWars/ScriptHookV/GameLoopController.cs
git commit -m "feat: show claim prompt after combat victory"
```

---

## Task 7: Build and Test

**Step 1: Build the solution**

```bash
dotnet build src/FactionWars/FactionWars.csproj -c Release
```

**Step 2: Deploy to GTA V**

```bash
cp src/FactionWars/bin/Release/net48/FactionWars.dll "E:/SteamLibrary/steamapps/common/Grand Theft Auto V/scripts/"
```

**Step 3: Manual test checklist**

- [ ] Start new game - each faction has 3 zones
- [ ] Check map - 22 zones are neutral (gray)
- [ ] Enter neutral zone - see claim prompt
- [ ] Press E - zone claimed, $200 deducted
- [ ] Enter enemy zone - combat starts, defenders spawn
- [ ] Win combat - zone becomes neutral, claim prompt appears
- [ ] Press E - zone claimed

**Step 4: Final commit**

```bash
git add -A
git commit -m "feat: complete neutral zone mechanics implementation"
```

---

## Summary of Changes

| File | Change |
|------|--------|
| `FactionInitializer.cs` | 3 zones each, 5 troops, $5k, normalized |
| `CombatResultHandler.cs` | Victory → neutral (not captured) |
| `CombatResultOutcome.cs` | Add ZoneNeutralized enum |
| `TerritoryManager.cs` | Add NeutralZoneEntered event |
| `GameLoopController.cs` | E key claim, prompt system |
| `ServiceContainerFactory.cs` | Wire allocation service to initializer |
