using System;
using System.Collections.Generic;
using FactionWars.Core.Models;

namespace FactionWars.Core.Interfaces
{
    /// <summary>
    /// Service for managing followers (bodyguards) that accompany the player.
    /// Followers can be recruited, dismissed, and fight alongside the player.
    /// </summary>
    public interface IFollowerService
    {
        /// <summary>
        /// Recruits a new follower for the specified faction.
        /// </summary>
        /// <param name="factionId">The faction to recruit the follower for.</param>
        /// <param name="tier">The quality tier of the follower.</param>
        /// <returns>A result indicating success or failure with the recruited follower.</returns>
        FollowerRecruitResult Recruit(string factionId, DefenderTier tier);

        /// <summary>
        /// Gets all followers belonging to a faction.
        /// </summary>
        /// <param name="factionId">The faction to get followers for.</param>
        /// <returns>A read-only list of followers.</returns>
        IReadOnlyList<Follower> GetFollowers(string factionId);

        /// <summary>
        /// Gets the current number of followers for a faction.
        /// </summary>
        /// <param name="factionId">The faction to count followers for.</param>
        /// <returns>The number of active followers.</returns>
        int GetFollowerCount(string factionId);

        /// <summary>
        /// Gets the maximum number of followers allowed.
        /// </summary>
        /// <returns>The maximum follower limit.</returns>
        int GetMaxFollowers();

        /// <summary>
        /// Dismisses a specific follower by ID.
        /// The follower is despawned and removed (no refund).
        /// </summary>
        /// <param name="followerId">The ID of the follower to dismiss.</param>
        /// <returns>True if the follower was found and dismissed, false otherwise.</returns>
        bool DismissFollower(Guid followerId);

        /// <summary>
        /// Dismisses all followers belonging to a faction.
        /// Called when player switches characters.
        /// </summary>
        /// <param name="factionId">The faction to dismiss all followers for.</param>
        void DismissAllFollowers(string factionId);

        /// <summary>
        /// Handles a follower's death in combat.
        /// The follower is marked as dead and removed (permanent loss).
        /// </summary>
        /// <param name="followerId">The ID of the follower that died.</param>
        void HandleFollowerDeath(Guid followerId);

        /// <summary>
        /// Gets a follower by their unique ID.
        /// </summary>
        /// <param name="followerId">The ID of the follower to find.</param>
        /// <returns>The follower if found, null otherwise.</returns>
        Follower? GetFollowerById(Guid followerId);
    }
}
