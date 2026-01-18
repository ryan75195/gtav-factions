using FactionWars.Loyalty.Interfaces;
using FactionWars.Loyalty.Models;
using System;
using System.Collections.Generic;
using System.Linq;

namespace FactionWars.Loyalty.Services
{
    /// <summary>
    /// Service responsible for applying loyalty changes to zones.
    /// </summary>
    public class LoyaltyChangeService : ILoyaltyChangeService
    {
        private const int NeighborInfluenceDivisor = 5;

        /// <inheritdoc />
        public void ApplyModifier(ZoneLoyalty zoneLoyalty, LoyaltyModifier modifier)
        {
            if (zoneLoyalty == null)
                throw new ArgumentNullException(nameof(zoneLoyalty));
            if (modifier == null)
                throw new ArgumentNullException(nameof(modifier));

            zoneLoyalty.AdjustLoyalty(modifier.Value);
        }

        /// <inheritdoc />
        public void ApplyModifiers(ZoneLoyalty zoneLoyalty, IEnumerable<LoyaltyModifier> modifiers)
        {
            if (zoneLoyalty == null)
                throw new ArgumentNullException(nameof(zoneLoyalty));
            if (modifiers == null)
                throw new ArgumentNullException(nameof(modifiers));

            foreach (var modifier in modifiers)
            {
                ApplyModifier(zoneLoyalty, modifier);
            }
        }

        /// <inheritdoc />
        public void ApplyDailyChange(ZoneLoyalty zoneLoyalty)
        {
            if (zoneLoyalty == null)
                throw new ArgumentNullException(nameof(zoneLoyalty));

            var modifier = LoyaltyModifier.CreateTimeBasedGain();
            ApplyModifier(zoneLoyalty, modifier);
            zoneLoyalty.AdvanceDay();
        }

        /// <inheritdoc />
        public void ApplyCombatResult(ZoneLoyalty zoneLoyalty, bool defenderWon)
        {
            if (zoneLoyalty == null)
                throw new ArgumentNullException(nameof(zoneLoyalty));

            var modifier = defenderWon
                ? LoyaltyModifier.CreateCombatVictory()
                : LoyaltyModifier.CreateCombatDefeat();

            ApplyModifier(zoneLoyalty, modifier);
        }

        /// <inheritdoc />
        public void ApplyResourceInvestment(ZoneLoyalty zoneLoyalty, int amount)
        {
            if (zoneLoyalty == null)
                throw new ArgumentNullException(nameof(zoneLoyalty));
            if (amount <= 0)
                throw new ArgumentOutOfRangeException(nameof(amount), "Amount must be greater than zero.");

            var totalBonus = LoyaltyModifier.DefaultResourceInvestmentBonus * amount;
            var modifier = LoyaltyModifier.CreateResourceInvestment(totalBonus);
            ApplyModifier(zoneLoyalty, modifier);
        }

        /// <inheritdoc />
        public void ApplyOppression(ZoneLoyalty zoneLoyalty, int severityMultiplier = 1)
        {
            if (zoneLoyalty == null)
                throw new ArgumentNullException(nameof(zoneLoyalty));
            if (severityMultiplier <= 0)
                throw new ArgumentOutOfRangeException(nameof(severityMultiplier), "Severity multiplier must be greater than zero.");

            var totalPenalty = LoyaltyModifier.DefaultOppressionPenalty * severityMultiplier;
            var modifier = LoyaltyModifier.CreateOppression(totalPenalty);
            ApplyModifier(zoneLoyalty, modifier);
        }

        /// <inheritdoc />
        public void ApplyPropaganda(ZoneLoyalty zoneLoyalty)
        {
            if (zoneLoyalty == null)
                throw new ArgumentNullException(nameof(zoneLoyalty));

            var modifier = LoyaltyModifier.CreatePropaganda();
            ApplyModifier(zoneLoyalty, modifier);
        }

        /// <inheritdoc />
        public int CalculateNeighborInfluence(ZoneLoyalty zoneLoyalty, IEnumerable<ZoneLoyalty> neighborLoyalties)
        {
            if (zoneLoyalty == null)
                throw new ArgumentNullException(nameof(zoneLoyalty));
            if (neighborLoyalties == null)
                throw new ArgumentNullException(nameof(neighborLoyalties));

            var sameFactionNeighbors = neighborLoyalties
                .Where(n => n.ControllingFactionId == zoneLoyalty.ControllingFactionId)
                .ToList();

            if (sameFactionNeighbors.Count == 0)
                return 0;

            var totalDifference = sameFactionNeighbors
                .Sum(n => n.LoyaltyValue - zoneLoyalty.LoyaltyValue);

            return totalDifference / NeighborInfluenceDivisor;
        }

        /// <inheritdoc />
        public void ApplyNeighborInfluence(ZoneLoyalty zoneLoyalty, IEnumerable<ZoneLoyalty> neighborLoyalties)
        {
            if (zoneLoyalty == null)
                throw new ArgumentNullException(nameof(zoneLoyalty));
            if (neighborLoyalties == null)
                throw new ArgumentNullException(nameof(neighborLoyalties));

            var influence = CalculateNeighborInfluence(zoneLoyalty, neighborLoyalties);
            if (influence != 0)
            {
                var modifier = LoyaltyModifier.CreateNeighborInfluence(influence);
                ApplyModifier(zoneLoyalty, modifier);
            }
        }

        /// <inheritdoc />
        public void ApplyConquestPenalty(ZoneLoyalty zoneLoyalty)
        {
            if (zoneLoyalty == null)
                throw new ArgumentNullException(nameof(zoneLoyalty));

            var modifier = LoyaltyModifier.CreateRecentConquest();
            ApplyModifier(zoneLoyalty, modifier);
        }

        /// <inheritdoc />
        public int CalculateTotalModifierValue(IEnumerable<LoyaltyModifier> modifiers)
        {
            if (modifiers == null)
                throw new ArgumentNullException(nameof(modifiers));

            return modifiers.Sum(m => m.Value);
        }

        /// <inheritdoc />
        public string? GetLevelChangeDescription(LoyaltyLevel oldLevel, LoyaltyLevel newLevel)
        {
            if (oldLevel == newLevel)
                return null;

            var direction = newLevel > oldLevel ? "improved" : "declined";
            return $"Loyalty has {direction} from {oldLevel} to {newLevel}";
        }
    }
}
