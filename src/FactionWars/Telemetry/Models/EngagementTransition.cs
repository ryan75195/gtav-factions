using FactionWars.Combat.Models;

namespace FactionWars.Telemetry.Models
{
    /// <summary>One engagement phase change for a squad member, captured at the game time it occurred
    /// so the event log keeps full tick fidelity even when drained on a slower cadence.</summary>
    public readonly struct EngagementTransition
    {
        public EngagementTransition(
            int handle,
            int atMs,
            EngagePhase fromPhase,
            EngagePhase toPhase,
            EngagePhaseChangeReason reason,
            float distToTarget,
            bool hasLineOfSight,
            int msSinceLos)
        {
            Handle = handle;
            AtMs = atMs;
            FromPhase = fromPhase;
            ToPhase = toPhase;
            Reason = reason;
            DistToTarget = distToTarget;
            HasLineOfSight = hasLineOfSight;
            MsSinceLos = msSinceLos;
        }

        public int Handle { get; }

        public int AtMs { get; }

        public EngagePhase FromPhase { get; }

        public EngagePhase ToPhase { get; }

        public EngagePhaseChangeReason Reason { get; }

        public float DistToTarget { get; }

        public bool HasLineOfSight { get; }

        public int MsSinceLos { get; }
    }
}
