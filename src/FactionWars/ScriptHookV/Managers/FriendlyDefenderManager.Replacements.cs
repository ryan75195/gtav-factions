using System;
using System.Collections.Generic;
using FactionWars.Combat.Models;
using FactionWars.Core.Interfaces;
using FactionWars.Core.Models;
using FactionWars.ScriptHookV.Logging;
using FactionWars.ScriptHookV.Models;
using FactionWars.ScriptHookV.Services;

namespace FactionWars.ScriptHookV.Managers
{
    public partial class FriendlyDefenderManager
    {
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
            // Single spawn site owns faction group, blip colour, and friendly combat stance.
            var pedHandle = _spawner.Spawn(_playerFactionId, _playerFactionId, model, spawnPos, zoneId);

            if (!pedHandle.IsValid) return;

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
                // Single spawn site owns faction group, blip colour, and friendly combat stance.
                var pedHandle = _spawner.Spawn(_playerFactionId, _playerFactionId, model, spawnPos, zoneId);
                if (!pedHandle.IsValid) continue;

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
            _gameBridge.SetPedCriticalHitsEnabled(pedHandle, false);
            _gameBridge.SetPedRagdollEnabled(pedHandle, tierConfig.RagdollEnabled);
            _gameBridge.SetPedCombatAttributes(pedHandle, canUseCover: true, willFightArmedPeds: true);

            // Elite tier uses RPG - prevent AI from switching to pistol (AI prefers pistol to avoid self-damage)
            if (tierConfig.Tier == DefenderTier.Elite)
            {
                _gameBridge.SetPedCanSwitchWeapons(pedHandle, false);
            }
        }
    }
}
