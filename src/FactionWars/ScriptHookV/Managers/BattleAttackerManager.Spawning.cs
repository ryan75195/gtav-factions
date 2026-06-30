using System;
using System.Collections.Generic;
using System.Linq;
using FactionWars.Combat.Models;
using FactionWars.Core.Interfaces;
using FactionWars.Core.Models;
using FactionWars.ScriptHookV.Logging;
using FactionWars.ScriptHookV.Models;
using FactionWars.ScriptHookV.Services;
using FactionWars.Core.Utils;
using FactionWars.Territory.Models;

namespace FactionWars.ScriptHookV.Managers
{
    public partial class BattleAttackerManager
    {
        private bool TrySpawnReplacement(string zoneId, DefenderRole preferredTier, ZoneBattle? battle)
        {
            if (battle == null) return false;

            var currentSpawned = GetSpawnedAttackerCount(zoneId);
            if (currentSpawned >= MaxSpawnedAttackers) return false;

            var zone = _zoneService.GetZone(zoneId);
            if (zone == null) return false;

            // Never spawn the player's own faction as a hostile attacker: resolve the hostile
            // attacker the same way the initial spawn does, and bail if there isn't one.
            var attackerFactionId = GetHostileAttackerForPlayer(battle)?.FactionId;
            if (attackerFactionId == null) return false;

            // Try preferred tier first
            if (battle.AttackerTroops.TryGetValue(preferredTier, out var allocatedCount))
            {
                var spawnedOfTier = GetSpawnedCountByTier(zoneId, preferredTier);

                if (allocatedCount > spawnedOfTier)
                {
                    SpawnSingleAttacker(zoneId, preferredTier, attackerFactionId, zone);
                    FileLogger.Combat($"BattleAttackerManager: Spawned replacement {preferredTier} in {zoneId}");
                    return true;
                }
            }

            // Try other tiers (highest first)
            foreach (var tier in new[] { DefenderRole.Rocketeer, DefenderRole.Rifleman, DefenderRole.Gunner, DefenderRole.Grunt })
            {
                if (tier == preferredTier) continue;

                if (battle.AttackerTroops.TryGetValue(tier, out allocatedCount))
                {
                    var spawnedOfTier = GetSpawnedCountByTier(zoneId, tier);

                    if (allocatedCount > spawnedOfTier)
                    {
                        SpawnSingleAttacker(zoneId, tier, attackerFactionId, zone);
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
        private void SpawnSingleAttacker(string zoneId, DefenderRole tier, string attackerFactionId, Zone zone)
        {
            if (!_pedSpawningService.CanSpawn()) return;

            var model = _modelsByTier.TryGetValue(tier, out var m) ? m : "g_m_y_famca_01";
            var roleConfig = _defenderRoleService.GetRoleConfig(tier);
            var random = new Random();

            var spawnPos = CalculateRandomSpawnPosition(zone.Center, zone.Radius, random);
            // Single spawn site owns relationship group, blip colour, and hostile stance.
            var pedHandle = _spawner.Spawn(attackerFactionId, _playerFactionId, model, spawnPos, zoneId);

            if (!pedHandle.IsValid) return;

            ConfigureAttacker(pedHandle.Handle, roleConfig, attackerFactionId, zone.Center, zone.Radius);

            EnsureSpawnTracking(zoneId);
            _spawnedPedTierByZone[zoneId][pedHandle.Handle] = tier;
            _spawnedPedFactionByZone[zoneId][pedHandle.Handle] = attackerFactionId;
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
        private void ConfigureAttacker(int pedHandle, DefenderRoleConfig roleConfig, string attackerFactionId, Vector3 zoneCenter, float wanderRadius)
        {
            var category = attackerFactionId == _playerFactionId
                ? CombatantCategory.Friendlies
                : CombatantCategory.Enemies;
            var stats = _statsProvider.GetRoleStats(category, roleConfig.Role);

            // Give weapons
            _gameBridge.GivePedWeapon(pedHandle, "weapon_pistol");
            _gameBridge.GivePedWeapon(pedHandle, stats.Weapon);
            _gameBridge.SetPedAccuracy(pedHandle, stats.Accuracy);
            _gameBridge.SetPedArmor(pedHandle, stats.Armor);
            _gameBridge.SetPedHealth(pedHandle, stats.Health);
            _gameBridge.SetPedWeaponDamageModifier(pedHandle, stats.DamageMultiplier);
            _gameBridge.SetPedCriticalHitsEnabled(pedHandle, true);
            _gameBridge.SetPedRagdollEnabled(pedHandle, roleConfig.RagdollEnabled);
            _gameBridge.SetPedCombatAttributes(pedHandle, canUseCover: true, willFightArmedPeds: true);

            // Elite tier uses RPG - prevent AI from switching to pistol (AI prefers pistol to avoid self-damage)
            if (roleConfig.Role == DefenderRole.Rocketeer)
            {
                _gameBridge.SetPedCanSwitchWeapons(pedHandle, false);
            }

            // Hostile stance (persistence, combat attributes) is set by ZoneCombatantSpawner at
            // spawn time; the relationship matrix decides who this ped hates. Here we only add the
            // tasking that drives them to seek any hated target in the battle, not only the player.
            _gameBridge.TaskCombatHatedTargetsAroundPed(pedHandle, wanderRadius);
        }

    }
}
