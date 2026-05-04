using System.Collections.Generic;

namespace FactionWars.ScriptHookV.Managers
{
    public partial class FollowerManager
    {
        private void AssignFollowersToVehicle(List<int> aliveFollowerHandles, int playerVehicle)
        {
            var prioritizedSeats = _seatPriorityService.GetPrioritizedFreeSeats(playerVehicle);
            var nearbyFollowers = _seatPriorityService.FilterFollowersByProximity(
                aliveFollowerHandles.ToArray(), playerVehicle, 15f);

            var seatIndex = 0;
            foreach (var pedHandle in nearbyFollowers)
            {
                if (seatIndex >= prioritizedSeats.Length)
                    break;

                var inVehicle = _gameBridge.IsPedInVehicle(pedHandle);
                var tryingToEnter = _gameBridge.IsPedTryingToEnterVehicle(pedHandle);

                if (!inVehicle && !tryingToEnter)
                {
                    _gameBridge.TaskPedEnterVehicle(pedHandle, playerVehicle, prioritizedSeats[seatIndex]);
                    seatIndex++;
                }
                else if (inVehicle)
                {
                    seatIndex++;
                }
            }
        }

        private void ExitFollowersFromVehicles(IEnumerable<int> aliveFollowerHandles)
        {
            foreach (var pedHandle in aliveFollowerHandles)
            {
                if (_gameBridge.IsPedInVehicle(pedHandle))
                    _gameBridge.TaskPedLeaveVehicle(pedHandle);
            }
        }

        /// <summary>
        /// Gets the current number of followers for a faction.
        /// </summary>
        /// <param name="factionId">The faction to count followers for.</param>
        /// <returns>The number of active followers.</returns>
    }
}
