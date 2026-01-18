namespace FactionWars.Lieutenants.Models
{
    /// <summary>
    /// Represents the status of a lieutenant flip mission.
    /// </summary>
    public enum FlipMissionStatus
    {
        /// <summary>
        /// Mission has been created but not yet started.
        /// </summary>
        Pending = 0,

        /// <summary>
        /// Mission is currently in progress.
        /// </summary>
        InProgress = 1,

        /// <summary>
        /// Mission completed successfully - lieutenant has defected.
        /// </summary>
        Succeeded = 2,

        /// <summary>
        /// Mission failed - lieutenant did not defect.
        /// </summary>
        Failed = 3,

        /// <summary>
        /// Mission was cancelled before completion.
        /// </summary>
        Cancelled = 4,

        /// <summary>
        /// Mission was detected by the target faction.
        /// </summary>
        Detected = 5
    }
}
