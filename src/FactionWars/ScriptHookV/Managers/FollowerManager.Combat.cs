using System;
using FactionWars.Combat.Models;
using FactionWars.Core.Interfaces;
using FactionWars.Core.Models;

namespace FactionWars.ScriptHookV.Managers
{
    public partial class FollowerManager
    {
        // Combat profile values for SetPedCombatProfile (-1 = leave engine default).
        private const int CombatAbilityProfessional = 2;
        private const int CombatRangeFar = 2;
        private const int CombatMovementOffensive = 2;
        private const int CombatKeepDefault = -1;

        private void ConfigureFollowerCombat(int pedHandle, DefenderRoleConfig roleConfig)
        {
            var stats = _statsProvider.GetRoleStats(CombatantCategory.Squad, roleConfig.Role);

            // Give pistol first as secondary weapon for drive-by shooting from vehicles
            _gameBridge.GivePedWeapon(pedHandle, "weapon_pistol");

            // Give tier-appropriate weapon last so it becomes the equipped/primary weapon
            _gameBridge.GivePedWeapon(pedHandle, stats.Weapon);

            // Set shooting accuracy
            _gameBridge.SetPedAccuracy(pedHandle, stats.Accuracy);

            // Set armor based on tier
            _gameBridge.SetPedArmor(pedHandle, stats.Armor);

            // Set health based on tier
            _gameBridge.SetPedHealth(pedHandle, stats.Health);
            _gameBridge.SetPedWeaponDamageModifier(pedHandle, stats.DamageMultiplier);
            _gameBridge.SetPedCriticalHitsEnabled(pedHandle, false);
            _gameBridge.SetPedRagdollEnabled(pedHandle, false);

            // Configure combat behavior - followers should take cover and fight armed enemies
            _gameBridge.SetPedCombatAttributes(pedHandle, canUseCover: true, willFightArmedPeds: true);

            // Snipers inherit the gang model's default (Poor) combat ability, which makes them
            // aim a scoped rifle but hesitate to fire. Give them Professional ability + Far range
            // so they commit to the shot and keep their distance. Movement left default — they
            // should hold and snipe, not advance.
            if (roleConfig.Role == DefenderRole.Sniper)
            {
                _gameBridge.SetPedCombatProfile(pedHandle, CombatAbilityProfessional, CombatRangeFar, CombatKeepDefault);
            }
            // Assault roles default to cautious movement, so in Search & Destroy they hold and
            // shoot from range instead of closing in. Professional ability + Offensive movement
            // makes them advance and press the attack. Range left default so they don't abandon
            // the player to chase distant enemies while escorting. Rocketeers are excluded — an
            // RPG user charging into close range would splash itself.
            else if (roleConfig.Role == DefenderRole.Grunt
                || roleConfig.Role == DefenderRole.Gunner
                || roleConfig.Role == DefenderRole.Rifleman)
            {
                _gameBridge.SetPedCombatProfile(pedHandle, CombatAbilityProfessional, CombatKeepDefault, CombatMovementOffensive);
            }

            // Elite tier uses RPG - prevent AI from switching to pistol (AI prefers pistol to avoid self-damage)
            if (roleConfig.Role == DefenderRole.Rocketeer)
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
