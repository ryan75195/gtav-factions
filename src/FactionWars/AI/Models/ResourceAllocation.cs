using System;

namespace FactionWars.AI.Models
{
    /// <summary>
    /// Represents an allocation of resources (troops, cash) for a specific operation.
    /// Used to track how resources are distributed between attack and defense actions.
    /// </summary>
    public class ResourceAllocation
    {
        /// <summary>
        /// The ID of the target zone for this allocation.
        /// </summary>
        public string TargetZoneId { get; }

        /// <summary>
        /// Number of troops allocated to this operation.
        /// </summary>
        public int Troops { get; }

        /// <summary>
        /// Amount of cash allocated to this operation.
        /// </summary>
        public int Cash { get; }

        /// <summary>
        /// The type of operation this allocation is for.
        /// </summary>
        public AIDecisionType DecisionType { get; }

        /// <summary>
        /// Creates a new resource allocation.
        /// </summary>
        /// <param name="targetZoneId">The ID of the target zone.</param>
        /// <param name="troops">Number of troops to allocate (must be non-negative).</param>
        /// <param name="cash">Amount of cash to allocate (must be non-negative).</param>
        /// <param name="decisionType">The type of operation.</param>
        /// <exception cref="ArgumentOutOfRangeException">Thrown if troops or cash is negative.</exception>
        public ResourceAllocation(string targetZoneId, int troops, int cash, AIDecisionType decisionType)
        {
            if (troops < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(troops), "Troops cannot be negative.");
            }

            if (cash < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(cash), "Cash cannot be negative.");
            }

            TargetZoneId = targetZoneId;
            Troops = troops;
            Cash = cash;
            DecisionType = decisionType;
        }

        /// <summary>
        /// Checks if this allocation has any resources assigned.
        /// </summary>
        public bool HasResources => Troops > 0 || Cash > 0;

        public override string ToString()
        {
            return $"ResourceAllocation[{DecisionType}] -> {TargetZoneId}: Troops={Troops}, Cash={Cash}";
        }
    }
}
