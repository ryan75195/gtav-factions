using System;
using FactionWars.Core.Interfaces;
using FactionWars.Core.Models;

namespace FactionWars.Core.Services
{
    /// <summary>
    /// Service for simulating battles between AI factions when the player isn't present.
    /// Calculates combat outcomes based on attacker/defender troop counts and tiers.
    /// Uses deterministic calculations based on relative strength ratios.
    /// </summary>
    public sealed class BattleSimulationService : IBattleSimulationService
    {
        /// <summary>
        /// Defender advantage modifier (defenders have defensive position).
        /// Applied as a multiplier to defender strength in calculations.
        /// </summary>
        private const float DefenderAdvantage = 1.2f;

        /// <summary>
        /// Resilience modifier for basic troops (higher = more casualties taken).
        /// </summary>
        private const float BasicResilienceModifier = 1.0f;

        /// <summary>
        /// Resilience modifier for medium troops (lower = more resilient).
        /// </summary>
        private const float MediumResilienceModifier = 0.75f;

        /// <summary>
        /// Resilience modifier for heavy troops (lowest = most resilient).
        /// </summary>
        private const float HeavyResilienceModifier = 0.5f;

        /// <summary>
        /// Base casualty rate applied to combat (before strength ratio calculations).
        /// </summary>
        private const float BaseCasualtyRate = 0.3f;

        /// <inheritdoc />
        public BattleSimulationResult SimulateBattle(
            string attackerFactionId,
            string defenderFactionId,
            string zoneId,
            TroopComposition attackerTroops,
            TroopComposition defenderTroops)
        {
            if (attackerFactionId == null)
                throw new ArgumentNullException(nameof(attackerFactionId));
            if (defenderFactionId == null)
                throw new ArgumentNullException(nameof(defenderFactionId));
            if (zoneId == null)
                throw new ArgumentNullException(nameof(zoneId));
            if (attackerTroops == null)
                throw new ArgumentNullException(nameof(attackerTroops));
            if (defenderTroops == null)
                throw new ArgumentNullException(nameof(defenderTroops));

            // Calculate win probability
            float winProbability = CalculateWinProbability(attackerTroops, defenderTroops);

            // Calculate casualties for both sides
            float attackerStrength = attackerTroops.TotalStrength;
            float defenderStrength = defenderTroops.TotalStrength * DefenderAdvantage;

            var attackerCasualties = CalculateCasualties(attackerTroops, defenderStrength);
            var defenderCasualties = CalculateCasualties(defenderTroops, attackerStrength);

            // Determine outcome - attacker wins if probability > 0.5
            // For deterministic simulation, use the threshold directly
            bool attackerWins = winProbability > 0.5f;

            if (attackerWins)
            {
                return BattleSimulationResult.AttackerVictory(
                    attackerFactionId,
                    defenderFactionId,
                    zoneId,
                    attackerCasualties,
                    defenderCasualties);
            }
            else
            {
                return BattleSimulationResult.DefenderVictory(
                    attackerFactionId,
                    defenderFactionId,
                    zoneId,
                    attackerCasualties,
                    defenderCasualties);
            }
        }

        /// <inheritdoc />
        public float CalculateWinProbability(TroopComposition attackerTroops, TroopComposition defenderTroops)
        {
            if (attackerTroops == null)
                throw new ArgumentNullException(nameof(attackerTroops));
            if (defenderTroops == null)
                throw new ArgumentNullException(nameof(defenderTroops));

            float attackerStrength = attackerTroops.TotalStrength;
            float defenderStrength = defenderTroops.TotalStrength;

            // Edge cases
            if (attackerStrength <= 0 && defenderStrength <= 0)
            {
                // Both empty - defender holds (no attacker to take it)
                return 0f;
            }

            if (defenderStrength <= 0)
            {
                // No defenders - attacker automatically wins
                return 1f;
            }

            if (attackerStrength <= 0)
            {
                // No attackers - attacker cannot win
                return 0f;
            }

            // Apply defender advantage
            float effectiveDefenderStrength = defenderStrength * DefenderAdvantage;

            // Calculate probability based on strength ratio
            // Using a logistic-like function for smooth probability curve
            float totalStrength = attackerStrength + effectiveDefenderStrength;
            float rawProbability = attackerStrength / totalStrength;

            // Clamp to valid range
            return Math.Max(0f, Math.Min(1f, rawProbability));
        }

        /// <inheritdoc />
        public TroopComposition CalculateCasualties(TroopComposition troops, float opposingStrength)
        {
            if (troops == null)
                throw new ArgumentNullException(nameof(troops));

            // Negative or zero opposing strength means no casualties
            if (opposingStrength <= 0)
            {
                return TroopComposition.Empty;
            }

            if (troops.IsEmpty)
            {
                return TroopComposition.Empty;
            }

            // Calculate casualty rate based on opposing strength vs our strength
            float ourStrength = troops.TotalStrength;

            // Calculate intensity of combat (how deadly is this fight)
            // Higher opposing strength relative to our strength = more casualties
            float intensityRatio = opposingStrength / (ourStrength + 1f); // +1 to avoid division by zero
            float effectiveCasualtyRate = BaseCasualtyRate * Math.Min(intensityRatio, 2.0f); // Cap at 2x base rate

            // Calculate casualties per tier, accounting for resilience
            // Basic troops take casualties first (highest rate), then medium, then heavy
            int basicCasualties = CalculateTierCasualties(troops.Basic, effectiveCasualtyRate, BasicResilienceModifier);
            int mediumCasualties = CalculateTierCasualties(troops.Medium, effectiveCasualtyRate, MediumResilienceModifier);
            int heavyCasualties = CalculateTierCasualties(troops.Heavy, effectiveCasualtyRate, HeavyResilienceModifier);

            return new TroopComposition(basicCasualties, mediumCasualties, heavyCasualties);
        }

        /// <summary>
        /// Calculates casualties for a specific tier based on casualty rate and resilience.
        /// </summary>
        private static int CalculateTierCasualties(int troopCount, float casualtyRate, float resilienceModifier)
        {
            if (troopCount <= 0)
            {
                return 0;
            }

            // Apply resilience modifier (lower = more resilient = fewer casualties)
            float effectiveRate = casualtyRate * resilienceModifier;

            // Calculate casualties as percentage of troops
            float rawCasualties = troopCount * effectiveRate;

            // Round up to ensure at least some casualties when rate is significant
            int casualties = (int)Math.Ceiling(rawCasualties);

            // Clamp to valid range (can't lose more than you have)
            return Math.Min(casualties, troopCount);
        }
    }
}
