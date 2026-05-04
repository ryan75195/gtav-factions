using System;
using System.Linq;
using FactionWars.AI.Interfaces;
using FactionWars.AI.Models;
using FactionWars.Core.Interfaces;
using FactionWars.Territory.Models;

namespace FactionWars.AI.Services
{
    /// <summary>
    /// Service for intelligent capital deployment decisions.
    /// Helps AI factions use their cash effectively instead of hoarding.
    /// </summary>
    public class CapitalDeploymentService : ICapitalDeploymentService
    {
        private const float MaxStrategicValue = 10f;
        private const int BaseRecruitmentRate = 10;
        private const int CashDivisor = 10000;
        private const int MaxRecruitment = 50;
        private const float OverwhelmMultiplier = 3.0f;
        private const float MinCommitPercent = 0.5f;
        private const float AttackOpportunityThreshold = 0.4f;  // Lowered from 0.7 to allow expansion to mid-value zones

        private readonly IAIBudgetService _budgetService;
        private readonly IZoneDefenderAllocationService _allocationService;

        /// <summary>
        /// Creates a new capital deployment service.
        /// </summary>
        /// <param name="budgetService">Service for checking attack affordability.</param>
        /// <param name="allocationService">Service for zone defender allocations.</param>
        /// <exception cref="ArgumentNullException">Thrown if any parameter is null.</exception>
        public CapitalDeploymentService(
            IAIBudgetService budgetService,
            IZoneDefenderAllocationService allocationService)
        {
            _budgetService = budgetService ?? throw new ArgumentNullException(nameof(budgetService));
            _allocationService = allocationService ?? throw new ArgumentNullException(nameof(allocationService));
        }

        /// <inheritdoc />
        public float GetDefensePriority(Zone zone, AIContext context)
        {
            if (zone == null)
                throw new ArgumentNullException(nameof(zone));
            if (context == null)
                throw new ArgumentNullException(nameof(context));

            float threatLevel = CalculateThreatLevel(zone, context);
            float zoneValue = zone.StrategicValue / MaxStrategicValue;
            int currentDefenders = GetDefenderCount(zone.OwnerFactionId, zone.Id);

            // Formula: threatLevel * zoneValue * (10 / max(1, currentDefenders))
            return threatLevel * zoneValue * (10f / Math.Max(1, currentDefenders));
        }

        /// <inheritdoc />
        public float GetAttackOpportunity(Zone target, AIContext context)
        {
            if (target == null)
                throw new ArgumentNullException(nameof(target));
            if (context == null)
                throw new ArgumentNullException(nameof(context));

            int ourTroops = context.FactionState.TroopCount;
            int enemyDefenders = GetDefenderCount(target.OwnerFactionId, target.Id);

            // Calculate the force we would actually commit (not all troops)
            int neededForce = GetOverwhelmingAttackForce(ourTroops, enemyDefenders);

            // Use the minimum of: what we need and what we have
            // NOTE: Deployment cost removed - troops are free to deploy once recruited
            int effectiveTroops = Math.Min(neededForce, ourTroops);

            // Win probability based on effective troops we can deploy
            float winProbability = effectiveTroops / (float)(enemyDefenders * 2 + 1);

            // Zone value normalized to 0-1
            float zoneValue = target.StrategicValue / MaxStrategicValue;

            // Minimum troop requirement: need at least 10 troops to consider attack
            float hasEnoughTroops = effectiveTroops >= 10 ? 1.0f : 0.0f;

            // Result: min(1, winProbability) * zoneValue * hasEnoughTroops
            return Math.Min(1f, winProbability) * zoneValue * hasEnoughTroops;
        }

        /// <inheritdoc />
        public int GetScaledRecruitmentMax(int cash)
        {
            // Handle negative cash gracefully
            int effectiveCash = Math.Max(0, cash);

            // Formula: BaseRate + (Cash / CashDivisor), capped at MaxRecruitment
            int scaled = BaseRecruitmentRate + (effectiveCash / CashDivisor);
            return Math.Min(scaled, MaxRecruitment);
        }

        /// <inheritdoc />
        public int GetOverwhelmingAttackForce(int availableTroops, int enemyDefenders)
        {
            // Formula: max(enemyDefenders * OverwhelmMultiplier, availableTroops * MinCommitPercent)
            int overwhelm = (int)(enemyDefenders * OverwhelmMultiplier);
            int minimum = (int)(availableTroops * MinCommitPercent);
            return Math.Max(overwhelm, minimum);
        }

        /// <inheritdoc />
        public AIDecision? GetBestDecision(AIContext context)
        {
            if (context == null)
                throw new ArgumentNullException(nameof(context));

            float maxDefensePriority = GetBestDefensePriority(context, out var bestDefenseZone);
            float maxAttackOpportunity = GetBestAttackOpportunity(context, out var bestAttackTarget);
            bool hasContestedZone = maxDefensePriority >= 1.0f && bestDefenseZone != null && bestDefenseZone.IsContested;

            if (hasContestedZone)
            {
                return new AIDecision(
                    AIDecisionType.Defend,
                    bestDefenseZone!.Id,
                    Math.Min(1f, maxDefensePriority),
                    0); // Defense doesn't commit troops from reserves
            }

            // Attack if opportunity is reasonable (lowered threshold for expansion)
            if (maxAttackOpportunity >= AttackOpportunityThreshold && bestAttackTarget != null)
            {
                int enemyDefenders = GetDefenderCount(bestAttackTarget.OwnerFactionId, bestAttackTarget.Id);
                int desiredForce = GetOverwhelmingAttackForce(context.FactionState.TroopCount, enemyDefenders);

                // NOTE: Deployment cost removed - troops are free to deploy once recruited
                // Commit what we need, capped by what we have
                int troopsToCommit = Math.Min(desiredForce, context.FactionState.TroopCount);

                return new AIDecision(
                    AIDecisionType.Attack,
                    bestAttackTarget.Id,
                    maxAttackOpportunity,
                    troopsToCommit);
            }

            // Hold - no urgent action needed
            return null;
        }

        private float GetBestDefensePriority(AIContext context, out Zone? bestDefenseZone)
        {
            float maxDefensePriority = 0f;
            bestDefenseZone = null;

            foreach (var zone in context.OwnedZones)
            {
                float priority = GetDefensePriority(zone, context);
                if (priority > maxDefensePriority)
                {
                    maxDefensePriority = priority;
                    bestDefenseZone = zone;
                }
            }

            return maxDefensePriority;
        }

        private float GetBestAttackOpportunity(AIContext context, out Zone? bestAttackTarget)
        {
            float maxAttackOpportunity = 0f;
            bestAttackTarget = null;

            foreach (var zone in context.GetAdjacentAttackableZones())
            {
                float opportunity = GetAttackOpportunity(zone, context);
                if (opportunity > maxAttackOpportunity)
                {
                    maxAttackOpportunity = opportunity;
                    bestAttackTarget = zone;
                }
            }

            return maxAttackOpportunity;
        }

        /// <summary>
        /// Calculates the threat level for a zone.
        /// </summary>
        /// <param name="zone">The zone to evaluate.</param>
        /// <param name="context">The AI context.</param>
        /// <returns>Threat level: 2.0 for contested, 0.5 for adjacent enemy, 0.0 for safe.</returns>
        private float CalculateThreatLevel(Zone zone, AIContext context)
        {
            // Highest threat: zone is actively contested
            if (zone.IsContested)
                return 2.0f;

            // Medium threat: zone is adjacent to enemy territory
            if (HasAdjacentEnemyTerritory(zone, context))
                return 0.5f;

            // Safe: no immediate threat
            return 0.0f;
        }

        /// <summary>
        /// Checks if a zone has adjacent enemy-owned territory.
        /// </summary>
        /// <param name="zone">The zone to check.</param>
        /// <param name="context">The AI context.</param>
        /// <returns>True if any adjacent zone is enemy-owned.</returns>
        private bool HasAdjacentEnemyTerritory(Zone zone, AIContext context)
        {
            var enemyFactionIds = context.EnemyFactions.Select(f => f.Id).ToHashSet();

            foreach (var adjacentZoneId in zone.AdjacentZoneIds)
            {
                var adjacentZone = context.AllZones.FirstOrDefault(z => z.Id == adjacentZoneId);
                if (adjacentZone != null &&
                    adjacentZone.OwnerFactionId != null &&
                    enemyFactionIds.Contains(adjacentZone.OwnerFactionId))
                {
                    return true;
                }
            }

            return false;
        }

        /// <summary>
        /// Gets the defender count for a zone owned by a faction.
        /// </summary>
        /// <param name="factionId">The faction ID that owns the zone (may be null for neutral).</param>
        /// <param name="zoneId">The zone ID.</param>
        /// <returns>Total defenders in the zone, or 0 if none/neutral.</returns>
        private int GetDefenderCount(string? factionId, string zoneId)
        {
            if (string.IsNullOrEmpty(factionId))
                return 0;

            var allocation = _allocationService.GetAllocation(factionId!, zoneId);
            return allocation?.TotalTroops ?? 0;
        }
    }
}
