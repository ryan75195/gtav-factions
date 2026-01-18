namespace FactionWars.Lieutenants.Models
{
    /// <summary>
    /// Represents the current status of a lieutenant.
    /// </summary>
    public enum LieutenantStatus
    {
        /// <summary>
        /// Lieutenant is active and can be assigned to zones.
        /// </summary>
        Active,

        /// <summary>
        /// Lieutenant has been captured by an enemy faction.
        /// </summary>
        Captured,

        /// <summary>
        /// Lieutenant is deceased and cannot be used.
        /// </summary>
        Deceased,

        /// <summary>
        /// Lieutenant is recovering from injuries and temporarily unavailable.
        /// </summary>
        Recovering
    }
}
