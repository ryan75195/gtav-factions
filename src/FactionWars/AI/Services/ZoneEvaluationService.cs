using System;
using System.Collections.Generic;
using System.Linq;
using FactionWars.AI.Interfaces;
using FactionWars.AI.Models;
using FactionWars.Core.Interfaces;
using FactionWars.Territory.Models;

namespace FactionWars.AI.Services
{
    /// <summary>
    /// Service for evaluating zone attractiveness for capture.
    /// Calculates how attractive a zone is based on multiple factors:
    /// strategic value, traits, ownership, adjacency, and combat status.
    /// </summary>
    public class ZoneEvaluationService : IZoneEvaluationService
    {
        /// <summary>
        /// Maximum strategic value used for normalization.
        /// </summary>
        private const float MaxStrategicValue = 10f;

        /// <summary>
        /// Default adjacency radius for considering zones as adjacent.
        /// </summary>
        private const float AdjacencyRadius = 500f;

        /// <summary>
        /// Penalty multiplier for owned zones (they shouldn't be attack targets).
        /// </summary>
        private const float OwnedZonePenalty = 0.1f;

        /// <summary>
        /// Penalty multiplier for enemy zones (harder to capture than neutral).
        /// </summary>
        private const float EnemyZonePenalty = 0.8f;

        /// <summary>
        /// Bonus multiplier for contested enemy zones (opportunity).
        /// </summary>
        private const float ContestedOpportunityBonus = 1.15f;

        /// <summary>
        /// Maximum trait bonus to prevent excessive stacking.
        /// </summary>
        private const float MaxTraitBonus = 0.5f;

        #region Configuration Properties

        /// <inheritdoc />
        public float StrategicValueWeight { get; } = 0.4f;

        /// <inheritdoc />
        public float TraitWeight { get; } = 0.25f;

        /// <inheritdoc />
        public float AdjacencyWeight { get; } = 0.2f;

        /// <inheritdoc />
        public float NeutralZoneBonus { get; } = 1.2f;

        #endregion

        #region Trait Score Values

        private readonly Dictionary<ZoneTrait, float> _traitScores = new Dictionary<ZoneTrait, float>
        {
            { ZoneTrait.HighValue, 0.15f },
            { ZoneTrait.Commercial, 0.10f },
            { ZoneTrait.Industrial, 0.08f },
            { ZoneTrait.Residential, 0.07f },
            { ZoneTrait.Port, 0.09f },
            { ZoneTrait.Airfield, 0.08f },
            { ZoneTrait.Fortified, 0.05f }
        };

        #endregion

        /// <inheritdoc />
        public float EvaluateZone(Zone zone, AIContext context)
        {
            if (zone == null)
                throw new ArgumentNullException(nameof(zone));
            if (context == null)
                throw new ArgumentNullException(nameof(context));

            // Calculate individual score components
            float strategicScore = CalculateStrategicScore(zone);
            float traitScore = CalculateTraitScore(zone.Traits);
            float ownershipModifier = CalculateOwnershipModifier(zone, context);
            float adjacencyBonus = CalculateAdjacencyBonus(zone, context);
            float contestedModifier = CalculateContestedModifier(zone, context);

            // Combine scores with weights
            float baseScore = (strategicScore * StrategicValueWeight) +
                             (traitScore * TraitWeight) +
                             (adjacencyBonus * AdjacencyWeight);

            // Apply ownership and contested modifiers
            float finalScore = baseScore * ownershipModifier * contestedModifier;

            // Clamp to valid range
            return Math.Max(0f, Math.Min(1f, finalScore));
        }

        /// <inheritdoc />
        public IDictionary<string, float> GetZoneScoreBreakdown(Zone zone, AIContext context)
        {
            if (zone == null)
                throw new ArgumentNullException(nameof(zone));
            if (context == null)
                throw new ArgumentNullException(nameof(context));

            var breakdown = new Dictionary<string, float>
            {
                ["StrategicValue"] = CalculateStrategicScore(zone) * StrategicValueWeight,
                ["TraitBonus"] = CalculateTraitScore(zone.Traits) * TraitWeight,
                ["OwnershipModifier"] = CalculateOwnershipModifier(zone, context),
                ["AdjacencyBonus"] = CalculateAdjacencyBonus(zone, context) * AdjacencyWeight,
                ["ContestedModifier"] = CalculateContestedModifier(zone, context),
                ["TotalScore"] = EvaluateZone(zone, context)
            };

            return breakdown;
        }

        /// <inheritdoc />
        public IList<RankedZone> RankZonesByAttractiveness(IEnumerable<Zone> zones, AIContext context)
        {
            if (zones == null)
                throw new ArgumentNullException(nameof(zones));
            if (context == null)
                throw new ArgumentNullException(nameof(context));

            return zones
                .Select(z => new RankedZone(z, EvaluateZone(z, context)))
                .OrderByDescending(r => r.Score)
                .ToList();
        }

        /// <inheritdoc />
        public Zone? GetBestAttackTarget(IEnumerable<Zone> zones, AIContext context)
        {
            if (zones == null)
                throw new ArgumentNullException(nameof(zones));
            if (context == null)
                throw new ArgumentNullException(nameof(context));

            // Filter out owned zones and find the best target
            var validTargets = zones
                .Where(z => z.OwnerFactionId != context.Faction.Id)
                .ToList();

            if (!validTargets.Any())
                return null;

            var ranked = RankZonesByAttractiveness(validTargets, context);
            return ranked.FirstOrDefault()?.Zone;
        }

        /// <inheritdoc />
        public float CalculateTraitScore(ZoneTrait traits)
        {
            if (traits == ZoneTrait.None)
                return 0f;

            float totalScore = 0f;

            foreach (var traitEntry in _traitScores)
            {
                if (traits.HasFlag(traitEntry.Key))
                {
                    totalScore += traitEntry.Value;
                }
            }

            // Cap the trait bonus
            return Math.Min(totalScore, MaxTraitBonus);
        }

        #region Private Helper Methods

        /// <summary>
        /// Calculates the normalized strategic value score.
        /// </summary>
        private float CalculateStrategicScore(Zone zone)
        {
            return zone.StrategicValue / MaxStrategicValue;
        }

        /// <summary>
        /// Calculates the ownership modifier based on who owns the zone.
        /// </summary>
        private float CalculateOwnershipModifier(Zone zone, AIContext context)
        {
            // Owned by us - not a valid target
            if (zone.OwnerFactionId == context.Faction.Id)
            {
                return OwnedZonePenalty;
            }

            // Neutral zone - easier to capture
            if (zone.OwnerFactionId == null)
            {
                return NeutralZoneBonus;
            }

            // Enemy zone - harder to capture
            return EnemyZonePenalty;
        }

        /// <summary>
        /// Calculates the adjacency bonus based on proximity to owned territory.
        /// </summary>
        private float CalculateAdjacencyBonus(Zone zone, AIContext context)
        {
            if (!context.OwnedZones.Any())
            {
                // No owned territory - adjacency doesn't apply
                return 0.5f; // Neutral value
            }

            // Find the closest owned zone
            float minDistance = float.MaxValue;
            foreach (var ownedZone in context.OwnedZones)
            {
                float distance = CalculateDistance(zone.Center, ownedZone.Center);
                if (distance < minDistance)
                {
                    minDistance = distance;
                }
            }

            // Calculate adjacency score based on distance
            if (minDistance <= AdjacencyRadius)
            {
                // Adjacent - full bonus
                return 1.0f;
            }
            else if (minDistance <= AdjacencyRadius * 4)
            {
                // Close - partial bonus (linear decay)
                float normalizedDistance = (minDistance - AdjacencyRadius) / (AdjacencyRadius * 3);
                return 1.0f - (normalizedDistance * 0.5f); // Decays from 1.0 to 0.5
            }
            else
            {
                // Far away - reduced attractiveness
                float normalizedDistance = Math.Min(minDistance / (AdjacencyRadius * 10), 1f);
                return 0.5f * (1f - normalizedDistance); // Decays from 0.5 to 0
            }
        }

        /// <summary>
        /// Calculates the contested status modifier.
        /// </summary>
        private float CalculateContestedModifier(Zone zone, AIContext context)
        {
            // Contested enemy zone is an opportunity
            if (zone.IsContested && zone.OwnerFactionId != null && zone.OwnerFactionId != context.Faction.Id)
            {
                return ContestedOpportunityBonus;
            }

            return 1.0f;
        }

        /// <summary>
        /// Calculates the Euclidean distance between two points.
        /// </summary>
        private float CalculateDistance(Vector3 a, Vector3 b)
        {
            float dx = a.X - b.X;
            float dy = a.Y - b.Y;
            float dz = a.Z - b.Z;
            return (float)Math.Sqrt(dx * dx + dy * dy + dz * dz);
        }

        #endregion
    }
}
