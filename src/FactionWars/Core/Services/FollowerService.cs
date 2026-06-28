using System;
using System.Collections.Generic;
using System.Linq;
using FactionWars.Core.Interfaces;
using FactionWars.Core.Models;

namespace FactionWars.Core.Services
{
    /// <summary>
    /// Default implementation of IFollowerService.
    /// Manages followers (bodyguards) that accompany the player.
    /// </summary>
    public class FollowerService : IFollowerService
    {
        private readonly List<Follower> _followers;
        private readonly int _maxFollowers;

        /// <summary>
        /// Creates a new FollowerService with the specified maximum follower limit.
        /// </summary>
        /// <param name="maxFollowers">The maximum number of followers allowed (default: 6).</param>
        public FollowerService(int maxFollowers = 6)
        {
            _maxFollowers = maxFollowers;
            _followers = new List<Follower>();
        }

        /// <inheritdoc />
        public FollowerRecruitResult Recruit(string factionId, DefenderRole tier)
        {
            // Validate faction ID
            if (string.IsNullOrEmpty(factionId))
            {
                return FollowerRecruitResult.Failed(FollowerRecruitFailureReason.InvalidFaction);
            }

            // Check max follower limit (global limit across all factions)
            if (_followers.Count >= _maxFollowers)
            {
                return FollowerRecruitResult.Failed(FollowerRecruitFailureReason.MaxFollowersReached);
            }

            // Create the follower
            var follower = new Follower(factionId, tier);
            _followers.Add(follower);

            return FollowerRecruitResult.Succeeded(follower);
        }

        /// <inheritdoc />
        public IReadOnlyList<Follower> GetFollowers(string factionId)
        {
            return _followers
                .Where(f => f.FactionId == factionId)
                .ToList()
                .AsReadOnly();
        }

        /// <inheritdoc />
        public int GetFollowerCount(string factionId)
        {
            return _followers.Count(f => f.FactionId == factionId);
        }

        /// <inheritdoc />
        public int GetMaxFollowers()
        {
            return _maxFollowers;
        }

        /// <inheritdoc />
        public bool DismissFollower(Guid followerId)
        {
            var follower = _followers.FirstOrDefault(f => f.Id == followerId);
            if (follower == null)
            {
                return false;
            }

            _followers.Remove(follower);
            return true;
        }

        /// <inheritdoc />
        public void DismissAllFollowers(string factionId)
        {
            if (string.IsNullOrEmpty(factionId))
            {
                return;
            }

            _followers.RemoveAll(f => f.FactionId == factionId);
        }

        /// <inheritdoc />
        public void HandleFollowerDeath(Guid followerId)
        {
            var follower = _followers.FirstOrDefault(f => f.Id == followerId);
            if (follower == null)
            {
                return;
            }

            // Mark as dead and remove from active list
            follower.MarkAsDead();
            _followers.Remove(follower);
        }

        /// <inheritdoc />
        public Follower? GetFollowerById(Guid followerId)
        {
            return _followers.FirstOrDefault(f => f.Id == followerId);
        }
    }
}
