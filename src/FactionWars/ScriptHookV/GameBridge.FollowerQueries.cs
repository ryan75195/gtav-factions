using System;
using FactionWars.ScriptHookV.Logging;
using GTA;
using GTA.Native;

namespace FactionWars.ScriptHookV
{
    public partial class GameBridge
    {
        public bool IsPedFollowingPlayer(int pedHandle)
        {
            try
            {
                var ped = Entity.FromHandle(pedHandle) as Ped;
                if (ped == null || !ped.Exists())
                    return false;

                var player = Game.Player.Character;
                if (player == null || !player.Exists())
                    return false;

                var playerGroup = Function.Call<int>(Hash.GET_PLAYER_GROUP, Game.Player.Handle);
                var pedGroup = Function.Call<int>(Hash.GET_PED_GROUP_INDEX, ped.Handle);
                return playerGroup == pedGroup;
            }
            catch
            {
                return false;
            }
        }

        /// <inheritdoc />
        public void RemovePedFromFollowerGroup(int pedHandle)
        {
            try
            {
                var ped = Entity.FromHandle(pedHandle) as Ped;
                if (ped == null || !ped.Exists())
                {
                    FileLogger.AI($"RemovePedFromFollowerGroup: ped {pedHandle} doesn't exist");
                    return;
                }

                // Detach from the player's ped group so native group-follow stops yanking the ped
                // back to the leader, allowing issued guard/combat tasks to take effect.
                Function.Call(Hash.REMOVE_PED_FROM_GROUP, ped.Handle);
                FileLogger.AI($"RemovePedFromFollowerGroup: ped {pedHandle} detached from player group");
            }
            catch (Exception ex)
            {
                FileLogger.Error("RemovePedFromFollowerGroup exception", ex);
            }
        }
    }
}
