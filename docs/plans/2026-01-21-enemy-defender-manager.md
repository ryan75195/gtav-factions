# Enemy Defender Manager Implementation Plan

> **For Claude:** REQUIRED SUB-SKILL: Use superpowers:executing-plans to implement this plan task-by-task.

**Goal:** Create an EnemyDefenderManager that spawns enemy defenders when player enters enemy territory, with max 12 active, reserve replacement, ground-level spawning, and wander-but-engage behavior.

**Architecture:** Mirror FriendlyDefenderManager for enemy zones. Add GetGroundZ for ground-level spawning. Add SetPedAsHostileWanderer for wander + engage behavior. Enemy defenders use zone allocations and spawn replacements from reserves when killed.

**Tech Stack:** C#, .NET Framework 4.8, ScriptHookVDotNet3

---

### Task 1: Add GetGroundZ to GameBridge

**Files:**
- Modify: `src/FactionWars/Core/Interfaces/IGameBridge.cs`
- Modify: `src/FactionWars/ScriptHookV/GameBridge.cs`
- Modify: `src/FactionWars/Core/Utils/MockGameBridge.cs`

**Step 1: Add interface method to IGameBridge**

Add after line ~280 in `src/FactionWars/Core/Interfaces/IGameBridge.cs`:

```csharp
/// <summary>
/// Gets the ground Z coordinate at the specified X/Y position.
/// Uses GTA V's GET_GROUND_Z_FOR_3D_COORD native.
/// </summary>
/// <param name="x">X coordinate.</param>
/// <param name="y">Y coordinate.</param>
/// <param name="z">Starting Z coordinate for search.</param>
/// <returns>The ground Z coordinate, or the input Z if ground not found.</returns>
float GetGroundZ(float x, float y, float z);
```

**Step 2: Implement in GameBridge**

Add to `src/FactionWars/ScriptHookV/GameBridge.cs`:

```csharp
/// <inheritdoc />
public float GetGroundZ(float x, float y, float z)
{
    try
    {
        float groundZ = z;
        bool found = Function.Call<bool>(
            Hash.GET_GROUND_Z_FOR_3D_COORD,
            x, y, z + 100f,  // Start search from above
            new OutputArgument(),
            false,  // ignoreWater
            false); // ignoreDistToWaterLevelCheck

        if (found)
        {
            // Re-call to get actual value via output argument
            var outArg = new OutputArgument();
            Function.Call<bool>(Hash.GET_GROUND_Z_FOR_3D_COORD, x, y, z + 100f, outArg, false, false);
            groundZ = outArg.GetResult<float>();
        }

        return groundZ;
    }
    catch
    {
        return z;
    }
}
```

**Step 3: Add mock implementation**

Add to `src/FactionWars/Core/Utils/MockGameBridge.cs`:

```csharp
public float GetGroundZ(float x, float y, float z)
{
    return z; // Mock just returns input Z
}
```

**Step 4: Build to verify**

Run: `dotnet build src/FactionWars/FactionWars.csproj --no-restore`
Expected: Build succeeded

**Step 5: Commit**

```bash
git add -A && git commit -m "feat: add GetGroundZ to GameBridge for ground-level spawning"
```

---

### Task 2: Add SetPedAsHostileWanderer to GameBridge

**Files:**
- Modify: `src/FactionWars/Core/Interfaces/IGameBridge.cs`
- Modify: `src/FactionWars/ScriptHookV/GameBridge.cs`
- Modify: `src/FactionWars/Core/Utils/MockGameBridge.cs`

**Step 1: Add interface method**

Add to `src/FactionWars/Core/Interfaces/IGameBridge.cs`:

```csharp
/// <summary>
/// Sets a ped as hostile to the player's faction but allows wandering.
/// The ped will engage player and player's followers on sight while patrolling.
/// </summary>
/// <param name="pedHandle">The ped handle.</param>
void SetPedAsHostileWanderer(int pedHandle);
```

**Step 2: Implement in GameBridge**

Add to `src/FactionWars/ScriptHookV/GameBridge.cs`:

```csharp
/// <inheritdoc />
public void SetPedAsHostileWanderer(int pedHandle)
{
    try
    {
        var ped = Entity.FromHandle(pedHandle) as Ped;
        if (ped == null || !ped.Exists())
            return;

        var player = Game.Player.Character;
        if (player == null || !player.Exists())
            return;

        // Create or get enemy relationship group
        var enemyGroup = World.AddRelationshipGroup("DEFENDER_ENEMIES");
        var playerGroup = player.RelationshipGroup;

        // Set ped to enemy group
        ped.RelationshipGroup = enemyGroup;

        // Make the groups hate each other (bidirectional)
        enemyGroup.SetRelationshipBetweenGroups(playerGroup, Relationship.Hate, true);

        // Configure ped for patrol + engage behavior
        ped.IsPersistent = true;
        ped.KeepTaskWhenMarkedAsNoLongerNeeded = true;
        ped.BlockPermanentEvents = false; // Allow reaction to enemies

        // Set combat attributes
        Function.Call(Hash.SET_PED_COMBAT_ATTRIBUTES, ped.Handle, 46, true); // CanFightArmedPedsWhenNotArmed
        Function.Call(Hash.SET_PED_COMBAT_ATTRIBUTES, ped.Handle, 5, true);  // CanUseCover
        Function.Call(Hash.SET_PED_COMBAT_ATTRIBUTES, ped.Handle, 2, true);  // CanDoDrivebys

        // Set combat ability and range
        Function.Call(Hash.SET_PED_COMBAT_ABILITY, ped.Handle, 2); // Professional
        Function.Call(Hash.SET_PED_COMBAT_RANGE, ped.Handle, 2);   // Far

        // Set alertness high so they notice enemies while wandering
        Function.Call(Hash.SET_PED_ALERTNESS, ped.Handle, 3); // Full alertness

        // DO NOT give Combat task - let them wander and engage via relationship system
    }
    catch (Exception ex)
    {
        FileLogger.Error("SetPedAsHostileWanderer exception", ex);
    }
}
```

**Step 3: Add mock implementation**

Add to `src/FactionWars/Core/Utils/MockGameBridge.cs`:

```csharp
public void SetPedAsHostileWanderer(int pedHandle)
{
    // Mock - no-op
}
```

**Step 4: Build to verify**

Run: `dotnet build src/FactionWars/FactionWars.csproj --no-restore`
Expected: Build succeeded

**Step 5: Commit**

```bash
git add -A && git commit -m "feat: add SetPedAsHostileWanderer for patrol + engage behavior"
```

---

### Task 3: Update FriendlyDefenderManager for Ground-Level Spawning

**Files:**
- Modify: `src/FactionWars/ScriptHookV/Managers/FriendlyDefenderManager.cs`

**Step 1: Update CalculateSpawnPosition to use ground Z**

Replace the `CalculateSpawnPosition` method:

```csharp
/// <summary>
/// Calculates the spawn position for a defender based on the zone center and
/// the defender's index in the spawn sequence. Ensures ground-level spawning.
/// </summary>
private Vector3 CalculateSpawnPosition(Vector3 center, int index, int totalCount)
{
    var angle = (2 * Math.PI * index) / Math.Max(totalCount, 1);
    var distance = 30f + (index % 3) * 10f;
    var x = center.X + (float)(Math.Cos(angle) * distance);
    var y = center.Y + (float)(Math.Sin(angle) * distance);
    var z = _gameBridge.GetGroundZ(x, y, center.Z);
    return new Vector3(x, y, z);
}
```

**Step 2: Build to verify**

Run: `dotnet build src/FactionWars/FactionWars.csproj --no-restore`
Expected: Build succeeded

**Step 3: Commit**

```bash
git add -A && git commit -m "fix: use ground-level spawning for friendly defenders"
```

---

### Task 4: Create EnemyDefenderManager

**Files:**
- Create: `src/FactionWars/ScriptHookV/Managers/EnemyDefenderManager.cs`

**Step 1: Create the EnemyDefenderManager class**

Create `src/FactionWars/ScriptHookV/Managers/EnemyDefenderManager.cs`:

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
    /// Manages enemy defenders that spawn when the player enters enemy territory.
    /// Defenders patrol the zone and engage the player and player's troops on sight.
    /// Supports death detection, replacement spawning from reserves, and ground-level spawning.
    /// </summary>
    public class EnemyDefenderManager
    {
        private readonly IGameBridge _gameBridge;
        private readonly IZoneDefenderAllocationService _allocationService;
        private readonly IPedSpawningService _pedSpawningService;
        private readonly IDefenderTierService _defenderTierService;
        private readonly IPedBlipService _pedBlipService;
        private readonly IZoneService _zoneService;

        private readonly Dictionary<DefenderTier, string> _modelsByTier;
        private readonly Dictionary<string, Dictionary<int, DefenderTier>> _spawnedPedTierByZone;
        private string? _currentEnemyZoneId;

        private const float WanderRadius = 40f;
        private const float MinSpawnRadius = 30f;
        private const float MaxSpawnRadius = 50f;

        /// <summary>
        /// Maximum number of enemy defenders that can be spawned at once per zone.
        /// </summary>
        public const int MaxSpawnedDefenders = 12;

        /// <summary>
        /// Creates a new EnemyDefenderManager instance.
        /// </summary>
        public EnemyDefenderManager(
            IGameBridge gameBridge,
            IZoneDefenderAllocationService allocationService,
            IPedSpawningService pedSpawningService,
            IDefenderTierService defenderTierService,
            IPedBlipService pedBlipService,
            IZoneService zoneService)
        {
            _gameBridge = gameBridge ?? throw new ArgumentNullException(nameof(gameBridge));
            _allocationService = allocationService ?? throw new ArgumentNullException(nameof(allocationService));
            _pedSpawningService = pedSpawningService ?? throw new ArgumentNullException(nameof(pedSpawningService));
            _defenderTierService = defenderTierService ?? throw new ArgumentNullException(nameof(defenderTierService));
            _pedBlipService = pedBlipService ?? throw new ArgumentNullException(nameof(pedBlipService));
            _zoneService = zoneService ?? throw new ArgumentNullException(nameof(zoneService));

            // Enemy faction ped models (different from player's faction)
            _modelsByTier = new Dictionary<DefenderTier, string>
            {
                { DefenderTier.Basic, "g_m_y_famca_01" },
                { DefenderTier.Medium, "g_m_y_famdnf_01" },
                { DefenderTier.Heavy, "g_m_y_famfor_01" }
            };

            _spawnedPedTierByZone = new Dictionary<string, Dictionary<int, DefenderTier>>();
        }

        /// <summary>
        /// Called when the player enters an enemy zone. Spawns enemy defenders
        /// from the zone's allocation. Respects MaxSpawnedDefenders limit.
        /// </summary>
        /// <param name="zone">The enemy zone that was entered.</param>
        /// <param name="enemyFactionId">The faction that owns the zone.</param>
        public void OnEnemyZoneEntered(Zone zone, string enemyFactionId)
        {
            if (zone == null || string.IsNullOrEmpty(enemyFactionId)) return;

            FileLogger.Combat($"EnemyDefenderManager: Player entered enemy zone {zone.Id} owned by {enemyFactionId}");

            _currentEnemyZoneId = zone.Id;

            var allocation = _allocationService.GetAllocation(enemyFactionId, zone.Id);
            if (allocation == null)
            {
                FileLogger.Combat($"EnemyDefenderManager: No allocation found for {enemyFactionId} in {zone.Id}");
                return;
            }

            // Initialize tracking for this zone
            if (!_spawnedPedTierByZone.ContainsKey(zone.Id))
            {
                _spawnedPedTierByZone[zone.Id] = new Dictionary<int, DefenderTier>();
            }

            var totalSpawned = 0;
            var random = new Random();

            foreach (DefenderTier tier in new[] { DefenderTier.Heavy, DefenderTier.Medium, DefenderTier.Basic })
            {
                var count = allocation.GetTroopCount(tier);
                if (count <= 0) continue;

                var model = _modelsByTier.TryGetValue(tier, out var m) ? m : "g_m_y_famca_01";
                var tierConfig = _defenderTierService.GetTierConfig(tier);

                for (int i = 0; i < count && totalSpawned < MaxSpawnedDefenders; i++)
                {
                    if (!_pedSpawningService.CanSpawn()) break;

                    var spawnPos = CalculateRandomSpawnPosition(zone.Center, random);
                    var pedHandle = _pedSpawningService.SpawnPed(model, spawnPos, enemyFactionId, zone.Id);
                    if (!pedHandle.IsValid) continue;

                    // Configure as hostile wanderer
                    ConfigureEnemyDefender(pedHandle.Handle, tierConfig, zone.Center);
                    _pedBlipService.CreateBlipForPed(pedHandle.Handle, BlipColor.Red);

                    // Track ped with its tier
                    _spawnedPedTierByZone[zone.Id][pedHandle.Handle] = tier;
                    totalSpawned++;
                }
            }

            FileLogger.Combat($"EnemyDefenderManager: Spawned {totalSpawned} enemy defenders in {zone.Id}");
        }

        /// <summary>
        /// Called when the player exits an enemy zone. Despawns all enemy defenders.
        /// </summary>
        /// <param name="zone">The zone that was exited.</param>
        public void OnEnemyZoneExited(Zone zone)
        {
            if (zone == null) return;

            FileLogger.Combat($"EnemyDefenderManager: Player exited enemy zone {zone.Id}");

            if (_currentEnemyZoneId == zone.Id)
                _currentEnemyZoneId = null;

            if (!_spawnedPedTierByZone.TryGetValue(zone.Id, out var pedTiers)) return;

            foreach (var pedHandle in pedTiers.Keys)
            {
                _pedBlipService.RemoveBlipForPed(pedHandle);
                _gameBridge.DeletePed(pedHandle);
            }
            _spawnedPedTierByZone.Remove(zone.Id);
        }

        /// <summary>
        /// Despawns all enemy defenders across all zones.
        /// </summary>
        public void DespawnAllDefenders()
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
            _currentEnemyZoneId = null;
        }

        /// <summary>
        /// Gets the number of spawned enemy defenders for a specific zone.
        /// </summary>
        public int GetSpawnedDefenderCount(string zoneId)
        {
            return _spawnedPedTierByZone.TryGetValue(zoneId, out var pedTiers) ? pedTiers.Count : 0;
        }

        /// <summary>
        /// Gets the number of spawned defenders of a specific tier in a zone.
        /// </summary>
        public int GetSpawnedCountByTier(string zoneId, DefenderTier tier)
        {
            if (!_spawnedPedTierByZone.TryGetValue(zoneId, out var pedTiers))
                return 0;

            return pedTiers.Values.Count(t => t == tier);
        }

        /// <summary>
        /// Updates enemy defender state. Should be called each game tick.
        /// Checks for deaths, handles cleanup, and spawns replacements.
        /// </summary>
        /// <param name="enemyFactionId">The enemy faction ID for the current zone.</param>
        public void Update(string? enemyFactionId)
        {
            if (_currentEnemyZoneId == null || string.IsNullOrEmpty(enemyFactionId)) return;

            var deadPeds = new List<(string zoneId, int pedHandle, DefenderTier tier)>();

            // Check all spawned enemy defenders for death
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

            // Process each dead defender
            foreach (var (zoneId, pedHandle, tier) in deadPeds)
            {
                HandleDefenderDeath(zoneId, pedHandle, tier, enemyFactionId);
            }
        }

        /// <summary>
        /// Handles the death of an enemy defender.
        /// </summary>
        private void HandleDefenderDeath(string zoneId, int pedHandle, DefenderTier tier, string enemyFactionId)
        {
            FileLogger.Combat($"EnemyDefenderManager: Defender died in {zoneId}, tier={tier}");

            // Remove from tracking
            if (_spawnedPedTierByZone.TryGetValue(zoneId, out var pedTiers))
            {
                pedTiers.Remove(pedHandle);
            }

            // Remove blip and delete ped
            _pedBlipService.RemoveBlipForPed(pedHandle);
            _gameBridge.DeletePed(pedHandle);

            // Get allocation before decrementing
            var allocation = _allocationService.GetAllocation(enemyFactionId, zoneId);

            // Try to spawn replacement first
            bool replacementSpawned = TrySpawnReplacement(zoneId, tier, enemyFactionId, allocation);

            // Only decrement allocation if no replacement spawned
            if (!replacementSpawned && allocation != null)
            {
                allocation.RemoveTroops(tier, 1);
                FileLogger.Combat($"EnemyDefenderManager: Decremented {tier} allocation in {zoneId}");
            }
        }

        /// <summary>
        /// Tries to spawn a replacement defender from reserves.
        /// </summary>
        private bool TrySpawnReplacement(string zoneId, DefenderTier preferredTier, string enemyFactionId, ZoneDefenderAllocation? allocation)
        {
            if (allocation == null) return false;

            var currentSpawned = GetSpawnedDefenderCount(zoneId);
            if (currentSpawned >= MaxSpawnedDefenders) return false;

            var zone = _zoneService.GetZone(zoneId);
            if (zone == null) return false;

            // Try preferred tier first
            var allocatedCount = allocation.GetTroopCount(preferredTier);
            var spawnedOfTier = GetSpawnedCountByTier(zoneId, preferredTier);

            if (allocatedCount > spawnedOfTier)
            {
                SpawnSingleDefender(zoneId, preferredTier, enemyFactionId, zone.Center);
                FileLogger.Combat($"EnemyDefenderManager: Spawned replacement {preferredTier} in {zoneId}");
                return true;
            }

            // Try other tiers (highest first)
            foreach (var tier in new[] { DefenderTier.Heavy, DefenderTier.Medium, DefenderTier.Basic })
            {
                if (tier == preferredTier) continue;

                allocatedCount = allocation.GetTroopCount(tier);
                spawnedOfTier = GetSpawnedCountByTier(zoneId, tier);

                if (allocatedCount > spawnedOfTier)
                {
                    SpawnSingleDefender(zoneId, tier, enemyFactionId, zone.Center);
                    FileLogger.Combat($"EnemyDefenderManager: Spawned replacement {tier} in {zoneId}");
                    return true;
                }
            }

            return false;
        }

        /// <summary>
        /// Spawns a single enemy defender.
        /// </summary>
        private void SpawnSingleDefender(string zoneId, DefenderTier tier, string enemyFactionId, Vector3 center)
        {
            if (!_pedSpawningService.CanSpawn()) return;

            var model = _modelsByTier.TryGetValue(tier, out var m) ? m : "g_m_y_famca_01";
            var tierConfig = _defenderTierService.GetTierConfig(tier);
            var random = new Random();

            var spawnPos = CalculateRandomSpawnPosition(center, random);
            var pedHandle = _pedSpawningService.SpawnPed(model, spawnPos, enemyFactionId, zoneId);

            if (!pedHandle.IsValid) return;

            ConfigureEnemyDefender(pedHandle.Handle, tierConfig, center);
            _pedBlipService.CreateBlipForPed(pedHandle.Handle, BlipColor.Red);

            if (!_spawnedPedTierByZone.ContainsKey(zoneId))
            {
                _spawnedPedTierByZone[zoneId] = new Dictionary<int, DefenderTier>();
            }
            _spawnedPedTierByZone[zoneId][pedHandle.Handle] = tier;
        }

        /// <summary>
        /// Calculates a random spawn position around the zone center at ground level.
        /// </summary>
        private Vector3 CalculateRandomSpawnPosition(Vector3 center, Random random)
        {
            var angle = random.NextDouble() * 2 * Math.PI;
            var distance = MinSpawnRadius + (float)(random.NextDouble() * (MaxSpawnRadius - MinSpawnRadius));
            var x = center.X + (float)(Math.Cos(angle) * distance);
            var y = center.Y + (float)(Math.Sin(angle) * distance);
            var z = _gameBridge.GetGroundZ(x, y, center.Z);
            return new Vector3(x, y, z);
        }

        /// <summary>
        /// Configures an enemy defender's combat attributes and behavior.
        /// </summary>
        private void ConfigureEnemyDefender(int pedHandle, DefenderTierConfig tierConfig, Vector3 zoneCenter)
        {
            // Give weapons
            _gameBridge.GivePedWeapon(pedHandle, "weapon_pistol");
            _gameBridge.GivePedWeapon(pedHandle, tierConfig.Weapon);
            _gameBridge.SetPedAccuracy(pedHandle, tierConfig.Accuracy);
            _gameBridge.SetPedArmor(pedHandle, tierConfig.Armor);
            _gameBridge.SetPedHealth(pedHandle, tierConfig.Health);
            _gameBridge.SetPedCombatAttributes(pedHandle, canUseCover: true, willFightArmedPeds: true);

            // Set as hostile wanderer - will engage player and followers on sight
            _gameBridge.SetPedAsHostileWanderer(pedHandle);

            // Give wander task
            _gameBridge.TaskPedWanderInArea(pedHandle, zoneCenter, WanderRadius);
        }
    }
}
```

**Step 2: Build to verify**

Run: `dotnet build src/FactionWars/FactionWars.csproj --no-restore`
Expected: Build succeeded

**Step 3: Commit**

```bash
git add -A && git commit -m "feat: create EnemyDefenderManager with reserve replacement and ground-level spawning"
```

---

### Task 5: Register EnemyDefenderManager in ServiceContainerFactory

**Files:**
- Modify: `src/FactionWars/ScriptHookV/ServiceContainerFactory.cs`

**Step 1: Add registration in RegisterUIServices or create new section**

Add after the FriendlyDefenderManager-related services (or in a logical location):

```csharp
// Note: EnemyDefenderManager is created in GameLoopController with runtime dependencies
// (playerFactionId is needed at runtime, not at container creation time)
```

Actually, looking at FriendlyDefenderManager, it's created in GameLoopController, not the container. We'll do the same for EnemyDefenderManager.

**Step 2: Commit (skip if no changes needed)**

No changes needed - EnemyDefenderManager will be created in GameLoopController like FriendlyDefenderManager.

---

### Task 6: Integrate EnemyDefenderManager in GameLoopController

**Files:**
- Modify: `src/FactionWars/ScriptHookV/GameLoopController.cs`

**Step 1: Add field for EnemyDefenderManager**

Add near the FriendlyDefenderManager field:

```csharp
private EnemyDefenderManager? _enemyDefenderManager;
```

**Step 2: Initialize EnemyDefenderManager in Initialize method**

Find where FriendlyDefenderManager is created and add nearby:

```csharp
_enemyDefenderManager = new EnemyDefenderManager(
    _gameBridge,
    _container.Resolve<IZoneDefenderAllocationService>(),
    _container.Resolve<IPedSpawningService>(),
    _container.Resolve<IDefenderTierService>(),
    _container.Resolve<IPedBlipService>(),
    _container.Resolve<IZoneService>());
```

**Step 3: Call EnemyDefenderManager.Update in the game loop**

Find where FriendlyDefenderManager.Update is called and add:

```csharp
// Update enemy defenders (pass the enemy faction ID if in enemy zone)
var enemyFactionId = _currentZone?.OwnerFactionId;
if (enemyFactionId != null && enemyFactionId != _playerFactionId)
{
    _enemyDefenderManager?.Update(enemyFactionId);
}
```

**Step 4: Call OnEnemyZoneEntered when entering enemy zone**

Find zone entry handling and add logic for enemy zones:

```csharp
// When entering enemy zone
if (zone.OwnerFactionId != null && zone.OwnerFactionId != _playerFactionId)
{
    _enemyDefenderManager?.OnEnemyZoneEntered(zone, zone.OwnerFactionId);
}
```

**Step 5: Call OnEnemyZoneExited when exiting enemy zone**

Find zone exit handling and add:

```csharp
// When exiting enemy zone
if (previousZone != null && previousZone.OwnerFactionId != null && previousZone.OwnerFactionId != _playerFactionId)
{
    _enemyDefenderManager?.OnEnemyZoneExited(previousZone);
}
```

**Step 6: Despawn enemy defenders on combat end or zone transition**

Ensure enemy defenders are cleaned up appropriately when combat ends.

**Step 7: Build to verify**

Run: `dotnet build src/FactionWars/FactionWars.csproj --no-restore`
Expected: Build succeeded

**Step 8: Commit**

```bash
git add -A && git commit -m "feat: integrate EnemyDefenderManager into GameLoopController"
```

---

### Task 7: Test and Deploy

**Step 1: Build the full solution**

Run: `dotnet build src/FactionWars/FactionWars.csproj --no-restore`
Expected: Build succeeded with no errors

**Step 2: Deploy to GTA V**

```bash
cp "C:/Users/ryan7/programming/gtav-factions/src/FactionWars/bin/Debug/net48/FactionWars.dll" "E:/SteamLibrary/steamapps/common/Grand Theft Auto V/scripts/"
```

**Step 3: Test in-game**

1. Enter an enemy faction's territory
2. Verify enemy defenders spawn spread around zone (not on rooftops)
3. Verify they wander but engage player on sight
4. Verify they engage player's followers too
5. Kill some defenders and verify replacements spawn from reserves
6. Exit zone and verify defenders despawn

**Step 4: Final commit**

```bash
git add -A && git commit -m "feat: complete enemy defender manager implementation"
```
