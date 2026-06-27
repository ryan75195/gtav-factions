using FactionWars.Core.Models;

namespace FactionWars.Combat.Models
{
    /// <summary>
    /// Event data for when one faction kills an enemy troop in a timed battle.
    /// </summary>
    public class BattleKillEvent
    {
        /// <summary>
        /// The battle this kill occurred in.
        /// </summary>
        public string BattleId { get; }

        /// <summary>
        /// The faction that got the kill.
        /// </summary>
        public string KillerFactionId { get; }

        /// <summary>
        /// The tier of the troop that got the kill.
        /// </summary>
        public DefenderRole KillerTier { get; }

        /// <summary>
        /// The faction that lost a troop.
        /// </summary>
        public string VictimFactionId { get; }

        /// <summary>
        /// The tier of the troop that was killed.
        /// </summary>
        public DefenderRole VictimTier { get; }

        /// <summary>
        /// The zone where the battle is taking place.
        /// </summary>
        public string ZoneId { get; }

        /// <summary>
        /// The name of the zone (for display).
        /// </summary>
        public string ZoneName { get; }

        public BattleKillEvent(
            string battleId,
            string killerFactionId,
            DefenderRole killerTier,
            string victimFactionId,
            DefenderRole victimTier,
            string zoneId,
            string zoneName)
        {
            BattleId = battleId;
            KillerFactionId = killerFactionId;
            KillerTier = killerTier;
            VictimFactionId = victimFactionId;
            VictimTier = victimTier;
            ZoneId = zoneId;
            ZoneName = zoneName;
        }
    }
}
