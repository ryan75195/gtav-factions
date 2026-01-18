using System;
using Xunit;
using Moq;
using FactionWars.Balance.Models;
using FactionWars.Balance.Interfaces;
using FactionWars.Balance.Services;
using FactionWars.AI.Models;

namespace FactionWars.Tests.Unit.Balance
{
    public class BalanceConfigurationServiceTests
    {
        #region Constructor Tests

        [Fact]
        public void Constructor_WithDefaultConfiguration_SetsNormalPreset()
        {
            // Arrange & Act
            var service = new BalanceConfigurationService();

            // Assert
            Assert.NotNull(service.CurrentConfiguration);
            Assert.Equal("Default", service.CurrentConfiguration.PresetName);
        }

        [Fact]
        public void Constructor_WithConfiguration_SetsProvidedConfiguration()
        {
            // Arrange
            var config = BalancePresets.Hard();

            // Act
            var service = new BalanceConfigurationService(config);

            // Assert
            Assert.Equal("Hard", service.CurrentConfiguration.PresetName);
        }

        [Fact]
        public void Constructor_WithNullConfiguration_ThrowsArgumentNullException()
        {
            // Arrange & Act & Assert
            Assert.Throws<ArgumentNullException>(() => new BalanceConfigurationService(null));
        }

        #endregion

        #region ApplyPreset Tests

        [Theory]
        [InlineData(AIDifficulty.Easy, "Easy")]
        [InlineData(AIDifficulty.Normal, "Normal")]
        [InlineData(AIDifficulty.Hard, "Hard")]
        [InlineData(AIDifficulty.Veteran, "Veteran")]
        public void ApplyPreset_ChangesCurrentConfiguration(AIDifficulty difficulty, string expectedName)
        {
            // Arrange
            var service = new BalanceConfigurationService();

            // Act
            service.ApplyPreset(difficulty);

            // Assert
            Assert.Equal(expectedName, service.CurrentConfiguration.PresetName);
        }

        [Fact]
        public void ApplyPreset_RaisesConfigurationChangedEvent()
        {
            // Arrange
            var service = new BalanceConfigurationService();
            var eventRaised = false;
            service.ConfigurationChanged += (sender, args) => eventRaised = true;

            // Act
            service.ApplyPreset(AIDifficulty.Hard);

            // Assert
            Assert.True(eventRaised);
        }

        [Fact]
        public void ApplyPreset_EventContainsOldAndNewConfiguration()
        {
            // Arrange
            var service = new BalanceConfigurationService();
            service.ApplyPreset(AIDifficulty.Easy); // Start with Easy

            BalanceConfigurationChangedEventArgs capturedArgs = null;
            service.ConfigurationChanged += (sender, args) => capturedArgs = args;

            // Act
            service.ApplyPreset(AIDifficulty.Hard);

            // Assert
            Assert.NotNull(capturedArgs);
            Assert.Equal("Easy", capturedArgs.OldConfiguration.PresetName);
            Assert.Equal("Hard", capturedArgs.NewConfiguration.PresetName);
        }

        #endregion

        #region UpdateConfiguration Tests

        [Fact]
        public void UpdateConfiguration_AppliesChanges()
        {
            // Arrange
            var service = new BalanceConfigurationService();
            var newConfig = new BalanceConfiguration { BaseCashGeneration = 200 };
            newConfig.PresetName = "Custom";

            // Act
            service.UpdateConfiguration(newConfig);

            // Assert
            Assert.Equal(200, service.CurrentConfiguration.BaseCashGeneration);
            Assert.Equal("Custom", service.CurrentConfiguration.PresetName);
        }

        [Fact]
        public void UpdateConfiguration_WithNull_ThrowsArgumentNullException()
        {
            // Arrange
            var service = new BalanceConfigurationService();

            // Act & Assert
            Assert.Throws<ArgumentNullException>(() => service.UpdateConfiguration(null));
        }

        [Fact]
        public void UpdateConfiguration_WithInvalidConfig_ThrowsInvalidOperationException()
        {
            // Arrange
            var service = new BalanceConfigurationService();
            var invalidConfig = new BalanceConfiguration
            {
                MinPedsPerWave = 20,
                MaxPedsPerWave = 5 // Invalid: min > max
            };

            // Act & Assert
            Assert.Throws<InvalidOperationException>(() => service.UpdateConfiguration(invalidConfig));
        }

        [Fact]
        public void UpdateConfiguration_RaisesConfigurationChangedEvent()
        {
            // Arrange
            var service = new BalanceConfigurationService();
            var eventRaised = false;
            service.ConfigurationChanged += (sender, args) => eventRaised = true;

            var newConfig = new BalanceConfiguration();

            // Act
            service.UpdateConfiguration(newConfig);

            // Assert
            Assert.True(eventRaised);
        }

        #endregion

        #region GetCurrentConfiguration Tests

        [Fact]
        public void GetCurrentConfiguration_ReturnsClone()
        {
            // Arrange
            var service = new BalanceConfigurationService();

            // Act
            var config = service.CurrentConfiguration;
            config.BaseCashGeneration = 9999;

            // Assert - Original should be unchanged
            Assert.NotEqual(9999, service.CurrentConfiguration.BaseCashGeneration);
        }

        #endregion

        #region Reset Tests

        [Fact]
        public void Reset_ReturnsToDefaultConfiguration()
        {
            // Arrange
            var service = new BalanceConfigurationService();
            service.ApplyPreset(AIDifficulty.Veteran);

            // Act
            service.Reset();

            // Assert
            Assert.Equal("Default", service.CurrentConfiguration.PresetName);
        }

        [Fact]
        public void Reset_RaisesConfigurationChangedEvent()
        {
            // Arrange
            var service = new BalanceConfigurationService();
            service.ApplyPreset(AIDifficulty.Hard);

            var eventRaised = false;
            service.ConfigurationChanged += (sender, args) => eventRaised = true;

            // Act
            service.Reset();

            // Assert
            Assert.True(eventRaised);
        }

        #endregion

        #region GetEffectiveMultiplier Tests

        [Fact]
        public void GetEffectiveResourceMultiplier_CombinesPlayerAndBaseMultiplier()
        {
            // Arrange
            var config = new BalanceConfiguration
            {
                PlayerResourceMultiplier = 1.5f
            };
            var service = new BalanceConfigurationService(config);

            // Act
            var multiplier = service.GetEffectiveResourceMultiplier(isPlayerFaction: true);

            // Assert
            Assert.Equal(1.5f, multiplier);
        }

        [Fact]
        public void GetEffectiveResourceMultiplier_ForAI_ReturnsOne()
        {
            // Arrange
            var config = new BalanceConfiguration
            {
                PlayerResourceMultiplier = 1.5f
            };
            var service = new BalanceConfigurationService(config);

            // Act
            var multiplier = service.GetEffectiveResourceMultiplier(isPlayerFaction: false);

            // Assert
            Assert.Equal(1.0f, multiplier);
        }

        [Fact]
        public void GetEffectiveCombatMultiplier_AppliesPlayerBonus()
        {
            // Arrange
            var config = new BalanceConfiguration
            {
                PlayerCombatMultiplier = 1.25f
            };
            var service = new BalanceConfigurationService(config);

            // Act
            var multiplier = service.GetEffectiveCombatMultiplier(isPlayerFaction: true);

            // Assert
            Assert.Equal(1.25f, multiplier);
        }

        [Fact]
        public void GetEffectiveDefenseMultiplier_AppliesPlayerBonus()
        {
            // Arrange
            var config = new BalanceConfiguration
            {
                PlayerDefenseMultiplier = 1.3f
            };
            var service = new BalanceConfigurationService(config);

            // Act
            var multiplier = service.GetEffectiveDefenseMultiplier(isPlayerFaction: true);

            // Assert
            Assert.Equal(1.3f, multiplier);
        }

        #endregion

        #region Configuration Snapshot Tests

        [Fact]
        public void CreateSnapshot_ReturnsCopyOfCurrentConfiguration()
        {
            // Arrange
            var service = new BalanceConfigurationService();
            service.ApplyPreset(AIDifficulty.Hard);

            // Act
            var snapshot = service.CreateSnapshot();
            service.ApplyPreset(AIDifficulty.Easy);

            // Assert - Snapshot should be unchanged
            Assert.Equal("Hard", snapshot.PresetName);
            Assert.Equal("Easy", service.CurrentConfiguration.PresetName);
        }

        [Fact]
        public void RestoreSnapshot_RestoresPreviousConfiguration()
        {
            // Arrange
            var service = new BalanceConfigurationService();
            service.ApplyPreset(AIDifficulty.Hard);
            var snapshot = service.CreateSnapshot();
            service.ApplyPreset(AIDifficulty.Easy);

            // Act
            service.RestoreSnapshot(snapshot);

            // Assert
            Assert.Equal("Hard", service.CurrentConfiguration.PresetName);
        }

        [Fact]
        public void RestoreSnapshot_WithNull_ThrowsArgumentNullException()
        {
            // Arrange
            var service = new BalanceConfigurationService();

            // Act & Assert
            Assert.Throws<ArgumentNullException>(() => service.RestoreSnapshot(null));
        }

        #endregion
    }
}
