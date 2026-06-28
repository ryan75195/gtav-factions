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

        private static List<int> FilterSniperHandles(IEnumerable<Follower> followers, ICollection<int> aliveHandles)
        {
            var sniperHandles = new List<int>();
            foreach (var follower in followers)
            {
                if (follower.Tier == DefenderRole.Sniper && aliveHandles.Contains(follower.PedHandle))
                {
                    sniperHandles.Add(follower.PedHandle);
                }
            }

            return sniperHandles;
        }
    }
}
