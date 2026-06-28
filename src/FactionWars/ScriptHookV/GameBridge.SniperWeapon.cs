using System;
using FactionWars.ScriptHookV.Logging;
using GTA;
using GTA.Native;

namespace FactionWars.ScriptHookV
{
    public partial class GameBridge
    {
        /// <inheritdoc />
        public void SetPedActiveWeapon(int pedHandle, string weaponName)
        {
            try
            {
                var ped = Entity.FromHandle(pedHandle) as Ped;
                if (ped == null || !ped.Exists())
                {
                    FileLogger.Warn($"SetPedActiveWeapon: ped {pedHandle} missing");
                    return;
                }

                var weaponHash = GetWeaponHash(weaponName);
                // SET_CURRENT_PED_WEAPON(ped, weaponHash, bForceInHand)
                Function.Call(Hash.SET_CURRENT_PED_WEAPON, ped.Handle, (uint)weaponHash, true);
                FileLogger.AI($"SetPedActiveWeapon: ped {pedHandle} -> {weaponName}");
            }
            catch (Exception ex)
            {
                FileLogger.Error($"SetPedActiveWeapon exception for ped {pedHandle}, weapon {weaponName}", ex);
            }
        }
    }
}
