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
    public abstract partial class BaseAIStrategy : IAIStrategy
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
                return MakeCapitalDeploymentDecision(context);
            }

            AddDefenseDecisions(context, decisions);
            AddAttackDecision(context, decisions);
            return decisions.OrderByDescending(d => d.Priority).ToList();
        }

        private IList<AIDecision> MakeCapitalDeploymentDecision(AIContext context)
        {
            var decisions = new List<AIDecision>();
            FileLogger.AI($"      [Strategy] Using CapitalDeploymentService for {context.Faction.Id}");
            var bestDecision = _capitalDeploymentService!.GetBestDecision(context);

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

        private void AddDefenseDecisions(AIContext context, List<AIDecision> decisions)
        {
            foreach (var zone in context.GetThreatenedZones())
            {
                if (!ShouldDefend(zone, context))
                    continue;

                float priority = CalculateDefendPriority(zone, context);
                int troops = GetTroopsForDefense(zone, context);
                if (troops > 0)
                    decisions.Add(new AIDecision(AIDecisionType.Defend, zone.Id, priority, troops));
            }
        }

        private void AddAttackDecision(AIContext context, List<AIDecision> decisions)
        {
            FileLogger.AI($"      [Strategy] Getting adjacent attackable zones for {context.Faction.Id}");
            var potentialTargets = context.GetAdjacentAttackableZones()
                .Where(z => ShouldAttack(z, context))
                .Select(z => (Zone: z, Score: EvaluateZone(z, context)))
                .Where(x => x.Score > 0)
                .ToList();

            FileLogger.AI($"      [Strategy] Found {potentialTargets.Count} potential targets after filtering");
            foreach (var target in potentialTargets)
            {
                FileLogger.AI($"        - {target.Zone.Id} ({target.Zone.Name}): Score={target.Score:F3}, Owner={target.Zone.OwnerFactionId ?? "neutral"}");
            }

            if (potentialTargets.Count > 0)
            {
                AddSelectedAttackDecision(context, decisions, potentialTargets);
            }
            else
            {
                FileLogger.AI($"      [Strategy] No valid attack targets found");
            }
        }

        private void AddSelectedAttackDecision(
            AIContext context,
            List<AIDecision> decisions,
            IList<(Zone Zone, float Score)> potentialTargets)
        {
            var selectedZone = SelectTargetByWeight(potentialTargets);
            if (selectedZone == null)
                return;

            FileLogger.AI($"      [Strategy] Selected target: {selectedZone.Id} ({selectedZone.Name})");
            float priority = CalculateAttackPriority(selectedZone, context);
            int troops = GetTroopsForAttack(selectedZone, context);
            FileLogger.AI($"      [Strategy] Attack priority={priority:F2}, troops={troops}, minimum={MinimumAttackTroops}");
            if (troops >= MinimumAttackTroops)
                decisions.Add(new AIDecision(AIDecisionType.Attack, selectedZone.Id, priority, troops));
            else
                FileLogger.AI($"      [Strategy] Not enough troops for attack (need {MinimumAttackTroops})");
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
    }
}
