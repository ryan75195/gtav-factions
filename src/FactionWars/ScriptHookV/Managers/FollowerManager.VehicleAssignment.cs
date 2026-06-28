using System.Collections.Generic;

namespace FactionWars.ScriptHookV.Managers
{
    public partial class FollowerManager
    {
        // How long to wait before re-issuing a board order to a follower that hasn't boarded yet.
        // Re-spamming the order every tick is what made followers oscillate in/out during combat.
        private const int BoardReissueIntervalMs = 1500;
        private readonly Dictionary<int, int> _lastBoardOrderMs = new Dictionary<int, int>();

        private void AssignFollowersToVehicle(List<int> aliveFollowerHandles, int playerVehicle)
        {
            var prioritizedSeats = _seatPriorityService.GetPrioritizedFreeSeats(playerVehicle);
            var nearbyFollowers = _seatPriorityService.FilterFollowersByProximity(
                aliveFollowerHandles.ToArray(), playerVehicle, 15f);

            int now = _gameBridge.GetGameTime();
            var seatIndex = 0;
            foreach (var pedHandle in nearbyFollowers)
            {
                if (seatIndex >= prioritizedSeats.Length)
                    break;

                if (_gameBridge.IsPedInVehicle(pedHandle))
                {
                    _lastBoardOrderMs.Remove(pedHandle);
                    seatIndex++;
                    continue;
                }

                // Already heading in, or ordered recently: don't re-issue. Re-spamming the board
                // order each tick is what caused the enter/exit oscillation during a battle.
                if (_gameBridge.IsPedTryingToEnterVehicle(pedHandle))
                {
                    seatIndex++;
                    continue;
                }
                if (_lastBoardOrderMs.TryGetValue(pedHandle, out var last) && now - last < BoardReissueIntervalMs)
                {
                    seatIndex++;
                    continue;
                }

                // Break off combat first so native combat AI doesn't immediately abort the enter
                // task, then order the follower to board — letting the player flee with the squad.
                if (_gameBridge.IsPedInCombat(pedHandle))
                    _gameBridge.ClearPedTasks(pedHandle);

                _gameBridge.TaskPedEnterVehicle(pedHandle, playerVehicle, prioritizedSeats[seatIndex]);
                _lastBoardOrderMs[pedHandle] = now;
                seatIndex++;
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
