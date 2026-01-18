using System;
using FactionWars.Lieutenants.Interfaces;
using FactionWars.Lieutenants.Models;

namespace FactionWars.Lieutenants.Services
{
    /// <summary>
    /// Service for handling lieutenant defection mechanics.
    /// </summary>
    public class DefectionService : IDefectionService
    {
        private readonly IRandomProvider _randomProvider;

        // Base chance modifiers
        private const double LoyalTraitReduction = 0.25;
        private const double AmbitiousTraitIncrease = 0.20;
        private const double VeteranTraitReduction = 0.05;
        private const double CapturedStateIncrease = 0.25;
        private const double PreviousDefectorIncrease = 0.10;

        // Bribe calculations
        private const double BribeEffectivenessBase = 0.00001; // Per dollar
        private const double CorruptBribeMultiplier = 2.0;
        private const int BaseBribePerLoyaltyPoint = 2000;

        /// <summary>
        /// Creates a new defection service.
        /// </summary>
        /// <param name="randomProvider">The random provider for defection rolls.</param>
        /// <exception cref="ArgumentNullException">Thrown if randomProvider is null.</exception>
        public DefectionService(IRandomProvider randomProvider)
        {
            _randomProvider = randomProvider ?? throw new ArgumentNullException(nameof(randomProvider));
        }

        /// <inheritdoc/>
        public double CalculateDefectionChance(Lieutenant lieutenant, int bribeAmount = 0)
        {
            if (lieutenant == null)
                throw new ArgumentNullException(nameof(lieutenant));

            // Deceased lieutenants cannot defect
            if (lieutenant.Status == LieutenantStatus.Deceased)
                return 0.0;

            // Base chance is inverse of loyalty (0 loyalty = 100% chance, 100 loyalty = 0% chance)
            double baseChance = (100 - lieutenant.Loyalty) / 100.0;

            // Apply trait modifiers
            if (lieutenant.HasTrait(LieutenantTrait.Loyal))
            {
                baseChance -= LoyalTraitReduction;
            }

            if (lieutenant.HasTrait(LieutenantTrait.Ambitious))
            {
                baseChance += AmbitiousTraitIncrease;
            }

            if (lieutenant.HasTrait(LieutenantTrait.Veteran))
            {
                baseChance -= VeteranTraitReduction;
            }

            // Captured state increases defection chance
            if (lieutenant.Status == LieutenantStatus.Captured)
            {
                baseChance += CapturedStateIncrease;
            }

            // Previous defectors are more likely to defect again
            if (lieutenant.HasDefected)
            {
                baseChance += PreviousDefectorIncrease;
            }

            // Apply bribe effect with diminishing returns
            if (bribeAmount > 0)
            {
                double bribeEffect = CalculateBribeEffect(lieutenant, bribeAmount);
                baseChance += bribeEffect;
            }

            // Clamp between 0 and 1
            return Math.Max(0.0, Math.Min(1.0, baseChance));
        }

        /// <inheritdoc/>
        public DefectionResult AttemptDefection(Lieutenant lieutenant, string targetFactionId, int bribeAmount = 0)
        {
            if (lieutenant == null)
                throw new ArgumentNullException(nameof(lieutenant));
            if (targetFactionId == null)
                throw new ArgumentNullException(nameof(targetFactionId));
            if (string.IsNullOrWhiteSpace(targetFactionId))
                throw new ArgumentException("Target faction ID cannot be empty.", nameof(targetFactionId));

            // Check if defection is possible
            if (!CanAttemptDefection(lieutenant, targetFactionId))
            {
                string reason = lieutenant.Status == LieutenantStatus.Deceased
                    ? "Lieutenant is deceased"
                    : lieutenant.FactionId == targetFactionId
                        ? "Cannot defect to the same faction"
                        : "Defection not possible";
                return DefectionResult.Failed(0.0, 0.0, reason);
            }

            // Calculate defection chance
            double chance = CalculateDefectionChance(lieutenant, bribeAmount);

            // Roll for defection
            double roll = _randomProvider.NextDouble();

            if (roll < chance)
            {
                // Success - lieutenant defects
                lieutenant.Defect(targetFactionId);
                return DefectionResult.Succeeded(chance, roll);
            }
            else
            {
                // Failure - lieutenant remains loyal
                return DefectionResult.Failed(chance, roll, "Defection attempt failed");
            }
        }

        /// <inheritdoc/>
        public bool CanAttemptDefection(Lieutenant? lieutenant, string targetFactionId)
        {
            if (lieutenant == null)
                return false;

            if (string.IsNullOrWhiteSpace(targetFactionId))
                return false;

            // Deceased lieutenants cannot be flipped
            if (lieutenant.Status == LieutenantStatus.Deceased)
                return false;

            // Cannot defect to the same faction
            if (lieutenant.FactionId == targetFactionId)
                return false;

            // If captured, only the capturing faction can attempt to flip them
            if (lieutenant.Status == LieutenantStatus.Captured)
            {
                return lieutenant.CapturedByFactionId == targetFactionId;
            }

            return true;
        }

        /// <inheritdoc/>
        public int GetRequiredBribeForGuaranteedDefection(Lieutenant lieutenant)
        {
            if (lieutenant == null)
                throw new ArgumentNullException(nameof(lieutenant));

            // Calculate base chance without bribe
            double baseChance = CalculateDefectionChance(lieutenant, 0);

            // If already at 100%, no bribe needed
            if (baseChance >= 1.0)
                return 0;

            // Calculate how much additional chance is needed
            double neededChance = 1.0 - baseChance;

            // Work backwards from the bribe effect formula
            // For non-corrupt lieutenants: bribeEffect = sqrt(bribe * BribeEffectivenessBase)
            // For corrupt lieutenants: bribeEffect = sqrt(bribe * BribeEffectivenessBase * CorruptBribeMultiplier)
            double multiplier = BribeEffectivenessBase;
            if (lieutenant.HasTrait(LieutenantTrait.Corrupt))
            {
                multiplier *= CorruptBribeMultiplier;
            }

            // Captured lieutenants are easier to bribe
            if (lieutenant.Status == LieutenantStatus.Captured)
            {
                multiplier *= 1.5;
            }

            // Loyal lieutenants are harder to bribe
            if (lieutenant.HasTrait(LieutenantTrait.Loyal))
            {
                multiplier /= 2.0;
            }

            // Solve: neededChance = sqrt(bribe * multiplier)
            // neededChance^2 = bribe * multiplier
            // bribe = neededChance^2 / multiplier
            double requiredBribe = (neededChance * neededChance) / multiplier;

            return (int)Math.Ceiling(requiredBribe);
        }

        /// <inheritdoc/>
        public bool IsFormerMember(Lieutenant? lieutenant, string factionId)
        {
            if (lieutenant == null)
                return false;

            if (string.IsNullOrWhiteSpace(factionId))
                return false;

            // Check if the lieutenant was originally from this faction
            // and has since defected to a different faction
            return lieutenant.OriginalFactionId == factionId && lieutenant.FactionId != factionId;
        }

        private double CalculateBribeEffect(Lieutenant lieutenant, int bribeAmount)
        {
            if (bribeAmount <= 0)
                return 0.0;

            double multiplier = BribeEffectivenessBase;

            // Corrupt lieutenants are more susceptible to bribes
            if (lieutenant.HasTrait(LieutenantTrait.Corrupt))
            {
                multiplier *= CorruptBribeMultiplier;
            }

            // Captured lieutenants are more susceptible to bribes
            if (lieutenant.Status == LieutenantStatus.Captured)
            {
                multiplier *= 1.5;
            }

            // Loyal lieutenants are less susceptible to bribes
            if (lieutenant.HasTrait(LieutenantTrait.Loyal))
            {
                multiplier /= 2.0;
            }

            // Square root for diminishing returns
            return Math.Sqrt(bribeAmount * multiplier);
        }
    }
}
