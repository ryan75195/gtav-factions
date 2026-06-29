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

        /// <inheritdoc />
        public void SetPedBlockPermanentEvents(int pedHandle, bool block)
        {
            try
            {
                var ped = Entity.FromHandle(pedHandle) as Ped;
                if (ped == null || !ped.Exists())
                {
                    FileLogger.AI($"SetPedBlockPermanentEvents: ped {pedHandle} doesn't exist");
                    return;
                }

                // SET_BLOCKING_OF_NON_TEMPORARY_EVENTS via SHVDN. When true the ped ignores threat
                // events (gunfire, spotting enemies) and holds its current task, so an Escort
                // bodyguard runs all the way back to the player instead of stopping to fight.
                ped.BlockPermanentEvents = block;
                FileLogger.AI($"SetPedBlockPermanentEvents: ped {pedHandle} block={block}");
            }
            catch (Exception ex)
            {
                FileLogger.Error("SetPedBlockPermanentEvents exception", ex);
            }
        }
    }
}
