namespace FactionWars.Combat.Models
{
    /// <summary>Output of <c>ISquadEngagementResolver.Resolve</c>: the new phase, the role's engage
    /// range (used as the advance stopping range), and the updated consecutive-LOS-miss counter.</summary>
    public readonly struct EngageDecision
    {
        public EngageDecision(EngagePhase phase, float engageRange, int consecutiveLosMisses)
        {
            Phase = phase;
            EngageRange = engageRange;
            ConsecutiveLosMisses = consecutiveLosMisses;
        }

        public EngagePhase Phase { get; }

        public float EngageRange { get; }

        public int ConsecutiveLosMisses { get; }
    }
}
