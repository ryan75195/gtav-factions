using System;
using FactionWars.Combat.Models;
using FactionWars.Core.Interfaces;
using FactionWars.Core.Models;

namespace FactionWars.ScriptHookV.Managers
{
    public partial class FollowerManager
    {
        private void ConfigureFollowerCombat(int pedHandle, DefenderTierConfig tierConfig)
        {
            // Give pistol first as secondary weapon for drive-by shooting from vehicles
            _gameBridge.GivePedWeapon(pedHandle, "weapon_pistol");

            // Give tier-appropriate weapon last so it becomes the equipped/primary weapon
            _gameBridge.GivePedWeapon(pedHandle, tierConfig.Weapon);

            // Set shooting accuracy
            _gameBridge.SetPedAccuracy(pedHandle, tierConfig.Accuracy);

            // Set armor based on tier
            _gameBridge.SetPedArmor(pedHandle, tierConfig.Armor);

            // Set health based on tier
            _gameBridge.SetPedHealth(pedHandle, tierConfig.Health);
            _gameBridge.SetPedCriticalHitsEnabled(pedHandle, false);

            // Configure combat behavior - followers should take cover and fight armed enemies
            _gameBridge.SetPedCombatAttributes(pedHandle, canUseCover: true, willFightArmedPeds: true);

            // Elite tier uses RPG - prevent AI from switching to pistol (AI prefers pistol to avoid self-damage)
            if (tierConfig.Tier == DefenderTier.Elite)
            {
                _gameBridge.SetPedCanSwitchWeapons(pedHandle, false);
            }
        }

        /// <summary>
        /// Calculates a spawn position for a follower near the player.
        /// </summary>
        /// <param name="playerPos">The player's current position.</param>
        /// <returns>A position slightly offset from the player.</returns>
        private Vector3 CalculateFollowerSpawnPosition(Vector3 playerPos)
        {
            // Spawn slightly behind and to the side of the player
            return new Vector3(
                playerPos.X + 2f,
                playerPos.Y + 2f,
                playerPos.Z);
        }
    }
}
