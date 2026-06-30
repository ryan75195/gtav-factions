using System;
using FactionWars.ScriptHookV.Logging;
using GTA;
using GTA.Native;

namespace FactionWars.ScriptHookV
{
    public partial class GameBridge
    {
        public void GivePedWeapon(int pedHandle, string weaponName)
        {
            try
            {
                var ped = Entity.FromHandle(pedHandle) as Ped;
                if (ped == null || !ped.Exists()) return;

                // Get weapon hash from name
                var weaponHash = GetWeaponHash(weaponName);

                // Give the weapon with max ammo and equip it
                var weapon = ped.Weapons.Give(weaponHash, 9999, true, true);

                // Explicitly select the weapon to ensure they're holding it
                if (weapon != null)
                {
                    ped.Weapons.Select(weapon);
                }
            }
            catch (Exception ex)
            {
                FileLogger.Error($"GivePedWeapon exception for ped {pedHandle}, weapon {weaponName}", ex);
            }
        }

        /// <inheritdoc />
        public void SetPedCanSwitchWeapons(int pedHandle, bool canSwitch)
        {
            try
            {
                var ped = Entity.FromHandle(pedHandle) as Ped;
                if (ped == null || !ped.Exists()) return;

                ped.CanSwitchWeapons = canSwitch;
                FileLogger.AI($"SetPedCanSwitchWeapons: ped {pedHandle} canSwitch={canSwitch}");
            }
            catch (Exception ex)
            {
                FileLogger.Error($"SetPedCanSwitchWeapons exception for ped {pedHandle}", ex);
            }
        }

        /// <inheritdoc />
        public void SetPedAccuracy(int pedHandle, float accuracy)
        {
            try
            {
                var ped = Entity.FromHandle(pedHandle) as Ped;
                if (ped == null || !ped.Exists()) return;

                // GTA V accuracy is 0-100 integer, we use 0.0-1.0 float
                int gtaAccuracy = (int)(accuracy * 100f);
                ped.Accuracy = Math.Max(0, Math.Min(100, gtaAccuracy));
            }
            catch (Exception ex)
            {
                FileLogger.Error($"SetPedAccuracy exception for ped {pedHandle}", ex);
            }
        }

        /// <inheritdoc />
        public void SetPedArmor(int pedHandle, int armor)
        {
            try
            {
                var ped = Entity.FromHandle(pedHandle) as Ped;
                if (ped == null || !ped.Exists()) return;

                ped.Armor = Math.Max(0, armor);
            }
            catch (Exception ex)
            {
                FileLogger.Error($"SetPedArmor exception for ped {pedHandle}", ex);
            }
        }

        /// <inheritdoc />
        public void SetPedWeaponDamageModifier(int pedHandle, float multiplier)
        {
            try
            {
                var ped = Entity.FromHandle(pedHandle) as Ped;
                if (ped == null || !ped.Exists()) return;
                Function.Call((Hash)0x4757F00BC6323CFEUL, ped.Handle, multiplier);
                FileLogger.Combat($"SetPedWeaponDamageModifier: ped {pedHandle} x{multiplier:F2}");
            }
            catch (Exception ex)
            {
                FileLogger.Error($"SetPedWeaponDamageModifier exception for ped {pedHandle}", ex);
            }
        }
    }
}
