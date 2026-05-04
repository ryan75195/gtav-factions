using System;
using System.Collections.Generic;
using System.Linq;
using FactionWars.AI.Models;
using FactionWars.Territory.Models;

namespace FactionWars.AI.Strategies
{
    public abstract partial class BaseAIStrategy
    {
        #region Protected Helper Methods

        /// <summary>
        /// Calculates the priority for defending a zone.
        /// </summary>
        protected virtual float CalculateDefendPriority(Zone zone, AIContext context)
        {
            float basePriority = zone.StrategicValue / MaxStrategicValue;

            // Contested zones get higher priority
            if (zone.IsContested)
            {
                basePriority += 0.3f;
            }

            // Less aggressive strategies prioritize defense more
            basePriority *= (1.5f - _aggressiveness);

            return Math.Max(0f, Math.Min(1f, basePriority));
        }

        /// <summary>
        /// Calculates the priority for attacking a zone.
        /// </summary>
        protected virtual float CalculateAttackPriority(Zone zone, AIContext context)
        {
            float basePriority = EvaluateZone(zone, context);

            // Adjust based on aggressiveness
            basePriority *= (0.5f + _aggressiveness);

            return Math.Max(0f, Math.Min(1f, basePriority));
        }

        /// <summary>
        /// Gets the number of troops to use for defense.
        /// </summary>
        protected virtual int GetTroopsForDefense(Zone zone, AIContext context)
        {
            int available = context.FactionState.TroopCount;
            float zoneValue = zone.StrategicValue / MaxStrategicValue;

            // Allocate based on zone value (20% to 50% of troops)
            float allocation = 0.2f + (zoneValue * 0.3f);
            int troops = (int)(available * allocation);

            // Contested zones need more troops
            if (zone.IsContested)
            {
                troops = (int)(troops * 1.5f);
            }

            return Math.Max(MinimumDefenseTroops, Math.Min(troops, available));
        }

        /// <summary>
        /// Gets the number of troops to use for attack.
        /// </summary>
        protected virtual int GetTroopsForAttack(Zone zone, AIContext context)
        {
            int available = context.FactionState.TroopCount;
            float zoneValue = zone.StrategicValue / MaxStrategicValue;

            // Allocate based on zone value and aggressiveness (20% to 60% of troops)
            float allocation = 0.2f + (zoneValue * 0.2f) + (_aggressiveness * 0.2f);
            int troops = (int)(available * allocation);

            // Apply risk tolerance
            troops = (int)(troops * (0.7f + (_riskTolerance * 0.3f)));

            return Math.Max(MinimumAttackTroops, Math.Min(troops, available));
        }

        /// <summary>
        /// Calculates a weighted score for a target zone based on defense and ownership.
        /// Used for probability-weighted target selection.
        /// </summary>
        protected static float CalculateTargetScore(Zone zone, IDictionary<string, int> defenderCounts)
        {
            // Base score from strategic value
            float score = zone.StrategicValue;

            // Defense multiplier based on defender count
            int defenders = defenderCounts.TryGetValue(zone.Id, out var count) ? count : 0;
            float defenseMultiplier;
            if (defenders == 0)
                defenseMultiplier = 3.0f;      // Unguarded
            else if (defenders <= 3)
                defenseMultiplier = 2.0f;      // Lightly guarded
            else if (defenders <= 7)
                defenseMultiplier = 1.0f;      // Moderately guarded
            else
                defenseMultiplier = 0.3f;      // Heavily guarded

            // Ownership multiplier
            float ownershipMultiplier = zone.OwnerFactionId == null ? 1.5f : 1.0f;  // Neutral bonus

            return score * defenseMultiplier * ownershipMultiplier;
        }

        /// <summary>
        /// Selects a target zone using probability-weighted random selection.
        /// </summary>
        protected Zone? SelectTargetByProbability(
            IList<Zone> zones,
            IDictionary<string, int> defenderCounts,
            Random? random = null)
        {
            if (zones == null || zones.Count == 0)
                return null;

            random ??= new Random();

            var scores = zones.Select(z => new { Zone = z, Score = CalculateTargetScore(z, defenderCounts) }).ToList();
            var totalScore = scores.Sum(s => s.Score);

            if (totalScore <= 0)
                return zones[0];

            var roll = random.NextDouble() * totalScore;
            float cumulative = 0;

            foreach (var item in scores)
            {
                cumulative += item.Score;
                if (roll <= cumulative)
                    return item.Zone;
            }

            return scores.Last().Zone;
        }

        /// <summary>
        /// Selects a target zone using weighted random selection based on evaluation scores.
        /// Higher scores have higher probability of being selected, but selection is not deterministic.
        /// </summary>
        private Zone? SelectTargetByWeight(IList<(Zone Zone, float Score)> targets)
        {
            if (targets == null || targets.Count == 0)
                return null;

            if (targets.Count == 1)
                return targets[0].Zone;

            // Square the scores to make high-value targets more likely while still allowing variety
            var weightedTargets = targets.Select(t => (t.Zone, Weight: t.Score * t.Score)).ToList();
            var totalWeight = weightedTargets.Sum(t => t.Weight);

            if (totalWeight <= 0)
                return targets[0].Zone;

            var roll = _random.NextDouble() * totalWeight;
            float cumulative = 0;

            foreach (var item in weightedTargets)
            {
                cumulative += item.Weight;
                if (roll <= cumulative)
                    return item.Zone;
            }

            return weightedTargets.Last().Zone;
        }

        #endregion
    }
}
