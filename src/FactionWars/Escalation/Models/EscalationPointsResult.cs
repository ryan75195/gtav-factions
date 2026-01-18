namespace FactionWars.Escalation.Models
{
    /// <summary>
    /// Result of an escalation points modification operation.
    /// </summary>
    public class EscalationPointsResult
    {
        /// <summary>
        /// Whether the operation was successful.
        /// </summary>
        public bool Success { get; }

        /// <summary>
        /// Whether the tier changed as a result of this operation.
        /// </summary>
        public bool TierChanged { get; }

        /// <summary>
        /// The tier before the operation (if tier changed).
        /// </summary>
        public EscalationTier OldTier { get; }

        /// <summary>
        /// The tier after the operation.
        /// </summary>
        public EscalationTier NewTier { get; }

        /// <summary>
        /// The current points after the operation.
        /// </summary>
        public int CurrentPoints { get; }

        private EscalationPointsResult(bool success, bool tierChanged, EscalationTier oldTier, EscalationTier newTier, int currentPoints)
        {
            Success = success;
            TierChanged = tierChanged;
            OldTier = oldTier;
            NewTier = newTier;
            CurrentPoints = currentPoints;
        }

        /// <summary>
        /// Creates a successful result where the tier did not change.
        /// </summary>
        public static EscalationPointsResult Succeeded(EscalationTier currentTier, int currentPoints)
        {
            return new EscalationPointsResult(true, false, currentTier, currentTier, currentPoints);
        }

        /// <summary>
        /// Creates a successful result where the tier changed.
        /// </summary>
        public static EscalationPointsResult TierChangedResult(EscalationTier oldTier, EscalationTier newTier, int currentPoints)
        {
            return new EscalationPointsResult(true, true, oldTier, newTier, currentPoints);
        }

        /// <summary>
        /// Creates a failed result.
        /// </summary>
        public static EscalationPointsResult Failed()
        {
            return new EscalationPointsResult(false, false, EscalationTier.Tier1, EscalationTier.Tier1, 0);
        }
    }
}
