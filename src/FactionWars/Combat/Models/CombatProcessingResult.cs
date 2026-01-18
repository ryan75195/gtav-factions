namespace FactionWars.Combat.Models
{
    /// <summary>
    /// Represents the result of processing a completed combat encounter.
    /// Contains information about the outcome and any ownership changes.
    /// </summary>
    public class CombatProcessingResult
    {
        /// <summary>
        /// Whether the combat result was processed successfully.
        /// </summary>
        public bool IsSuccess { get; }

        /// <summary>
        /// The outcome of processing the combat result.
        /// </summary>
        public CombatResultOutcome Outcome { get; }

        /// <summary>
        /// The zone ID that was affected by the combat.
        /// </summary>
        public string ZoneId { get; }

        /// <summary>
        /// The faction ID of the new owner after combat resolution.
        /// Null if processing failed.
        /// </summary>
        public string? NewOwnerFactionId { get; }

        /// <summary>
        /// The faction ID of the previous owner before combat resolution.
        /// Null if processing failed.
        /// </summary>
        public string? PreviousOwnerFactionId { get; }

        private CombatProcessingResult(
            bool isSuccess,
            CombatResultOutcome outcome,
            string zoneId,
            string? newOwnerFactionId,
            string? previousOwnerFactionId)
        {
            IsSuccess = isSuccess;
            Outcome = outcome;
            ZoneId = zoneId;
            NewOwnerFactionId = newOwnerFactionId;
            PreviousOwnerFactionId = previousOwnerFactionId;
        }

        /// <summary>
        /// Creates a successful combat processing result.
        /// </summary>
        /// <param name="outcome">The outcome of the combat.</param>
        /// <param name="zoneId">The zone ID that was affected.</param>
        /// <param name="newOwnerFactionId">The new owner faction ID.</param>
        /// <param name="previousOwnerFactionId">The previous owner faction ID.</param>
        /// <returns>A new successful CombatProcessingResult.</returns>
        public static CombatProcessingResult Success(
            CombatResultOutcome outcome,
            string zoneId,
            string? newOwnerFactionId,
            string? previousOwnerFactionId)
        {
            return new CombatProcessingResult(true, outcome, zoneId, newOwnerFactionId, previousOwnerFactionId);
        }

        /// <summary>
        /// Creates a failed combat processing result.
        /// </summary>
        /// <param name="outcome">The outcome indicating the failure reason.</param>
        /// <param name="zoneId">The zone ID that was attempted to be affected.</param>
        /// <returns>A new failed CombatProcessingResult.</returns>
        public static CombatProcessingResult Failure(CombatResultOutcome outcome, string zoneId)
        {
            return new CombatProcessingResult(false, outcome, zoneId, null, null);
        }
    }
}
