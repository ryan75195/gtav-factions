using System;

namespace FactionWars.Telemetry.Models
{
    public sealed class BattleEventRow
    {
        public DateTime Timestamp { get; }
        public long PlayTimeSeconds { get; }
        public BattleEventType Type { get; }
        public string ZoneId { get; }
        public string AttackerFactionId { get; }
        public string DefenderFactionId { get; }
        public int AttackerTroops { get; }
        public int DefenderTroops { get; }
        public BattleOutcome? Outcome { get; }
        public int AttackerCasualties { get; }
        public int DefenderCasualties { get; }

        public BattleEventRow(DateTime timestamp, long playTimeSeconds,
            BattleEventType type, string zoneId,
            string attackerFactionId, string defenderFactionId,
            int attackerTroops, int defenderTroops,
            BattleOutcome? outcome, int attackerCasualties, int defenderCasualties)
        {
            Timestamp = timestamp;
            PlayTimeSeconds = playTimeSeconds;
            Type = type;
            ZoneId = zoneId ?? throw new ArgumentNullException(nameof(zoneId));
            AttackerFactionId = attackerFactionId ?? throw new ArgumentNullException(nameof(attackerFactionId));
            DefenderFactionId = defenderFactionId ?? throw new ArgumentNullException(nameof(defenderFactionId));
            AttackerTroops = attackerTroops;
            DefenderTroops = defenderTroops;
            Outcome = outcome;
            AttackerCasualties = attackerCasualties;
            DefenderCasualties = defenderCasualties;
        }
    }
}
