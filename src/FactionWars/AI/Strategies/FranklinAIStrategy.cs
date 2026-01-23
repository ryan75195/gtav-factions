using System;
using FactionWars.AI.Interfaces;
using FactionWars.AI.Models;
using FactionWars.Factions.Models;
using FactionWars.Territory.Models;

namespace FactionWars.AI.Strategies
{
    /// <summary>
    /// AI strategy for Franklin Clinton's faction.
    /// Franklin's approach is opportunistic, mobile, and flexible.
    ///
    /// Characteristics:
    /// - Medium aggressiveness (default 0.6): Balanced, opportunistic approach
    /// - Medium risk tolerance (default 0.6): Takes calculated risks for opportunities
    /// - Opportunistic focus: Targets easy wins (neutral zones, uncontested territories)
    /// - Mobility bonuses: Efficient troop usage, maintains reserves
    /// - Flexibility: Avoids overcommitting to losing battles
    /// </summary>
    public class FranklinAIStrategy : BaseAIStrategy
    {
        /// <summary>
        /// Default aggressiveness - medium, balanced opportunistic approach.
        /// </summary>
        private const float DefaultAggressiveness = 0.6f;

        /// <summary>
        /// Default risk tolerance - medium, takes calculated risks.
        /// </summary>
        private const float DefaultRiskTolerance = 0.6f;

        /// <summary>
        /// Bonus multiplier applied to neutral (easy target) zones.
        /// Franklin prefers easy wins.
        /// </summary>
        private const float OpportunityBonusMultiplier = 1.4f;

        /// <summary>
        /// Penalty applied to contested zones (already being fought over).
        /// Franklin avoids messy situations.
        /// </summary>
        private const float ContestedPenaltyMultiplier = 0.8f;

        /// <summary>
        /// Minimum strategic value threshold to consider attacking.
        /// Franklin is more selective than Trevor but less than Michael.
        /// </summary>
        private const float MinimumAttackThreshold = 0.3f;

        /// <summary>
        /// Maximum percentage of troops Franklin will commit to a single action.
        /// He maintains reserves for mobility and future opportunities.
        /// </summary>
        private const float MaxTroopCommitmentRatio = 0.7f;

        /// <summary>
        /// Creates a new Franklin AI Strategy with configurable parameters.
        /// </summary>
        /// <param name="aggressiveness">Aggressiveness level (0-1). Default is 0.6.</param>
        /// <param name="riskTolerance">Risk tolerance level (0-1). Default is 0.6.</param>
        public FranklinAIStrategy(float aggressiveness = DefaultAggressiveness, float riskTolerance = DefaultRiskTolerance)
            : base(FactionType.Franklin, aggressiveness, riskTolerance)
        {
        }

        /// <summary>
        /// Evaluates a zone's attractiveness as a target.
        /// Franklin applies opportunity bonuses to easy targets (neutral, uncontested zones).
        /// </summary>
        public override float EvaluateZone(Zone zone, AIContext context)
        {
            if (zone == null)
                throw new ArgumentNullException(nameof(zone));
            if (context == null)
                throw new ArgumentNullException(nameof(context));

            // Start with base strategic value (normalized to 0-1)
            float score = zone.StrategicValue / MaxStrategicValue;

            // Franklin's opportunistic evaluation - he PREFERS easy targets
            if (zone.OwnerFactionId == null)
            {
                // Neutral zones are prime opportunities - easy to take
                score *= OpportunityBonusMultiplier;
            }
            else if (zone.OwnerFactionId != context.Faction.Id)
            {
                // Enemy zones are harder - standard multiplier
                score *= 1.0f;
            }
            // Own zones get no modifier

            // Contested zones are messy - Franklin avoids them for attacks
            if (zone.IsContested && zone.OwnerFactionId != context.Faction.Id)
            {
                score *= ContestedPenaltyMultiplier;
            }

            // Apply Franklin's medium aggressiveness modifier
            float aggressiveness = GetAggressiveness();
            score *= (0.5f + (aggressiveness * 0.5f));

            // Clamp to valid range
            return Math.Max(0f, Math.Min(1f, score));
        }

        /// <summary>
        /// Determines whether Franklin should attack a zone.
        /// Franklin attacks when there's a good opportunity - prefers easy targets.
        /// </summary>
        public override bool ShouldAttack(Zone zone, AIContext context)
        {
            if (zone == null)
                throw new ArgumentNullException(nameof(zone));
            if (context == null)
                throw new ArgumentNullException(nameof(context));

            // Can't attack own zones
            if (zone.OwnerFactionId == context.Faction.Id)
            {
                return false;
            }

            // Need minimum troops to attack
            if (context.FactionState.TroopCount < MinimumAttackTroops)
            {
                return false;
            }

            // Franklin's opportunistic approach: balanced thresholds
            float normalizedValue = zone.StrategicValue / MaxStrategicValue;

            // Neutral zones are opportunities - lower threshold
            if (zone.OwnerFactionId == null)
            {
                return normalizedValue >= MinimumAttackThreshold;
            }

            // Enemy zones - moderate threshold (between Michael's 0.7 and Trevor's 0.2)
            return normalizedValue >= 0.4f;
        }

        /// <summary>
        /// Determines whether Franklin should defend a zone.
        /// Franklin defends strategically valuable zones and active fights.
        /// </summary>
        public override bool ShouldDefend(Zone zone, AIContext context)
        {
            if (zone == null)
                throw new ArgumentNullException(nameof(zone));
            if (context == null)
                throw new ArgumentNullException(nameof(context));

            // Can only defend owned zones
            if (zone.OwnerFactionId != context.Faction.Id)
            {
                return false;
            }

            // Contested zones need defense
            if (zone.IsContested)
            {
                return true;
            }

            // Franklin's balanced defense: defend moderate value zones
            float normalizedValue = zone.StrategicValue / MaxStrategicValue;

            // Defend zones with at least moderate value (60% threshold)
            return normalizedValue >= 0.6f;
        }

        /// <summary>
        /// Calculates the priority for defending a zone.
        /// Franklin has balanced defense priorities.
        /// </summary>
        protected override float CalculateDefendPriority(Zone zone, AIContext context)
        {
            float basePriority = base.CalculateDefendPriority(zone, context);

            // Franklin's defense priority is slightly boosted for contested zones
            if (zone.IsContested)
            {
                basePriority += 0.15f;
            }

            return Math.Max(0f, Math.Min(1f, basePriority));
        }

        /// <summary>
        /// Calculates the priority for attacking a zone.
        /// Franklin prioritizes easy opportunities (neutral, uncontested zones).
        /// </summary>
        protected override float CalculateAttackPriority(Zone zone, AIContext context)
        {
            float basePriority = base.CalculateAttackPriority(zone, context);

            // Neutral zones get priority boost (easy opportunities)
            if (zone.OwnerFactionId == null)
            {
                basePriority *= 1.2f;
            }

            // Contested zones get priority penalty (messy situations)
            if (zone.IsContested)
            {
                basePriority *= 0.7f;
            }

            return Math.Max(0f, Math.Min(1f, basePriority));
        }

        /// <summary>
        /// Gets troops for defense - Franklin allocates adequately but not excessively.
        /// </summary>
        protected override int GetTroopsForDefense(Zone zone, AIContext context)
        {
            int baseTroops = base.GetTroopsForDefense(zone, context);
            int available = context.FactionState.TroopCount;

            // Franklin defends adequately but maintains reserves
            int allocated = baseTroops;

            // Contested zones get more commitment
            if (zone.IsContested)
            {
                allocated = (int)(allocated * 1.1f);
            }

            // Cap at max commitment ratio to maintain reserves
            int maxTroops = (int)(available * MaxTroopCommitmentRatio);

            return Math.Max(MinimumDefenseTroops, Math.Min(allocated, maxTroops));
        }

        /// <summary>
        /// Gets troops for attack - Franklin is efficient, not over-committing.
        /// Maintains reserves for mobility and future opportunities.
        /// </summary>
        protected override int GetTroopsForAttack(Zone zone, AIContext context)
        {
            int baseTroops = base.GetTroopsForAttack(zone, context);
            int available = context.FactionState.TroopCount;

            // Franklin is efficient - slightly reduced commitment
            int allocated = (int)(baseTroops * 0.95f);

            // Easy targets (neutral zones) need fewer troops
            if (zone.OwnerFactionId == null)
            {
                allocated = (int)(allocated * 0.9f);
            }

            // Cap at max commitment ratio to maintain reserves
            int maxTroops = (int)(available * MaxTroopCommitmentRatio);

            // Ensure minimum attack requirements
            if (allocated < MinimumAttackTroops && available >= MinimumAttackTroops)
            {
                return MinimumAttackTroops;
            }

            return Math.Min(allocated, maxTroops);
        }
    }
}
