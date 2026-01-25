# Commander NPC Implementation Plan

> **For Claude:** REQUIRED SUB-SKILL: Use superpowers:executing-plans to implement this plan task-by-task.

**Goal:** Add a Commander NPC to each player-owned zone that opens the mod menu when the player aims at them and presses E.

**Architecture:** New `CommanderManager` class follows existing `FriendlyDefenderManager` patterns. Commander spawns/despawns with zone entry/exit, respawns immediately on death, and despawns when territory is lost. Uses new IGameBridge methods for aiming detection.

**Tech Stack:** C# .NET Framework 4.8, ScriptHookVDotNet, xUnit + Moq for testing

---

## Task 1: Add Targeting Natives to IGameBridge

**Files:**
- Modify: `src/FactionWars/Core/Interfaces/IGameBridge.cs`

**Step 1: Add interface methods**

Add these methods to the end of `IGameBridge.cs` (before the closing brace):

```csharp
/// <summary>
/// Checks if the player is currently free-aiming (aiming a weapon or in aim mode).
/// </summary>
/// <returns>True if the player is free-aiming.</returns>
bool IsPlayerFreeAiming();

/// <summary>
/// Gets the entity handle that the player is currently aiming at.
/// Returns 0 if not aiming at any entity.
/// </summary>
/// <returns>Entity handle, or 0 if not aiming at an entity.</returns>
int GetEntityPlayerIsAimingAt();

/// <summary>
/// Displays help text at the bottom of the screen (like "Press E to...").
/// </summary>
/// <param name="text">The text to display. Supports GTA text formatting.</param>
void DisplayHelpText(string text);
```

**Step 2: Commit**

```bash
git add src/FactionWars/Core/Interfaces/IGameBridge.cs
git commit -m "feat(interface): add targeting and help text methods to IGameBridge"
```

---

## Task 2: Implement Targeting Natives in MockGameBridge

**Files:**
- Modify: `src/FactionWars/Core/Utils/MockGameBridge.cs`
- Test: `tests/FactionWars.Tests/Unit/Core/MockGameBridgeTests.cs`

**Step 1: Write the failing test**

Add to `MockGameBridgeTests.cs`:

```csharp
[Fact]
public void IsPlayerFreeAiming_DefaultsFalse()
{
    var bridge = new MockGameBridge();
    Assert.False(bridge.IsPlayerFreeAiming());
}

[Fact]
public void SetPlayerFreeAiming_ChangesState()
{
    var bridge = new MockGameBridge();
    bridge.SetPlayerFreeAiming(true);
    Assert.True(bridge.IsPlayerFreeAiming());
}

[Fact]
public void GetEntityPlayerIsAimingAt_DefaultsToZero()
{
    var bridge = new MockGameBridge();
    Assert.Equal(0, bridge.GetEntityPlayerIsAimingAt());
}

[Fact]
public void SetEntityPlayerIsAimingAt_SetsTarget()
{
    var bridge = new MockGameBridge();
    bridge.SetEntityPlayerIsAimingAt(123);
    Assert.Equal(123, bridge.GetEntityPlayerIsAimingAt());
}

[Fact]
public void DisplayHelpText_StoresLastHelpText()
{
    var bridge = new MockGameBridge();
    bridge.DisplayHelpText("Press ~INPUT_CONTEXT~ to interact");
    Assert.Equal("Press ~INPUT_CONTEXT~ to interact", bridge.LastHelpText);
}
```

**Step 2: Run test to verify it fails**

Run: `dotnet test tests/FactionWars.Tests --filter "MockGameBridgeTests" --no-build`
Expected: FAIL - methods don't exist

**Step 3: Implement in MockGameBridge**

Add fields near other tracking fields:

```csharp
private bool _isPlayerFreeAiming;
private int _entityPlayerIsAimingAt;
public string? LastHelpText { get; private set; }
```

Add methods:

```csharp
public bool IsPlayerFreeAiming() => _isPlayerFreeAiming;

public void SetPlayerFreeAiming(bool aiming) => _isPlayerFreeAiming = aiming;

public int GetEntityPlayerIsAimingAt() => _entityPlayerIsAimingAt;

public void SetEntityPlayerIsAimingAt(int entityHandle) => _entityPlayerIsAimingAt = entityHandle;

public void DisplayHelpText(string text) => LastHelpText = text;
```

**Step 4: Run test to verify it passes**

Run: `dotnet test tests/FactionWars.Tests --filter "MockGameBridgeTests"`
Expected: PASS

**Step 5: Commit**

```bash
git add src/FactionWars/Core/Utils/MockGameBridge.cs tests/FactionWars.Tests/Unit/Core/MockGameBridgeTests.cs
git commit -m "feat(mock): implement targeting methods in MockGameBridge"
```

---

## Task 3: Implement Targeting Natives in GameBridge

**Files:**
- Modify: `src/FactionWars/ScriptHookV/GameBridge.cs`

**Step 1: Implement the real natives**

Add these methods to `GameBridge.cs`:

```csharp
public bool IsPlayerFreeAiming()
{
    return Function.Call<bool>(Hash.IS_PLAYER_FREE_AIMING, Game.Player.Handle);
}

public int GetEntityPlayerIsAimingAt()
{
    int entityHandle = 0;
    if (Function.Call<bool>(Hash.GET_ENTITY_PLAYER_IS_FREE_AIMING_AT, Game.Player.Handle, &entityHandle))
    {
        return entityHandle;
    }
    return 0;
}

public void DisplayHelpText(string text)
{
    Function.Call(Hash.BEGIN_TEXT_COMMAND_DISPLAY_HELP, "STRING");
    Function.Call(Hash.ADD_TEXT_COMPONENT_SUBSTRING_PLAYER_NAME, text);
    Function.Call(Hash.END_TEXT_COMMAND_DISPLAY_HELP, 0, false, true, -1);
}
```

**Step 2: Build to verify compilation**

Run: `dotnet build src/FactionWars/FactionWars.csproj`
Expected: Build succeeded

**Step 3: Commit**

```bash
git add src/FactionWars/ScriptHookV/GameBridge.cs
git commit -m "feat(gamebridge): implement targeting natives for Commander interaction"
```

---

## Task 4: Create CommanderManager - Basic Structure and Tests

**Files:**
- Create: `src/FactionWars/ScriptHookV/Managers/CommanderManager.cs`
- Create: `tests/FactionWars.Tests/Unit/ScriptHookV/Managers/CommanderManagerTests.cs`

**Step 1: Write the failing tests**

Create `tests/FactionWars.Tests/Unit/ScriptHookV/Managers/CommanderManagerTests.cs`:

```csharp
using System;
using FactionWars.Core.Interfaces;
using FactionWars.Core.Models;
using FactionWars.Core.Utils;
using FactionWars.ScriptHookV.Managers;
using FactionWars.Territory.Interfaces;
using FactionWars.Territory.Models;
using FactionWars.UI.Interfaces;
using Moq;
using Xunit;

namespace FactionWars.Tests.Unit.ScriptHookV.Managers
{
    public class CommanderManagerTests
    {
        private MockGameBridge _gameBridge = null!;
        private Mock<IPedSpawningService> _pedSpawningServiceMock = null!;
        private Mock<IPedDespawnService> _pedDespawnServiceMock = null!;
        private Mock<IPedBlipService> _pedBlipServiceMock = null!;
        private Mock<IZoneService> _zoneServiceMock = null!;
        private CommanderManager _manager = null!;

        private const string PlayerFactionId = "michael";
        private const string EnemyFactionId = "ballas";
        private const string TestZoneId = "zone_1";

        private void SetupManager(Action<MainMenuController>? openMenuCallback = null)
        {
            _gameBridge = new MockGameBridge();
            _pedSpawningServiceMock = new Mock<IPedSpawningService>();
            _pedDespawnServiceMock = new Mock<IPedDespawnService>();
            _pedBlipServiceMock = new Mock<IPedBlipService>();
            _zoneServiceMock = new Mock<IZoneService>();

            _pedSpawningServiceMock.Setup(p => p.CanSpawn()).Returns(true);
            _pedSpawningServiceMock.Setup(p => p.SpawnPed(It.IsAny<string>(), It.IsAny<Vector3>(), It.IsAny<string>(), It.IsAny<string>()))
                .Returns(() => new PedHandle(_gameBridge.CreatePed("test", new Vector3(0, 0, 0))));

            _pedBlipServiceMock.Setup(p => p.CreateBlipForPed(It.IsAny<int>(), It.IsAny<BlipColor>()))
                .Returns(1);

            _manager = new CommanderManager(
                _gameBridge,
                _pedSpawningServiceMock.Object,
                _pedDespawnServiceMock.Object,
                _pedBlipServiceMock.Object,
                _zoneServiceMock.Object,
                PlayerFactionId,
                openMenuCallback);
        }

        private Zone CreateFriendlyZone()
        {
            var zone = new Zone(TestZoneId, "Test Zone", new Vector3(100, 100, 0), 150f, 1);
            zone.OwnerFactionId = PlayerFactionId;
            return zone;
        }

        private Zone CreateEnemyZone()
        {
            var zone = new Zone(TestZoneId, "Test Zone", new Vector3(100, 100, 0), 150f, 1);
            zone.OwnerFactionId = EnemyFactionId;
            return zone;
        }

        [Fact]
        public void Constructor_ThrowsOnNullGameBridge()
        {
            SetupManager();
            Assert.Throws<ArgumentNullException>(() => new CommanderManager(
                null!,
                _pedSpawningServiceMock.Object,
                _pedDespawnServiceMock.Object,
                _pedBlipServiceMock.Object,
                _zoneServiceMock.Object,
                PlayerFactionId,
                null));
        }

        [Fact]
        public void OnZoneEntered_SpawnsCommanderInFriendlyZone()
        {
            SetupManager();
            var zone = CreateFriendlyZone();

            _manager.OnZoneEntered(zone);

            Assert.True(_manager.HasCommanderInZone(TestZoneId));
            _pedSpawningServiceMock.Verify(
                p => p.SpawnPed(CommanderManager.CommanderModel, It.IsAny<Vector3>(), PlayerFactionId, TestZoneId),
                Times.Once);
        }

        [Fact]
        public void OnZoneEntered_DoesNotSpawnCommanderInEnemyZone()
        {
            SetupManager();
            var zone = CreateEnemyZone();

            _manager.OnZoneEntered(zone);

            Assert.False(_manager.HasCommanderInZone(TestZoneId));
            _pedSpawningServiceMock.Verify(
                p => p.SpawnPed(It.IsAny<string>(), It.IsAny<Vector3>(), It.IsAny<string>(), It.IsAny<string>()),
                Times.Never);
        }

        [Fact]
        public void OnZoneEntered_CreatesBlueBlipForCommander()
        {
            SetupManager();
            var zone = CreateFriendlyZone();

            _manager.OnZoneEntered(zone);

            _pedBlipServiceMock.Verify(
                p => p.CreateBlipForPed(It.IsAny<int>(), BlipColor.Blue),
                Times.Once);
        }

        [Fact]
        public void OnZoneExited_DespawnsCommander()
        {
            SetupManager();
            var zone = CreateFriendlyZone();
            _manager.OnZoneEntered(zone);
            Assert.True(_manager.HasCommanderInZone(TestZoneId));

            _manager.OnZoneExited(zone);

            Assert.False(_manager.HasCommanderInZone(TestZoneId));
        }
    }
}
```

**Step 2: Run test to verify it fails**

Run: `dotnet test tests/FactionWars.Tests --filter "CommanderManagerTests" --no-build`
Expected: FAIL - CommanderManager doesn't exist

**Step 3: Create minimal CommanderManager implementation**

Create `src/FactionWars/ScriptHookV/Managers/CommanderManager.cs`:

```csharp
using System;
using System.Collections.Generic;
using FactionWars.Core.Interfaces;
using FactionWars.Core.Models;
using FactionWars.Territory.Interfaces;
using FactionWars.Territory.Models;
using FactionWars.UI.Interfaces;

namespace FactionWars.ScriptHookV.Managers
{
    /// <summary>
    /// Manages Commander NPCs that spawn in player-owned zones.
    /// Commanders provide an immersive way to access the mod menu.
    /// </summary>
    public class CommanderManager
    {
        public const string CommanderModel = "s_m_y_armymech_01";
        public const string CommanderWeapon = "weapon_carbinerifle";
        public const int CommanderHealth = 300;
        public const int CommanderArmor = 100;
        public const float CommanderAccuracy = 0.75f;

        private readonly IGameBridge _gameBridge;
        private readonly IPedSpawningService _pedSpawningService;
        private readonly IPedDespawnService _pedDespawnService;
        private readonly IPedBlipService _pedBlipService;
        private readonly IZoneService _zoneService;
        private readonly Action<MainMenuController>? _openMenuCallback;
        private string _playerFactionId;

        private readonly Dictionary<string, int> _commanderByZone; // zoneId -> pedHandle
        private readonly HashSet<string> _zonesInBattle;
        private string? _currentZoneId;
        private readonly Random _random = new Random();

        private const float MinSpawnRadiusFraction = 0.3f;

        public CommanderManager(
            IGameBridge gameBridge,
            IPedSpawningService pedSpawningService,
            IPedDespawnService pedDespawnService,
            IPedBlipService pedBlipService,
            IZoneService zoneService,
            string playerFactionId,
            Action<MainMenuController>? openMenuCallback = null)
        {
            _gameBridge = gameBridge ?? throw new ArgumentNullException(nameof(gameBridge));
            _pedSpawningService = pedSpawningService ?? throw new ArgumentNullException(nameof(pedSpawningService));
            _pedDespawnService = pedDespawnService ?? throw new ArgumentNullException(nameof(pedDespawnService));
            _pedBlipService = pedBlipService ?? throw new ArgumentNullException(nameof(pedBlipService));
            _zoneService = zoneService ?? throw new ArgumentNullException(nameof(zoneService));
            _playerFactionId = playerFactionId ?? throw new ArgumentNullException(nameof(playerFactionId));
            _openMenuCallback = openMenuCallback;

            _commanderByZone = new Dictionary<string, int>();
            _zonesInBattle = new HashSet<string>();
        }

        public bool HasCommanderInZone(string zoneId) => _commanderByZone.ContainsKey(zoneId);

        public void OnZoneEntered(Zone zone)
        {
            if (zone == null) return;
            _currentZoneId = zone.Id;

            if (zone.OwnerFactionId != _playerFactionId) return;

            SpawnCommander(zone);
        }

        public void OnZoneExited(Zone zone)
        {
            if (zone == null) return;
            if (_currentZoneId == zone.Id)
                _currentZoneId = null;

            DespawnCommander(zone.Id);
        }

        private void SpawnCommander(Zone zone)
        {
            if (_commanderByZone.ContainsKey(zone.Id)) return;
            if (!_pedSpawningService.CanSpawn()) return;

            var spawnPos = CalculateRandomSpawnPosition(zone.Center, zone.Radius);
            var pedHandle = _pedSpawningService.SpawnPed(CommanderModel, spawnPos, _playerFactionId, zone.Id);

            if (!pedHandle.IsValid) return;

            ConfigureCommander(pedHandle.Handle, zone);
            _pedBlipService.CreateBlipForPed(pedHandle.Handle, BlipColor.Blue);
            _commanderByZone[zone.Id] = pedHandle.Handle;
        }

        private void DespawnCommander(string zoneId)
        {
            if (!_commanderByZone.TryGetValue(zoneId, out var pedHandle)) return;

            _pedBlipService.RemoveBlipForPed(pedHandle);
            _pedDespawnService.DespawnPed(pedHandle);
            _commanderByZone.Remove(zoneId);
        }

        private void ConfigureCommander(int pedHandle, Zone zone)
        {
            _gameBridge.SetPedAsFriendly(pedHandle);
            _gameBridge.GivePedWeapon(pedHandle, "weapon_pistol");
            _gameBridge.GivePedWeapon(pedHandle, CommanderWeapon);
            _gameBridge.SetPedAccuracy(pedHandle, CommanderAccuracy);
            _gameBridge.SetPedArmor(pedHandle, CommanderArmor);
            _gameBridge.SetPedHealth(pedHandle, CommanderHealth);
            _gameBridge.SetPedCombatAttributes(pedHandle, canUseCover: true, willFightArmedPeds: true);

            if (_zonesInBattle.Contains(zone.Id))
            {
                _gameBridge.TaskPedWanderInAreaSprinting(pedHandle, zone.Center, zone.Radius);
            }
            else
            {
                _gameBridge.TaskPedWanderInArea(pedHandle, zone.Center, zone.Radius);
            }
        }

        private Vector3 CalculateRandomSpawnPosition(Vector3 center, float zoneRadius)
        {
            var angle = _random.NextDouble() * 2 * Math.PI;
            var minRadius = zoneRadius * MinSpawnRadiusFraction;
            var distance = minRadius + (float)(_random.NextDouble() * (zoneRadius - minRadius));
            var x = center.X + (float)(Math.Cos(angle) * distance);
            var y = center.Y + (float)(Math.Sin(angle) * distance);

            var targetPos = new Vector3(x, y, center.Z);
            return _gameBridge.GetSafeCoordForPed(targetPos);
        }
    }
}
```

**Step 4: Run test to verify it passes**

Run: `dotnet test tests/FactionWars.Tests --filter "CommanderManagerTests"`
Expected: PASS

**Step 5: Commit**

```bash
git add src/FactionWars/ScriptHookV/Managers/CommanderManager.cs tests/FactionWars.Tests/Unit/ScriptHookV/Managers/CommanderManagerTests.cs
git commit -m "feat(commander): add CommanderManager with spawn/despawn on zone entry/exit"
```

---

## Task 5: Add Death Detection and Immediate Respawn

**Files:**
- Modify: `src/FactionWars/ScriptHookV/Managers/CommanderManager.cs`
- Modify: `tests/FactionWars.Tests/Unit/ScriptHookV/Managers/CommanderManagerTests.cs`

**Step 1: Write the failing test**

Add to `CommanderManagerTests.cs`:

```csharp
[Fact]
public void Update_RespawnsDeadCommander()
{
    SetupManager();
    var zone = CreateFriendlyZone();
    _zoneServiceMock.Setup(z => z.GetZone(TestZoneId)).Returns(zone);

    _manager.OnZoneEntered(zone);
    Assert.True(_manager.HasCommanderInZone(TestZoneId));

    // Kill the commander
    var peds = _gameBridge.GetSpawnedPeds();
    Assert.Single(peds);
    _gameBridge.KillPed(peds[0]);

    // Update should detect death and respawn
    _manager.Update();

    // Should still have a commander (respawned)
    Assert.True(_manager.HasCommanderInZone(TestZoneId));
    // Spawn was called twice (initial + respawn)
    _pedSpawningServiceMock.Verify(
        p => p.SpawnPed(CommanderManager.CommanderModel, It.IsAny<Vector3>(), PlayerFactionId, TestZoneId),
        Times.Exactly(2));
}
```

**Step 2: Run test to verify it fails**

Run: `dotnet test tests/FactionWars.Tests --filter "Update_RespawnsDeadCommander"`
Expected: FAIL - Update method doesn't exist or doesn't respawn

**Step 3: Implement Update method**

Add to `CommanderManager.cs`:

```csharp
public void Update()
{
    var deadCommanders = new List<string>();

    foreach (var kvp in _commanderByZone)
    {
        var zoneId = kvp.Key;
        var pedHandle = kvp.Value;

        if (!_gameBridge.IsPedAlive(pedHandle))
        {
            deadCommanders.Add(zoneId);
        }
    }

    foreach (var zoneId in deadCommanders)
    {
        RespawnCommander(zoneId);
    }
}

private void RespawnCommander(string zoneId)
{
    // Remove old commander
    if (_commanderByZone.TryGetValue(zoneId, out var oldHandle))
    {
        _pedBlipService.RemoveBlipForPed(oldHandle);
        _gameBridge.DeletePed(oldHandle);
        _commanderByZone.Remove(zoneId);
    }

    // Get zone and spawn new commander
    var zone = _zoneService.GetZone(zoneId);
    if (zone != null && zone.OwnerFactionId == _playerFactionId)
    {
        SpawnCommander(zone);
    }
}
```

**Step 4: Run test to verify it passes**

Run: `dotnet test tests/FactionWars.Tests --filter "CommanderManagerTests"`
Expected: PASS

**Step 5: Commit**

```bash
git add src/FactionWars/ScriptHookV/Managers/CommanderManager.cs tests/FactionWars.Tests/Unit/ScriptHookV/Managers/CommanderManagerTests.cs
git commit -m "feat(commander): add death detection and immediate respawn"
```

---

## Task 6: Add Territory Loss Handling

**Files:**
- Modify: `src/FactionWars/ScriptHookV/Managers/CommanderManager.cs`
- Modify: `tests/FactionWars.Tests/Unit/ScriptHookV/Managers/CommanderManagerTests.cs`

**Step 1: Write the failing test**

Add to `CommanderManagerTests.cs`:

```csharp
[Fact]
public void OnTerritoryLost_DespawnsCommander()
{
    SetupManager();
    var zone = CreateFriendlyZone();

    _manager.OnZoneEntered(zone);
    Assert.True(_manager.HasCommanderInZone(TestZoneId));

    // Territory is lost
    _manager.OnTerritoryLost(TestZoneId);

    Assert.False(_manager.HasCommanderInZone(TestZoneId));
    _pedDespawnServiceMock.Verify(p => p.DespawnPed(It.IsAny<int>()), Times.Once);
}
```

**Step 2: Run test to verify it fails**

Run: `dotnet test tests/FactionWars.Tests --filter "OnTerritoryLost_DespawnsCommander"`
Expected: FAIL - OnTerritoryLost doesn't exist

**Step 3: Implement OnTerritoryLost**

Add to `CommanderManager.cs`:

```csharp
public void OnTerritoryLost(string zoneId)
{
    DespawnCommander(zoneId);
}
```

**Step 4: Run test to verify it passes**

Run: `dotnet test tests/FactionWars.Tests --filter "CommanderManagerTests"`
Expected: PASS

**Step 5: Commit**

```bash
git add src/FactionWars/ScriptHookV/Managers/CommanderManager.cs tests/FactionWars.Tests/Unit/ScriptHookV/Managers/CommanderManagerTests.cs
git commit -m "feat(commander): despawn commander on territory loss"
```

---

## Task 7: Add Interaction Detection and Menu Opening

**Files:**
- Modify: `src/FactionWars/ScriptHookV/Managers/CommanderManager.cs`
- Modify: `tests/FactionWars.Tests/Unit/ScriptHookV/Managers/CommanderManagerTests.cs`

**Step 1: Write the failing tests**

Add to `CommanderManagerTests.cs`:

```csharp
[Fact]
public void Update_ShowsHelpTextWhenAimingAtCommander()
{
    SetupManager();
    var zone = CreateFriendlyZone();
    _manager.OnZoneEntered(zone);

    var commanderHandle = _gameBridge.GetSpawnedPeds()[0];
    _gameBridge.SetPlayerFreeAiming(true);
    _gameBridge.SetEntityPlayerIsAimingAt(commanderHandle);

    _manager.Update();

    Assert.Contains("Commander", _gameBridge.LastHelpText);
}

[Fact]
public void OnKeyDown_OpensMenuWhenAimingAtCommanderAndPressingE()
{
    bool menuOpened = false;
    SetupManager(_ => menuOpened = true);
    var zone = CreateFriendlyZone();
    _manager.OnZoneEntered(zone);

    var commanderHandle = _gameBridge.GetSpawnedPeds()[0];
    _gameBridge.SetPlayerFreeAiming(true);
    _gameBridge.SetEntityPlayerIsAimingAt(commanderHandle);

    _manager.OnKeyDown(0x45); // E key

    Assert.True(menuOpened);
}

[Fact]
public void OnKeyDown_DoesNotOpenMenuWhenNotAimingAtCommander()
{
    bool menuOpened = false;
    SetupManager(_ => menuOpened = true);
    var zone = CreateFriendlyZone();
    _manager.OnZoneEntered(zone);

    _gameBridge.SetPlayerFreeAiming(true);
    _gameBridge.SetEntityPlayerIsAimingAt(999); // Not a commander

    _manager.OnKeyDown(0x45); // E key

    Assert.False(menuOpened);
}
```

**Step 2: Run test to verify it fails**

Run: `dotnet test tests/FactionWars.Tests --filter "ShowsHelpTextWhenAimingAtCommander"`
Expected: FAIL

**Step 3: Implement interaction detection**

Add constant and method to `CommanderManager.cs`:

```csharp
private const int InteractKeyCode = 0x45; // E key

public void OnKeyDown(int keyCode)
{
    if (keyCode != InteractKeyCode) return;
    if (!_gameBridge.IsPlayerFreeAiming()) return;

    var targetEntity = _gameBridge.GetEntityPlayerIsAimingAt();
    if (targetEntity == 0) return;

    if (IsCommander(targetEntity))
    {
        _openMenuCallback?.Invoke(null!);
    }
}

private bool IsCommander(int pedHandle)
{
    return _commanderByZone.ContainsValue(pedHandle);
}
```

Update the `Update` method to show help text:

```csharp
public void Update()
{
    // Check for dead commanders
    var deadCommanders = new List<string>();

    foreach (var kvp in _commanderByZone)
    {
        var zoneId = kvp.Key;
        var pedHandle = kvp.Value;

        if (!_gameBridge.IsPedAlive(pedHandle))
        {
            deadCommanders.Add(zoneId);
        }
    }

    foreach (var zoneId in deadCommanders)
    {
        RespawnCommander(zoneId);
    }

    // Show help text if aiming at commander
    if (_gameBridge.IsPlayerFreeAiming())
    {
        var targetEntity = _gameBridge.GetEntityPlayerIsAimingAt();
        if (targetEntity != 0 && IsCommander(targetEntity))
        {
            _gameBridge.DisplayHelpText("Press ~INPUT_CONTEXT~ to talk to Commander");
        }
    }
}
```

**Step 4: Run test to verify it passes**

Run: `dotnet test tests/FactionWars.Tests --filter "CommanderManagerTests"`
Expected: PASS

**Step 5: Commit**

```bash
git add src/FactionWars/ScriptHookV/Managers/CommanderManager.cs tests/FactionWars.Tests/Unit/ScriptHookV/Managers/CommanderManagerTests.cs
git commit -m "feat(commander): add interaction detection and menu opening"
```

---

## Task 8: Add Battle Mode (Sprinting Wander)

**Files:**
- Modify: `src/FactionWars/ScriptHookV/Managers/CommanderManager.cs`
- Modify: `tests/FactionWars.Tests/Unit/ScriptHookV/Managers/CommanderManagerTests.cs`

**Step 1: Write the failing test**

Add to `CommanderManagerTests.cs`:

```csharp
[Fact]
public void OnBattleStarted_SwitchesCommanderToSprinting()
{
    SetupManager();
    var zone = CreateFriendlyZone();
    _zoneServiceMock.Setup(z => z.GetZone(TestZoneId)).Returns(zone);
    _manager.OnZoneEntered(zone);

    var commanderHandle = _gameBridge.GetSpawnedPeds()[0];
    Assert.False(_gameBridge.IsPedWanderingSprinting(commanderHandle));

    _manager.OnBattleStarted(TestZoneId);

    Assert.True(_gameBridge.IsPedWanderingSprinting(commanderHandle));
}

[Fact]
public void OnBattleEnded_SwitchesCommanderToWalking()
{
    SetupManager();
    var zone = CreateFriendlyZone();
    _zoneServiceMock.Setup(z => z.GetZone(TestZoneId)).Returns(zone);
    _manager.OnZoneEntered(zone);
    _manager.OnBattleStarted(TestZoneId);

    var commanderHandle = _gameBridge.GetSpawnedPeds()[0];
    Assert.True(_gameBridge.IsPedWanderingSprinting(commanderHandle));

    _manager.OnBattleEnded(TestZoneId);

    Assert.True(_gameBridge.IsPedWandering(commanderHandle));
    Assert.False(_gameBridge.IsPedWanderingSprinting(commanderHandle));
}
```

**Step 2: Run test to verify it fails**

Run: `dotnet test tests/FactionWars.Tests --filter "OnBattleStarted_SwitchesCommanderToSprinting"`
Expected: FAIL

**Step 3: Implement battle mode methods**

Add to `CommanderManager.cs`:

```csharp
public void OnBattleStarted(string zoneId)
{
    _zonesInBattle.Add(zoneId);

    if (!_commanderByZone.TryGetValue(zoneId, out var pedHandle)) return;

    var zone = _zoneService.GetZone(zoneId);
    if (zone == null) return;

    _gameBridge.TaskPedWanderInAreaSprinting(pedHandle, zone.Center, zone.Radius);
}

public void OnBattleEnded(string zoneId)
{
    _zonesInBattle.Remove(zoneId);

    if (!_commanderByZone.TryGetValue(zoneId, out var pedHandle)) return;

    var zone = _zoneService.GetZone(zoneId);
    if (zone == null) return;

    _gameBridge.TaskPedWanderInArea(pedHandle, zone.Center, zone.Radius);
}
```

**Step 4: Run test to verify it passes**

Run: `dotnet test tests/FactionWars.Tests --filter "CommanderManagerTests"`
Expected: PASS

**Step 5: Commit**

```bash
git add src/FactionWars/ScriptHookV/Managers/CommanderManager.cs tests/FactionWars.Tests/Unit/ScriptHookV/Managers/CommanderManagerTests.cs
git commit -m "feat(commander): add battle mode with sprinting wander"
```

---

## Task 9: Wire CommanderManager into GameLoopController

**Files:**
- Modify: `src/FactionWars/ScriptHookV/GameLoopController.cs`

**Step 1: Add field and property**

Add near other manager fields (around line 50):

```csharp
private CommanderManager? _commanderManager;
```

**Step 2: Initialize in InitializeGameData**

Find where `_friendlyDefenderManager` is created (in `InitializeGameData` method) and add after it:

```csharp
// Create CommanderManager
_commanderManager = new CommanderManager(
    _gameBridge,
    pedSpawningService,
    pedDespawnService,
    pedBlipService,
    _zoneService,
    playerFactionId,
    _ => _mainMenuController?.ShowMainMenu());
```

**Step 3: Wire up events**

After the `_friendlyDefenderManager.TerritoryLost` subscription, add:

```csharp
// Wire commander to territory loss
_friendlyDefenderManager.TerritoryLost += (sender, args) =>
{
    _commanderManager?.OnTerritoryLost(args.ZoneId);
};
```

In the zone entered event handler (where `_friendlyDefenderManager.OnZoneEntered` is called), add:

```csharp
_commanderManager?.OnZoneEntered(zone);
```

In the zone exited event handler, add:

```csharp
_commanderManager?.OnZoneExited(zone);
```

**Step 4: Add Update call**

In the `OnTick` method, find where `_friendlyDefenderManager?.Update()` is called and add after it:

```csharp
_commanderManager?.Update();
```

**Step 5: Add OnKeyDown handling**

In the `OnKeyDown` method, add:

```csharp
_commanderManager?.OnKeyDown(keyCode);
```

**Step 6: Wire battle events**

Find where `ZoneBattleManager.BattleStarted` is subscribed and add:

```csharp
_commanderManager?.OnBattleStarted(args.ZoneId);
```

Find where `ZoneBattleManager.BattleEnded` is subscribed and add:

```csharp
_commanderManager?.OnBattleEnded(args.ZoneId);
```

**Step 7: Build and verify**

Run: `dotnet build src/FactionWars/FactionWars.csproj`
Expected: Build succeeded

**Step 8: Commit**

```bash
git add src/FactionWars/ScriptHookV/GameLoopController.cs
git commit -m "feat(commander): wire CommanderManager into GameLoopController"
```

---

## Task 10: Make MainMenuController.ShowMainMenu Public

**Files:**
- Modify: `src/FactionWars/ScriptHookV/UI/MainMenuController.cs`

**Step 1: Change visibility**

Find the `ShowMainMenu()` method and change from `private` to `public`:

```csharp
/// <summary>
/// Creates and displays the main menu with all submenu options.
/// </summary>
public void ShowMainMenu()
```

**Step 2: Build and verify**

Run: `dotnet build src/FactionWars/FactionWars.csproj`
Expected: Build succeeded

**Step 3: Commit**

```bash
git add src/FactionWars/ScriptHookV/UI/MainMenuController.cs
git commit -m "refactor(menu): make ShowMainMenu public for Commander interaction"
```

---

## Task 11: Run All Tests and Final Verification

**Step 1: Run all tests**

Run: `dotnet test tests/FactionWars.Tests`
Expected: All tests pass

**Step 2: Build release**

Run: `dotnet build src/FactionWars/FactionWars.csproj -c Release`
Expected: Build succeeded

**Step 3: Final commit with summary**

```bash
git add -A
git commit -m "feat: complete Commander NPC implementation

Adds Commander NPCs to player-owned zones that provide immersive menu access.
- Commander spawns at random position in owned zones (blue blip)
- Aim at Commander + press E to open menu
- Commander respawns immediately if killed
- Commander despawns when territory is lost
- F7 still works as alternative menu access"
```

---

## Summary

| Task | Description | Estimated Complexity |
|------|-------------|---------------------|
| 1 | Add targeting methods to IGameBridge | Simple |
| 2 | Implement in MockGameBridge with tests | Simple |
| 3 | Implement in GameBridge (real natives) | Simple |
| 4 | Create CommanderManager structure + tests | Medium |
| 5 | Add death detection and respawn | Simple |
| 6 | Add territory loss handling | Simple |
| 7 | Add interaction detection | Medium |
| 8 | Add battle mode | Simple |
| 9 | Wire into GameLoopController | Medium |
| 10 | Make ShowMainMenu public | Trivial |
| 11 | Final verification | Simple |
