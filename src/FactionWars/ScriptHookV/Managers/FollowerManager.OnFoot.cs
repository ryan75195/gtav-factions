using System.Collections.Generic;

namespace FactionWars.ScriptHookV.Managers
{
    public partial class FollowerManager
    {
        /// <summary>
        /// Minimum gap between re-establishing the follower group on a ped that
        /// reports as "not following". A follower given a combat task detaches from
        /// the player group, so the follow check can stay false indefinitely;
        /// without this throttle the full reconfigure ran every tick.
        /// </summary>
        private const int FollowerReassertIntervalMs = 2000;

        private readonly Dictionary<int, int> _lastFollowReassertMs = new Dictionary<int, int>();

        private void UpdateOnFootFollowers(IEnumerable<int> aliveFollowerHandles)
        {
            if (_gameBridge.IsPlayerDead())
            {
                return;
            }

            var now = _gameBridge.GetGameTime();

            foreach (var pedHandle in aliveFollowerHandles)
            {
                if (_gameBridge.IsPedInVehicle(pedHandle))
                {
                    _gameBridge.TaskPedLeaveVehicle(pedHandle);
                    continue;
                }

                if (_gameBridge.IsPedFollowingPlayer(pedHandle))
                {
                    // Back in the group — drop any throttle state so a future drop-out
                    // is repaired promptly.
                    _lastFollowReassertMs.Remove(pedHandle);
                    continue;
                }

                // Don't yank a bodyguard that's actively fighting: re-establishing the
                // group clears its tasks and cancels combat, and (because combat keeps
                // detaching it) the repair would re-fire every tick.
                if (_gameBridge.IsPedInCombat(pedHandle))
                {
                    continue;
                }

                // Rate-limit the repair so a follower that won't register as "following"
                // doesn't trigger a full SetPedAsFollower reconfigure every tick.
                if (_lastFollowReassertMs.TryGetValue(pedHandle, out var last) &&
                    now - last < FollowerReassertIntervalMs)
                {
                    continue;
                }

                _gameBridge.SetPedAsFollower(pedHandle);
                _lastFollowReassertMs[pedHandle] = now;
            }
        }
    }
}
