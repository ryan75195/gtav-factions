using System;
using System.IO;
using FactionWars.Core.Interfaces;
using FactionWars.ScriptHookV.Logging;
using GTA;
using GTA.Native;
using DomainVector3 = FactionWars.Core.Interfaces.Vector3;
using DomainBlipColor = FactionWars.Core.Interfaces.BlipColor;

namespace FactionWars.ScriptHookV
{
    public partial class GameBridge
    {
        public int GetPlayerMoney()
        {
            try
            {
                return Game.Player.Money;
            }
            catch
            {
                return 0;
            }
        }

        /// <inheritdoc />
        public void AddPlayerMoney(int amount)
        {
            try
            {
                Game.Player.Money += amount;
            }
            catch
            {
                // Silently ignore
            }
        }

        /// <inheritdoc />
        public void SetPlayerMoney(int amount)
        {
            try
            {
                Game.Player.Money = amount;
                FileLogger.Info($"SetPlayerMoney: Set player money to ${amount:N0}");
            }
            catch (Exception ex)
            {
                FileLogger.Error("SetPlayerMoney exception", ex);
            }
        }

        /// <inheritdoc />
        public long? GetTotalPlayTimeSeconds()
        {
            try
            {
                int activeChar = GetActiveSpCharacterIndex();
                string statName = $"SP{activeChar}_TOTAL_PLAYING_TIME";
                int hash = Function.Call<int>(Hash.GET_HASH_KEY, statName);
                var outArg = new OutputArgument();
                bool ok = Function.Call<bool>(Hash.STAT_GET_INT, hash, outArg, -1);
                if (!ok) return null;
                return outArg.GetResult<int>() / 1000L;
            }
            catch (Exception ex)
            {
                FileLogger.Error("GetTotalPlayTimeSeconds exception", ex);
                return null;
            }
        }

        public int GetActiveCharacterIndex() => GetActiveSpCharacterIndex();

        /// <inheritdoc />
        public int GetCompletedMissionCount()
        {
            try
            {
                int activeChar = GetActiveSpCharacterIndex();
                string statName = $"SP{activeChar}_TOTAL_MISSIONS_PASSED";
                int hash = Function.Call<int>(Hash.GET_HASH_KEY, statName);
                var outArg = new OutputArgument();
                bool ok = Function.Call<bool>(Hash.STAT_GET_INT, hash, outArg, -1);
                int valueOut = ok ? outArg.GetResult<int>() : 0;
                FileLogger.Debug($"GetCompletedMissionCount: stat={statName} ok={ok} value={valueOut}");
                return valueOut;
            }
            catch (Exception ex)
            {
                FileLogger.Error("GetCompletedMissionCount exception", ex);
                return 0;
            }
        }

        /// <inheritdoc />
        public int GetInGameClockMinutes()
        {
            try
            {
                int hours = Function.Call<int>(Hash.GET_CLOCK_HOURS);
                int minutes = Function.Call<int>(Hash.GET_CLOCK_MINUTES);
                int total = hours * 60 + minutes;
                FileLogger.Debug($"GetInGameClockMinutes: {hours:D2}:{minutes:D2} = {total}");
                return total;
            }
            catch (Exception ex)
            {
                FileLogger.Error("GetInGameClockMinutes exception", ex);
                return 0;
            }
        }

        private int GetActiveSpCharacterIndex()
        {
            string model = GetPlayerCharacterModel();
            if (string.Equals(model, "player_one", StringComparison.OrdinalIgnoreCase)) return 1;
            if (string.Equals(model, "player_two", StringComparison.OrdinalIgnoreCase)) return 2;
            return 0;
        }

        /// <inheritdoc />
        public void RemoveAllPlayerWeapons()
        {
            try
            {
                var player = Game.Player.Character;
                if (player == null || !player.Exists())
                {
                    FileLogger.Warn("RemoveAllPlayerWeapons: Player doesn't exist");
                    return;
                }

                // REMOVE_ALL_PED_WEAPONS native removes all weapons from a ped
                Function.Call(Hash.REMOVE_ALL_PED_WEAPONS, player.Handle, true);
                FileLogger.Info("RemoveAllPlayerWeapons: Removed all player weapons");
            }
            catch (Exception ex)
            {
                FileLogger.Error("RemoveAllPlayerWeapons exception", ex);
            }
        }

        /// <inheritdoc />
        public void GivePlayerWeapon(string weaponName, int ammo)
        {
            try
            {
                var player = Game.Player.Character;
                if (player == null || !player.Exists())
                {
                    FileLogger.Warn("GivePlayerWeapon: Player doesn't exist");
                    return;
                }

                // Get weapon hash from name
                var weaponHash = GetWeaponHash(weaponName);

                // Give the weapon with specified ammo and equip it
                var weapon = player.Weapons.Give(weaponHash, ammo, true, true);

                // Explicitly select the weapon
                if (weapon != null)
                {
                    player.Weapons.Select(weapon);
                }

                FileLogger.Info($"GivePlayerWeapon: Gave player {weaponName} with {ammo} ammo");
            }
            catch (Exception ex)
            {
                FileLogger.Error($"GivePlayerWeapon exception for weapon {weaponName}", ex);
            }
        }

        /// <inheritdoc />
        public void ConfigurePlayerSettings()
        {
            try
            {
                var player = Game.Player.Character;
                if (player == null || !player.Exists())
                {
                    FileLogger.Warn("ConfigurePlayerSettings: Player doesn't exist");
                    return;
                }

                // Prevent player from dropping weapons when killed
                // This makes weapons persist across deaths
                Function.Call(Hash.SET_PED_DROPS_WEAPONS_WHEN_DEAD, player.Handle, false);

                FileLogger.Info("ConfigurePlayerSettings: Player weapon drop on death disabled");
            }
            catch (Exception ex)
            {
                FileLogger.Error("ConfigurePlayerSettings exception", ex);
            }
        }

        /// <inheritdoc />
        public void SetPlayerMaxHealth(int maxHealth)
        {
            try
            {
                var player = Game.Player.Character;
                if (player == null || !player.Exists()) return;
                Function.Call(Hash.SET_PED_MAX_HEALTH, player.Handle, maxHealth);
                Function.Call(Hash.SET_ENTITY_HEALTH, player.Handle, maxHealth);
                FileLogger.Info($"SetPlayerMaxHealth: max={maxHealth}");
            }
            catch (Exception ex) { FileLogger.Error("SetPlayerMaxHealth exception", ex); }
        }

        /// <inheritdoc />
        public void SetPlayerWeaponDamageModifier(float multiplier)
        {
            try
            {
                uint nativeHash = Function.Call<uint>(Hash.GET_HASH_KEY, "SET_PLAYER_WEAPON_DAMAGE_MODIFIER");
                Function.Call((Hash)nativeHash, Game.Player.Handle, multiplier);
                FileLogger.Info($"SetPlayerWeaponDamageModifier: x{multiplier:F2}");
            }
            catch (Exception ex) { FileLogger.Error("SetPlayerWeaponDamageModifier exception", ex); }
        }

        /// <inheritdoc />
        public void SetPlayerWeaponDefenseModifier(float multiplier)
        {
            try
            {
                uint nativeHash = Function.Call<uint>(Hash.GET_HASH_KEY, "SET_PLAYER_WEAPON_DEFENSE_MODIFIER");
                Function.Call((Hash)nativeHash, Game.Player.Handle, multiplier);
                FileLogger.Info($"SetPlayerWeaponDefenseModifier: x{multiplier:F2}");
            }
            catch (Exception ex) { FileLogger.Error("SetPlayerWeaponDefenseModifier exception", ex); }
        }
    }
}
