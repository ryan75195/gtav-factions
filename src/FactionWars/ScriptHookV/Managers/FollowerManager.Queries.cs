using FactionWars.Core.Models;
using FactionWars.UI.Interfaces;
using System.Collections.Generic;

namespace FactionWars.ScriptHookV.Managers
{
    public partial class FollowerManager
    {
        public int GetFollowerCount(string factionId)
        {
            return _followerService.GetFollowerCount(factionId);
        }

        /// <summary>
        /// Gets the maximum number of followers allowed.
        /// </summary>
        /// <returns>The maximum follower limit.</returns>
        public int GetMaxFollowers()
        {
            return _followerService.GetMaxFollowers();
        }

        /// <summary>
        /// Gets all followers belonging to a faction.
        /// </summary>
        /// <param name="factionId">The faction to get followers for.</param>
        /// <returns>A read-only list of followers.</returns>
        public IReadOnlyList<Follower> GetFollowers(string factionId)
        {
            return _followerService.GetFollowers(factionId);
        }

        /// <summary>
        /// Gets the cost to recruit a follower of the specified tier.
        /// </summary>
        /// <param name="tier">The tier to get cost for.</param>
        /// <returns>The cost in dollars.</returns>
        public int GetRecruitCost(DefenderTier tier)
        {
            var config = _defenderTierService.GetTierConfig(tier);
            return config.Cost;
        }

        /// <summary>
        /// Checks if a new follower can be recruited for the specified faction.
        /// Does not check if player has sufficient funds.
        /// </summary>
        /// <param name="factionId">The faction to check.</param>
        /// <returns>True if recruitment is possible, false otherwise.</returns>
        public bool CanRecruit(string factionId)
        {
            // Check if below max followers
            if (_followerService.GetFollowerCount(factionId) >= _followerService.GetMaxFollowers())
            {
                return false;
            }

            // Check if ped pool can spawn
            return _pedSpawningService.CanSpawn();
        }

        /// <summary>
        /// Checks if a new follower of the specified tier can be recruited for the faction,
        /// including whether the player has sufficient funds.
        /// </summary>
        /// <param name="factionId">The faction to check.</param>
        /// <param name="tier">The tier of follower to recruit.</param>
        /// <returns>True if recruitment is possible and player has funds, false otherwise.</returns>
        public bool CanRecruitWithCost(string factionId, DefenderTier tier)
        {
            // First check basic recruitment constraints
            if (!CanRecruit(factionId))
            {
                return false;
            }

            // Check if player has enough money
            var tierConfig = _defenderTierService.GetTierConfig(tier);
            var playerMoney = _gameBridge.GetPlayerMoney();
            return playerMoney >= tierConfig.Cost;
        }

        /// <summary>
        /// Configures a follower's combat attributes based on their tier.
        /// Sets weapon, accuracy, armor, health, and combat behavior.
        /// </summary>
        /// <param name="pedHandle">Handle of the ped to configure.</param>
        /// <param name="tierConfig">The tier configuration to apply.</param>
    }
}
