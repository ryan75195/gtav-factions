namespace FactionWars.Tension.Models
{
    /// <summary>
    /// Represents the current status of a covert operation.
    /// </summary>
    public enum CovertOperationStatus
    {
        /// <summary>
        /// The operation has been planned but not yet started.
        /// </summary>
        Pending = 0,

        /// <summary>
        /// The operation is currently being executed.
        /// </summary>
        InProgress = 1,

        /// <summary>
        /// The operation completed successfully without detection.
        /// </summary>
        Succeeded = 2,

        /// <summary>
        /// The operation failed to achieve its objective.
        /// </summary>
        Failed = 3,

        /// <summary>
        /// The operation was detected by the target faction.
        /// This may have succeeded or failed but the initiator was identified.
        /// </summary>
        Detected = 4,

        /// <summary>
        /// The operation was cancelled before completion.
        /// </summary>
        Cancelled = 5
    }
}
