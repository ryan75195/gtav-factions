using System;
using System.Collections.Generic;
using System.Linq;
using FactionWars.AI.Interfaces;
using FactionWars.AI.Models;
using FactionWars.Factions.Models;

namespace FactionWars.AI.Services
{
    /// <summary>
    /// Service that tracks aggression against AI factions and determines appropriate responses.
    /// This enables AI factions to react dynamically to player attacks.
    /// </summary>
    public class AggressionResponseService : IAggressionResponseService
    {
        private const float MaxThreatDamage = 150f;
        private const float LowThreatThreshold = 0.1f;
        private const float HighThreatThreshold = 0.4f;
        private const int MinimumTroopsForRetaliation = 20;
        private const int MinimumDefenseTroops = 3;
        private const int MinimumAttackTroops = 5;

        private readonly Dictionary<string, List<AggressionRecord>> _aggressionByAggressor;

        /// <summary>
        /// Creates a new aggression response service.
        /// </summary>
        public AggressionResponseService()
        {
            _aggressionByAggressor = new Dictionary<string, List<AggressionRecord>>();
        }

        /// <inheritdoc/>
        public void RecordAggression(string aggressorId, string targetZoneId, int damage)
        {
            if (aggressorId == null)
                throw new ArgumentNullException(nameof(aggressorId));
            if (string.IsNullOrWhiteSpace(aggressorId))
                throw new ArgumentException("Aggressor ID cannot be empty or whitespace.", nameof(aggressorId));

            if (targetZoneId == null)
                throw new ArgumentNullException(nameof(targetZoneId));
            if (string.IsNullOrWhiteSpace(targetZoneId))
                throw new ArgumentException("Target zone ID cannot be empty or whitespace.", nameof(targetZoneId));

            if (damage < 0)
                throw new ArgumentOutOfRangeException(nameof(damage), "Damage cannot be negative.");

            var record = new AggressionRecord(aggressorId, targetZoneId, damage);

            if (!_aggressionByAggressor.TryGetValue(aggressorId, out var records))
            {
                records = new List<AggressionRecord>();
                _aggressionByAggressor[aggressorId] = records;
            }

            records.Add(record);
        }

        /// <inheritdoc/>
        public float GetThreatLevel(string aggressorId, string defenderId)
        {
            if (aggressorId == null)
                throw new ArgumentNullException(nameof(aggressorId));
            if (defenderId == null)
                throw new ArgumentNullException(nameof(defenderId));

            if (!_aggressionByAggressor.TryGetValue(aggressorId, out var records) || records.Count == 0)
            {
                return 0f;
            }

            // Calculate total damage
            int totalDamage = records.Sum(r => r.DamageDealt);

            // Normalize to 0-1 range
            float threatLevel = Math.Min(1f, totalDamage / MaxThreatDamage);

            return threatLevel;
        }

        /// <inheritdoc/>
        public AggressionResponse GetAggressionResponse(AIContext context, string aggressorId)
        {
            if (context == null)
                throw new ArgumentNullException(nameof(context));
            if (aggressorId == null)
                throw new ArgumentNullException(nameof(aggressorId));

            float threatLevel = GetThreatLevel(aggressorId, context.Faction.Id);

            if (threatLevel <= 0f)
            {
                return AggressionResponse.NoResponse;
            }

            var decisions = new List<AIDecision>();
            var responseType = DetermineResponseType(threatLevel, context);

            switch (responseType)
            {
                case AggressionResponseType.Defensive:
                    decisions = GenerateDefensiveDecisions(context, aggressorId, threatLevel);
                    break;
                case AggressionResponseType.Retaliation:
                    decisions = GenerateRetaliationDecisions(context, aggressorId, threatLevel);
                    break;
                case AggressionResponseType.Escalation:
                    decisions = GenerateEscalationDecisions(context, aggressorId, threatLevel);
                    break;
            }

            return new AggressionResponse(responseType, decisions, threatLevel);
        }

        /// <inheritdoc/>
        public void DecayThreatLevels(float decayRate)
        {
            if (decayRate < 0f || decayRate > 1f)
                throw new ArgumentOutOfRangeException(nameof(decayRate), "Decay rate must be between 0 and 1.");

            if (decayRate == 0f)
            {
                return; // No decay
            }

            if (decayRate >= 1f)
            {
                // Full decay - clear all records
                _aggressionByAggressor.Clear();
                return;
            }

            // Partial decay - reduce damage values on all records
            // Create a copy of keys to avoid modifying during iteration
            var aggressorKeys = _aggressionByAggressor.Keys.ToList();
            var aggressorsToRemove = new List<string>();

            foreach (var aggressorId in aggressorKeys)
            {
                var newRecords = DecayRecords(_aggressionByAggressor[aggressorId], decayRate);
                if (newRecords.Count > 0)
                {
                    _aggressionByAggressor[aggressorId] = newRecords;
                }
                else
                {
                    aggressorsToRemove.Add(aggressorId);
                }
            }

            foreach (var aggressor in aggressorsToRemove)
            {
                _aggressionByAggressor.Remove(aggressor);
            }
        }

        private static List<AggressionRecord> DecayRecords(
            IEnumerable<AggressionRecord> records,
            float decayRate)
        {
            var newRecords = new List<AggressionRecord>();

            foreach (var record in records)
            {
                int reducedDamage = (int)(record.DamageDealt * (1f - decayRate));
                if (reducedDamage <= 0)
                    continue;

                newRecords.Add(new AggressionRecord(
                    record.AggressorId,
                    record.TargetZoneId,
                    reducedDamage,
                    record.Timestamp));
            }

            return newRecords;
        }

        /// <inheritdoc/>
        public IList<AggressionRecord> GetRecentAggressions(string defenderId)
        {
            if (defenderId == null)
                throw new ArgumentNullException(nameof(defenderId));

            var allRecords = new List<AggressionRecord>();
            foreach (var records in _aggressionByAggressor.Values)
            {
                allRecords.AddRange(records);
            }

            return allRecords.OrderByDescending(r => r.Timestamp).ToList();
        }

        /// <inheritdoc/>
        public void ClearAggressionHistory(string aggressorId)
        {
            if (aggressorId == null)
                throw new ArgumentNullException(nameof(aggressorId));

            _aggressionByAggressor.Remove(aggressorId);
        }

        /// <inheritdoc/>
        public bool IsUnderAttack(string defenderId)
        {
            if (defenderId == null)
                throw new ArgumentNullException(nameof(defenderId));

            // Check if there's any threat from any aggressor
            foreach (var kvp in _aggressionByAggressor)
            {
                if (kvp.Value.Any(r => r.DamageDealt > 0))
                {
                    return true;
                }
            }

            return false;
        }

        /// <inheritdoc/>
        public string? GetPrimaryThreat(string defenderId)
        {
            if (defenderId == null)
                throw new ArgumentNullException(nameof(defenderId));

            string? highestThreatAggressor = null;
            float highestThreat = 0f;

            foreach (var aggressorId in _aggressionByAggressor.Keys)
            {
                float threat = GetThreatLevel(aggressorId, defenderId);
                if (threat > highestThreat)
                {
                    highestThreat = threat;
                    highestThreatAggressor = aggressorId;
                }
            }

            return highestThreatAggressor;
        }

        #region Private Helper Methods

        private AggressionResponseType DetermineResponseType(float threatLevel, AIContext context)
        {
            int availableTroops = context.FactionState.TroopCount;
            var factionType = GetFactionType(context);

            // Very low threat or no troops = no response
            if (threatLevel < LowThreatThreshold * 0.5f || availableTroops < MinimumDefenseTroops)
            {
                return AggressionResponseType.None;
            }

            // Adjust thresholds based on faction type
            float aggressionModifier = GetAggressionModifier(factionType);
            float adjustedHighThreshold = HighThreatThreshold * (1f - aggressionModifier * 0.3f);

            // High threat and enough troops for counter-attack
            if (threatLevel >= adjustedHighThreshold && availableTroops >= MinimumTroopsForRetaliation)
            {
                // Trevor escalates quickly
                if (factionType == FactionType.Trevor && threatLevel >= adjustedHighThreshold * 0.8f)
                {
                    return AggressionResponseType.Retaliation;
                }

                return AggressionResponseType.Retaliation;
            }

            // Medium threat or limited troops = defensive
            if (threatLevel >= LowThreatThreshold)
            {
                return AggressionResponseType.Defensive;
            }

            return AggressionResponseType.None;
        }

        private FactionType GetFactionType(AIContext context)
        {
            // Try to determine faction type from context
            var factionName = context.Faction.Name.ToLowerInvariant();

            if (factionName.Contains("trevor") || factionName.Contains("philips"))
                return FactionType.Trevor;
            if (factionName.Contains("michael") || factionName.Contains("santa"))
                return FactionType.Michael;
            if (factionName.Contains("franklin") || factionName.Contains("clinton"))
                return FactionType.Franklin;

            // Default to balanced (Michael)
            return FactionType.Michael;
        }

        private float GetAggressionModifier(FactionType type)
        {
            switch (type)
            {
                case FactionType.Trevor:
                    return 0.8f; // Very aggressive
                case FactionType.Franklin:
                    return 0.5f; // Balanced
                case FactionType.Michael:
                default:
                    return 0.3f; // More defensive
            }
        }

        private List<AIDecision> GenerateDefensiveDecisions(AIContext context, string aggressorId, float threatLevel)
        {
            var decisions = new List<AIDecision>();
            int availableTroops = context.FactionState.TroopCount;

            if (availableTroops < MinimumDefenseTroops)
            {
                return decisions;
            }

            // Get attacked zones
            var attackedZoneIds = GetAttackedZoneIds(aggressorId);
            var ownedZones = context.OwnedZones.ToList();

            foreach (var zone in ownedZones)
            {
                if (attackedZoneIds.Contains(zone.Id) || zone.IsContested)
                {
                    // Calculate troops for this zone defense
                    float priority = 0.7f + (threatLevel * 0.3f);
                    int troops = Math.Min(availableTroops / 2, Math.Max(MinimumDefenseTroops, (int)(availableTroops * 0.4f)));

                    decisions.Add(new AIDecision(
                        AIDecisionType.Defend,
                        zone.Id,
                        priority,
                        troops));
                }
            }

            // If no specific zones to defend, defend highest value zone
            if (decisions.Count == 0 && ownedZones.Count > 0)
            {
                var highestValueZone = ownedZones.OrderByDescending(z => z.StrategicValue).First();
                int troops = Math.Max(MinimumDefenseTroops, (int)(availableTroops * 0.3f));

                decisions.Add(new AIDecision(
                    AIDecisionType.Defend,
                    highestValueZone.Id,
                    0.6f,
                    troops));
            }

            return decisions;
        }

        private List<AIDecision> GenerateRetaliationDecisions(AIContext context, string aggressorId, float threatLevel)
        {
            var decisions = new List<AIDecision>();
            int availableTroops = context.FactionState.TroopCount;
            var factionType = GetFactionType(context);

            // First, add defensive decisions
            var defensiveDecisions = GenerateDefensiveDecisions(context, aggressorId, threatLevel);
            int troopsForDefense = defensiveDecisions.Sum(d => d.TroopsToCommit);

            // For Michael, prioritize defense more
            if (factionType == FactionType.Michael)
            {
                decisions.AddRange(defensiveDecisions);
            }

            // Calculate remaining troops for counter-attack
            int remainingTroops = availableTroops - troopsForDefense;

            // Find aggressor's zones to attack
            var aggressorZones = context.AllZones
                .Where(z => z.OwnerFactionId == aggressorId)
                .OrderByDescending(z => z.StrategicValue)
                .ToList();

            if (aggressorZones.Count > 0 && remainingTroops >= MinimumAttackTroops)
            {
                var targetZone = aggressorZones.First();
                float attackPriority = factionType == FactionType.Trevor ? 0.9f : 0.6f;

                // Trevor commits more troops to attack
                int attackTroops = factionType == FactionType.Trevor
                    ? Math.Max(MinimumAttackTroops, (int)(remainingTroops * 0.8f))
                    : Math.Max(MinimumAttackTroops, (int)(remainingTroops * 0.5f));

                decisions.Add(new AIDecision(
                    AIDecisionType.Attack,
                    targetZone.Id,
                    attackPriority,
                    attackTroops));
            }

            // For non-Michael factions, add defense after attack decisions
            if (factionType != FactionType.Michael)
            {
                decisions.AddRange(defensiveDecisions);
            }

            return decisions.OrderByDescending(d => d.Priority).ToList();
        }

        private List<AIDecision> GenerateEscalationDecisions(AIContext context, string aggressorId, float threatLevel)
        {
            // Escalation is similar to retaliation but with maximum commitment
            var decisions = GenerateRetaliationDecisions(context, aggressorId, threatLevel);

            // Increase priority and troop commitment for all attack decisions
            var escalatedDecisions = new List<AIDecision>();
            foreach (var decision in decisions)
            {
                if (decision.DecisionType == AIDecisionType.Attack)
                {
                    escalatedDecisions.Add(new AIDecision(
                        AIDecisionType.Attack,
                        decision.TargetZoneId,
                        Math.Min(1f, decision.Priority + 0.2f),
                        (int)(decision.TroopsToCommit * 1.5f)));
                }
                else
                {
                    escalatedDecisions.Add(decision);
                }
            }

            return escalatedDecisions;
        }

        private HashSet<string> GetAttackedZoneIds(string aggressorId)
        {
            var zoneIds = new HashSet<string>();

            if (_aggressionByAggressor.TryGetValue(aggressorId, out var records))
            {
                foreach (var record in records)
                {
                    zoneIds.Add(record.TargetZoneId);
                }
            }

            return zoneIds;
        }

        #endregion
    }
}
