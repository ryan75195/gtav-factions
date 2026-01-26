using System;
using System.Collections.Generic;
using FactionWars.Core.Interfaces;
using FactionWars.Core.Models;
using FactionWars.Core.Services;
using Xunit;

namespace FactionWars.Tests.Unit.Core
{
    public class DefenderTierServiceTests
    {
        private readonly IDefenderTierService _service;

        public DefenderTierServiceTests()
        {
            _service = new DefenderTierService();
        }

        [Fact]
        public void GetTierConfig_Basic_ReturnsCorrectConfiguration()
        {
            // Act
            var config = _service.GetTierConfig(DefenderTier.Basic);

            // Assert
            Assert.Equal(DefenderTier.Basic, config.Tier);
            Assert.Equal(200, config.Cost);
            Assert.Equal(200, config.Health);
            Assert.Equal(50, config.Armor);
            Assert.Equal("WEAPON_PISTOL", config.Weapon);
            Assert.Equal(0.3f, config.Accuracy, 2);
        }

        [Fact]
        public void GetTierConfig_Medium_ReturnsCorrectConfiguration()
        {
            // Act
            var config = _service.GetTierConfig(DefenderTier.Medium);

            // Assert
            Assert.Equal(DefenderTier.Medium, config.Tier);
            Assert.Equal(500, config.Cost);
            Assert.Equal(350, config.Health);
            Assert.Equal(100, config.Armor);
            Assert.Equal("WEAPON_SMG", config.Weapon);
            Assert.Equal(0.5f, config.Accuracy, 2);
        }

        [Fact]
        public void GetTierConfig_Heavy_ReturnsCorrectConfiguration()
        {
            // Act
            var config = _service.GetTierConfig(DefenderTier.Heavy);

            // Assert
            Assert.Equal(DefenderTier.Heavy, config.Tier);
            Assert.Equal(1000, config.Cost);
            Assert.Equal(500, config.Health);
            Assert.Equal(200, config.Armor);
            Assert.Equal("WEAPON_CARBINERIFLE", config.Weapon);
            Assert.Equal(0.7f, config.Accuracy, 2);
        }

        [Fact]
        public void GetTierConfig_Elite_ReturnsCorrectConfiguration()
        {
            // Act
            var config = _service.GetTierConfig(DefenderTier.Elite);

            // Assert
            Assert.Equal(DefenderTier.Elite, config.Tier);
            Assert.Equal(2000, config.Cost);
            Assert.Equal(650, config.Health);
            Assert.Equal(200, config.Armor);
            Assert.Equal("WEAPON_RPG", config.Weapon);
            Assert.Equal(0.8f, config.Accuracy, 2);
            Assert.Equal(2.5f, config.CombatModifier, 2);
        }

        [Fact]
        public void GetAllTierConfigs_ReturnsAllFourTiers()
        {
            // Act
            var configs = _service.GetAllTierConfigs();

            // Assert
            Assert.Equal(4, configs.Count);
            Assert.Contains(configs, c => c.Tier == DefenderTier.Basic);
            Assert.Contains(configs, c => c.Tier == DefenderTier.Medium);
            Assert.Contains(configs, c => c.Tier == DefenderTier.Heavy);
            Assert.Contains(configs, c => c.Tier == DefenderTier.Elite);
        }

        [Fact]
        public void GetCost_ReturnsCorrectCostForEachTier()
        {
            // Assert
            Assert.Equal(200, _service.GetCost(DefenderTier.Basic));
            Assert.Equal(500, _service.GetCost(DefenderTier.Medium));
            Assert.Equal(1000, _service.GetCost(DefenderTier.Heavy));
            Assert.Equal(2000, _service.GetCost(DefenderTier.Elite));
        }

        [Fact]
        public void GetCombatModifier_ReturnsCorrectModifierForEachTier()
        {
            // Basic=1.0, Medium=1.5, Heavy=2.0, Elite=2.5
            Assert.Equal(1.0f, _service.GetCombatModifier(DefenderTier.Basic), 2);
            Assert.Equal(1.5f, _service.GetCombatModifier(DefenderTier.Medium), 2);
            Assert.Equal(2.0f, _service.GetCombatModifier(DefenderTier.Heavy), 2);
            Assert.Equal(2.5f, _service.GetCombatModifier(DefenderTier.Elite), 2);
        }

        [Fact]
        public void CalculateTotalCost_WithMixedTiers_ReturnsCorrectSum()
        {
            // Arrange
            var troops = new Dictionary<DefenderTier, int>
            {
                { DefenderTier.Basic, 10 },   // 10 * 200 = 2000
                { DefenderTier.Medium, 5 },   // 5 * 500 = 2500
                { DefenderTier.Heavy, 2 }     // 2 * 1000 = 2000
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
            var troops = new Dictionary<DefenderTier, int>();

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
            var troops = new Dictionary<DefenderTier, int>
            {
                { DefenderTier.Basic, 10 },   // 10 * 1.0 = 10
                { DefenderTier.Medium, 5 },   // 5 * 1.5 = 7.5
                { DefenderTier.Heavy, 2 }     // 2 * 2.0 = 4
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
            var troops = new Dictionary<DefenderTier, int>();

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
        public void DefenderTierService_ImplementsIDefenderTierService()
        {
            // Assert
            Assert.IsAssignableFrom<IDefenderTierService>(_service);
        }
    }
}
