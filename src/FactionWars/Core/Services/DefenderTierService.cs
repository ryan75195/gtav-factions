using System;
using System.Collections.Generic;
using FactionWars.Core.Interfaces;
using FactionWars.Core.Models;

namespace FactionWars.Core.Services
{
    /// <summary>
    /// Default implementation of IDefenderTierService.
    /// Provides defender tier configurations with costs, stats, and combat modifiers.
    /// </summary>
    public class DefenderTierService : IDefenderTierService
    {
        private readonly Dictionary<DefenderTier, DefenderTierConfig> _configs;
        private readonly IReadOnlyList<DefenderTierConfig> _allConfigs;

        /// <summary>
        /// Creates a new DefenderTierService with default tier configurations.
        /// </summary>
        public DefenderTierService()
        {
            _configs = new Dictionary<DefenderTier, DefenderTierConfig>
            {
                {
                    DefenderTier.Basic,
                    new DefenderTierConfig(
                        tier: DefenderTier.Basic,
                        cost: 200,
                        health: 100,
                        armor: 0,
                        weapon: "Pistol",
                        accuracy: 0.3f,
                        combatModifier: 1.0f)
                },
                {
                    DefenderTier.Medium,
                    new DefenderTierConfig(
                        tier: DefenderTier.Medium,
                        cost: 500,
                        health: 150,
                        armor: 50,
                        weapon: "SMG",
                        accuracy: 0.5f,
                        combatModifier: 1.5f)
                },
                {
                    DefenderTier.Heavy,
                    new DefenderTierConfig(
                        tier: DefenderTier.Heavy,
                        cost: 1000,
                        health: 200,
                        armor: 100,
                        weapon: "Carbine",
                        accuracy: 0.7f,
                        combatModifier: 2.0f)
                }
            };

            _allConfigs = new List<DefenderTierConfig>(_configs.Values).AsReadOnly();
        }

        /// <inheritdoc />
        public DefenderTierConfig GetTierConfig(DefenderTier tier)
        {
            return _configs[tier];
        }

        /// <inheritdoc />
        public IReadOnlyList<DefenderTierConfig> GetAllTierConfigs()
        {
            return _allConfigs;
        }

        /// <inheritdoc />
        public int GetCost(DefenderTier tier)
        {
            return _configs[tier].Cost;
        }

        /// <inheritdoc />
        public float GetCombatModifier(DefenderTier tier)
        {
            return _configs[tier].CombatModifier;
        }

        /// <inheritdoc />
        public int CalculateTotalCost(Dictionary<DefenderTier, int> troopsByTier)
        {
            if (troopsByTier == null)
                throw new ArgumentNullException(nameof(troopsByTier));

            int totalCost = 0;
            foreach (var kvp in troopsByTier)
            {
                totalCost += GetCost(kvp.Key) * kvp.Value;
            }
            return totalCost;
        }

        /// <inheritdoc />
        public float CalculateTotalStrength(Dictionary<DefenderTier, int> troopsByTier)
        {
            if (troopsByTier == null)
                throw new ArgumentNullException(nameof(troopsByTier));

            float totalStrength = 0f;
            foreach (var kvp in troopsByTier)
            {
                totalStrength += GetCombatModifier(kvp.Key) * kvp.Value;
            }
            return totalStrength;
        }
    }
}
