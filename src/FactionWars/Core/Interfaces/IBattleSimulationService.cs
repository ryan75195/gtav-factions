using FactionWars.Core.Models;

namespace FactionWars.Core.Interfaces
{
    /// <summary>
    /// Service for simulating battles between AI factions when the player isn't present.
    /// Calculates combat outcomes based on attacker/defender troop counts and tiers.
    /// </summary>
    public interface IBattleSimulationService
    {
        /// <summary>
        /// Simulates a battle between an attacking and defending faction for control of a zone.
        /// </summary>
        /// <param name="attackerFactionId">The faction ID of the attacking faction.</param>
        /// <param name="defenderFactionId">The faction ID of the defending faction.</param>
        /// <param name="zoneId">The zone ID where the battle occurs.</param>
        /// <param name="attackerTroops">The troop composition committed by the attacker.</param>
        /// <param name="defenderTroops">The troop composition defending the zone.</param>
        /// <returns>The result of the simulated battle including outcome and casualties.</returns>
        BattleSimulationResult SimulateBattle(
            string attackerFactionId,
            string defenderFactionId,
            string zoneId,
            TroopComposition attackerTroops,
            TroopComposition defenderTroops);

        /// <summary>
        /// Calculates the probability (0-1) that the attacker will win the battle.
        /// </summary>
        /// <param name="attackerTroops">The attacker's troop composition.</param>
        /// <param name="defenderTroops">The defender's troop composition.</param>
        /// <returns>A value between 0 and 1 representing win probability.</returns>
        float CalculateWinProbability(TroopComposition attackerTroops, TroopComposition defenderTroops);

        /// <summary>
        /// Calculates the expected casualties for a troop composition given opposing strength.
        /// </summary>
        /// <param name="troops">The troop composition to calculate casualties for.</param>
        /// <param name="opposingStrength">The combat strength of the opposing force.</param>
        /// <returns>The expected casualties as a troop composition.</returns>
        TroopComposition CalculateCasualties(TroopComposition troops, float opposingStrength);
    }
}
