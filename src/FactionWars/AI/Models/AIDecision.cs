using System;

namespace FactionWars.AI.Models
{
    /// <summary>
    /// Represents a decision made by an AI strategy.
    /// Contains the action type, target, and resources to commit.
    /// </summary>
    public class AIDecision
    {
        private readonly float _priority;
        private readonly int _troopsToCommit;

        /// <summary>
        /// The type of decision (Attack, Defend, Reinforce, Hold, Retreat).
        /// </summary>
        public AIDecisionType DecisionType { get; }

        /// <summary>
        /// The zone ID this decision targets. May be null for Hold decisions.
        /// </summary>
        public string? TargetZoneId { get; }

        /// <summary>
        /// Priority of this decision (0.0 to 1.0). Higher means more urgent.
        /// </summary>
        public float Priority => _priority;

        /// <summary>
        /// Number of troops to commit to this action.
        /// </summary>
        public int TroopsToCommit => _troopsToCommit;

        /// <summary>
        /// Creates a new AI decision.
        /// </summary>
        /// <param name="decisionType">The type of decision to make.</param>
        /// <param name="targetZoneId">The target zone ID, or null for Hold decisions.</param>
        /// <param name="priority">Priority between 0 and 1 (clamped if outside range).</param>
        /// <param name="troopsToCommit">Number of troops to commit (must be non-negative).</param>
        /// <exception cref="ArgumentOutOfRangeException">Thrown if troopsToCommit is negative.</exception>
        public AIDecision(AIDecisionType decisionType, string? targetZoneId, float priority, int troopsToCommit)
        {
            if (troopsToCommit < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(troopsToCommit),
                    "Troops to commit cannot be negative.");
            }

            DecisionType = decisionType;
            TargetZoneId = targetZoneId;
            _priority = Math.Max(0f, Math.Min(1f, priority));
            _troopsToCommit = troopsToCommit;
        }

        public override string ToString()
        {
            return $"{DecisionType} {TargetZoneId ?? "(no target)"} - Priority: {Priority:F2}, Troops: {TroopsToCommit}";
        }
    }
}
