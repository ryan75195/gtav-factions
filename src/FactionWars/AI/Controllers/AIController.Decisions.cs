using System.Linq;
using FactionWars.AI.Models;
using FactionWars.Factions.Models;
using FactionWars.ScriptHookV.Logging;

namespace FactionWars.AI.Controllers
{
    public partial class AIController
    {
        private void MakeDecisionForFaction(string factionId)
        {
            if (!_strategies.TryGetValue(factionId, out var strategy))
            {
                FileLogger.AI($"    No strategy found for faction: {factionId}");
                return;
            }

            var faction = _factionService.GetFaction(factionId);
            if (faction == null)
            {
                FileLogger.AI($"    Faction not found: {factionId}");
                return;
            }

            var factionState = _factionService.GetFactionState(factionId);
            if (factionState == null)
            {
                FileLogger.AI($"    Faction state not found: {factionId}");
                return;
            }

            FileLogger.AI($"    {factionId} state: Cash=${factionState.Cash}, Troops={factionState.TroopCount}, Zones={factionState.ZoneCount}");

            var context = BuildAIContext(faction, factionState);
            var decisions = strategy.MakeDecisions(context);

            FileLogger.AI($"    Strategy returned {decisions.Count} decision(s)");

            foreach (var decision in decisions)
            {
                FileLogger.AI($"      Decision: {decision.DecisionType} -> {decision.TargetZoneId ?? "none"}, Priority={decision.Priority:F2}, Troops={decision.TroopsToCommit}");

                if (decision.DecisionType == AIDecisionType.Attack)
                {
                    ExecuteAttackDecision(factionId, decision);
                }
                else if (decision.DecisionType == AIDecisionType.Defend)
                {
                    ExecuteDefendDecision(factionId, factionState, decision);
                }
            }
        }

        private AIContext BuildAIContext(Faction faction, FactionState factionState)
        {
            var allZones = _zoneService.GetAllZones().ToList();
            var ownedZones = _zoneService.GetZonesByOwner(faction.Id);
            var allFactions = _factionService.GetAllFactions();
            var enemyFactions = allFactions.Where(f => f.Id != faction.Id);

            return new AIContext(
                faction,
                factionState,
                ownedZones,
                allZones,
                enemyFactions);
        }

    }
}
