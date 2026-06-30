using System.Collections.Generic;
using FactionWars.Combat.Models;
using FactionWars.Core.Interfaces;
using FactionWars.Core.Models;
using FactionWars.ScriptHookV.Logging;

namespace FactionWars.ScriptHookV
{
    public partial class GameLoopController
    {
        private void ConfigureSpawnedDefenders(IList<PedHandle> spawnedPeds, DefenderRole tier, string defenderFactionId)
        {
            FileLogger.Combat($"ConfigureSpawnedDefenders: {spawnedPeds.Count} peds, tier={tier}, faction={defenderFactionId}");

            var defenderRoleService = _container.Resolve<IDefenderRoleService>();
            var config = defenderRoleService.GetRoleConfig(tier);
            var statsProvider = _container.Resolve<ICombatantStatsProvider>();

            var category = defenderFactionId == CurrentPlayerFactionId
                ? CombatantCategory.Friendlies
                : CombatantCategory.Enemies;
            var stats = statsProvider.GetRoleStats(category, tier);

            foreach (var ped in spawnedPeds)
            {
                if (!ped.IsValid)
                {
                    FileLogger.Warn($"Skipping invalid ped handle {ped.Handle}");
                    continue;
                }

                FileLogger.Combat($"Configuring defender ped {ped.Handle}");

                // Set health and armor based on tier
                _gameBridge.SetPedHealth(ped.Handle, stats.Health);
                _gameBridge.SetPedCriticalHitsEnabled(ped.Handle, true);
                _gameBridge.SetPedRagdollEnabled(ped.Handle, config.RagdollEnabled);
                _gameBridge.SetPedArmor(ped.Handle, stats.Armor);

                // Give weapon
                _gameBridge.GivePedWeapon(ped.Handle, stats.Weapon);

                // Set accuracy
                _gameBridge.SetPedAccuracy(ped.Handle, stats.Accuracy);

                // Set combat attributes - make them aggressive fighters
                _gameBridge.SetPedCombatAttributes(ped.Handle, canUseCover: true, willFightArmedPeds: true);

                // Set hostile relationship to player
                SetPedHostileToPlayer(ped.Handle, defenderFactionId);

                // CRITICAL: Give defender a task to fight the player
                _gameBridge.SetPedToAttackPlayer(ped.Handle);

                _gameBridge.SetPedWeaponDamageModifier(ped.Handle, stats.DamageMultiplier);

                FileLogger.Combat($"Defender {ped.Handle} configured with weapon={stats.Weapon}, health={stats.Health}, accuracy={stats.Accuracy}");
            }
        }

        /// <summary>
        /// Sets up a ped to be hostile to the player by configuring relationship groups.
        /// </summary>
        /// <param name="pedHandle">The ped handle.</param>
        /// <param name="factionId">The faction ID for the relationship group.</param>
        private void SetPedHostileToPlayer(int pedHandle, string factionId)
        {
            // The ped's relationship group is already set by PedSpawningService
            // We need to make that group hostile to the player's group
            // This is done via native calls to SET_RELATIONSHIP_BETWEEN_GROUPS

            // Get player faction to set up hostility
            var playerFactionId = CurrentPlayerFactionId ?? "";
            if (string.IsNullOrEmpty(playerFactionId) || string.IsNullOrEmpty(factionId))
                return;

            // Set relationship groups to be enemies using native call through GameBridge
            // Relationship types: 0=Companion, 1=Respect, 2=Like, 3=Neutral, 4=Dislike, 5=Hate
            SetFactionRelationship(factionId, playerFactionId, 5); // Defenders hate player
            SetFactionRelationship(playerFactionId, factionId, 5); // Player faction hates defenders
        }

        /// <summary>
        /// Sets up faction relationship between two groups.
        /// </summary>
        private void SetFactionRelationship(string factionId1, string factionId2, int relationship)
        {
            try
            {
                // This uses native function through game bridge extension
                // The relationship will make peds attack each other
                var group1 = factionId1.ToUpperInvariant();
                var group2 = factionId2.ToUpperInvariant();

                // Use GTA native: SET_RELATIONSHIP_BETWEEN_GROUPS(int relationship, Hash group1, Hash group2)
                GTA.Native.Function.Call(
                    GTA.Native.Hash.SET_RELATIONSHIP_BETWEEN_GROUPS,
                    relationship,
                    GTA.World.AddRelationshipGroup(group1),
                    GTA.World.AddRelationshipGroup(group2));
            }
            catch
            {
                // Silently ignore - relationship setup failed but combat may still work
            }
        }
    }
}
