using FactionWars.Combat.Models;
using FactionWars.Core.Models;

namespace FactionWars.Combat.Interfaces
{
    /// <summary>Decides whether a follower should advance toward or engage its assigned enemy,
    /// with hysteresis so the phase cannot flip every tick.</summary>
    public interface ISquadEngagementResolver
    {
        EngageDecision Resolve(
            float distToTarget,
            bool hasLineOfSight,
            DefenderRole role,
            EngagePhase currentPhase,
            int consecutiveLosMisses);
    }
}
