using System.Collections.Generic;
using FactionWars.Combat.Models;

namespace FactionWars.Combat.Interfaces
{
    /// <summary>
    /// Maps each bodyguard to a known enemy target. Assignment is sticky: a bodyguard
    /// keeps its previous target while that target is still a live enemy, so commitments
    /// do not thrash. New assignments disperse across distinct enemies, biased toward
    /// enemies far from already-assigned targets so bodyguards fan out across the zone.
    /// </summary>
    public interface ITargetAssignmentResolver
    {
        IReadOnlyDictionary<int, int> Assign(
            IReadOnlyList<BodyguardPosition> bodyguards,
            IReadOnlyList<EnemyTarget> enemies,
            IReadOnlyDictionary<int, int> previousAssignment);
    }
}
