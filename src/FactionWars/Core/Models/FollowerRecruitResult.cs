namespace FactionWars.Core.Models
{
    /// <summary>
    /// Result of attempting to recruit a follower.
    /// Contains success status, the recruited follower (if successful),
    /// and failure reason (if failed).
    /// </summary>
    public class FollowerRecruitResult
    {
        /// <summary>
        /// Whether the recruitment was successful.
        /// </summary>
        public bool Success { get; }

        /// <summary>
        /// The recruited follower, if successful. Null if failed.
        /// </summary>
        public Follower? Follower { get; }

        /// <summary>
        /// The reason for failure, if failed. Null if successful.
        /// </summary>
        public FollowerRecruitFailureReason? FailureReason { get; }

        private FollowerRecruitResult(bool success, Follower? follower, FollowerRecruitFailureReason? failureReason)
        {
            Success = success;
            Follower = follower;
            FailureReason = failureReason;
        }

        /// <summary>
        /// Creates a successful recruitment result.
        /// </summary>
        /// <param name="follower">The recruited follower.</param>
        /// <returns>A successful result.</returns>
        public static FollowerRecruitResult Succeeded(Follower follower)
        {
            return new FollowerRecruitResult(true, follower, null);
        }

        /// <summary>
        /// Creates a failed recruitment result.
        /// </summary>
        /// <param name="reason">The reason for failure.</param>
        /// <returns>A failed result.</returns>
        public static FollowerRecruitResult Failed(FollowerRecruitFailureReason reason)
        {
            return new FollowerRecruitResult(false, null, reason);
        }
    }

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
