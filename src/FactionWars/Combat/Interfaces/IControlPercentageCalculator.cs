using FactionWars.Combat.Models;

namespace FactionWars.Combat.Interfaces
{
    /// <summary>
    /// Interface for calculating zone control percentages based on ped counts.
    /// Used during combat encounters to determine which faction has more control.
    /// </summary>
    public interface IControlPercentageCalculator
    {
        /// <summary>
        /// Calculates control percentages based on attacker and defender ped counts.
        /// </summary>
        /// <param name="attackerCount">The number of attacking peds.</param>
        /// <param name="defenderCount">The number of defending peds.</param>
        /// <returns>A result containing the calculated percentages for both sides.</returns>
        ControlPercentageResult Calculate(int attackerCount, int defenderCount);

        /// <summary>
        /// Calculates control percentages for a combat encounter based on its ped counts.
        /// </summary>
        /// <param name="encounter">The combat encounter to calculate for.</param>
        /// <returns>A result containing the calculated percentages for both sides.</returns>
        /// <exception cref="System.ArgumentNullException">Thrown if encounter is null.</exception>
        ControlPercentageResult CalculateForEncounter(CombatEncounter encounter);

        /// <summary>
        /// Calculates and applies control percentages directly to a combat encounter.
        /// Updates the encounter's AttackerControlPercentage and DefenderControlPercentage properties.
        /// </summary>
        /// <param name="encounter">The combat encounter to update.</param>
        /// <exception cref="System.ArgumentNullException">Thrown if encounter is null.</exception>
        /// <exception cref="System.InvalidOperationException">Thrown if the encounter has already ended.</exception>
        void ApplyToEncounter(CombatEncounter encounter);
    }
}
