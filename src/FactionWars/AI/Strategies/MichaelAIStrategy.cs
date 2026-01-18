using System;
using FactionWars.AI.Interfaces;
using FactionWars.AI.Models;
using FactionWars.Factions.Models;
using FactionWars.Territory.Models;

namespace FactionWars.AI.Strategies
{
    /// <summary>
    /// AI strategy for Michael De Santa's faction.
    /// Michael's approach is calculated, focused on high-value targets and strong defense.
    ///
    /// Characteristics:
    /// - Low aggressiveness (0.3): Takes a calculated, methodical approach
    /// - Low risk tolerance (0.3): Prefers safe, reliable operations
    /// - High-value focus: Strongly prioritizes strategic value when evaluating zones
    /// - Defense-oriented: Protects valuable holdings before expanding
    /// </summary>
    public class MichaelAIStrategy : BaseAIStrategy
    {
        /// <summary>
        /// Michael's aggressiveness - low, calculated approach.
        /// </summary>
        private const float MichaelAggressiveness = 0.3f;

        /// <summary>
        /// Michael's risk tolerance - low, prefers safe operations.
        /// </summary>
        private const float MichaelRiskTolerance = 0.3f;

        /// <summary>
        /// Bonus multiplier applied to high-value zone evaluation.
        /// Michael values strategic zones more than other factions.
        /// </summary>
        private const float HighValueBonusMultiplier = 1.5f;

        /// <summary>
        /// Threshold (as fraction of max strategic value) above which a zone is considered "high value".
        /// </summary>
        private const float HighValueThreshold = 0.6f;

        /// <summary>
        /// Creates a new Michael AI Strategy with his characteristic calculated approach.
        /// </summary>
        public MichaelAIStrategy()
            : base(FactionType.Michael, MichaelAggressiveness, MichaelRiskTolerance)
        {
        }

        /// <summary>
        /// Evaluates a zone's attractiveness as a target.
        /// Michael applies additional weight to high-value zones, reflecting his focus on quality over quantity.
        /// </summary>
        public override float EvaluateZone(Zone zone, AIContext context)
        {
            if (zone == null)
                throw new ArgumentNullException(nameof(zone));
            if (context == null)
                throw new ArgumentNullException(nameof(context));

            // Get base evaluation score
            float score = base.EvaluateZone(zone, context);

            // Apply Michael's high-value focus bonus
            float normalizedValue = zone.StrategicValue / MaxStrategicValue;
            if (normalizedValue >= HighValueThreshold)
            {
                // Apply bonus that scales with value - highest value zones get the most boost
                float bonusStrength = (normalizedValue - HighValueThreshold) / (1f - HighValueThreshold);
                score *= 1f + (bonusStrength * (HighValueBonusMultiplier - 1f));
            }

            // Clamp to valid range
            return Math.Max(0f, Math.Min(1f, score));
        }

        /// <summary>
        /// Determines whether Michael should attack a zone.
        /// Michael only attacks when the target is sufficiently valuable.
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

            // Michael's calculated approach: only attack high-value or neutral targets
            float normalizedValue = zone.StrategicValue / MaxStrategicValue;

            // Neutral zones are easier to take, lower threshold
            if (zone.OwnerFactionId == null)
            {
                return normalizedValue >= 0.5f; // Attack neutral zones with at least 50% value
            }

            // Enemy zones require higher value to justify the risk
            return normalizedValue >= 0.7f; // Only attack high-value enemy zones
        }

        /// <summary>
        /// Determines whether Michael should defend a zone.
        /// Michael is defense-focused and will defend zones with lower thresholds than other factions.
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

            // Always defend contested zones
            if (zone.IsContested)
            {
                return true;
            }

            // Michael's defensive focus: defend zones with lower threshold
            float normalizedValue = zone.StrategicValue / MaxStrategicValue;

            // Michael defends any zone with at least moderate value (50% threshold)
            // This is lower than the base strategy's typical threshold
            return normalizedValue >= 0.5f;
        }

        /// <summary>
        /// Calculates the priority for defending a zone.
        /// Michael gives higher priority to defense than the base strategy.
        /// </summary>
        protected override float CalculateDefendPriority(Zone zone, AIContext context)
        {
            float basePriority = base.CalculateDefendPriority(zone, context);

            // Michael's defense priority is boosted
            float boostedPriority = basePriority * 1.2f;

            // High-value zones get additional priority boost
            float normalizedValue = zone.StrategicValue / MaxStrategicValue;
            if (normalizedValue >= HighValueThreshold)
            {
                boostedPriority += 0.1f;
            }

            return Math.Max(0f, Math.Min(1f, boostedPriority));
        }

        /// <summary>
        /// Calculates the priority for attacking a zone.
        /// Michael's attack priority is focused on zone value rather than aggression.
        /// </summary>
        protected override float CalculateAttackPriority(Zone zone, AIContext context)
        {
            // Michael's attack priority is primarily driven by zone value
            float normalizedValue = zone.StrategicValue / MaxStrategicValue;

            // Apply high-value bonus to priority
            float priority = normalizedValue;
            if (normalizedValue >= HighValueThreshold)
            {
                priority *= HighValueBonusMultiplier;
            }

            // Neutral zones are more attractive (easier to capture)
            if (zone.OwnerFactionId == null)
            {
                priority *= 1.1f;
            }

            return Math.Max(0f, Math.Min(1f, priority));
        }

        /// <summary>
        /// Gets troops for defense, allocating generously for Michael's defensive focus.
        /// </summary>
        protected override int GetTroopsForDefense(Zone zone, AIContext context)
        {
            int baseTroops = base.GetTroopsForDefense(zone, context);

            // Michael allocates more troops to defense (up to 20% more)
            int boosted = (int)(baseTroops * 1.2f);

            return Math.Min(boosted, context.FactionState.TroopCount);
        }

        /// <summary>
        /// Gets troops for attack, being conservative for Michael's calculated approach.
        /// </summary>
        protected override int GetTroopsForAttack(Zone zone, AIContext context)
        {
            int baseTroops = base.GetTroopsForAttack(zone, context);

            // Michael is more conservative with attacks (reduces by 10%)
            int reduced = (int)(baseTroops * 0.9f);

            // Ensure we still meet minimum attack requirements
            if (reduced < MinimumAttackTroops && baseTroops >= MinimumAttackTroops)
            {
                return MinimumAttackTroops;
            }

            return reduced;
        }
    }
}
