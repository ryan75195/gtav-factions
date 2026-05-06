using System;
using FactionWars.Combat.Models;
using FactionWars.ScriptHookV.Logging;

namespace FactionWars.AI.Controllers
{
    public partial class AIController
    {
        private float ApplyReinforcementDecay(string factionId, string zoneId, float baseDeployPercent)
        {
            if (_zoneBattleManager.GetBattleForZone(zoneId) == null)
            {
                _activeBattleReinforcementCounts.Remove(ReinforcementDecayKey(factionId, zoneId));
                return baseDeployPercent;
            }

            var key = ReinforcementDecayKey(factionId, zoneId);
            _activeBattleReinforcementCounts.TryGetValue(key, out var priorDeployments);
            var multiplier = Clamp01(_aiConfig.ReinforcementDeploymentDecayMultiplier);
            var decayedPercent = baseDeployPercent * (float)Math.Pow(multiplier, priorDeployments);

            if (priorDeployments > 0)
            {
                FileLogger.AI(
                    $"      ReinforcementDecay: {factionId}/{zoneId} repeat={priorDeployments}, " +
                    $"multiplier={multiplier:F2}, deploy={decayedPercent:P0}");
            }

            return decayedPercent;
        }

        private void RecordReinforcementDeployment(string factionId, string zoneId, int totalDeployed)
        {
            if (totalDeployed <= 0)
                return;

            if (_zoneBattleManager.GetBattleForZone(zoneId) == null)
                return;

            var key = ReinforcementDecayKey(factionId, zoneId);
            _activeBattleReinforcementCounts.TryGetValue(key, out var current);
            _activeBattleReinforcementCounts[key] = current + 1;
        }

        private void OnZoneBattleEndedForReinforcementDecay(ZoneBattle battle, BattleOutcome outcome)
        {
            if (battle == null)
                return;

            foreach (var participant in battle.Participants)
            {
                _activeBattleReinforcementCounts.Remove(
                    ReinforcementDecayKey(participant.FactionId, battle.ZoneId));
            }
        }

        private static string ReinforcementDecayKey(string factionId, string zoneId)
            => factionId + "|" + zoneId;

        private static float Clamp01(float value)
        {
            if (value < 0f) return 0f;
            if (value > 1f) return 1f;
            return value;
        }
    }
}
