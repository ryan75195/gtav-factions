# Battle Desync Fix Implementation Plan

> **For Claude:** REQUIRED SUB-SKILL: Use superpowers:executing-plans to implement this plan task-by-task.

**Goal:** Fix two desync issues: (1) Battle HUD not updating when troops are allocated during active battle, (2) Attacker NPCs not spawning when player enters their own zone under attack.

**Architecture:** Add `AddDefenderTroops()` method to `ActiveBattle` and wire `TroopsAllocated` event to `IActiveBattleManager`. Create new `BattleAttackerManager` to spawn attacker NPCs when player enters their defending zone during an active battle.

**Tech Stack:** C#, ScriptHookVDotNet, xUnit, Moq

---

## Task 1: Add AddDefenderTroops method to ActiveBattle

**Files:**
- Modify: `src/FactionWars/Combat/Models/ActiveBattle.cs:152-176`
- Test: `tests/FactionWars.Tests/Unit/Combat/ActiveBattleTests.cs`

**Step 1: Write the failing test**

```csharp
[Fact]
public void AddDefenderTroops_ShouldIncreaseTroopCount()
{
    var attackerTroops = new Dictionary<DefenderTier, int> { { DefenderTier.Basic, 5 } };
    var defenderTroops = new Dictionary<DefenderTier, int> { { DefenderTier.Basic, 3 } };
    var battle = new ActiveBattle("attacker", "defender", "zone1", attackerTroops, defenderTroops, 60f, 6f);

    battle.AddDefenderTroops(DefenderTier.Basic, 2);

    Assert.Equal(5, battle.TotalDefenderTroops);
    Assert.Equal(5, battle.DefenderTroops[DefenderTier.Basic]);
}

[Fact]
public void AddDefenderTroops_NewTier_ShouldAddTier()
{
    var attackerTroops = new Dictionary<DefenderTier, int> { { DefenderTier.Basic, 5 } };
    var defenderTroops = new Dictionary<DefenderTier, int> { { DefenderTier.Basic, 3 } };
    var battle = new ActiveBattle("attacker", "defender", "zone1", attackerTroops, defenderTroops, 60f, 6f);

    battle.AddDefenderTroops(DefenderTier.Medium, 2);

    Assert.Equal(5, battle.TotalDefenderTroops);
    Assert.Equal(2, battle.DefenderTroops[DefenderTier.Medium]);
}
```

**Step 2: Run test to verify it fails**

Run: `dotnet test tests/FactionWars.Tests --filter "FullyQualifiedName~ActiveBattleTests.AddDefenderTroops" -v n`
Expected: FAIL with "ActiveBattle does not contain a definition for 'AddDefenderTroops'"

**Step 3: Write minimal implementation**

Add to `src/FactionWars/Combat/Models/ActiveBattle.cs` after line 176 (after `RemoveDefenderTroop`):

```csharp
/// <summary>
/// Adds troops of the specified tier to the defender.
/// Used when player allocates reinforcements during active battle.
/// </summary>
public void AddDefenderTroops(DefenderTier tier, int count)
{
    if (count <= 0) return;

    if (DefenderTroops.ContainsKey(tier))
    {
        DefenderTroops[tier] += count;
    }
    else
    {
        DefenderTroops[tier] = count;
    }
}
```

**Step 4: Run test to verify it passes**

Run: `dotnet test tests/FactionWars.Tests --filter "FullyQualifiedName~ActiveBattleTests.AddDefenderTroops" -v n`
Expected: PASS

**Step 5: Commit**

```bash
git add src/FactionWars/Combat/Models/ActiveBattle.cs tests/FactionWars.Tests/Unit/Combat/ActiveBattleTests.cs
git commit -m "feat: add AddDefenderTroops method to ActiveBattle"
```

---

## Task 2: Add AddDefenderTroops to IActiveBattleManager interface

**Files:**
- Modify: `src/FactionWars/Combat/Interfaces/IActiveBattleManager.cs:64-65`
- Modify: `src/FactionWars/Combat/Services/ActiveBattleManager.cs`

**Step 1: Add interface method**

Add to `src/FactionWars/Combat/Interfaces/IActiveBattleManager.cs` after line 64:

```csharp
/// <summary>
/// Adds defender troops to an active battle in the specified zone.
/// Used when player allocates reinforcements during active battle.
/// </summary>
/// <param name="zoneId">The zone with the active battle.</param>
/// <param name="tier">The tier of troops to add.</param>
/// <param name="count">The number of troops to add.</param>
/// <returns>True if troops were added to an existing battle, false if no battle exists.</returns>
bool AddDefenderTroops(string zoneId, DefenderTier tier, int count);
```

**Step 2: Implement in ActiveBattleManager**

Add to `src/FactionWars/Combat/Services/ActiveBattleManager.cs` after `ReportTroopKilled` method:

```csharp
public bool AddDefenderTroops(string zoneId, DefenderTier tier, int count)
{
    var battle = GetBattleForZone(zoneId);
    if (battle == null) return false;

    battle.AddDefenderTroops(tier, count);
    FileLogger.Combat($"ActiveBattleManager: Added {count} {tier} defenders to battle in {zoneId}, new total: {battle.TotalDefenderTroops}");
    return true;
}
```

**Step 3: Run all tests**

Run: `dotnet test tests/FactionWars.Tests -v n`
Expected: All tests pass

**Step 4: Commit**

```bash
git add src/FactionWars/Combat/Interfaces/IActiveBattleManager.cs src/FactionWars/Combat/Services/ActiveBattleManager.cs
git commit -m "feat: add AddDefenderTroops to IActiveBattleManager"
```

---

## Task 3: Wire TroopsAllocated event to ActiveBattleManager in GameLoopController

**Files:**
- Modify: `src/FactionWars/ScriptHookV/GameLoopController.cs:558-565`

**Step 1: Extend the existing TroopsAllocated handler**

Find the existing subscription at line 558-565:
```csharp
// Subscribe to troop allocation events for immediate spawning when player is in zone
allocationService.TroopsAllocated += (sender, e) =>
{
    var zone = _zoneService?.GetZone(e.ZoneId);
    if (zone != null)
    {
        _friendlyDefenderManager.OnTroopsAllocated(e.FactionId, e.ZoneId, e.Tier, e.Count, zone.Center);
    }
};
```

Replace with:
```csharp
// Subscribe to troop allocation events for immediate spawning when player is in zone
// and for updating active battles when player allocates reinforcements
allocationService.TroopsAllocated += (sender, e) =>
{
    var zone = _zoneService?.GetZone(e.ZoneId);
    if (zone != null)
    {
        _friendlyDefenderManager.OnTroopsAllocated(e.FactionId, e.ZoneId, e.Tier, e.Count, zone.Center);
    }

    // If there's an active battle in this zone where player is defender, add troops to battle
    if (e.FactionId == CurrentPlayerFactionId)
    {
        var battle = _activeBattleManager?.GetBattleForZone(e.ZoneId);
        if (battle != null && battle.DefenderFactionId == e.FactionId)
        {
            _activeBattleManager?.AddDefenderTroops(e.ZoneId, e.Tier, e.Count);
        }
    }
};
```

**Step 2: Run game to verify HUD updates**

Deploy and test:
1. Start a game where an AI faction is attacking your zone
2. Open menu and allocate troops to that zone
3. Verify Battle HUD troop count increases

**Step 3: Commit**

```bash
git add src/FactionWars/ScriptHookV/GameLoopController.cs
git commit -m "feat: wire TroopsAllocated to update active battle defender count"
```

---

## Task 4: Create BattleAttackerManager for spawning attackers in player's defending zone

**Files:**
- Create: `src/FactionWars/ScriptHookV/Managers/BattleAttackerManager.cs`
- Test: `tests/FactionWars.Tests/Unit/ScriptHookV/BattleAttackerManagerTests.cs`

**Step 1: Write the failing test**

Create `tests/FactionWars.Tests/Unit/ScriptHookV/BattleAttackerManagerTests.cs`:

```csharp
using FactionWars.Combat.Interfaces;
using FactionWars.Combat.Models;
using FactionWars.Core.Interfaces;
using FactionWars.Core.Models;
using FactionWars.ScriptHookV.Managers;
using FactionWars.Territory.Interfaces;
using FactionWars.Territory.Models;
using FactionWars.UI.Interfaces;
using Moq;
using System.Collections.Generic;
using Xunit;

namespace FactionWars.Tests.Unit.ScriptHookV
{
    public class BattleAttackerManagerTests
    {
        private readonly Mock<IGameBridge> _gameBridgeMock;
        private readonly Mock<IActiveBattleManager> _battleManagerMock;
        private readonly Mock<IPedSpawningService> _pedSpawningMock;
        private readonly Mock<IDefenderTierService> _tierServiceMock;
        private readonly Mock<IPedBlipService> _blipServiceMock;
        private readonly Mock<IZoneService> _zoneServiceMock;

        public BattleAttackerManagerTests()
        {
            _gameBridgeMock = new Mock<IGameBridge>();
            _battleManagerMock = new Mock<IActiveBattleManager>();
            _pedSpawningMock = new Mock<IPedSpawningService>();
            _tierServiceMock = new Mock<IDefenderTierService>();
            _blipServiceMock = new Mock<IPedBlipService>();
            _zoneServiceMock = new Mock<IZoneService>();

            _tierServiceMock.Setup(t => t.GetTierConfig(It.IsAny<DefenderTier>()))
                .Returns(new DefenderTierConfig(DefenderTier.Basic, 100, 100, 0, 50, "weapon_pistol"));
        }

        [Fact]
        public void OnPlayerZoneEntered_WithActiveBattle_AsDefender_ShouldSpawnAttackers()
        {
            // Arrange
            var zone = new Zone("downtown", "Downtown", new Vector3(0, 0, 0), 100f) { OwnerFactionId = "player" };
            var attackerTroops = new Dictionary<DefenderTier, int> { { DefenderTier.Basic, 5 } };
            var defenderTroops = new Dictionary<DefenderTier, int> { { DefenderTier.Basic, 3 } };
            var battle = new ActiveBattle("enemy", "player", "downtown", attackerTroops, defenderTroops, 60f, 6f);

            _battleManagerMock.Setup(b => b.GetBattleForZone("downtown")).Returns(battle);
            _pedSpawningMock.Setup(p => p.CanSpawn()).Returns(true);
            _pedSpawningMock.Setup(p => p.SpawnPed(It.IsAny<string>(), It.IsAny<Vector3>(), It.IsAny<string>(), It.IsAny<string>()))
                .Returns(new PedHandle(100));
            _gameBridgeMock.Setup(g => g.GetGroundZ(It.IsAny<float>(), It.IsAny<float>(), It.IsAny<float>())).Returns(0f);

            var manager = CreateManager("player");

            // Act
            manager.OnPlayerZoneEntered(zone);

            // Assert - should spawn up to MaxSpawnedAttackers (or total attackers, whichever is less)
            _pedSpawningMock.Verify(p => p.SpawnPed(It.IsAny<string>(), It.IsAny<Vector3>(), "enemy", "downtown"),
                Times.Exactly(5));
        }

        [Fact]
        public void OnPlayerZoneEntered_NoBattle_ShouldNotSpawnAttackers()
        {
            // Arrange
            var zone = new Zone("downtown", "Downtown", new Vector3(0, 0, 0), 100f) { OwnerFactionId = "player" };
            _battleManagerMock.Setup(b => b.GetBattleForZone("downtown")).Returns((ActiveBattle?)null);

            var manager = CreateManager("player");

            // Act
            manager.OnPlayerZoneEntered(zone);

            // Assert
            _pedSpawningMock.Verify(p => p.SpawnPed(It.IsAny<string>(), It.IsAny<Vector3>(), It.IsAny<string>(), It.IsAny<string>()),
                Times.Never);
        }

        [Fact]
        public void OnPlayerZoneEntered_PlayerIsAttacker_ShouldNotSpawnAttackers()
        {
            // Arrange
            var zone = new Zone("downtown", "Downtown", new Vector3(0, 0, 0), 100f) { OwnerFactionId = "enemy" };
            var attackerTroops = new Dictionary<DefenderTier, int> { { DefenderTier.Basic, 5 } };
            var defenderTroops = new Dictionary<DefenderTier, int> { { DefenderTier.Basic, 3 } };
            var battle = new ActiveBattle("player", "enemy", "downtown", attackerTroops, defenderTroops, 60f, 6f);

            _battleManagerMock.Setup(b => b.GetBattleForZone("downtown")).Returns(battle);

            var manager = CreateManager("player");

            // Act
            manager.OnPlayerZoneEntered(zone);

            // Assert - should NOT spawn because player is attacker, not defender
            _pedSpawningMock.Verify(p => p.SpawnPed(It.IsAny<string>(), It.IsAny<Vector3>(), It.IsAny<string>(), It.IsAny<string>()),
                Times.Never);
        }

        private BattleAttackerManager CreateManager(string playerFactionId)
        {
            return new BattleAttackerManager(
                _gameBridgeMock.Object,
                _battleManagerMock.Object,
                _pedSpawningMock.Object,
                _tierServiceMock.Object,
                _blipServiceMock.Object,
                _zoneServiceMock.Object,
                playerFactionId);
        }
    }
}
```

**Step 2: Run test to verify it fails**

Run: `dotnet test tests/FactionWars.Tests --filter "FullyQualifiedName~BattleAttackerManagerTests" -v n`
Expected: FAIL with "type or namespace 'BattleAttackerManager' could not be found"

**Step 3: Write minimal implementation**

Create `src/FactionWars/ScriptHookV/Managers/BattleAttackerManager.cs`:

```csharp
using System;
using System.Collections.Generic;
using System.Linq;
using FactionWars.Combat.Interfaces;
using FactionWars.Combat.Models;
using FactionWars.Core.Interfaces;
using FactionWars.Core.Models;
using FactionWars.Territory.Interfaces;
using FactionWars.Territory.Models;
using FactionWars.UI.Interfaces;
using FactionWars.ScriptHookV.Logging;

namespace FactionWars.ScriptHookV.Managers
{
    /// <summary>
    /// Manages attacker NPCs that spawn when the player enters their own zone
    /// that is currently under attack by an AI faction.
    /// </summary>
    public class BattleAttackerManager
    {
        private readonly IGameBridge _gameBridge;
        private readonly IActiveBattleManager _activeBattleManager;
        private readonly IPedSpawningService _pedSpawningService;
        private readonly IDefenderTierService _defenderTierService;
        private readonly IPedBlipService _pedBlipService;
        private readonly IZoneService _zoneService;
        private string _playerFactionId;

        private readonly Dictionary<DefenderTier, string> _modelsByTier;
        private readonly Dictionary<string, Dictionary<int, DefenderTier>> _spawnedPedTierByZone;
        private string? _currentBattleZoneId;

        private const float MinSpawnRadiusFraction = 0.3f;

        /// <summary>
        /// Maximum number of attacker NPCs that can be spawned at once.
        /// </summary>
        public const int MaxSpawnedAttackers = 12;

        public BattleAttackerManager(
            IGameBridge gameBridge,
            IActiveBattleManager activeBattleManager,
            IPedSpawningService pedSpawningService,
            IDefenderTierService defenderTierService,
            IPedBlipService pedBlipService,
            IZoneService zoneService,
            string playerFactionId)
        {
            _gameBridge = gameBridge ?? throw new ArgumentNullException(nameof(gameBridge));
            _activeBattleManager = activeBattleManager ?? throw new ArgumentNullException(nameof(activeBattleManager));
            _pedSpawningService = pedSpawningService ?? throw new ArgumentNullException(nameof(pedSpawningService));
            _defenderTierService = defenderTierService ?? throw new ArgumentNullException(nameof(defenderTierService));
            _pedBlipService = pedBlipService ?? throw new ArgumentNullException(nameof(pedBlipService));
            _zoneService = zoneService ?? throw new ArgumentNullException(nameof(zoneService));
            _playerFactionId = playerFactionId ?? throw new ArgumentNullException(nameof(playerFactionId));

            _modelsByTier = new Dictionary<DefenderTier, string>
            {
                { DefenderTier.Basic, "g_m_y_famca_01" },
                { DefenderTier.Medium, "g_m_y_famdnf_01" },
                { DefenderTier.Heavy, "g_m_y_famfor_01" }
            };

            _spawnedPedTierByZone = new Dictionary<string, Dictionary<int, DefenderTier>>();
        }

        /// <summary>
        /// Sets the player's faction ID.
        /// </summary>
        public void SetPlayerFaction(string factionId)
        {
            if (string.IsNullOrEmpty(factionId))
                throw new ArgumentNullException(nameof(factionId));
            DespawnAllAttackers();
            _playerFactionId = factionId;
        }

        /// <summary>
        /// Called when the player enters a zone. Spawns attacker NPCs if there's
        /// an active battle where the player is the defender.
        /// </summary>
        public void OnPlayerZoneEntered(Zone zone)
        {
            if (zone == null) return;

            // Check if there's an active battle in this zone
            var battle = _activeBattleManager.GetBattleForZone(zone.Id);
            if (battle == null) return;

            // Only spawn attackers if player is the DEFENDER (their zone is being attacked)
            if (battle.DefenderFactionId != _playerFactionId) return;

            FileLogger.Combat($"BattleAttackerManager: Player entered defending zone {zone.Id} under attack by {battle.AttackerFactionId}");

            _currentBattleZoneId = zone.Id;

            // Initialize tracking for this zone
            if (!_spawnedPedTierByZone.ContainsKey(zone.Id))
            {
                _spawnedPedTierByZone[zone.Id] = new Dictionary<int, DefenderTier>();
            }

            var totalSpawned = 0;
            var random = new Random();

            // Spawn attackers based on battle's current attacker troop counts
            foreach (DefenderTier tier in new[] { DefenderTier.Heavy, DefenderTier.Medium, DefenderTier.Basic })
            {
                if (!battle.AttackerTroops.TryGetValue(tier, out int count) || count <= 0)
                    continue;

                var model = _modelsByTier.TryGetValue(tier, out var m) ? m : "g_m_y_famca_01";
                var tierConfig = _defenderTierService.GetTierConfig(tier);

                for (int i = 0; i < count && totalSpawned < MaxSpawnedAttackers; i++)
                {
                    if (!_pedSpawningService.CanSpawn()) break;

                    var spawnPos = CalculateRandomSpawnPosition(zone.Center, zone.Radius, random);
                    var pedHandle = _pedSpawningService.SpawnPed(model, spawnPos, battle.AttackerFactionId, zone.Id);
                    if (!pedHandle.IsValid) continue;

                    ConfigureAttacker(pedHandle.Handle, tierConfig, zone.Center, zone.Radius);
                    _pedBlipService.CreateBlipForPed(pedHandle.Handle, BlipColor.Red);

                    _spawnedPedTierByZone[zone.Id][pedHandle.Handle] = tier;
                    totalSpawned++;
                }
            }

            FileLogger.Combat($"BattleAttackerManager: Spawned {totalSpawned} attackers in {zone.Id}");
        }

        /// <summary>
        /// Called when the player exits a zone. Despawns all attacker NPCs.
        /// </summary>
        public void OnPlayerZoneExited(Zone zone)
        {
            if (zone == null) return;

            if (_currentBattleZoneId == zone.Id)
                _currentBattleZoneId = null;

            if (!_spawnedPedTierByZone.TryGetValue(zone.Id, out var pedTiers)) return;

            foreach (var pedHandle in pedTiers.Keys)
            {
                _pedBlipService.RemoveBlipForPed(pedHandle);
                _gameBridge.DeletePed(pedHandle);
            }
            _spawnedPedTierByZone.Remove(zone.Id);
        }

        /// <summary>
        /// Despawns all attacker NPCs across all zones.
        /// </summary>
        public void DespawnAllAttackers()
        {
            foreach (var zonePedTiers in _spawnedPedTierByZone.Values)
            {
                foreach (var pedHandle in zonePedTiers.Keys)
                {
                    _pedBlipService.RemoveBlipForPed(pedHandle);
                    _gameBridge.DeletePed(pedHandle);
                }
            }
            _spawnedPedTierByZone.Clear();
            _currentBattleZoneId = null;
        }

        /// <summary>
        /// Gets the number of spawned attackers for a zone.
        /// </summary>
        public int GetSpawnedAttackerCount(string zoneId)
        {
            return _spawnedPedTierByZone.TryGetValue(zoneId, out var pedTiers) ? pedTiers.Count : 0;
        }

        /// <summary>
        /// Updates attacker state. Checks for deaths and reports to battle manager.
        /// </summary>
        public void Update()
        {
            if (_currentBattleZoneId == null) return;

            var battle = _activeBattleManager.GetBattleForZone(_currentBattleZoneId);
            if (battle == null) return;

            var deadPeds = new List<(string zoneId, int pedHandle, DefenderTier tier)>();

            foreach (var kvp in _spawnedPedTierByZone)
            {
                var zoneId = kvp.Key;
                var pedTiers = kvp.Value;

                foreach (var pedKvp in pedTiers)
                {
                    var pedHandle = pedKvp.Key;
                    var tier = pedKvp.Value;

                    if (!_gameBridge.IsPedAlive(pedHandle))
                    {
                        deadPeds.Add((zoneId, pedHandle, tier));
                    }
                }
            }

            foreach (var (zoneId, pedHandle, tier) in deadPeds)
            {
                HandleAttackerDeath(zoneId, pedHandle, tier, battle.AttackerFactionId);
            }
        }

        private void HandleAttackerDeath(string zoneId, int pedHandle, DefenderTier tier, string attackerFactionId)
        {
            FileLogger.Combat($"BattleAttackerManager: Attacker died in {zoneId}, tier={tier}");

            if (_spawnedPedTierByZone.TryGetValue(zoneId, out var pedTiers))
            {
                pedTiers.Remove(pedHandle);
            }

            _pedBlipService.RemoveBlipForPed(pedHandle);
            _gameBridge.DeletePed(pedHandle);

            // Report kill to active battle manager
            _activeBattleManager.ReportTroopKilled(zoneId, attackerFactionId, tier);
        }

        private Vector3 CalculateRandomSpawnPosition(Vector3 center, float zoneRadius, Random random)
        {
            var angle = random.NextDouble() * 2 * Math.PI;
            var minRadius = zoneRadius * MinSpawnRadiusFraction;
            var distance = minRadius + (float)(random.NextDouble() * (zoneRadius - minRadius));
            var x = center.X + (float)(Math.Cos(angle) * distance);
            var y = center.Y + (float)(Math.Sin(angle) * distance);
            var z = _gameBridge.GetGroundZ(x, y, center.Z);
            return new Vector3(x, y, z);
        }

        private void ConfigureAttacker(int pedHandle, DefenderTierConfig tierConfig, Vector3 zoneCenter, float wanderRadius)
        {
            _gameBridge.GivePedWeapon(pedHandle, "weapon_pistol");
            _gameBridge.GivePedWeapon(pedHandle, tierConfig.Weapon);
            _gameBridge.SetPedAccuracy(pedHandle, tierConfig.Accuracy);
            _gameBridge.SetPedArmor(pedHandle, tierConfig.Armor);
            _gameBridge.SetPedHealth(pedHandle, tierConfig.Health);
            _gameBridge.SetPedCombatAttributes(pedHandle, canUseCover: true, willFightArmedPeds: true);
            _gameBridge.SetPedAsHostileWanderer(pedHandle);
            _gameBridge.SetPedToAttackPlayer(pedHandle);
        }
    }
}
```

**Step 4: Run tests to verify they pass**

Run: `dotnet test tests/FactionWars.Tests --filter "FullyQualifiedName~BattleAttackerManagerTests" -v n`
Expected: All 3 tests pass

**Step 5: Commit**

```bash
git add src/FactionWars/ScriptHookV/Managers/BattleAttackerManager.cs tests/FactionWars.Tests/Unit/ScriptHookV/BattleAttackerManagerTests.cs
git commit -m "feat: create BattleAttackerManager for spawning attackers in player's defending zone"
```

---

## Task 5: Integrate BattleAttackerManager into GameLoopController

**Files:**
- Modify: `src/FactionWars/ScriptHookV/GameLoopController.cs`

**Step 1: Add field declaration**

Add after line 49 (`private EnemyDefenderManager? _enemyDefenderManager;`):

```csharp
private BattleAttackerManager? _battleAttackerManager;
```

**Step 2: Initialize in InitializeGameData**

Add after line 583 (after EnemyDefenderManager initialization):

```csharp
// Initialize battle attacker manager for spawning attackers when player defends their zone
_battleAttackerManager = new BattleAttackerManager(
    _gameBridge,
    _activeBattleManager,
    pedSpawningService,
    defenderTierService,
    pedBlipService,
    _zoneService,
    CurrentPlayerFactionId ?? "");
```

**Step 3: Subscribe to zone events**

Add after line 646 (after `_territoryManager.ZoneExited += OnZoneExited;`):

```csharp
// Subscribe battle attacker manager to zone events
_territoryManager.ZoneEntered += (sender, zone) => _battleAttackerManager?.OnPlayerZoneEntered(zone);
_territoryManager.ZoneExited += (sender, zone) => _battleAttackerManager?.OnPlayerZoneExited(zone);
```

**Step 4: Add Update call in OnTick**

Add after line 345 (after enemy defender manager update):

```csharp
// Update battle attacker manager (death detection for attackers in player's defending zone)
_battleAttackerManager?.Update();
```

**Step 5: Handle character switch**

Add in `HandleCharacterSwitched` method after line 895 (after `_friendlyDefenderManager.SetPlayerFaction(newFactionId);`):

```csharp
// Update battle attacker manager faction
if (!string.IsNullOrEmpty(newFactionId))
{
    _battleAttackerManager?.SetPlayerFaction(newFactionId);
}
```

**Step 6: Clean up in OnAbort**

Add after line 868 (after enemy defender manager cleanup):

```csharp
// Clean up battle attacker manager
_battleAttackerManager?.DespawnAllAttackers();
_battleAttackerManager = null;
```

**Step 7: Run all tests**

Run: `dotnet test tests/FactionWars.Tests -v n`
Expected: All tests pass

**Step 8: Commit**

```bash
git add src/FactionWars/ScriptHookV/GameLoopController.cs
git commit -m "feat: integrate BattleAttackerManager into GameLoopController"
```

---

## Task 6: Deploy and verify fixes

**Step 1: Build the project**

Run: `dotnet build src/FactionWars -c Debug`
Expected: Build succeeded

**Step 2: Deploy to GTA V**

Run: `cp "C:/Users/ryan7/programming/gtav-factions/src/FactionWars/bin/Debug/net48/FactionWars.dll" "E:/SteamLibrary/steamapps/common/Grand Theft Auto V/scripts/"`

**Step 3: Test Fix 1 - Battle HUD updates when allocating troops**

1. Start game and wait for AI to attack one of your zones
2. Watch Battle HUD showing the troop counts
3. Open Zone Management menu and allocate troops to that zone
4. Verify Battle HUD immediately shows increased defender count

**Step 4: Test Fix 2 - Attackers spawn when entering your zone under attack**

1. Wait for AI to attack one of your zones (watch for Battle HUD)
2. Travel to that zone
3. Verify red-blipped enemy attackers spawn and attack you
4. Kill attackers and verify Battle HUD attacker count decreases

**Step 5: Commit final verification**

```bash
git add -A
git commit -m "fix: resolve battle desync issues with HUD updates and attacker spawning"
```

---

## Task 7: Fix FriendlyDefenderManager to report kills to ActiveBattleManager

> **Status:** Implemented on 2026-01-22

**Problem:** When friendly defenders die during an active battle:
- Territory Indicator (bottom-right) shows decreased count (reads spawned ped count)
- Battle HUD shows unchanged count (reads battle state)
- This desync occurs because `FriendlyDefenderManager` doesn't call `ReportTroopKilled()`

**Root Cause:**
- `BattleAttackerManager` calls `ReportTroopKilled()` for attacker deaths ✓
- `EnemyDefenderManager` calls `ReportTroopKilled()` for enemy defender deaths ✓
- `FriendlyDefenderManager` does NOT call `ReportTroopKilled()` ✗

**Files Modified:**
- `src/FactionWars/ScriptHookV/Managers/FriendlyDefenderManager.cs`
  - Added `IActiveBattleManager?` as optional constructor parameter
  - Added call to `ReportTroopKilled()` in `HandleDefenderDeath()` when battle is active and player is present
- `src/FactionWars/ScriptHookV/GameLoopController.cs`
  - Moved `_activeBattleManager` resolution earlier so it can be passed to `FriendlyDefenderManager`
  - Pass `_activeBattleManager` to `FriendlyDefenderManager` constructor
- `tests/FactionWars.Tests/Unit/ScriptHookV/Managers/FriendlyDefenderManagerDeathTests.cs`
  - Added test: `Update_WhenDefenderDiesDuringActiveBattle_ShouldReportTroopKilled`

**Test:** All 30 FriendlyDefenderManager tests pass
