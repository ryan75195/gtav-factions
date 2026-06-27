using System.Collections.Generic;
using FactionWars.Core.Models;

namespace FactionWars.ScriptHookV.Managers
{
    public partial class FollowerManager
    {
        private List<int> GetAliveFollowerHandles(IEnumerable<Follower> followers)
        {
            var aliveFollowerHandles = new List<int>();

            foreach (var follower in followers)
            {
                if (follower.PedHandle < 0)
                {
                    continue;
                }

                // Check if the ped is dead
                if (!_gameBridge.IsPedAlive(follower.PedHandle))
                {
                    // Handle death - remove blip, delete ped and notify service
                    _pedBlipService.RemoveBlipForPed(follower.PedHandle);
                    _gameBridge.DeletePed(follower.PedHandle);
                    _followerService.HandleFollowerDeath(follower.Id);

                    // Raise event
                    FollowerDied?.Invoke(this, follower);
                    continue;
                }

                aliveFollowerHandles.Add(follower.PedHandle);
            }

            return aliveFollowerHandles;
        }
    }
}
