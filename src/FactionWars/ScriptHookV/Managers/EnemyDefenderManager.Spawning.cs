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
        private void ConfigureEnemyDefender(int pedHandle, DefenderTierConfig tierConfig, Vector3 zoneCenter, float wanderRadius)
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

            // Set zone-wide perception so enemies can detect friendly defenders across the zone
            var perceptionRange = wanderRadius * 1.2f;
            _gameBridge.SetPedSeeingRange(pedHandle, perceptionRange);
            _gameBridge.SetPedHearingRange(pedHandle, perceptionRange);

            // Set as hostile wanderer - will engage player and followers on sight
            _gameBridge.SetPedAsHostileWanderer(pedHandle);

            // Task to seek and fight hated targets (player, followers, friendly defenders)
            _gameBridge.TaskCombatHatedTargetsAroundPed(pedHandle, wanderRadius);
        }

        private void ConfigureBattleRelationships(string zoneId)
        {
            var battle = _zoneBattleManager?.GetBattleForZone(zoneId);
            if (battle == null)
                return;

            for (int i = 0; i < battle.Participants.Count; i++)
            {
                for (int j = i + 1; j < battle.Participants.Count; j++)
                {
                    var group1 = _pedSpawningService.GetRelationshipGroup(battle.Participants[i].FactionId);
                    var group2 = _pedSpawningService.GetRelationshipGroup(battle.Participants[j].FactionId);
                    _gameBridge.SetRelationshipBetweenGroups(group1, group2, relationship: 5, bidirectional: true);
                }
            }
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
