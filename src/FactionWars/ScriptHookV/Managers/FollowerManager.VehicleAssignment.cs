using System.Collections.Generic;

namespace FactionWars.ScriptHookV.Managers
{
    public partial class FollowerManager
    {
        // How long to wait before re-issuing a board order to a follower that hasn't boarded yet.
        // Re-spamming the order every tick is what made followers oscillate in/out during combat.
        private const int BoardReissueIntervalMs = 1500;
        private readonly Dictionary<int, int> _lastBoardOrderMs = new Dictionary<int, int>();

        // Commits each follower to a single seat so a re-issued board order keeps targeting the SAME
        // door instead of shuffling as the free-seat list shifts while others board.
        private readonly StickyVehicleSeatAssigner _seatAssigner = new StickyVehicleSeatAssigner();

        private void AssignFollowersToVehicle(List<int> aliveFollowerHandles, int playerVehicle)
        {
            var prioritizedSeats = _seatPriorityService.GetPrioritizedFreeSeats(playerVehicle);
            var nearbyFollowers = _seatPriorityService.FilterFollowersByProximity(
                aliveFollowerHandles.ToArray(), playerVehicle, 15f);

            var boarded = new HashSet<int>();
            foreach (var pedHandle in nearbyFollowers)
                if (_gameBridge.IsPedInVehicle(pedHandle)) boarded.Add(pedHandle);

            _seatAssigner.Sync(nearbyFollowers, prioritizedSeats, boarded);

            int now = _gameBridge.GetGameTime();
            foreach (var pedHandle in nearbyFollowers)
            {
                if (_gameBridge.IsPedInVehicle(pedHandle))
                {
                    _lastBoardOrderMs.Remove(pedHandle);
                    continue;
                }
                if (!_seatAssigner.TryGetSeat(pedHandle, out var seat))
                    continue; // no free seat committed to this follower yet

                // Already heading in, or ordered recently: don't re-issue. Re-spamming the board
                // order each tick is what caused the enter/exit oscillation during a battle.
                if (_gameBridge.IsPedTryingToEnterVehicle(pedHandle))
                    continue;
                if (_lastBoardOrderMs.TryGetValue(pedHandle, out var last) && now - last < BoardReissueIntervalMs)
                    continue;

                // Break off combat first so native combat AI doesn't immediately abort the enter
                // task, then order the follower to board its committed seat.
                if (_gameBridge.IsPedInCombat(pedHandle))
                    _gameBridge.ClearPedTasks(pedHandle);

                _gameBridge.TaskPedEnterVehicle(pedHandle, playerVehicle, seat);
                _lastBoardOrderMs[pedHandle] = now;
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
