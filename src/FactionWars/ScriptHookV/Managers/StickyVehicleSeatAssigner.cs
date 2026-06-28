using System.Collections.Generic;

namespace FactionWars.ScriptHookV.Managers
{
    /// <summary>
    /// Commits each boarding follower to a single vehicle seat and holds that commitment until the
    /// follower boards or leaves proximity. Without this, the per-tick positional pairing of
    /// (nearby follower, free seat) reshuffled seats as others boarded, so a still-approaching
    /// follower was re-tasked to a different door every re-issue and never got in.
    /// Pure state machine: no game-bridge dependency, so it is unit-tested directly.
    /// </summary>
    public sealed class StickyVehicleSeatAssigner
    {
        private readonly Dictionary<int, int> _seatByPed = new Dictionary<int, int>();

        /// <summary>
        /// Reconciles seat commitments against the current world. Drops commitments for followers
        /// that have boarded or are no longer nearby, then assigns a seat to each still-approaching
        /// follower that has none, drawing only from seats not already committed to someone else.
        /// </summary>
        /// <param name="nearbyFollowers">Followers within boarding range, in stable order.</param>
        /// <param name="prioritizedSeats">Free seats, best-first.</param>
        /// <param name="boardedFollowers">Followers that are already in the vehicle.</param>
        public void Sync(IReadOnlyList<int> nearbyFollowers, IReadOnlyList<int> prioritizedSeats, ISet<int> boardedFollowers)
        {
            var nearbySet = new HashSet<int>(nearbyFollowers);
            foreach (var ped in new List<int>(_seatByPed.Keys))
            {
                if (!nearbySet.Contains(ped) || boardedFollowers.Contains(ped))
                    _seatByPed.Remove(ped);
            }

            // Seats already committed to still-approaching followers are off the table, so a new
            // follower never double-books a seat someone else is already walking to.
            var claimed = new HashSet<int>(_seatByPed.Values);
            var available = new Queue<int>();
            foreach (var seat in prioritizedSeats)
            {
                if (!claimed.Contains(seat)) available.Enqueue(seat);
            }

            foreach (var ped in nearbyFollowers)
            {
                if (boardedFollowers.Contains(ped) || _seatByPed.ContainsKey(ped)) continue;
                if (available.Count == 0) break;
                _seatByPed[ped] = available.Dequeue();
            }
        }

        /// <summary>Gets the seat this follower is committed to, if any.</summary>
        public bool TryGetSeat(int pedHandle, out int seat) => _seatByPed.TryGetValue(pedHandle, out seat);
    }
}
