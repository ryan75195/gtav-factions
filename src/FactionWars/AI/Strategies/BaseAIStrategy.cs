using System;
using System.Collections.Generic;
using System.Linq;
using FactionWars.AI.Interfaces;
using FactionWars.AI.Models;
using FactionWars.Factions.Models;
using FactionWars.ScriptHookV.Logging;
using FactionWars.Territory.Models;

namespace FactionWars.AI.Strategies
{
    /// <summary>
    /// Abstract base class for AI faction strategies.
    /// Provides common functionality for zone evaluation, decision making, and troop allocation.
    /// Derived classes can override methods to customize behavior for specific faction types.
    /// </summary>
    public abstract class BaseAIStrategy : IAIStrategy
    {
        private readonly FactionType _factionType;
        private readonly float _aggressiveness;
        private readonly float _riskTolerance;
        private readonly Random _random = new Random();
        private ICapitalDeploymentService? _capitalDeploymentService;

        /// <summary>
        /// Minimum troops required to consider an attack.
        /// </summary>
        protected const int MinimumAttackTroops = 5;

        /// <summary>
        /// Minimum troops required to defend a zone.
        /// </summary>
        protected const int MinimumDefenseTroops = 3;

        /// <summary>
        /// Maximum strategic value used for score normalization.
        /// </summary>
        protected const float MaxStrategicValue = 10f;

        /// <summary>
        /// The faction type this strategy is designed for.
        /// </summary>
        public FactionType FactionType => _factionType;

        /// <summary>
        /// Creates a new base AI strategy with the specified parameters.
        /// </summary>
        /// <param name="factionType">The faction type this strategy is for.</param>
        /// <param name="aggressiveness">Aggressiveness level (0-1), clamped to valid range.</param>
        /// <param name="riskTolerance">Risk tolerance level (0-1), clamped to valid range.</param>
        protected BaseAIStrategy(FactionType factionType, float aggressiveness, float riskTolerance)
        {
            _factionType = factionType;
            _aggressiveness = Math.Max(0f, Math.Min(1f, aggressiveness));
            _riskTolerance = Math.Max(0f, Math.Min(1f, riskTolerance));
        }

        /// <summary>
        /// Sets the capital deployment service for intelligent decision-making.
        /// When set, MakeDecisions() will delegate to this service for primary decisions.
        /// </summary>
        /// <param name="service">The capital deployment service to use.</param>
        public void SetCapitalDeploymentService(ICapitalDeploymentService service)
        {
            _capitalDeploymentService = service;
        }

        /// <summary>
        /// Gets the aggressiveness level of this strategy.
        /// </summary>
        public virtual float GetAggressiveness()
        {
            return _aggressiveness;
        }

        /// <summary>
        /// Gets the risk tolerance of this strategy.
        /// </summary>
        public virtual float GetRiskTolerance()
        {
            return _riskTolerance;
        }

        /// <summary>
        /// Evaluates a zone's attractiveness as a target.
        /// Base implementation considers strategic value and ownership.
        /// </summary>
        public virtual float EvaluateZone(Zone zone, AIContext context)
        {
            if (zone == null)
                throw new ArgumentNullException(nameof(zone));
            if (context == null)
                throw new ArgumentNullException(nameof(context));

            // Base score from strategic value (normalized to 0-1)
            float score = zone.StrategicValue / MaxStrategicValue;

            // Neutral zones are easier to capture
            if (zone.OwnerFactionId == null)
            {
                score *= 1.2f;
            }
            // Enemy zones are harder to capture
            else if (zone.OwnerFactionId != context.Faction.Id)
            {
                score *= 0.8f;
            }

            // Apply aggressiveness modifier
            score *= (0.5f + (_aggressiveness * 0.5f));

            // Clamp to 0-1 range
            return Math.Max(0f, Math.Min(1f, score));
        }

        /// <summary>
        /// Makes strategic decisions based on the current game state.
        /// Base implementation generates defend, attack, and reinforce decisions.
        /// </summary>
        public virtual IList<AIDecision> MakeDecisions(AIContext context)
        {
            if (context == null)
                throw new ArgumentNullException(nameof(context));

            var decisions = new List<AIDecision>();

            // No troops = no decisions (or hold)
            if (context.FactionState.TroopCount == 0)
            {
                return decisions;
            }

            // When capital deployment service is available, use its intelligent decision-making
            if (_capitalDeploymentService != null)
            {
                FileLogger.AI($"      [Strategy] Using CapitalDeploymentService for {context.Faction.Id}");
                var bestDecision = _capitalDeploymentService.GetBestDecision(context);

                if (bestDecision != null)
                {
                    FileLogger.AI($"      [Strategy] Service decision: {bestDecision.DecisionType} on {bestDecision.TargetZoneId}, troops={bestDecision.TroopsToCommit}");
                    decisions.Add(bestDecision);
                }
                else
                {
                    FileLogger.AI($"      [Strategy] Service decision: Hold (no action)");
                }

                return decisions;
            }

            // Fallback: Use existing logic when no capital deployment service
            // Priority 1: Defend threatened zones
            var threatenedZones = context.GetThreatenedZones().ToList();
            foreach (var zone in threatenedZones)
            {
                if (ShouldDefend(zone, context))
                {
                    float priority = CalculateDefendPriority(zone, context);
                    int troops = GetTroopsForDefense(zone, context);
                    if (troops > 0)
                    {
                        decisions.Add(new AIDecision(AIDecisionType.Defend, zone.Id, priority, troops));
                    }
                }
            }

            // Priority 2: Attack targets based on aggressiveness
            // Only attack zones adjacent to owned territory (territorial expansion)
            // Use weighted random selection to add variety (higher-value zones more likely, but not guaranteed)
            FileLogger.AI($"      [Strategy] Getting adjacent attackable zones for {context.Faction.Id}");
            var potentialTargets = context.GetAdjacentAttackableZones()
                .Where(z => ShouldAttack(z, context))
                .Select(z => new { Zone = z, Score = EvaluateZone(z, context) })
                .Where(x => x.Score > 0)
                .ToList();

            FileLogger.AI($"      [Strategy] Found {potentialTargets.Count} potential targets after filtering");
            foreach (var target in potentialTargets)
            {
                FileLogger.AI($"        - {target.Zone.Id} ({target.Zone.Name}): Score={target.Score:F3}, Owner={target.Zone.OwnerFactionId ?? "neutral"}");
            }

            if (potentialTargets.Count > 0)
            {
                // Weighted random selection based on evaluation scores
                var selectedZone = SelectTargetByWeight(potentialTargets.Select(x => (x.Zone, x.Score)).ToList());
                if (selectedZone != null)
                {
                    FileLogger.AI($"      [Strategy] Selected target: {selectedZone.Id} ({selectedZone.Name})");
                    float priority = CalculateAttackPriority(selectedZone, context);
                    int troops = GetTroopsForAttack(selectedZone, context);
                    FileLogger.AI($"      [Strategy] Attack priority={priority:F2}, troops={troops}, minimum={MinimumAttackTroops}");
                    if (troops >= MinimumAttackTroops)
                    {
                        decisions.Add(new AIDecision(AIDecisionType.Attack, selectedZone.Id, priority, troops));
                    }
                    else
                    {
                        FileLogger.AI($"      [Strategy] Not enough troops for attack (need {MinimumAttackTroops})");
                    }
                }
            }
            else
            {
                FileLogger.AI($"      [Strategy] No valid attack targets found");
            }

            // Order by priority (highest first)
            return decisions.OrderByDescending(d => d.Priority).ToList();
        }

        /// <summary>
        /// Determines whether the faction should attack a specific zone.
        /// Only checks basic constraints - CapitalDeploymentService handles prioritization.
        /// </summary>
        public virtual bool ShouldAttack(Zone zone, AIContext context)
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

            // Attack any zone - CapitalDeploymentService handles intelligent prioritization
            // via EvaluateZone scoring. Removed rigid thresholds to allow AI expansion.
            return true;
        }

        /// <summary>
        /// Determines whether the faction should defend a specific zone.
        /// </summary>
        public virtual bool ShouldDefend(Zone zone, AIContext context)
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

            // Contested zones always need defense
            if (zone.IsContested)
            {
                return true;
            }

            // High value zones might need preemptive defense
            float defenseThreshold = 0.7f * (1f - _aggressiveness); // Less aggressive = more defensive
            float zoneValue = zone.StrategicValue / MaxStrategicValue;

            return zoneValue >= defenseThreshold;
        }

        /// <summary>
        /// Calculates how many troops to commit to a specific action.
        /// </summary>
        public virtual int GetTroopsForAction(AIDecision decision, AIContext context)
        {
            if (decision == null)
                throw new ArgumentNullException(nameof(decision));
            if (context == null)
                throw new ArgumentNullException(nameof(context));

            int availableTroops = context.FactionState.TroopCount;
            if (availableTroops == 0)
            {
                return 0;
            }

            // Base allocation based on priority (30% to 70% of troops based on priority)
            float baseAllocation = 0.3f + (decision.Priority * 0.4f);
            int troops = (int)(availableTroops * baseAllocation);

            // Apply risk tolerance for attacks
            if (decision.DecisionType == AIDecisionType.Attack)
            {
                troops = (int)(troops * (0.5f + (_riskTolerance * 0.5f)));
            }

            // Ensure minimum troops for meaningful action
            int minimum = decision.DecisionType == AIDecisionType.Defend
                ? MinimumDefenseTroops
                : MinimumAttackTroops;

            if (troops < minimum && availableTroops >= minimum)
            {
                troops = minimum;
            }

            // Never exceed available troops
            return Math.Min(troops, availableTroops);
        }

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
