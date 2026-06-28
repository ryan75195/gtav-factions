using FactionWars.Combat.Models;
using FactionWars.Core.Interfaces;

namespace FactionWars.Combat.Interfaces
{
    /// <summary>
    /// Pure geometry logic mapping a stance + anchor + bodyguard slot to a single order.
    /// Live target assignment for Search &amp; Destroy is handled separately by
    /// <see cref="ITargetAssignmentResolver"/>; this resolver returns the seek fallback.
    /// </summary>
    public interface ISquadStanceResolver
    {
        BodyguardOrder Resolve(SquadStance stance, Vector3 anchorCenter, float anchorRadius, int bodyguardIndex, int bodyguardCount);
    }
}
