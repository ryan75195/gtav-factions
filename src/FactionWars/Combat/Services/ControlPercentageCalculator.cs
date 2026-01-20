using System;
using FactionWars.Combat.Interfaces;
using FactionWars.Combat.Models;
using FactionWars.ScriptHookV.Logging;

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

            // If no peds, return 50/50 (neutral state) - this prevents immediate victory
            // before any peds have spawned
            if (total == 0)
            {
                FileLogger.Debug($"ControlCalc: No peds (attackers={attackerCount}, defenders={defenderCount}) -> 50/50 neutral");
                return new ControlPercentageResult(50f, 50f, 0);
            }

            // Calculate percentages proportionally
            float attackerPercentage = (validAttackers / (float)total) * 100f;
            float defenderPercentage = (validDefenders / (float)total) * 100f;

            FileLogger.Debug($"ControlCalc: attackers={validAttackers}, defenders={validDefenders} -> {attackerPercentage:F1}% / {defenderPercentage:F1}%");

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
