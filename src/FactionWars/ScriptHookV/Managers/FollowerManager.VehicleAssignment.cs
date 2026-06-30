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

        // Followers currently locked into the player's vehicle (can't-leave applied). Used to apply
        // the lock once on seating and restore can-leave exactly once when they stop riding.
        private readonly HashSet<int> _lockedInPlayerVehicle = new HashSet<int>();

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
                BoardOrLockFollower(pedHandle, playerVehicle, now);
        }

        // Per-follower body of the board loop: seated followers get locked in (can't leave, events
        // unblocked) once; on-foot followers with a committed seat get a board order with threat
        // events blocked so the sprint-to-seat runs straight instead of stopping to aim.
        private void BoardOrLockFollower(int pedHandle, int playerVehicle, int now)
        {
            if (_gameBridge.IsPedInVehicle(pedHandle))
            {
                _lastBoardOrderMs.Remove(pedHandle);
                // Seated: keep it in (drive-by, no bailing) and let it perceive/shoot again.
                if (_lockedInPlayerVehicle.Add(pedHandle))
                {
                    _gameBridge.SetPedBlockPermanentEvents(pedHandle, false);
                    _gameBridge.SetPedCanLeaveVehicle(pedHandle, false);
                }
                return;
            }
            if (!_seatAssigner.TryGetSeat(pedHandle, out var seat))
                return;

            // Already heading in, or ordered recently: don't re-issue. Re-spamming the board
            // order each tick is what caused the enter/exit oscillation during a battle.
            if (_gameBridge.IsPedTryingToEnterVehicle(pedHandle))
                return;
            if (_lastBoardOrderMs.TryGetValue(pedHandle, out var last) && now - last < BoardReissueIntervalMs)
                return;

            // Break off combat first so native combat AI doesn't immediately abort the enter
            // task, then order the follower to board its committed seat.
            if (_gameBridge.IsPedInCombat(pedHandle))
                _gameBridge.ClearPedTasks(pedHandle);

            // Block threat reactions so the sprint-to-seat runs straight instead of stopping to
            // aim at nearby enemies (same trick Escort uses for the on-foot sprint).
            _gameBridge.SetPedBlockPermanentEvents(pedHandle, true);
            _gameBridge.TaskPedEnterVehicle(pedHandle, playerVehicle, seat);
            _lastBoardOrderMs[pedHandle] = now;
        }

        // Restores can-leave + clears the event-block on any follower that was locked into the
        // player's vehicle but is no longer riding (player got out, or the squad is grounded for
        // HoldArea/S&D). Applied once per follower, then forgotten.
        private void RestoreLockedFollowers()
        {
            if (_lockedInPlayerVehicle.Count == 0) return;
            foreach (var pedHandle in new List<int>(_lockedInPlayerVehicle))
            {
                _gameBridge.SetPedCanLeaveVehicle(pedHandle, true);
                _gameBridge.SetPedBlockPermanentEvents(pedHandle, false);
            }
            _lockedInPlayerVehicle.Clear();
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
