using System;
using System.Collections.Generic;
using System.Linq;
using FactionWars.Combat.Models;
using FactionWars.Core.Interfaces;
using FactionWars.Core.Models;
using FactionWars.ScriptHookV.Logging;
using FactionWars.ScriptHookV.Models;
using FactionWars.ScriptHookV.Services;
using FactionWars.ScriptHookV.Utils;
using FactionWars.Territory.Models;

namespace FactionWars.ScriptHookV.Managers
{
    public partial class BattleAttackerManager
    {
        private bool TrySpawnReplacement(string zoneId, DefenderTier preferredTier, ZoneBattle? battle)
        {
            if (battle == null) return false;

            var currentSpawned = GetSpawnedAttackerCount(zoneId);
            if (currentSpawned >= MaxSpawnedAttackers) return false;

            var zone = _zoneService.GetZone(zoneId);
            if (zone == null) return false;

            // Try preferred tier first
            if (battle.AttackerTroops.TryGetValue(preferredTier, out var allocatedCount))
            {
                var spawnedOfTier = GetSpawnedCountByTier(zoneId, preferredTier);

                if (allocatedCount > spawnedOfTier)
                {
                    SpawnSingleAttacker(zoneId, preferredTier, battle.AttackerFactionId, zone);
                    FileLogger.Combat($"BattleAttackerManager: Spawned replacement {preferredTier} in {zoneId}");
                    return true;
                }
            }

            // Try other tiers (highest first)
            foreach (var tier in new[] { DefenderTier.Elite, DefenderTier.Heavy, DefenderTier.Medium, DefenderTier.Basic })
            {
                if (tier == preferredTier) continue;

                if (battle.AttackerTroops.TryGetValue(tier, out allocatedCount))
                {
                    var spawnedOfTier = GetSpawnedCountByTier(zoneId, tier);

                    if (allocatedCount > spawnedOfTier)
                    {
                        SpawnSingleAttacker(zoneId, tier, battle.AttackerFactionId, zone);
                        FileLogger.Combat($"BattleAttackerManager: Spawned replacement {tier} in {zoneId}");
                        return true;
                    }
                }
            }

            return false;
        }

        /// <summary>
        /// Spawns a single enemy attacker.
        /// </summary>
        private void SpawnSingleAttacker(string zoneId, DefenderTier tier, string attackerFactionId, Zone zone)
        {
            if (!_pedSpawningService.CanSpawn()) return;

            var model = _modelsByTier.TryGetValue(tier, out var m) ? m : "g_m_y_famca_01";
            var tierConfig = _defenderTierService.GetTierConfig(tier);
            var random = new Random();

            var spawnPos = CalculateRandomSpawnPosition(zone.Center, zone.Radius, random);
            var pedHandle = _pedSpawningService.SpawnPed(model, spawnPos, attackerFactionId, zoneId);

            if (!pedHandle.IsValid) return;

            ConfigureAttacker(pedHandle.Handle, tierConfig, zone.Center, zone.Radius);
            _pedBlipService.CreateBlipForPed(pedHandle.Handle, FactionBlipColor.ForFactionId(attackerFactionId));

            if (!_spawnedPedTierByZone.ContainsKey(zoneId))
            {
                _spawnedPedTierByZone[zoneId] = new Dictionary<int, DefenderTier>();
            }
            _spawnedPedTierByZone[zoneId][pedHandle.Handle] = tier;
        }

        /// <summary>
        /// Calculates a random spawn position around the zone center at ground level.
        /// Uses the zone's full radius for spawn area and navmesh-based safe coordinates
        /// to avoid spawning on rooftops.
        /// </summary>
        private Vector3 CalculateRandomSpawnPosition(Vector3 center, float zoneRadius, Random random)
        {
            var angle = random.NextDouble() * 2 * Math.PI;
            var minRadius = zoneRadius * MinSpawnRadiusFraction;
            var distance = minRadius + (float)(random.NextDouble() * (zoneRadius - minRadius));
            var x = center.X + (float)(Math.Cos(angle) * distance);
            var y = center.Y + (float)(Math.Sin(angle) * distance);

            // Use navmesh-based safe coordinate to avoid rooftop spawns
            var targetPos = new Vector3(x, y, center.Z);
            return _gameBridge.GetSafeCoordForPed(targetPos);
        }

        /// <summary>
        /// Configures an enemy attacker's combat attributes and behavior.
        /// </summary>
        private void ConfigureAttacker(int pedHandle, DefenderTierConfig tierConfig, Vector3 zoneCenter, float wanderRadius)
        {
            // Give weapons
            _gameBridge.GivePedWeapon(pedHandle, "weapon_pistol");
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

            // Set as hostile wanderer - will engage player and followers on sight
            _gameBridge.SetPedAsHostileWanderer(pedHandle);

            // Sprinting wander to actively search for and engage enemies
            _gameBridge.TaskPedWanderInAreaSprinting(pedHandle, zoneCenter, wanderRadius);

            // CRITICAL: Task to attack player immediately so they engage right away
            _gameBridge.SetPedToAttackPlayer(pedHandle);
        }
    }
}
