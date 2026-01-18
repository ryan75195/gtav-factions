namespace FactionWars.Lieutenants.Models
{
    /// <summary>
    /// Represents the result of a defection attempt.
    /// </summary>
    public class DefectionResult
    {
        /// <summary>
        /// Whether the defection attempt was successful.
        /// </summary>
        public bool Success { get; }

        /// <summary>
        /// The calculated defection chance that was used.
        /// </summary>
        public double DefectionChance { get; }

        /// <summary>
        /// The random roll that was generated (for debugging/display).
        /// </summary>
        public double Roll { get; }

        /// <summary>
        /// The reason for failure, if applicable.
        /// </summary>
        public string? FailureReason { get; }

        /// <summary>
        /// Creates a successful defection result.
        /// </summary>
        public static DefectionResult Succeeded(double defectionChance, double roll)
        {
            return new DefectionResult(true, defectionChance, roll, null);
        }

        /// <summary>
        /// Creates a failed defection result.
        /// </summary>
        public static DefectionResult Failed(double defectionChance, double roll, string? reason = null)
        {
            return new DefectionResult(false, defectionChance, roll, reason);
        }

        private DefectionResult(bool success, double defectionChance, double roll, string? failureReason)
        {
            Success = success;
            DefectionChance = defectionChance;
            Roll = roll;
            FailureReason = failureReason;
        }
    }
}
