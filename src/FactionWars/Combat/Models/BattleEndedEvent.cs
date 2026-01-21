namespace FactionWars.Combat.Models
{
    /// <summary>
    /// Event data for when a timed battle ends.
    /// </summary>
    public class BattleEndedEvent
    {
        /// <summary>
        /// The battle that ended.
        /// </summary>
        public string BattleId { get; }

        /// <summary>
        /// The attacking faction.
        /// </summary>
        public string AttackerFactionId { get; }

        /// <summary>
        /// The defending faction.
        /// </summary>
        public string DefenderFactionId { get; }

        /// <summary>
        /// The zone that was contested.
        /// </summary>
        public string ZoneId { get; }

        /// <summary>
        /// The name of the zone (for display).
        /// </summary>
        public string ZoneName { get; }

        /// <summary>
        /// Whether the attacker won.
        /// </summary>
        public bool AttackerWon { get; }

        /// <summary>
        /// Total casualties the attacker suffered.
        /// </summary>
        public int AttackerCasualties { get; }

        /// <summary>
        /// Total casualties the defender suffered.
        /// </summary>
        public int DefenderCasualties { get; }

        public BattleEndedEvent(
            string battleId,
            string attackerFactionId,
            string defenderFactionId,
            string zoneId,
            string zoneName,
            bool attackerWon,
            int attackerCasualties,
            int defenderCasualties)
        {
            BattleId = battleId;
            AttackerFactionId = attackerFactionId;
            DefenderFactionId = defenderFactionId;
            ZoneId = zoneId;
            ZoneName = zoneName;
            AttackerWon = attackerWon;
            AttackerCasualties = attackerCasualties;
            DefenderCasualties = defenderCasualties;
        }
    }
}
