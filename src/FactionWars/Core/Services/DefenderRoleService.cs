using System;
using System.Collections.Generic;
using FactionWars.Core.Interfaces;
using FactionWars.Core.Models;

namespace FactionWars.Core.Services
{
    /// <summary>
    /// Default implementation of IDefenderRoleService.
    /// Provides defender tier configurations with costs, stats, and combat modifiers.
    /// </summary>
    public class DefenderRoleService : IDefenderRoleService
    {
        private readonly Dictionary<DefenderRole, DefenderRoleConfig> _configs;
        private readonly IReadOnlyList<DefenderRoleConfig> _allConfigs;

        /// <summary>
        /// Creates a new DefenderRoleService with default tier configurations.
        /// </summary>
        public DefenderRoleService()
        {
            _configs = new Dictionary<DefenderRole, DefenderRoleConfig>
            {
                {
                    DefenderRole.Grunt,
                    new DefenderRoleConfig(
                        role: DefenderRole.Grunt,
                        cost: 200,
                        health: 200,
                        armor: 50,
                        weapon: "WEAPON_PISTOL",
                        accuracy: 0.25f,
                        combatModifier: 1.0f,
                        ragdollEnabled: true)
                },
                {
                    DefenderRole.Gunner,
                    new DefenderRoleConfig(
                        role: DefenderRole.Gunner,
                        cost: 500,
                        health: 350,
                        armor: 100,
                        weapon: "WEAPON_SMG",
                        accuracy: 0.45f,
                        combatModifier: 1.5f,
                        ragdollEnabled: true)
                },
                {
                    DefenderRole.Rifleman,
                    new DefenderRoleConfig(
                        role: DefenderRole.Rifleman,
                        cost: 1000,
                        health: 500,
                        armor: 200,
                        weapon: "WEAPON_CARBINERIFLE",
                        accuracy: 0.6f,
                        combatModifier: 2.0f,
                        ragdollEnabled: false)
                },
                {
                    DefenderRole.Rocketeer,
                    new DefenderRoleConfig(
                        role: DefenderRole.Rocketeer,
                        cost: 2000,
                        health: 650,
                        armor: 200,
                        weapon: "WEAPON_RPG",
                        accuracy: 0.7f,
                        combatModifier: 2.5f,
                        ragdollEnabled: false)
                },
                {
                    DefenderRole.Sniper,
                    new DefenderRoleConfig(
                        role: DefenderRole.Sniper,
                        cost: 1500,
                        health: 275,
                        armor: 50,
                        weapon: "WEAPON_SNIPERRIFLE",
                        accuracy: 0.7f,
                        combatModifier: 2.2f,
                        ragdollEnabled: false)
                }
            };

            _allConfigs = new List<DefenderRoleConfig>(_configs.Values).AsReadOnly();
        }

        /// <inheritdoc />
        public DefenderRoleConfig GetRoleConfig(DefenderRole tier)
        {
            return _configs[tier];
        }

        /// <inheritdoc />
        public IReadOnlyList<DefenderRoleConfig> GetAllRoleConfigs()
        {
            return _allConfigs;
        }

        /// <inheritdoc />
        public int GetCost(DefenderRole tier)
        {
            return _configs[tier].Cost;
        }

        /// <inheritdoc />
        public float GetCombatModifier(DefenderRole tier)
        {
            return _configs[tier].CombatModifier;
        }

        /// <inheritdoc />
        public int CalculateTotalCost(Dictionary<DefenderRole, int> troopsByTier)
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
        public float CalculateTotalStrength(Dictionary<DefenderRole, int> troopsByTier)
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
