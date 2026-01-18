using System;
using System.Collections.Generic;
using FactionWars.Economy.Models;
using FactionWars.Territory.Models;

namespace FactionWars.Economy.Services
{
    /// <summary>
    /// Calculates resource generation for zones based on their traits and strategic value.
    /// </summary>
    public class ResourceGenerationCalculator
    {
        // Trait bonus percentages
        private const float CommercialCashBonus = 0.50f;
        private const float ResidentialRecruitmentBonus = 0.50f;
        private const float IndustrialWeaponsBonus = 0.50f;
        private const float PortWeaponsBonus = 0.25f;
        private const float PortCashBonus = 0.25f;
        private const float HighValueMultiplier = 2.0f;

        /// <summary>
        /// Calculates the resource generation for a specific resource type from a zone.
        /// </summary>
        /// <param name="zone">The zone to calculate generation for.</param>
        /// <param name="resourceType">The type of resource to calculate.</param>
        /// <returns>The amount of resources generated per tick.</returns>
        /// <exception cref="ArgumentNullException">Thrown if zone is null.</exception>
        /// <exception cref="ArgumentException">Thrown if resourceType is invalid.</exception>
        public int CalculateGeneration(Zone zone, ResourceType resourceType)
        {
            if (zone == null)
                throw new ArgumentNullException(nameof(zone));

            if (!Enum.IsDefined(typeof(ResourceType), resourceType))
                throw new ArgumentException($"Invalid resource type: {resourceType}", nameof(resourceType));

            var info = ResourceTypeInfo.GetInfo(resourceType);
            float baseGeneration = info.BaseGenerationRate;

            // Apply strategic value multiplier
            float generation = baseGeneration * zone.StrategicValue;

            // Apply HighValue trait multiplier first (doubles base before percentage bonuses)
            if (zone.Traits.HasFlag(ZoneTrait.HighValue))
            {
                generation *= HighValueMultiplier;
            }

            // Apply trait-specific percentage bonuses
            generation = ApplyTraitBonuses(generation, zone.Traits, resourceType);

            return (int)generation;
        }

        /// <summary>
        /// Calculates the resource generation for all resource types from a zone.
        /// </summary>
        /// <param name="zone">The zone to calculate generation for.</param>
        /// <returns>A dictionary mapping resource types to their generation amounts.</returns>
        /// <exception cref="ArgumentNullException">Thrown if zone is null.</exception>
        public Dictionary<ResourceType, int> CalculateAllGeneration(Zone zone)
        {
            if (zone == null)
                throw new ArgumentNullException(nameof(zone));

            var result = new Dictionary<ResourceType, int>();

            foreach (ResourceType resourceType in Enum.GetValues(typeof(ResourceType)))
            {
                result[resourceType] = CalculateGeneration(zone, resourceType);
            }

            return result;
        }

        /// <summary>
        /// Applies trait-specific bonuses to the base generation amount.
        /// </summary>
        private float ApplyTraitBonuses(float baseAmount, ZoneTrait traits, ResourceType resourceType)
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

            return baseAmount * (1f + bonus);
        }
    }
}
