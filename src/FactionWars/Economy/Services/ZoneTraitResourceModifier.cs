using System;
using System.Collections.Generic;
using FactionWars.Economy.Interfaces;
using FactionWars.Economy.Models;
using FactionWars.Territory.Models;

namespace FactionWars.Economy.Services
{
    /// <summary>
    /// Provides resource generation modifiers based on zone traits.
    /// Implements the calculation logic for trait-based resource bonuses.
    /// </summary>
    public class ZoneTraitResourceModifier : IZoneTraitResourceModifier
    {
        // Trait bonus percentages (additive)
        private const float CommercialCashBonus = 0.50f;        // +50% cash
        private const float ResidentialRecruitmentBonus = 0.50f; // +50% recruitment
        private const float IndustrialWeaponsBonus = 0.50f;     // +50% weapons
        private const float PortWeaponsBonus = 0.25f;           // +25% weapons
        private const float PortCashBonus = 0.25f;              // +25% cash

        // HighValue is a multiplier applied before percentage bonuses
        private const float HighValueMultiplier = 2.0f;         // 2x all resources

        /// <inheritdoc />
        public float GetModifier(ZoneTrait traits, ResourceType resourceType)
        {
            if (!Enum.IsDefined(typeof(ResourceType), resourceType))
                throw new ArgumentException($"Invalid resource type: {resourceType}", nameof(resourceType));

            float baseModifier = 1.0f;

            // Apply HighValue multiplier first (multiplicative)
            if (traits.HasFlag(ZoneTrait.HighValue))
            {
                baseModifier *= HighValueMultiplier;
            }

            // Calculate additive percentage bonuses
            float bonus = GetPercentageBonus(traits, resourceType);

            // Apply percentage bonuses
            return baseModifier * (1.0f + bonus);
        }

        /// <inheritdoc />
        public Dictionary<ResourceType, float> GetTotalModifier(ZoneTrait traits)
        {
            var result = new Dictionary<ResourceType, float>();

            foreach (ResourceType resourceType in Enum.GetValues(typeof(ResourceType)))
            {
                result[resourceType] = GetModifier(traits, resourceType);
            }

            return result;
        }

        /// <inheritdoc />
        public bool HasResourceBonus(ZoneTrait traits, ResourceType resourceType)
        {
            return GetModifier(traits, resourceType) > 1.0f;
        }

        /// <summary>
        /// Calculates the additive percentage bonus for a specific resource type.
        /// </summary>
        private float GetPercentageBonus(ZoneTrait traits, ResourceType resourceType)
        {
            float bonus = 0f;

            switch (resourceType)
            {
                case ResourceType.Cash:
                    if (traits.HasFlag(ZoneTrait.Commercial))
                        bonus += CommercialCashBonus;
                    if (traits.HasFlag(ZoneTrait.Port))
                        bonus += PortCashBonus;
                    break;

                case ResourceType.Recruitment:
                    if (traits.HasFlag(ZoneTrait.Residential))
                        bonus += ResidentialRecruitmentBonus;
                    break;

                case ResourceType.Weapons:
                    if (traits.HasFlag(ZoneTrait.Industrial))
                        bonus += IndustrialWeaponsBonus;
                    if (traits.HasFlag(ZoneTrait.Port))
                        bonus += PortWeaponsBonus;
                    break;
            }

            return bonus;
        }
    }
}
