using System;
using FactionWars.Combat.Models;
using FactionWars.Core.Interfaces;
using FactionWars.Core.Models;
using FactionWars.ScriptHookV.Logging;
using FactionWars.ScriptHookV.Services;
using FactionWars.Territory.Models;

namespace FactionWars.ScriptHookV.Managers
{
    public partial class EnemyDefenderManager
    {
        private void ConfigureEnemyDefender(int pedHandle, DefenderRoleConfig roleConfig, Vector3 zoneCenter, float wanderRadius)
        {
            var stats = _statsProvider.GetRoleStats(CombatantCategory.Enemies, roleConfig.Role);

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

            // Set zone-wide perception so enemies can detect friendly defenders across the zone
            var perceptionRange = wanderRadius * 1.2f;
            _gameBridge.SetPedSeeingRange(pedHandle, perceptionRange);
            _gameBridge.SetPedHearingRange(pedHandle, perceptionRange);

            // Hostile stance (persistence, combat attributes) is set by ZoneCombatantSpawner at
            // spawn time; the relationship matrix decides who this ped hates. Here we only add the
            // zone patrol/seek tasking that drives them toward hated targets in range.
            _gameBridge.TaskCombatHatedTargetsAroundPed(pedHandle, wanderRadius);

            // Sniper-specific: move ped to high-ground perch and issue guard task. No-op for other roles.
            _sniperDeployment.DeployIfSniper(pedHandle, roleConfig, zoneCenter);
        }

        /// <summary>
        /// Gets the number of remaining reserves (allocated troops that haven't been spawned yet).
        /// </summary>
        /// <param name="zoneId">The zone ID.</param>
        /// <param name="enemyFactionId">The enemy faction ID.</param>
        /// <returns>The number of troops in reserve (allocation total - spawned count).</returns>
        public int GetRemainingReserves(string zoneId, string enemyFactionId)
        {
            var allocation = _allocationService.GetAllocation(enemyFactionId, zoneId);
            if (allocation == null) return 0;

            var spawnedCount = GetSpawnedDefenderCount(zoneId);
            var totalAllocated = allocation.TotalTroops;

            // Reserves = allocation total - currently spawned (clamped to 0)
            return Math.Max(0, totalAllocated - spawnedCount);
        }
    }
}
