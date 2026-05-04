using System;
using System.Collections.Generic;
using System.Linq;
using FactionWars.Combat.Models;
using FactionWars.Core.Interfaces;
using FactionWars.Core.Models;
using FactionWars.ScriptHookV.Logging;
using FactionWars.ScriptHookV.Services;
using FactionWars.Territory.Models;

namespace FactionWars.ScriptHookV.Managers
{
    public partial class FriendlyDefenderManager
    {
        public void Update()
        {
            var newlyDeadPeds = new List<(string zoneId, int pedHandle, DefenderTier tier)>();
            var streamedOutPeds = new List<(string zoneId, int pedHandle)>();
            var currentGameTime = _gameBridge.GetGameTime();

            // Check all spawned defenders for death
            foreach (var kvp in _spawnedPedTierByZone)
            {
                var zoneId = kvp.Key;
                var pedTiers = kvp.Value;

                foreach (var pedKvp in pedTiers)
                {
                    var pedHandle = pedKvp.Key;
                    var tier = pedKvp.Value;

                    // Skip if already tracked as corpse
                    if (_corpseDeathTimes.ContainsKey(pedHandle))
                        continue;

                    // Distinguish "ped culled by GTA's streaming/population manager"
                    // (entity gone) from "ped died in combat" (entity still here, just
                    // dead). Decrementing allocation for streamed-out peds would shed
                    // troops we never actually lost.
                    if (!_gameBridge.DoesPedExist(pedHandle))
                    {
                        streamedOutPeds.Add((zoneId, pedHandle));
                    }
                    else if (!_gameBridge.IsPedAlive(pedHandle))
                    {
                        newlyDeadPeds.Add((zoneId, pedHandle, tier));
                    }
                }
            }

            // Quietly untrack peds the engine culled — no allocation change, no event.
            foreach (var (zoneId, pedHandle) in streamedOutPeds)
            {
                if (_spawnedPedTierByZone.TryGetValue(zoneId, out var pedTiers))
                {
                    pedTiers.Remove(pedHandle);
                }
                _pedBlipService.RemoveBlipForPed(pedHandle);
                _pedDespawnService.UntrackPed(pedHandle);
            }

            // Process each newly dead defender
            foreach (var (zoneId, pedHandle, tier) in newlyDeadPeds)
            {
                HandleDefenderDeath(zoneId, pedHandle, tier);
            }

            // Clean up corpses that have exceeded the delay
            CleanupExpiredCorpses(currentGameTime);

            EnforceZoneLeash(currentGameTime);
        }

        /// <summary>
        /// Every <see cref="ZoneLeashEnforcer.LeashCheckIntervalMs"/>, scan all
        /// tracked defenders. Any whose distance from their zone center exceeds
        /// the hysteresis threshold gets its tasks cleared and a TaskGoToCoord
        /// back to a random point inside the inner half of the zone.
        /// </summary>
        private void EnforceZoneLeash(int currentGameTime)
        {
            if (currentGameTime - _lastLeashCheckMs < ZoneLeashEnforcer.LeashCheckIntervalMs)
                return;
            _lastLeashCheckMs = currentGameTime;

            foreach (var kvp in _spawnedPedTierByZone)
            {
                var zoneId = kvp.Key;
                var pedTiers = kvp.Value;

                var zone = _zoneService.GetZone(zoneId);
                if (zone == null)
                    continue;

                foreach (var pedHandle in pedTiers.Keys)
                {
                    if (_corpseDeathTimes.ContainsKey(pedHandle))
                        continue;
                    if (!_gameBridge.DoesPedExist(pedHandle) || !_gameBridge.IsPedAlive(pedHandle))
                        continue;

                    var pedPos = _gameBridge.GetPedPosition(pedHandle);
                    if (!ZoneLeashEnforcer.ShouldLeash(pedPos, zone.Center, zone.Radius))
                        continue;

                    var returnPoint = ZoneLeashEnforcer.PickReturnPoint(zone.Center, zone.Radius, _leashRandom);
                    _gameBridge.ClearPedTasks(pedHandle);
                    _gameBridge.TaskGoToCoord(pedHandle, returnPoint);
                    FileLogger.AI($"FriendlyDefenderManager: leashed ped {pedHandle} in zone {zoneId} from ({pedPos.X:F1},{pedPos.Y:F1}) back to ({returnPoint.X:F1},{returnPoint.Y:F1})");
                }
            }
        }

        /// <summary>
        /// Cleans up corpses that have exceeded the corpse delay time.
        /// </summary>
        private void CleanupExpiredCorpses(int currentGameTime)
        {
            var expiredCorpses = new List<int>();

            foreach (var kvp in _corpseDeathTimes)
            {
                var pedHandle = kvp.Key;
                var deathTime = kvp.Value;

                if (currentGameTime - deathTime >= CorpseDelayMs)
                {
                    expiredCorpses.Add(pedHandle);
                }
            }

            foreach (var pedHandle in expiredCorpses)
            {
                // Delete the visual entity (already untracked from pool on death)
                _pedDespawnService.DeletePedEntity(pedHandle);
                _corpseDeathTimes.Remove(pedHandle);
            }
        }

        /// <summary>
        /// Handles the death of a defender, including blip removal, allocation decrement,
        /// replacement spawning, and territory loss detection.
        /// Corpse cleanup is delayed for immersion.
        /// </summary>
        private void HandleDefenderDeath(string zoneId, int pedHandle, DefenderTier tier)
        {
            // Track death time for corpse cleanup (don't despawn yet - leave corpse visible)
            _corpseDeathTimes[pedHandle] = _gameBridge.GetGameTime();

            // Remove from active tracking (no longer counts toward spawned defenders)
            if (_spawnedPedTierByZone.TryGetValue(zoneId, out var pedTiers))
            {
                pedTiers.Remove(pedHandle);
            }

            // Remove blip immediately (dead peds shouldn't show on radar)
            _pedBlipService.RemoveBlipForPed(pedHandle);

            // IMPORTANT: Untrack from ped pool to free spawn slot (but keep corpse visible)
            _pedDespawnService.UntrackPed(pedHandle);

            var allocation = _allocationService.GetAllocation(_playerFactionId, zoneId);

            // ALWAYS decrement allocation when a defender dies (regardless of who killed them)
            // This must happen BEFORE checking for reserves, otherwise we create "phantom reserves"
            // where the spawned count decreases but allocation doesn't, making it look like reserves exist
            if (allocation != null)
            {
                allocation.RemoveTroops(tier, 1);
            }

            // Raise defender died event
            DefenderDied?.Invoke(this, new DefenderDiedEventArgs(zoneId, pedHandle, tier));

            // Try to spawn replacement from actual reserve (allocated > spawned means real reserves exist)
            TrySpawnReplacementFromReserve(zoneId, tier, allocation);

            // Check for territory loss (no spawned defenders AND no reserve)
            if (IsAllDefendersDead(zoneId, allocation))
            {
                HandleTerritoryLost(zoneId);
            }
        }

        /// <summary>
        /// Checks if all defenders are dead (no spawned and no reserve).
        /// </summary>
        private bool IsAllDefendersDead(string zoneId, ZoneDefenderAllocation? allocation)
        {
            var spawnedCount = GetSpawnedDefenderCount(zoneId);
            if (spawnedCount > 0) return false;

            if (allocation == null) return true;

            return allocation.TotalTroops == 0;
        }

        /// <summary>
        /// Handles territory loss when all defenders die.
        /// </summary>
        private void HandleTerritoryLost(string zoneId)
        {
            // Transfer zone to neutral
            _zoneService.TransferZoneOwnership(zoneId, null);

            // Raise event
            TerritoryLost?.Invoke(this, new TerritoryLostEventArgs(zoneId));
        }

        /// <summary>
        /// Tries to spawn a replacement defender from the reserve for a specific tier.
        /// Returns true if a replacement was spawned.
        /// </summary>
        private bool TrySpawnReplacementFromReserve(string zoneId, DefenderTier preferredTier, ZoneDefenderAllocation? allocation)
        {
            if (allocation == null) return false;

            var currentSpawned = GetSpawnedDefenderCount(zoneId);
            if (currentSpawned >= MaxSpawnedDefenders) return false;

            var zone = _zoneService.GetZone(zoneId);
            if (zone == null) return false;

            // First try the same tier as the one that died
            var allocatedCount = allocation.GetTroopCount(preferredTier);
            var spawnedOfTier = GetSpawnedCountByTier(zoneId, preferredTier);

            if (allocatedCount > spawnedOfTier)
            {
                SpawnSingleDefender(zoneId, preferredTier, zone.Center, zone.Radius);
                return true;
            }

            // If no reserve of that tier, try other tiers (highest first)
            var tiersToTry = new[] { DefenderTier.Heavy, DefenderTier.Medium, DefenderTier.Basic };
            foreach (var tier in tiersToTry)
            {
                if (tier == preferredTier) continue; // Already tried

                allocatedCount = allocation.GetTroopCount(tier);
                spawnedOfTier = GetSpawnedCountByTier(zoneId, tier);

                if (allocatedCount > spawnedOfTier)
                {
                    SpawnSingleDefender(zoneId, tier, zone.Center, zone.Radius);
                    return true;
                }
            }

            return false;
        }

        /// <summary>
        /// Spawns a single defender of the specified tier.
        /// Uses combat targeting if a battle is active in the zone.
        /// </summary>
        private void SpawnSingleDefender(string zoneId, DefenderTier tier, Vector3 center, float zoneRadius)
        {
            if (!_pedSpawningService.CanSpawn()) return;

            var model = FactionPedModels.GetModel(_playerFactionId, tier);
            var tierConfig = _defenderTierService.GetTierConfig(tier);

            var spawnPos = CalculateRandomSpawnPosition(center, zoneRadius);
            var pedHandle = _pedSpawningService.SpawnPed(model, spawnPos, _playerFactionId, zoneId);

            if (!pedHandle.IsValid) return;

            // Set friendly relationship with player
            _gameBridge.SetPedAsFriendly(pedHandle.Handle);
            ConfigureDefenderCombat(pedHandle.Handle, tierConfig);

            // Use combat targeting if battle is active, otherwise walking wander
            if (_zonesInBattle.Contains(zoneId))
            {
                ConfigureBattlePerception(pedHandle.Handle, zoneRadius);
                _gameBridge.TaskCombatHatedTargetsAroundPed(pedHandle.Handle, zoneRadius);
            }
            else
            {
                _gameBridge.TaskPedWanderInBoundedArea(pedHandle.Handle, center, zoneRadius);
            }

            _pedBlipService.CreateBlipForPed(pedHandle.Handle, BlipColor.LightBlue);

            // Track ped with its tier
            if (!_spawnedPedTierByZone.ContainsKey(zoneId))
            {
                _spawnedPedTierByZone[zoneId] = new Dictionary<int, DefenderTier>();
            }
            _spawnedPedTierByZone[zoneId][pedHandle.Handle] = tier;
        }

        /// <summary>
        /// Handles when troops are allocated to a zone. If the player is currently
        /// in that zone (and it's their own zone), spawns the new defenders immediately.
        /// Respects MaxSpawnedDefenders limit.
        /// </summary>
        /// <param name="factionId">The faction that allocated troops.</param>
        /// <param name="zoneId">The zone troops were allocated to.</param>
        /// <param name="tier">The tier of troops allocated.</param>
        /// <param name="count">The number of troops allocated.</param>
        /// <param name="zoneCenter">The center of the zone for spawn positioning.</param>
        /// <param name="zoneRadius">The radius of the zone for spawn distribution.</param>
        public void OnTroopsAllocated(string factionId, string zoneId, DefenderTier tier, int count, Vector3 zoneCenter, float zoneRadius)
        {
            // Only spawn if this is the player's faction and they're in this zone
            if (factionId != _playerFactionId) return;
            if (_currentZoneId != zoneId) return;

            var model = FactionPedModels.GetModel(_playerFactionId, tier);
            var tierConfig = _defenderTierService.GetTierConfig(tier);
            var inBattle = _zonesInBattle.Contains(zoneId);

            // Get existing spawned peds for this zone to calculate spawn positions
            if (!_spawnedPedTierByZone.TryGetValue(zoneId, out var existingPedTiers))
            {
                existingPedTiers = new Dictionary<int, DefenderTier>();
                _spawnedPedTierByZone[zoneId] = existingPedTiers;
            }

            for (int i = 0; i < count && existingPedTiers.Count < MaxSpawnedDefenders; i++)
            {
                if (!_pedSpawningService.CanSpawn()) break;

                var spawnPos = CalculateRandomSpawnPosition(zoneCenter, zoneRadius);
                var pedHandle = _pedSpawningService.SpawnPed(model, spawnPos, _playerFactionId, zoneId);
                if (!pedHandle.IsValid) continue;

                // Set friendly relationship with player
                _gameBridge.SetPedAsFriendly(pedHandle.Handle);
                ConfigureDefenderCombat(pedHandle.Handle, tierConfig);

                // Use combat targeting if battle is active, otherwise walking wander
                if (inBattle)
                {
                    ConfigureBattlePerception(pedHandle.Handle, zoneRadius);
                    _gameBridge.TaskCombatHatedTargetsAroundPed(pedHandle.Handle, zoneRadius);
                }
                else
                {
                    _gameBridge.TaskPedWanderInBoundedArea(pedHandle.Handle, zoneCenter, zoneRadius);
                }

                _pedBlipService.CreateBlipForPed(pedHandle.Handle, BlipColor.LightBlue);

                // Track ped with its tier
                existingPedTiers[pedHandle.Handle] = tier;
            }
        }

        /// <summary>
        /// Calculates a random spawn position around the zone center at ground level.
        /// Uses 80% of zone radius for spawn distance and navmesh-based safe
        /// coordinates to avoid spawning on rooftops.
        /// </summary>
        private Vector3 CalculateRandomSpawnPosition(Vector3 center, float zoneRadius)
        {
            var angle = _random.NextDouble() * 2 * Math.PI;
            var distance = zoneRadius * SpawnRadiusFraction;
            var x = center.X + (float)(Math.Cos(angle) * distance);
            var y = center.Y + (float)(Math.Sin(angle) * distance);

            // Use navmesh-based safe coordinate to avoid rooftop spawns
            var targetPos = new Vector3(x, y, center.Z);
            return _gameBridge.GetSafeCoordForPed(targetPos);
        }

        /// <summary>
        /// Configures a defender's combat attributes based on their tier configuration.
        /// </summary>
        private void ConfigureDefenderCombat(int pedHandle, DefenderTierConfig tierConfig)
        {
            // Give pistol first as secondary weapon for drive-by shooting
            _gameBridge.GivePedWeapon(pedHandle, "weapon_pistol");
            // Give tier-appropriate weapon last so it becomes the equipped/primary weapon
            _gameBridge.GivePedWeapon(pedHandle, tierConfig.Weapon);
            _gameBridge.SetPedAccuracy(pedHandle, tierConfig.Accuracy);
            _gameBridge.SetPedArmor(pedHandle, tierConfig.Armor);
            _gameBridge.SetPedHealth(pedHandle, tierConfig.Health);
            _gameBridge.SetPedCombatAttributes(pedHandle, canUseCover: true, willFightArmedPeds: true);

            // Elite tier uses RPG - prevent AI from switching to pistol (AI prefers pistol to avoid self-damage)
            if (tierConfig.Tier == DefenderTier.Elite)
            {
                _gameBridge.SetPedCanSwitchWeapons(pedHandle, false);
            }
        }
    }
}
