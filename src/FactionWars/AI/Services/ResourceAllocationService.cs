using System;
using System.Collections.Generic;
using System.Linq;
using FactionWars.AI.Interfaces;
using FactionWars.AI.Models;
using FactionWars.Territory.Models;

namespace FactionWars.AI.Services
{
    /// <summary>
    /// Service for allocating faction resources (troops, cash) between attack and defense operations.
    /// Determines optimal resource distribution based on strategic priorities, zone values, and current state.
    /// </summary>
    public class ResourceAllocationService : IResourceAllocationService
    {
        /// <summary>
        /// Minimum number of troops required for an attack operation.
        /// </summary>
        public const int MinimumAttackTroops = 5;

        /// <summary>
        /// Minimum number of troops required for a defense operation.
        /// </summary>
        public const int MinimumDefenseTroops = 3;

        /// <summary>
        /// Maximum strategic value used for normalization.
        /// </summary>
        private const float MaxStrategicValue = 10f;

        /// <summary>
        /// Base allocation percentage for attack operations.
        /// </summary>
        private const float BaseAttackAllocationPercent = 0.3f;

        /// <summary>
        /// Base allocation percentage for defense operations.
        /// </summary>
        private const float BaseDefenseAllocationPercent = 0.25f;

        /// <summary>
        /// Multiplier for contested zone defense allocation.
        /// </summary>
        private const float ContestedDefenseMultiplier = 1.5f;

        /// <summary>
        /// Multiplier for enemy zone attack allocation (harder than neutral).
        /// </summary>
        private const float EnemyZoneAttackMultiplier = 1.2f;

        /// <summary>
        /// Defense strength bonus (defenders have advantage).
        /// </summary>
        private const float DefenseStrengthBonus = 1.25f;

        /// <summary>
        /// Cash effectiveness multiplier for strength calculations.
        /// </summary>
        private const float CashEffectivenessMultiplier = 0.001f;

        /// <summary>
        /// Reserve troops per owned zone for emergencies.
        /// </summary>
        private const int ReservePerZone = 2;

        /// <summary>
        /// Minimum reserve percentage of total troops.
        /// </summary>
        private const float MinReservePercent = 0.1f;

        /// <inheritdoc />
        public ResourceAllocation AllocateForAttack(Zone targetZone, AIContext context)
        {
            if (targetZone == null)
                throw new ArgumentNullException(nameof(targetZone));
            if (context == null)
                throw new ArgumentNullException(nameof(context));

            int availableTroops = context.FactionState.TroopCount;
            int availableCash = context.FactionState.Cash;

            // Check if we have minimum troops
            if (availableTroops < MinimumAttackTroops)
            {
                return new ResourceAllocation(targetZone.Id, 0, 0, AIDecisionType.Attack);
            }

            // Calculate base allocation based on zone value
            float zoneValueFactor = targetZone.StrategicValue / MaxStrategicValue;
            float baseAllocation = BaseAttackAllocationPercent + (zoneValueFactor * 0.3f);

            // Adjust for enemy zones (need more troops)
            if (targetZone.OwnerFactionId != null)
            {
                baseAllocation *= EnemyZoneAttackMultiplier;
            }

            // Calculate troop allocation
            int troops = (int)(availableTroops * baseAllocation);
            troops = Math.Max(MinimumAttackTroops, Math.Min(troops, availableTroops));

            // Calculate cash allocation (10-20% based on zone value)
            float cashAllocation = 0.1f + (zoneValueFactor * 0.1f);
            int cash = (int)(availableCash * cashAllocation);

            return new ResourceAllocation(targetZone.Id, troops, cash, AIDecisionType.Attack);
        }

        /// <inheritdoc />
        public ResourceAllocation AllocateForDefense(Zone zone, AIContext context)
        {
            if (zone == null)
                throw new ArgumentNullException(nameof(zone));
            if (context == null)
                throw new ArgumentNullException(nameof(context));

            int availableTroops = context.FactionState.TroopCount;
            int availableCash = context.FactionState.Cash;

            // No troops available
            if (availableTroops == 0)
            {
                return new ResourceAllocation(zone.Id, 0, 0, AIDecisionType.Defend);
            }

            // Calculate base allocation based on zone value
            float zoneValueFactor = zone.StrategicValue / MaxStrategicValue;
            float baseAllocation = BaseDefenseAllocationPercent + (zoneValueFactor * 0.25f);

            // Contested zones need more defense
            if (zone.IsContested)
            {
                baseAllocation *= ContestedDefenseMultiplier;
            }

            // Calculate troop allocation
            int troops = (int)(availableTroops * baseAllocation);
            troops = Math.Max(MinimumDefenseTroops, Math.Min(troops, availableTroops));

            // Calculate cash allocation (5-15% based on zone value)
            float cashAllocation = 0.05f + (zoneValueFactor * 0.1f);
            int cash = (int)(availableCash * cashAllocation);

            return new ResourceAllocation(zone.Id, troops, cash, AIDecisionType.Defend);
        }

        /// <inheritdoc />
        public IList<ResourceAllocation> AllocateResources(IList<AIDecision> decisions, AIContext context)
        {
            if (decisions == null)
                throw new ArgumentNullException(nameof(decisions));
            if (context == null)
                throw new ArgumentNullException(nameof(context));

            if (!decisions.Any())
            {
                return new List<ResourceAllocation>();
            }

            var allocations = new List<ResourceAllocation>();
            int remainingTroops = context.FactionState.TroopCount;
            int remainingCash = context.FactionState.Cash;

            // Calculate reserve troops
            int reserve = GetRecommendedReserve(context);
            int availableForAllocation = Math.Max(0, remainingTroops - reserve);

            // Sort decisions by priority (highest first), with defense taking precedence at equal priority
            var sortedDecisions = decisions
                .OrderByDescending(d => d.Priority)
                .ThenByDescending(d => d.DecisionType == AIDecisionType.Defend ? 1 : 0)
                .ToList();

            // Calculate total priority weight for proportional allocation
            float totalPriority = sortedDecisions.Sum(d => d.Priority);
            if (totalPriority == 0) totalPriority = 1; // Avoid division by zero

            foreach (var decision in sortedDecisions)
            {
                if (availableForAllocation <= 0)
                {
                    break;
                }

                // Calculate proportional share based on priority
                float priorityShare = decision.Priority / totalPriority;
                int requestedTroops = decision.TroopsToCommit;

                // Calculate allocation based on priority share and request
                int maxTroopsForDecision = (int)(context.FactionState.TroopCount * priorityShare);
                int troopsToAllocate = Math.Min(requestedTroops, Math.Min(maxTroopsForDecision, availableForAllocation));

                // Ensure minimum troops for the operation type
                int minimumRequired = decision.DecisionType == AIDecisionType.Defend
                    ? MinimumDefenseTroops
                    : MinimumAttackTroops;

                if (troopsToAllocate < minimumRequired)
                {
                    // Can't meet minimum, skip if we don't have enough for minimum
                    if (availableForAllocation >= minimumRequired)
                    {
                        troopsToAllocate = minimumRequired;
                    }
                    else
                    {
                        continue;
                    }
                }

                // Calculate cash allocation proportionally
                int cashToAllocate = (int)(remainingCash * priorityShare * 0.5f);
                cashToAllocate = Math.Min(cashToAllocate, remainingCash);

                var allocation = new ResourceAllocation(
                    decision.TargetZoneId ?? "",
                    troopsToAllocate,
                    cashToAllocate,
                    decision.DecisionType);

                allocations.Add(allocation);
                availableForAllocation -= troopsToAllocate;
                remainingCash -= cashToAllocate;
            }

            return allocations;
        }

        /// <inheritdoc />
        public float CalculateAttackStrength(ResourceAllocation allocation)
        {
            if (allocation == null)
                throw new ArgumentNullException(nameof(allocation));

            // Base strength from troops
            float strength = allocation.Troops;

            // Cash provides a small bonus (equipment, bribes, etc.)
            strength += allocation.Cash * CashEffectivenessMultiplier;

            return strength;
        }

        /// <inheritdoc />
        public float CalculateDefenseStrength(ResourceAllocation allocation)
        {
            if (allocation == null)
                throw new ArgumentNullException(nameof(allocation));

            // Base strength from troops with defense bonus
            float strength = allocation.Troops * DefenseStrengthBonus;

            // Cash provides a small bonus (fortifications, supplies, etc.)
            strength += allocation.Cash * CashEffectivenessMultiplier;

            return strength;
        }

        /// <inheritdoc />
        public int GetRecommendedReserve(AIContext context)
        {
            if (context == null)
                throw new ArgumentNullException(nameof(context));

            int totalTroops = context.FactionState.TroopCount;
            int ownedZoneCount = context.OwnedZones.Count;

            // Calculate reserve based on owned zones
            int zoneBasedReserve = ownedZoneCount * ReservePerZone;

            // Calculate minimum percentage reserve
            int percentageReserve = (int)(totalTroops * MinReservePercent);

            // Take the higher of the two
            int reserve = Math.Max(zoneBasedReserve, percentageReserve);

            // Never exceed total troops
            return Math.Min(reserve, totalTroops);
        }
    }
}
