using FactionWars.Combat.Models;

namespace FactionWars.Telemetry.Models
{
    /// <summary>A squad member's engagement snapshot for the most recent tick it was tasked: its
    /// phase, whether it held line of sight to its target, and how long sight has been broken (ms).</summary>
    public readonly struct SquadEngagementState
    {
        public SquadEngagementState(EngagePhase phase, bool hasLineOfSight, int msSinceLos)
        {
            Phase = phase;
            HasLineOfSight = hasLineOfSight;
            MsSinceLos = msSinceLos;
        }

        public EngagePhase Phase { get; }

        public bool HasLineOfSight { get; }

        public int MsSinceLos { get; }
    }
}
