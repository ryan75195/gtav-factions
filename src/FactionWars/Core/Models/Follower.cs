using System;

namespace FactionWars.Core.Models
{
    /// <summary>
    /// Represents a follower (bodyguard) that accompanies the player.
    /// Followers can be recruited from the Army menu, fight alongside the player,
    /// and persist until death or dismissal.
    /// </summary>
    public class Follower
    {
        /// <summary>
        /// Unique identifier for this follower.
        /// </summary>
        public Guid Id { get; }

        /// <summary>
        /// The faction this follower belongs to.
        /// </summary>
        public string FactionId { get; }

        /// <summary>
        /// The quality tier of this follower (Basic, Medium, or Heavy).
        /// Determines cost, weapons, health, armor, and accuracy.
        /// </summary>
        public DefenderRole Tier { get; }

        /// <summary>
        /// The ped handle for this follower in the game world.
        /// -1 if not spawned.
        /// </summary>
        public int PedHandle { get; private set; }

        /// <summary>
        /// Whether this follower is currently alive.
        /// </summary>
        public bool IsAlive { get; private set; }

        /// <summary>
        /// The UTC time when this follower was recruited.
        /// </summary>
        public DateTime RecruitedAt { get; }

        /// <summary>
        /// Creates a new follower.
        /// </summary>
        /// <param name="factionId">The faction this follower belongs to.</param>
        /// <param name="tier">The quality tier of the follower.</param>
        /// <param name="pedHandle">The ped handle, or -1 if not spawned.</param>
        /// <exception cref="ArgumentNullException">Thrown if factionId is null or empty.</exception>
        public Follower(string factionId, DefenderRole tier, int pedHandle = -1)
        {
            if (string.IsNullOrEmpty(factionId))
            {
                throw new ArgumentNullException(nameof(factionId));
            }

            Id = Guid.NewGuid();
            FactionId = factionId;
            Tier = tier;
            PedHandle = pedHandle;
            IsAlive = true;
            RecruitedAt = DateTime.UtcNow;
        }

        /// <summary>
        /// Updates the ped handle when the follower is spawned in the game world.
        /// </summary>
        /// <param name="pedHandle">The new ped handle.</param>
        public void SetPedHandle(int pedHandle)
        {
            PedHandle = pedHandle;
        }

        /// <summary>
        /// Marks the follower as dead.
        /// </summary>
        public void MarkAsDead()
        {
            IsAlive = false;
        }

        /// <summary>
        /// Gets the time since this follower was recruited.
        /// </summary>
        /// <returns>The duration since recruitment.</returns>
        public TimeSpan GetServiceTime()
        {
            return DateTime.UtcNow - RecruitedAt;
        }
    }
}
