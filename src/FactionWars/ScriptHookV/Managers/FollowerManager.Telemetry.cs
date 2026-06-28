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
