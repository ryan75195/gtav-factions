using System.Collections.Generic;
using FactionWars.Combat.Models;

namespace FactionWars.Combat.Interfaces
{
    /// <summary>
    /// Maps each bodyguard to a known enemy target. Greedy-nearest with balancing so
    /// bodyguards spread across distinct enemies before doubling up.
    /// </summary>
    public interface ITargetAssignmentResolver
    {
        IReadOnlyDictionary<int, int> Assign(IReadOnlyList<BodyguardPosition> bodyguards, IReadOnlyList<EnemyTarget> enemies);
    }
}
