namespace FactionWars.Combat.Models
{
    /// <summary>Output of <c>ISquadEngagementResolver.Resolve</c>: the new phase, the stopping range
    /// to use when advancing (the role's engage range when closing for a shot, or a short reposition
    /// range when pushing toward the target to regain line of sight), and why the phase was chosen.</summary>
    public readonly struct EngageDecision
    {
        public EngageDecision(EngagePhase phase, float advanceStopRange, EngagePhaseChangeReason reason)
        {
            Phase = phase;
            AdvanceStopRange = advanceStopRange;
            Reason = reason;
        }

        public EngagePhase Phase { get; }

        public float AdvanceStopRange { get; }

        public EngagePhaseChangeReason Reason { get; }
    }
}
