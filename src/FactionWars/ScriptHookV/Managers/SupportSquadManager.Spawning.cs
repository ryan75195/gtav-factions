using FactionWars.Combat.Models;
using FactionWars.Core.Models;
using FactionWars.Core.Interfaces;

namespace FactionWars.ScriptHookV.Managers
{
    public partial class SupportSquadManager
    {
        /// <summary>
        /// Spawns the 8-ally composition at <paramref name="spawnPos"/> and seats each into the
        /// SUV. Friendly-but-not-a-follower: spawns in the player's own faction relationship group
        /// (companion of the player, hostile to enemies via the relationship matrix) but never
        /// calls SetPedAsFollower / joins the player's follower group.
        /// </summary>
        private void SpawnAndSeatAllies(int suv, Vector3 spawnPos, string zoneId)
        {
            for (var seatIndex = 0; seatIndex < Composition.Length; seatIndex++)
            {
                var role = Composition[seatIndex];
                var model = FactionPedModels.GetModel(_playerFactionId, role);
                var handle = _spawner.Spawn(_playerFactionId, _playerFactionId, model, spawnPos, zoneId);
                if (!handle.IsValid) continue;

                ConfigureAlly(handle.Handle, role);
                // Blocked while riding: the allies spawn hostile to the enemy faction with high
                // alertness, and in-game the ongoing zone battle put seated allies into combat
                // state within seconds — which overrides the driver's vehicle task and froze the
                // SUV at its spawn point. Unblocked again at dismount so S&D can fight.
                _gameBridge.SetPedBlockPermanentEvents(handle.Handle, true);
                // Seat index 0 = driver; 1-3 = cabin seats; 4-7 = FBI SUV side rails (unverified
                // in-game — see the FBI-SUV side-rail risk note in the support-squad design doc).
                _gameBridge.SetPedIntoVehicle(handle.Handle, suv, seatIndex);
                _rolesByHandle[handle.Handle] = role;
            }
        }

        /// <summary>
        /// Configures a support ally's combat stats/weapon, mirroring
        /// <c>BattleAttackerManager.ConfigureAttacker</c>'s Friendlies stat block. Deliberately
        /// omits SetPedAsFollower/group calls: these allies are temporary support, not crew.
        /// </summary>
        private void ConfigureAlly(int pedHandle, DefenderRole role)
        {
            var stats = _statsProvider.GetRoleStats(CombatantCategory.Friendlies, role);

            _gameBridge.GivePedWeapon(pedHandle, "weapon_pistol");
            _gameBridge.GivePedWeapon(pedHandle, stats.Weapon);
            _gameBridge.SetPedAccuracy(pedHandle, stats.Accuracy);
            _gameBridge.SetPedArmor(pedHandle, stats.Armor);
            _gameBridge.SetPedHealth(pedHandle, stats.Health);
            _gameBridge.SetPedWeaponDamageModifier(pedHandle, stats.DamageMultiplier);
            _gameBridge.SetPedCriticalHitsEnabled(pedHandle, true);
            _gameBridge.SetPedCombatAttributes(pedHandle, canUseCover: true, willFightArmedPeds: true);
        }
    }
}
