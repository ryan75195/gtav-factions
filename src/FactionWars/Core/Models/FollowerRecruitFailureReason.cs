namespace FactionWars.Core.Models
{
    /// <summary>
    /// Reasons why follower recruitment can fail.
    /// </summary>
    public enum FollowerRecruitFailureReason
    {
        /// <summary>
        /// The player has reached the maximum number of followers.
        /// </summary>
        MaxFollowersReached,

        /// <summary>
        /// The player does not have enough money to recruit.
        /// </summary>
        InsufficientFunds,

        /// <summary>
        /// The faction ID is invalid or not found.
        /// </summary>
        InvalidFaction,

        /// <summary>
        /// Failed to spawn the follower ped in the game world.
        /// </summary>
        SpawnFailed
    }
}
