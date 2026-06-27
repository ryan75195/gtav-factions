using System;
using System.Collections.Generic;
using FactionWars.Core.Interfaces;
using FactionWars.Core.Models;
using FactionWars.Core.Services;
using Xunit;

namespace FactionWars.Tests.Unit.Core
{
    public class DefenderRoleServiceTests
    {
        private readonly IDefenderRoleService _service;

        public DefenderRoleServiceTests()
        {
            _service = new DefenderRoleService();
        }

        [Fact]
        public void GetRoleConfig_Basic_ReturnsCorrectConfiguration()
        {
            // Act
            var config = _service.GetRoleConfig(DefenderRole.Grunt);

            // Assert
            Assert.Equal(DefenderRole.Grunt, config.Role);
            Assert.Equal(200, config.Cost);
            Assert.Equal(200, config.Health);
            Assert.Equal(50, config.Armor);
            Assert.Equal("WEAPON_PISTOL", config.Weapon);
            Assert.Equal(0.3f, config.Accuracy, 2);
            Assert.True(config.RagdollEnabled);
        }

        [Fact]
        public void GetRoleConfig_Medium_ReturnsCorrectConfiguration()
        {
            // Act
            var config = _service.GetRoleConfig(DefenderRole.Gunner);

            // Assert
            Assert.Equal(DefenderRole.Gunner, config.Role);
            Assert.Equal(500, config.Cost);
            Assert.Equal(350, config.Health);
            Assert.Equal(100, config.Armor);
            Assert.Equal("WEAPON_SMG", config.Weapon);
            Assert.Equal(0.5f, config.Accuracy, 2);
            Assert.True(config.RagdollEnabled);
        }

        [Fact]
        public void GetRoleConfig_Heavy_ReturnsCorrectConfiguration()
        {
            // Act
            var config = _service.GetRoleConfig(DefenderRole.Rifleman);

            // Assert
            Assert.Equal(DefenderRole.Rifleman, config.Role);
            Assert.Equal(1000, config.Cost);
            Assert.Equal(500, config.Health);
            Assert.Equal(200, config.Armor);
            Assert.Equal("WEAPON_CARBINERIFLE", config.Weapon);
            Assert.Equal(0.7f, config.Accuracy, 2);
            Assert.False(config.RagdollEnabled);
        }

        [Fact]
        public void GetRoleConfig_Elite_ReturnsCorrectConfiguration()
        {
            // Act
            var config = _service.GetRoleConfig(DefenderRole.Rocketeer);

            // Assert
            Assert.Equal(DefenderRole.Rocketeer, config.Role);
            Assert.Equal(2000, config.Cost);
            Assert.Equal(650, config.Health);
            Assert.Equal(200, config.Armor);
            Assert.Equal("WEAPON_RPG", config.Weapon);
            Assert.Equal(0.8f, config.Accuracy, 2);
            Assert.Equal(2.5f, config.CombatModifier, 2);
            Assert.False(config.RagdollEnabled);
        }

        [Fact]
        public void GetAllRoleConfigs_ReturnsAllFourTiers()
        {
            // Act
            var configs = _service.GetAllRoleConfigs();

            // Assert
            Assert.Equal(4, configs.Count);
            Assert.Contains(configs, c => c.Role == DefenderRole.Grunt);
            Assert.Contains(configs, c => c.Role == DefenderRole.Gunner);
            Assert.Contains(configs, c => c.Role == DefenderRole.Rifleman);
            Assert.Contains(configs, c => c.Role == DefenderRole.Rocketeer);
        }

        [Fact]
        public void GetCost_ReturnsCorrectCostForEachTier()
        {
            // Assert
            Assert.Equal(200, _service.GetCost(DefenderRole.Grunt));
            Assert.Equal(500, _service.GetCost(DefenderRole.Gunner));
            Assert.Equal(1000, _service.GetCost(DefenderRole.Rifleman));
            Assert.Equal(2000, _service.GetCost(DefenderRole.Rocketeer));
        }

        [Fact]
        public void GetCombatModifier_ReturnsCorrectModifierForEachTier()
        {
            // Basic=1.0, Medium=1.5, Heavy=2.0, Elite=2.5
            Assert.Equal(1.0f, _service.GetCombatModifier(DefenderRole.Grunt), 2);
            Assert.Equal(1.5f, _service.GetCombatModifier(DefenderRole.Gunner), 2);
            Assert.Equal(2.0f, _service.GetCombatModifier(DefenderRole.Rifleman), 2);
            Assert.Equal(2.5f, _service.GetCombatModifier(DefenderRole.Rocketeer), 2);
        }

        [Fact]
        public void CalculateTotalCost_WithMixedTiers_ReturnsCorrectSum()
        {
            // Arrange
            var troops = new Dictionary<DefenderRole, int>
            {
                { DefenderRole.Grunt, 10 },   // 10 * 200 = 2000
                { DefenderRole.Gunner, 5 },   // 5 * 500 = 2500
                { DefenderRole.Rifleman, 2 }     // 2 * 1000 = 2000
            };

            // Act
            var totalCost = _service.CalculateTotalCost(troops);

            // Assert
            Assert.Equal(6500, totalCost);
        }

        [Fact]
        public void CalculateTotalCost_WithEmptyDictionary_ReturnsZero()
        {
            // Arrange
            var troops = new Dictionary<DefenderRole, int>();

            // Act
            var totalCost = _service.CalculateTotalCost(troops);

            // Assert
            Assert.Equal(0, totalCost);
        }

        [Fact]
        public void CalculateTotalCost_WithNullDictionary_ThrowsArgumentNullException()
        {
            // Act & Assert
            Assert.Throws<ArgumentNullException>(() => _service.CalculateTotalCost(null!));
        }

        [Fact]
        public void CalculateTotalStrength_WithMixedTiers_ReturnsCorrectSum()
        {
            // Arrange
            var troops = new Dictionary<DefenderRole, int>
            {
                { DefenderRole.Grunt, 10 },   // 10 * 1.0 = 10
                { DefenderRole.Gunner, 5 },   // 5 * 1.5 = 7.5
                { DefenderRole.Rifleman, 2 }     // 2 * 2.0 = 4
            };

            // Act
            var totalStrength = _service.CalculateTotalStrength(troops);

            // Assert
            Assert.Equal(21.5f, totalStrength, 2);
        }

        [Fact]
        public void CalculateTotalStrength_WithEmptyDictionary_ReturnsZero()
        {
            // Arrange
            var troops = new Dictionary<DefenderRole, int>();

            // Act
            var totalStrength = _service.CalculateTotalStrength(troops);

            // Assert
            Assert.Equal(0f, totalStrength, 2);
        }

        [Fact]
        public void CalculateTotalStrength_WithNullDictionary_ThrowsArgumentNullException()
        {
            // Act & Assert
            Assert.Throws<ArgumentNullException>(() => _service.CalculateTotalStrength(null!));
        }

        [Fact]
        public void DefenderRoleService_ImplementsIDefenderRoleService()
        {
            // Assert
            Assert.IsAssignableFrom<IDefenderRoleService>(_service);
        }
    }
}
