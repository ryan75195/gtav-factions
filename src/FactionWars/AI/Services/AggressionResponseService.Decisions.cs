using System;
using System.Collections.Generic;
using System.Linq;
using FactionWars.AI.Models;
using FactionWars.Factions.Models;

namespace FactionWars.AI.Services
{
    public partial class AggressionResponseService
    {
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

    }
}
