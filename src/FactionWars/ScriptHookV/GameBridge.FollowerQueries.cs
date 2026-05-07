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
    }
}
