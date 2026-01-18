namespace FactionWars.Lieutenants.Models
{
    /// <summary>
    /// Represents the outcome of a flip mission execution.
    /// </summary>
    public class FlipMissionOutcome
    {
        /// <summary>
        /// Whether the mission was successful (lieutenant defected).
        /// </summary>
        public bool Success { get; }

        /// <summary>
        /// Whether the mission was detected by the target faction.
        /// </summary>
        public bool Detected { get; }

        /// <summary>
        /// The reason for failure, if applicable.
        /// </summary>
        public string? FailureReason { get; }

        private FlipMissionOutcome(bool success, bool detected, string? failureReason)
        {
            Success = success;
            Detected = detected;
            FailureReason = failureReason;
        }

        /// <summary>
        /// Creates a successful outcome.
        /// </summary>
        /// <param name="detected">Whether the mission was detected despite success.</param>
        /// <returns>A successful outcome.</returns>
        public static FlipMissionOutcome Succeeded(bool detected)
        {
            return new FlipMissionOutcome(true, detected, null);
        }

        /// <summary>
        /// Creates a failed outcome.
        /// </summary>
        /// <param name="detected">Whether the mission was detected.</param>
        /// <param name="failureReason">The reason for failure.</param>
        /// <returns>A failed outcome.</returns>
        public static FlipMissionOutcome Failed(bool detected, string? failureReason)
        {
            return new FlipMissionOutcome(false, detected, failureReason);
        }

        public override string ToString()
        {
            if (Success && !Detected)
                return "Success (Clean)";
            if (Success && Detected)
                return "Success (Detected)";
            if (!Success && Detected)
                return "Failed (Detected)";
            return "Failed (Silent)";
        }
    }
}
