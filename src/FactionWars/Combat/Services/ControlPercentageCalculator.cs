using System;
using FactionWars.Combat.Interfaces;
using FactionWars.Combat.Models;

namespace FactionWars.Combat.Services
{
    /// <summary>
    /// Calculates zone control percentages based on ped counts.
    /// Control is proportional to the number of alive peds each faction has in the zone.
    /// </summary>
    public class ControlPercentageCalculator : IControlPercentageCalculator
    {
        /// <inheritdoc/>
        public ControlPercentageResult Calculate(int attackerCount, int defenderCount)
        {
            // Clamp negative values to zero
            int validAttackers = Math.Max(0, attackerCount);
            int validDefenders = Math.Max(0, defenderCount);

            int total = validAttackers + validDefenders;

            // If no peds, return zero for both
            if (total == 0)
            {
                return new ControlPercentageResult(0f, 0f, 0);
            }

            // Calculate percentages proportionally
            float attackerPercentage = (validAttackers / (float)total) * 100f;
            float defenderPercentage = (validDefenders / (float)total) * 100f;

            return new ControlPercentageResult(attackerPercentage, defenderPercentage, total);
        }

        /// <inheritdoc/>
        public ControlPercentageResult CalculateForEncounter(CombatEncounter encounter)
        {
            if (encounter == null)
                throw new ArgumentNullException(nameof(encounter));

            return Calculate(encounter.AttackerPedCount, encounter.DefenderPedCount);
        }

        /// <inheritdoc/>
        public void ApplyToEncounter(CombatEncounter encounter)
        {
            if (encounter == null)
                throw new ArgumentNullException(nameof(encounter));

            if (!encounter.IsActive)
                throw new InvalidOperationException("Cannot apply control percentages to an ended encounter.");

            var result = CalculateForEncounter(encounter);
            encounter.AttackerControlPercentage = result.AttackerPercentage;
            encounter.DefenderControlPercentage = result.DefenderPercentage;
        }
    }
}
