using System;
using System.Collections.Generic;
using FactionWars.Combat.Models;
using FactionWars.Core.Models;
using FactionWars.ScriptHookV.Logging;
using FactionWars.ScriptHookV.Models;
using FactionWars.ScriptHookV.Services;
using FactionWars.Core.Utils;
using FactionWars.Territory.Models;

namespace FactionWars.ScriptHookV.Managers
{
    public partial class EnemyDefenderManager
    {
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
                SpawnSingleDefender(zoneId, preferredTier, enemyFactionId, zone);
                FileLogger.Combat($"EnemyDefenderManager: Spawned replacement {preferredTier} in {zoneId}");
                return true;
            }

            // Try other tiers (highest first)
            foreach (var tier in new[] { DefenderTier.Elite, DefenderTier.Heavy, DefenderTier.Medium, DefenderTier.Basic })
            {
                if (tier == preferredTier) continue;

                allocatedCount = allocation.GetTroopCount(tier);
                spawnedOfTier = GetSpawnedCountByTier(zoneId, tier);

                if (allocatedCount > spawnedOfTier)
                {
                    SpawnSingleDefender(zoneId, tier, enemyFactionId, zone);
                    FileLogger.Combat($"EnemyDefenderManager: Spawned replacement {tier} in {zoneId}");
                    return true;
                }
            }

            return false;
        }

        public void OnTroopsAllocated(string factionId, string zoneId, DefenderTier tier, int count)
        {
            if (string.IsNullOrEmpty(factionId) || string.IsNullOrEmpty(zoneId) || count <= 0)
                return;
            if (_currentEnemyZoneId != zoneId)
                return;

            var zone = _zoneService.GetZone(zoneId);
            if (zone == null || zone.OwnerFactionId != factionId)
                return;

            var allocation = _allocationService.GetAllocation(factionId, zoneId);
            if (allocation == null)
                return;

            int spawned = GetSpawnedDefenderCount(zoneId);
            int spawnBudget = Math.Min(count, MaxSpawnedDefenders - spawned);
            for (int i = 0; i < spawnBudget; i++)
            {
                int allocatedOfTier = allocation.GetTroopCount(tier);
                int spawnedOfTier = GetSpawnedCountByTier(zoneId, tier);
                if (allocatedOfTier <= spawnedOfTier)
                    break;

                int before = GetSpawnedDefenderCount(zoneId);
                SpawnSingleDefender(zoneId, tier, factionId, zone);
                if (GetSpawnedDefenderCount(zoneId) == before)
                    break;

                FileLogger.Combat($"EnemyDefenderManager: Spawned live reinforcement {tier} in {zoneId}");
            }
        }

        /// <summary>
        /// Spawns a single enemy defender.
        /// </summary>
        private void SpawnSingleDefender(string zoneId, DefenderTier tier, string enemyFactionId, Zone zone)
        {
            if (!_pedSpawningService.CanSpawn()) return;

            var model = FactionPedModels.GetModel(enemyFactionId, tier);
            var tierConfig = _defenderTierService.GetTierConfig(tier);
            var random = new Random();

            var spawnPos = CalculateRandomSpawnPosition(zone.Center, zone.Radius, random);
            var pedHandle = _pedSpawningService.SpawnPed(model, spawnPos, enemyFactionId, zoneId);

            if (!pedHandle.IsValid) return;

            ConfigureEnemyDefender(pedHandle.Handle, tierConfig, zone.Center, zone.Radius);
            _pedBlipService.CreateBlipForPed(pedHandle.Handle, FactionBlipColor.ForFactionId(enemyFactionId));

            if (!_spawnedPedTierByZone.ContainsKey(zoneId))
            {
                _spawnedPedTierByZone[zoneId] = new Dictionary<int, DefenderTier>();
            }
            _spawnedPedTierByZone[zoneId][pedHandle.Handle] = tier;
        }

        /// <summary>
        /// Calculates a random spawn position around the zone center at ground level.
        /// Uses 80% of zone radius for spawn distance and navmesh-based safe coordinates
        /// to avoid spawning on rooftops.
        /// </summary>
        private FactionWars.Core.Interfaces.Vector3 CalculateRandomSpawnPosition(FactionWars.Core.Interfaces.Vector3 center, float zoneRadius, Random random)
        {
            var angle = random.NextDouble() * 2 * Math.PI;
            var distance = zoneRadius * SpawnRadiusFraction;
            var x = center.X + (float)(Math.Cos(angle) * distance);
            var y = center.Y + (float)(Math.Sin(angle) * distance);

            // Use navmesh-based safe coordinate to avoid rooftop spawns
            var targetPos = new FactionWars.Core.Interfaces.Vector3(x, y, center.Z);
            return _gameBridge.GetSafeCoordForPed(targetPos);
        }

        /// <summary>
        /// Configures an enemy defender's combat attributes and behavior.
        /// </summary>
    }
}
