using System;
using FactionWars.Core.Interfaces;
using FactionWars.ScriptHookV.Logging;
using GTA;
using GTA.Native;

namespace FactionWars.ScriptHookV
{
    public partial class GameBridge
    {
        /// <inheritdoc />
        public PlayerDeathCause GetPlayerDeathInfo()
        {
            try
            {
                var player = Game.Player.Character;
                if (player == null || !player.Exists())
                {
                    return new PlayerDeathCause(string.Empty, -1);
                }

                int killer = Function.Call<int>(Hash.GET_PED_SOURCE_OF_DEATH, player.Handle);
                int causeHash = Function.Call<int>(Hash.GET_PED_CAUSE_OF_DEATH, player.Handle);
                string weapon = causeHash == 0
                    ? string.Empty
                    : ((WeaponHash)causeHash).ToString().ToUpperInvariant();

                FileLogger.Combat($"GetPlayerDeathInfo: weapon={weapon} killerHandle={killer}");
                return new PlayerDeathCause(weapon, killer);
            }
            catch (Exception ex)
            {
                FileLogger.Error("GetPlayerDeathInfo failed", ex);
                return new PlayerDeathCause(string.Empty, -1);
            }
        }
    }
}
