using System;

namespace FactionWars.Core.Models
{
    /// <summary>
    /// Represents the result of a simulated battle between two AI factions.
    /// Contains information about the outcome, casualties, and ownership changes.
    /// </summary>
    public sealed class BattleSimulationResult
    {
        /// <summary>
        /// The faction ID of the attacking faction.
        /// </summary>
        public string AttackerFactionId { get; }

        /// <summary>
        /// The faction ID of the defending faction.
        /// </summary>
        public string DefenderFactionId { get; }

        /// <summary>
        /// The zone ID where the battle took place.
        /// </summary>
        public string ZoneId { get; }

        /// <summary>
        /// True if the attacker won the battle, false if the defender held.
        /// </summary>
        public bool AttackerWon { get; }

        /// <summary>
        /// The troops lost by the attacker during the battle.
        /// </summary>
        public TroopComposition AttackerCasualties { get; }

        /// <summary>
        /// The troops lost by the defender during the battle.
        /// </summary>
        public TroopComposition DefenderCasualties { get; }

        /// <summary>
        /// The faction ID of the zone's owner after the battle.
        /// If the attacker won, this is the attacker's faction ID.
        /// If the defender held, this is the defender's faction ID.
        /// </summary>
        public string NewOwnerFactionId { get; }

        private BattleSimulationResult(
            string attackerFactionId,
            string defenderFactionId,
            string zoneId,
            bool attackerWon,
            TroopComposition attackerCasualties,
            TroopComposition defenderCasualties,
            string newOwnerFactionId)
        {
            AttackerFactionId = attackerFactionId ?? throw new ArgumentNullException(nameof(attackerFactionId));
            DefenderFactionId = defenderFactionId ?? throw new ArgumentNullException(nameof(defenderFactionId));
            ZoneId = zoneId ?? throw new ArgumentNullException(nameof(zoneId));
            AttackerWon = attackerWon;
            AttackerCasualties = attackerCasualties ?? throw new ArgumentNullException(nameof(attackerCasualties));
            DefenderCasualties = defenderCasualties ?? throw new ArgumentNullException(nameof(defenderCasualties));
            NewOwnerFactionId = newOwnerFactionId ?? throw new ArgumentNullException(nameof(newOwnerFactionId));
        }

        /// <summary>
        /// Creates a battle result where the attacker won and captured the zone.
        /// </summary>
        /// <param name="attackerFactionId">The attacking faction ID.</param>
        /// <param name="defenderFactionId">The defending faction ID.</param>
        /// <param name="zoneId">The zone where the battle occurred.</param>
        /// <param name="attackerCasualties">The troops lost by the attacker.</param>
        /// <param name="defenderCasualties">The troops lost by the defender.</param>
        /// <returns>A new BattleSimulationResult representing an attacker victory.</returns>
        public static BattleSimulationResult AttackerVictory(
            string attackerFactionId,
            string defenderFactionId,
            string zoneId,
            TroopComposition attackerCasualties,
            TroopComposition defenderCasualties)
        {
            return new BattleSimulationResult(
                attackerFactionId: attackerFactionId,
                defenderFactionId: defenderFactionId,
                zoneId: zoneId,
                attackerWon: true,
                attackerCasualties: attackerCasualties,
                defenderCasualties: defenderCasualties,
                newOwnerFactionId: attackerFactionId);
        }

        /// <summary>
        /// Creates a battle result where the defender successfully held the zone.
        /// </summary>
        /// <param name="attackerFactionId">The attacking faction ID.</param>
        /// <param name="defenderFactionId">The defending faction ID.</param>
        /// <param name="zoneId">The zone where the battle occurred.</param>
        /// <param name="attackerCasualties">The troops lost by the attacker.</param>
        /// <param name="defenderCasualties">The troops lost by the defender.</param>
        /// <returns>A new BattleSimulationResult representing a defender victory.</returns>
        public static BattleSimulationResult DefenderVictory(
            string attackerFactionId,
            string defenderFactionId,
            string zoneId,
            TroopComposition attackerCasualties,
            TroopComposition defenderCasualties)
        {
            return new BattleSimulationResult(
                attackerFactionId: attackerFactionId,
                defenderFactionId: defenderFactionId,
                zoneId: zoneId,
                attackerWon: false,
                attackerCasualties: attackerCasualties,
                defenderCasualties: defenderCasualties,
                newOwnerFactionId: defenderFactionId);
        }

        public override string ToString()
        {
            var outcome = AttackerWon ? $"{AttackerFactionId} captured" : $"{DefenderFactionId} defended";
            return $"Battle at {ZoneId}: {outcome}. Attacker lost {AttackerCasualties.TotalCount}, Defender lost {DefenderCasualties.TotalCount}.";
        }
    }
}
