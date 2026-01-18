namespace FactionWars.Tension.Models
{
    /// <summary>
    /// Represents the possible states of a diplomatic action throughout its lifecycle.
    /// </summary>
    public enum DiplomaticActionStatus
    {
        /// <summary>
        /// The action has been created but not yet sent to the target faction.
        /// </summary>
        Proposed = 0,

        /// <summary>
        /// The action has been sent and is awaiting a response from the target faction.
        /// </summary>
        Pending = 1,

        /// <summary>
        /// The target faction has accepted the action. Ready to be activated.
        /// </summary>
        Accepted = 2,

        /// <summary>
        /// The target faction has rejected the action. Terminal state.
        /// </summary>
        Rejected = 3,

        /// <summary>
        /// The action is currently in effect between the factions.
        /// </summary>
        Active = 4,

        /// <summary>
        /// The action has naturally expired after its duration. Terminal state.
        /// </summary>
        Expired = 5,

        /// <summary>
        /// The action was violated by one of the factions. Terminal state.
        /// </summary>
        Broken = 6,

        /// <summary>
        /// The action was cancelled before activation. Terminal state.
        /// </summary>
        Cancelled = 7
    }
}
