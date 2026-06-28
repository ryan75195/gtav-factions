using System;
using System.Collections.Generic;
using FactionWars.Core.Models;
using FactionWars.Telemetry.Interfaces;
using FactionWars.Telemetry.Models;

namespace FactionWars.ScriptHookV.Managers
{
    public partial class FollowerManager : ITrackedCombatantSource
    {
        // Snapshot of alive followers captured each Update so the behavior sampler reads a stable,
        // already-computed set instead of re-querying the service mid-tick.
        private IReadOnlyList<TrackedCombatant> _trackedFollowers = Array.Empty<TrackedCombatant>();

        IReadOnlyList<TrackedCombatant> ITrackedCombatantSource.GetTrackedCombatants() => _trackedFollowers;

        // Handle -> role for the alive on-foot followers from the most recent Update. Lets the squad
        // stance controller look up each follower's engage range without re-querying the service.
        private IReadOnlyDictionary<int, DefenderRole> _onFootBodyguardRoles =
            new Dictionary<int, DefenderRole>();

        /// <summary>Handle → role for the alive on-foot followers from the most recent Update.
        /// Empty when the player is in a vehicle or has no faction.</summary>
        public IReadOnlyDictionary<int, DefenderRole> OnFootBodyguardRoles => _onFootBodyguardRoles;

        private void CaptureFollowerRoles(IReadOnlyList<TrackedCombatant> tracked)
        {
            var map = new Dictionary<int, DefenderRole>(tracked.Count);
            foreach (var t in tracked) map[t.Handle] = t.Role;
            _onFootBodyguardRoles = map;
        }

        private static IReadOnlyList<TrackedCombatant> BuildTrackedFollowers(
            IReadOnlyList<Follower> followers, ICollection<int> aliveHandles)
        {
            var result = new List<TrackedCombatant>();
            foreach (var follower in followers)
            {
                if (aliveHandles.Contains(follower.PedHandle))
                {
                    result.Add(new TrackedCombatant(follower.PedHandle, CombatantKind.Follower, follower.Tier));
                }
            }

            return result;
        }
    }
}
