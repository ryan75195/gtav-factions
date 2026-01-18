using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace FactionWars.Territory.Models
{
    /// <summary>
    /// Provides modifier calculations and descriptions for zone traits.
    /// Each trait affects different aspects of zone performance (resources, defense, reinforcements).
    /// </summary>
    public class ZoneTraitEffects
    {
        // Modifier constants for each trait type
        private const float CommercialCashBonus = 0.5f;      // +50% cash
        private const float HighValueCashBonus = 0.25f;      // +25% cash
        private const float ResidentialRecruitBonus = 0.5f;  // +50% recruitment
        private const float HighValueRecruitBonus = 0.25f;   // +25% recruitment
        private const float IndustrialWeaponBonus = 0.5f;    // +50% weapons
        private const float PortWeaponBonus = 0.25f;         // +25% weapons (smuggling)
        private const float HighValueWeaponBonus = 0.25f;    // +25% weapons
        private const float FortifiedDefenseBonus = 0.5f;    // +50% defense
        private const float AirfieldReinforcementBonus = 0.5f; // +50% reinforcement speed
        private const float PortReinforcementBonus = 0.25f;  // +25% reinforcement speed

        // Trait descriptions
        private static readonly Dictionary<ZoneTrait, string> TraitDescriptions = new Dictionary<ZoneTrait, string>
        {
            { ZoneTrait.None, string.Empty },
            { ZoneTrait.Industrial, "Industrial: +50% weapons production" },
            { ZoneTrait.Commercial, "Commercial: +50% cash income" },
            { ZoneTrait.Residential, "Residential: +50% recruitment rate" },
            { ZoneTrait.Port, "Port: +25% weapons (smuggling), +25% reinforcement speed" },
            { ZoneTrait.Airfield, "Airfield: +50% reinforcement speed" },
            { ZoneTrait.Fortified, "Fortified: +50% defense bonus" },
            { ZoneTrait.HighValue, "High Value: +25% to all resource generation" }
        };

        /// <summary>
        /// Calculates the cash generation modifier for the given traits.
        /// </summary>
        /// <param name="traits">The combined zone traits.</param>
        /// <returns>Multiplier for cash generation (1.0 = baseline).</returns>
        public float GetCashModifier(ZoneTrait traits)
        {
            float modifier = 1.0f;

            if (traits.HasFlag(ZoneTrait.Commercial))
                modifier += CommercialCashBonus;

            if (traits.HasFlag(ZoneTrait.HighValue))
                modifier += HighValueCashBonus;

            return modifier;
        }

        /// <summary>
        /// Calculates the recruitment modifier for the given traits.
        /// </summary>
        /// <param name="traits">The combined zone traits.</param>
        /// <returns>Multiplier for recruitment rate (1.0 = baseline).</returns>
        public float GetRecruitmentModifier(ZoneTrait traits)
        {
            float modifier = 1.0f;

            if (traits.HasFlag(ZoneTrait.Residential))
                modifier += ResidentialRecruitBonus;

            if (traits.HasFlag(ZoneTrait.HighValue))
                modifier += HighValueRecruitBonus;

            return modifier;
        }

        /// <summary>
        /// Calculates the weapons production modifier for the given traits.
        /// </summary>
        /// <param name="traits">The combined zone traits.</param>
        /// <returns>Multiplier for weapons production (1.0 = baseline).</returns>
        public float GetWeaponsModifier(ZoneTrait traits)
        {
            float modifier = 1.0f;

            if (traits.HasFlag(ZoneTrait.Industrial))
                modifier += IndustrialWeaponBonus;

            if (traits.HasFlag(ZoneTrait.Port))
                modifier += PortWeaponBonus;

            if (traits.HasFlag(ZoneTrait.HighValue))
                modifier += HighValueWeaponBonus;

            return modifier;
        }

        /// <summary>
        /// Calculates the defense modifier for the given traits.
        /// </summary>
        /// <param name="traits">The combined zone traits.</param>
        /// <returns>Multiplier for defense effectiveness (1.0 = baseline).</returns>
        public float GetDefenseModifier(ZoneTrait traits)
        {
            float modifier = 1.0f;

            if (traits.HasFlag(ZoneTrait.Fortified))
                modifier += FortifiedDefenseBonus;

            return modifier;
        }

        /// <summary>
        /// Calculates the reinforcement speed modifier for the given traits.
        /// </summary>
        /// <param name="traits">The combined zone traits.</param>
        /// <returns>Multiplier for reinforcement speed (1.0 = baseline).</returns>
        public float GetReinforcementSpeedModifier(ZoneTrait traits)
        {
            float modifier = 1.0f;

            if (traits.HasFlag(ZoneTrait.Airfield))
                modifier += AirfieldReinforcementBonus;

            if (traits.HasFlag(ZoneTrait.Port))
                modifier += PortReinforcementBonus;

            return modifier;
        }

        /// <summary>
        /// Gets a human-readable description of the given traits.
        /// </summary>
        /// <param name="traits">The combined zone traits.</param>
        /// <returns>Description string, or empty if no traits.</returns>
        public string GetDescription(ZoneTrait traits)
        {
            if (traits == ZoneTrait.None)
                return string.Empty;

            var activeTraits = GetActiveTraits(traits);
            var descriptions = new StringBuilder();

            foreach (var trait in activeTraits)
            {
                if (TraitDescriptions.TryGetValue(trait, out var description) && !string.IsNullOrEmpty(description))
                {
                    if (descriptions.Length > 0)
                        descriptions.Append("; ");
                    descriptions.Append(description);
                }
            }

            return descriptions.ToString();
        }

        /// <summary>
        /// Returns all individual traits that are active in the combined trait flags.
        /// </summary>
        /// <param name="traits">The combined zone traits.</param>
        /// <returns>Enumerable of individual active traits (excludes None).</returns>
        public IEnumerable<ZoneTrait> GetActiveTraits(ZoneTrait traits)
        {
            if (traits == ZoneTrait.None)
                return Enumerable.Empty<ZoneTrait>();

            var allTraits = new[]
            {
                ZoneTrait.Industrial,
                ZoneTrait.Commercial,
                ZoneTrait.Residential,
                ZoneTrait.Port,
                ZoneTrait.Airfield,
                ZoneTrait.Fortified,
                ZoneTrait.HighValue
            };

            return allTraits.Where(t => traits.HasFlag(t));
        }
    }
}
