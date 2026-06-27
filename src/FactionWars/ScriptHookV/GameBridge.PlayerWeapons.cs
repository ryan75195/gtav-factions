using System;
using System.Collections.Generic;
using FactionWars.ScriptHookV.Logging;
using GTA;
using GTA.Native;
using DomainBlipColor = FactionWars.Core.Interfaces.BlipColor;

namespace FactionWars.ScriptHookV
{
    public partial class GameBridge
    {
        public System.Collections.Generic.Dictionary<string, int> GetPlayerWeapons()
        {
            var weapons = new System.Collections.Generic.Dictionary<string, int>();

            try
            {
                var player = Game.Player.Character;
                if (player == null || !player.Exists())
                {
                    FileLogger.Warn("GetPlayerWeapons: Player doesn't exist");
                    return weapons;
                }

                foreach (var weaponHash in GetCommonWeaponHashes())
                {
                    if (player.Weapons.HasWeapon(weaponHash))
                    {
                        var weapon = player.Weapons[weaponHash];
                        if (weapon != null)
                        {
                            var ammo = weapon.Ammo;
                            var weaponName = weaponHash.ToString().ToLowerInvariant();
                            // Convert enum name to weapon hash format (e.g., "pistol" -> "weapon_pistol")
                            weapons[$"weapon_{weaponName}"] = ammo;
                        }
                    }
                }

                FileLogger.Info($"GetPlayerWeapons: Found {weapons.Count} weapons");
            }
            catch (Exception ex)
            {
                FileLogger.Error("GetPlayerWeapons exception", ex);
            }

            return weapons;
        }

        private static WeaponHash[] GetCommonWeaponHashes()
        {
            return new[]
            {
                WeaponHash.Knife, WeaponHash.Nightstick, WeaponHash.Hammer, WeaponHash.Bat,
                WeaponHash.GolfClub, WeaponHash.Crowbar, WeaponHash.Bottle, WeaponHash.SwitchBlade,
                WeaponHash.Dagger, WeaponHash.Hatchet, WeaponHash.Machete, WeaponHash.Flashlight,
                WeaponHash.KnuckleDuster, WeaponHash.PoolCue, WeaponHash.Wrench, WeaponHash.BattleAxe,
                WeaponHash.Pistol, WeaponHash.CombatPistol, WeaponHash.APPistol, WeaponHash.Pistol50,
                WeaponHash.SNSPistol, WeaponHash.HeavyPistol, WeaponHash.VintagePistol, WeaponHash.MarksmanPistol,
                WeaponHash.Revolver, WeaponHash.DoubleActionRevolver, WeaponHash.FlareGun, WeaponHash.StunGun,
                WeaponHash.CeramicPistol, WeaponHash.NavyRevolver, WeaponHash.PericoPistol,
                WeaponHash.MicroSMG, WeaponHash.SMG, WeaponHash.AssaultSMG, WeaponHash.CombatPDW,
                WeaponHash.MachinePistol, WeaponHash.MiniSMG, WeaponHash.Gusenberg,
                WeaponHash.PumpShotgun, WeaponHash.SawnOffShotgun, WeaponHash.AssaultShotgun,
                WeaponHash.BullpupShotgun, WeaponHash.HeavyShotgun, WeaponHash.DoubleBarrelShotgun,
                WeaponHash.SweeperShotgun, WeaponHash.CombatShotgun,
                WeaponHash.AssaultRifle, WeaponHash.CarbineRifle, WeaponHash.AdvancedRifle,
                WeaponHash.SpecialCarbine, WeaponHash.BullpupRifle, WeaponHash.CompactRifle,
                WeaponHash.MilitaryRifle, WeaponHash.HeavyRifle,
                WeaponHash.MG, WeaponHash.CombatMG, WeaponHash.Gusenberg,
                WeaponHash.SniperRifle, WeaponHash.HeavySniper, WeaponHash.MarksmanRifle,
                WeaponHash.RPG, WeaponHash.GrenadeLauncher, WeaponHash.Minigun, WeaponHash.Firework,
                WeaponHash.Railgun, WeaponHash.HomingLauncher, WeaponHash.CompactGrenadeLauncher,
                WeaponHash.Grenade, WeaponHash.SmokeGrenade, WeaponHash.BZGas, WeaponHash.Molotov,
                WeaponHash.StickyBomb, WeaponHash.ProximityMine, WeaponHash.Snowball, WeaponHash.PipeBomb,
                WeaponHash.Ball, WeaponHash.Flare
            };
        }

        /// <inheritdoc />
        public bool IsControlPressed(int control)
        {
            return Game.IsControlPressed((GTA.Control)control);
        }

        /// <inheritdoc />
        public bool IsControlJustPressed(int control)
        {
            return Game.IsControlJustPressed((GTA.Control)control);
        }

        /// <inheritdoc />
        public void DisableControlThisFrame(int control)
        {
            Function.Call(Hash.DISABLE_CONTROL_ACTION, 0, control, true);
        }

        private static WeaponHash GetWeaponHash(string weaponName)
        {
            return (WeaponHash)StringHash.AtStringHash(weaponName.ToUpperInvariant(), 0);
        }

        /// <summary>
        /// Converts our domain BlipColor enum to GTA V's BlipColor.
        /// </summary>
        private GTA.BlipColor ConvertBlipColor(DomainBlipColor color)
        {
            return color switch
            {
                DomainBlipColor.White => GTA.BlipColor.White,
                DomainBlipColor.Red => GTA.BlipColor.Red,
                DomainBlipColor.Green => GTA.BlipColor.Green,
                DomainBlipColor.Blue => GTA.BlipColor.Blue,
                DomainBlipColor.Yellow => GTA.BlipColor.Yellow,
                DomainBlipColor.Orange => GTA.BlipColor.Orange,
                DomainBlipColor.Purple => GTA.BlipColor.Purple,
                DomainBlipColor.Pink => GTA.BlipColor.Pink,
                DomainBlipColor.MichaelBlue => GTA.BlipColor.Michael,
                DomainBlipColor.FranklinGreen => GTA.BlipColor.Franklin,
                DomainBlipColor.TrevorOrange => GTA.BlipColor.Trevor,
                _ => GTA.BlipColor.White
            };
        }
    }
}
