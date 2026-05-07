using System.Collections.Generic;

namespace FactionWars.ScriptHookV.Managers
{
    public partial class FollowerManager
    {
        private void UpdateOnFootFollowers(IEnumerable<int> aliveFollowerHandles)
        {
            if (_gameBridge.IsPlayerDead())
            {
                return;
            }

            foreach (var pedHandle in aliveFollowerHandles)
            {
                if (_gameBridge.IsPedInVehicle(pedHandle))
                {
                    _gameBridge.TaskPedLeaveVehicle(pedHandle);
                    continue;
                }

                if (!_gameBridge.IsPedFollowingPlayer(pedHandle))
                {
                    _gameBridge.SetPedAsFollower(pedHandle);
                }
            }
        }
    }
}
