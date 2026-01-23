using System;
using FactionWars.AI.Interfaces;
using FactionWars.AI.Models;
using FactionWars.Factions.Models;
using FactionWars.Territory.Models;

namespace FactionWars.AI.Strategies
{
    /// <summary>
    /// AI strategy for Trevor Philips' faction.
    /// Trevor's approach is aggressive, combat-focused, and risk-tolerant.
    ///
    /// Characteristics:
    /// - High aggressiveness (default 0.85): Attack-first mentality
    /// - High risk tolerance (default 0.8): Willing to take big chances
    /// - Combat focus: Prefers fighting enemies over taking neutral zones
    /// - Lower concern for strategic value: Will attack any target
    /// </summary>
    public class TrevorAIStrategy : BaseAIStrategy
    {
        /// <summary>
        /// Default aggressiveness - very high, attack-first approach.
        /// </summary>
        private const float DefaultAggressiveness = 0.85f;

        /// <summary>
        /// Default risk tolerance - high, willing to take chances.
        /// </summary>
        private const float DefaultRiskTolerance = 0.8f;

        /// <summary>
        /// Combat bonus multiplier for enemy-controlled zones.
        /// Trevor finds fighting enemies more attractive.
        /// </summary>
        private const float CombatBonusMultiplier = 1.3f;

        /// <summary>
        /// Minimum strategic value threshold to consider attacking.
        /// Trevor has a very low threshold - he'll attack almost anything.
        /// </summary>
        private const float MinimumAttackThreshold = 0.2f;

        /// <summary>
        /// Creates a new Trevor AI Strategy with configurable parameters.
        /// </summary>
        /// <param name="aggressiveness">Aggressiveness level (0-1). Default is 0.85.</param>
        /// <param name="riskTolerance">Risk tolerance level (0-1). Default is 0.8.</param>
        public TrevorAIStrategy(float aggressiveness = DefaultAggressiveness, float riskTolerance = DefaultRiskTolerance)
            : base(FactionType.Trevor, aggressiveness, riskTolerance)
        {
        }

        /// <summary>
        /// Evaluates a zone's attractiveness as a target.
        /// Trevor applies combat bonus to enemy zones - he prefers fighting.
        /// Unlike the base strategy, Trevor doesn't penalize enemy zones.
        /// </summary>
        public override float EvaluateZone(Zone zone, AIContext context)
        {
            if (zone == null)
                throw new ArgumentNullException(nameof(zone));
            if (context == null)
                throw new ArgumentNullException(nameof(context));

            // Start with base strategic value (normalized to 0-1)
            float score = zone.StrategicValue / MaxStrategicValue;

            // Trevor's unique zone evaluation - he PREFERS enemy zones (combat!)
            if (zone.OwnerFactionId != null && zone.OwnerFactionId != context.Faction.Id)
            {
                // Enemy zones get combat bonus - Trevor wants to fight
                score *= CombatBonusMultiplier;
            }
            else if (zone.OwnerFactionId == null)
            {
                // Neutral zones are okay but not as exciting as fighting
                score *= 1.1f;
            }
            // Own zones get no modifier

            // Apply Trevor's high aggressiveness modifier
            float aggressiveness = GetAggressiveness();
            score *= (0.5f + (aggressiveness * 0.5f));

            // Clamp to valid range
            return Math.Max(0f, Math.Min(1f, score));
        }

        /// <summary>
        /// Determines whether Trevor should attack a zone.
        /// Trevor attacks with minimal restraint - if it's not his, he wants it.
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

            // Trevor's aggressive approach: attack with very low thresholds
            float normalizedValue = zone.StrategicValue / MaxStrategicValue;

            // For enemy zones, Trevor wants to fight - very low threshold
            if (zone.OwnerFactionId != null)
            {
                return normalizedValue >= MinimumAttackThreshold;
            }

            // Neutral zones - still attack with low threshold
            return normalizedValue >= MinimumAttackThreshold;
        }

        /// <summary>
        /// Determines whether Trevor should defend a zone.
        /// Trevor is less defense-focused but still defends contested zones (it's a fight!).
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

            // Contested zones = fight! Trevor always defends those
            if (zone.IsContested)
            {
                return true;
            }

            // Trevor's aggressive focus means less preemptive defense
            // Only defend very high value zones when not contested
            float normalizedValue = zone.StrategicValue / MaxStrategicValue;
            return normalizedValue >= 0.8f; // High threshold for non-contested defense
        }

        /// <summary>
        /// Calculates the priority for defending a zone.
        /// Trevor gives lower priority to defense compared to base strategy.
        /// </summary>
        protected override float CalculateDefendPriority(Zone zone, AIContext context)
        {
            float basePriority = base.CalculateDefendPriority(zone, context);

            // Trevor's defense priority is reduced (he prefers attacking)
            float reducedPriority = basePriority * 0.8f;

            // But contested zones (active fight) get priority boost
            if (zone.IsContested)
            {
                reducedPriority += 0.2f;
            }

            return Math.Max(0f, Math.Min(1f, reducedPriority));
        }

        /// <summary>
        /// Calculates the priority for attacking a zone.
        /// Trevor's attack priority is boosted, especially for enemy zones.
        /// </summary>
        protected override float CalculateAttackPriority(Zone zone, AIContext context)
        {
            float basePriority = base.CalculateAttackPriority(zone, context);

            // Trevor's attack priority is boosted
            float boostedPriority = basePriority * 1.3f;

            // Enemy zones get additional combat bonus
            if (zone.OwnerFactionId != null && zone.OwnerFactionId != context.Faction.Id)
            {
                boostedPriority += 0.1f;
            }

            return Math.Max(0f, Math.Min(1f, boostedPriority));
        }

        /// <summary>
        /// Gets troops for defense - Trevor allocates less for passive defense.
        /// </summary>
        protected override int GetTroopsForDefense(Zone zone, AIContext context)
        {
            int baseTroops = base.GetTroopsForDefense(zone, context);

            // Contested zones (active combat) - Trevor commits fully
            if (zone.IsContested)
            {
                int boosted = (int)(baseTroops * 1.2f);
                return Math.Min(boosted, context.FactionState.TroopCount);
            }

            // Non-contested defense - Trevor commits less (prefers attacking)
            int reduced = (int)(baseTroops * 0.8f);
            return Math.Max(MinimumDefenseTroops, Math.Min(reduced, context.FactionState.TroopCount));
        }

        /// <summary>
        /// Gets troops for attack - Trevor commits aggressively with combat bonuses.
        /// </summary>
        protected override int GetTroopsForAttack(Zone zone, AIContext context)
        {
            int baseTroops = base.GetTroopsForAttack(zone, context);

            // Trevor's combat bonus - he commits more troops to attacks
            int boosted = (int)(baseTroops * 1.2f);

            // Enemy zones get even more troops (he wants a good fight)
            if (zone.OwnerFactionId != null && zone.OwnerFactionId != context.Faction.Id)
            {
                boosted = (int)(boosted * 1.1f);
            }

            return Math.Min(boosted, context.FactionState.TroopCount);
        }
    }
}
