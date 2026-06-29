using FactionWars.Combat.Models;
using FactionWars.Core.Models;

namespace FactionWars.Combat.Interfaces
{
    /// <summary>Decides whether a follower should advance toward or engage its assigned enemy,
    /// with hysteresis so the phase cannot flip every tick. <paramref name="msSinceLastLos"/> is the
    /// elapsed game time since the ped last held line of sight; sustained loss forces a reposition.
    /// <paramref name="msSinceReposition"/> is the elapsed game time since the ped last started a
    /// line-of-sight reposition; within a short momentum window it keeps advancing onto the target
    /// instead of re-engaging the instant LOS flickers back, so the reposition actually relocates it.</summary>
    public interface ISquadEngagementResolver
    {
        EngageDecision Resolve(
            float distToTarget,
            bool hasLineOfSight,
            DefenderRole role,
            EngagePhase currentPhase,
            int msSinceLastLos,
            int msSinceReposition);
    }
}
