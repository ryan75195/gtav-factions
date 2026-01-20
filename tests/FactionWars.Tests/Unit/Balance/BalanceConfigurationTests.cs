using System;
using Xunit;
using FactionWars.Balance.Models;

namespace FactionWars.Tests.Unit.Balance
{
    public class BalanceConfigurationTests
    {
        #region Default Values Tests

        [Fact]
        public void Constructor_WithDefaults_SetsReasonableEconomyValues()
        {
            // Arrange & Act
            var config = new BalanceConfiguration();

            // Assert - Economy defaults
            Assert.Equal(100, config.BaseCashGeneration);
            Assert.Equal(10, config.BaseRecruitmentGeneration);
            Assert.Equal(5, config.BaseWeaponsGeneration);
            Assert.Equal(100000, config.MaxCashStorage);
            Assert.Equal(1000, config.MaxRecruitmentStorage);
            Assert.Equal(500, config.MaxWeaponsStorage);
        }

        [Fact]
        public void Constructor_WithDefaults_SetsReasonableCombatValues()
        {
            // Arrange & Act
            var config = new BalanceConfiguration();

            // Assert - Combat defaults
            Assert.Equal(100f, config.AttackerVictoryThreshold);
            Assert.Equal(0f, config.DefenderVictoryThreshold);
            Assert.Equal(5f, config.MinimumHoldTimeSeconds);
            Assert.Equal(30, config.MaxActivePeds);
        }

        [Fact]
        public void Constructor_WithDefaults_SetsReasonableReinforcementValues()
        {
            // Arrange & Act
            var config = new BalanceConfiguration();

            // Assert - Reinforcement defaults
            Assert.Equal(30f, config.ReinforcementCooldownSeconds);
            Assert.Equal(5, config.MinPedsPerWave);
            Assert.Equal(10, config.MaxPedsPerWave);
            Assert.Equal(3, config.MaxActiveWaves);
            Assert.Equal(100, config.ResourceCostPerPed);
        }

        [Fact]
        public void Constructor_WithDefaults_SetsReasonableResourceTickValues()
        {
            // Arrange & Act
            var config = new BalanceConfiguration();

            // Assert - Resource tick defaults
            Assert.Equal(300f, config.ResourceTickIntervalSeconds); // 5 minutes
        }

        [Fact]
        public void Constructor_WithDefaults_SetsPlayerBonusToOne()
        {
            // Arrange & Act
            var config = new BalanceConfiguration();

            // Assert - Player bonus defaults (no bonuses)
            Assert.Equal(1.0f, config.PlayerResourceMultiplier);
            Assert.Equal(1.0f, config.PlayerCombatMultiplier);
            Assert.Equal(1.0f, config.PlayerDefenseMultiplier);
        }

        #endregion

        #region Validation Tests

        [Theory]
        [InlineData(0)]
        [InlineData(-1)]
        [InlineData(-100)]
        public void BaseCashGeneration_WhenSetToZeroOrNegative_ThrowsArgumentOutOfRangeException(int value)
        {
            // Arrange
            var config = new BalanceConfiguration();

            // Act & Assert
            Assert.Throws<ArgumentOutOfRangeException>(() => config.BaseCashGeneration = value);
        }

        [Theory]
        [InlineData(0)]
        [InlineData(-1)]
        public void BaseRecruitmentGeneration_WhenSetToZeroOrNegative_ThrowsArgumentOutOfRangeException(int value)
        {
            // Arrange
            var config = new BalanceConfiguration();

            // Act & Assert
            Assert.Throws<ArgumentOutOfRangeException>(() => config.BaseRecruitmentGeneration = value);
        }

        [Theory]
        [InlineData(0)]
        [InlineData(-1)]
        public void BaseWeaponsGeneration_WhenSetToZeroOrNegative_ThrowsArgumentOutOfRangeException(int value)
        {
            // Arrange
            var config = new BalanceConfiguration();

            // Act & Assert
            Assert.Throws<ArgumentOutOfRangeException>(() => config.BaseWeaponsGeneration = value);
        }

        [Theory]
        [InlineData(-0.1f)]
        [InlineData(-1f)]
        public void AttackerVictoryThreshold_WhenSetToNegative_ThrowsArgumentOutOfRangeException(float value)
        {
            // Arrange
            var config = new BalanceConfiguration();

            // Act & Assert
            Assert.Throws<ArgumentOutOfRangeException>(() => config.AttackerVictoryThreshold = value);
        }

        [Theory]
        [InlineData(100.1f)]
        [InlineData(200f)]
        public void AttackerVictoryThreshold_WhenSetAbove100_ThrowsArgumentOutOfRangeException(float value)
        {
            // Arrange
            var config = new BalanceConfiguration();

            // Act & Assert
            Assert.Throws<ArgumentOutOfRangeException>(() => config.AttackerVictoryThreshold = value);
        }

        [Theory]
        [InlineData(0f)]
        [InlineData(-0.1f)]
        public void PlayerResourceMultiplier_WhenSetToZeroOrNegative_ThrowsArgumentOutOfRangeException(float value)
        {
            // Arrange
            var config = new BalanceConfiguration();

            // Act & Assert
            Assert.Throws<ArgumentOutOfRangeException>(() => config.PlayerResourceMultiplier = value);
        }

        [Theory]
        [InlineData(0f)]
        [InlineData(-1f)]
        public void PlayerCombatMultiplier_WhenSetToZeroOrNegative_ThrowsArgumentOutOfRangeException(float value)
        {
            // Arrange
            var config = new BalanceConfiguration();

            // Act & Assert
            Assert.Throws<ArgumentOutOfRangeException>(() => config.PlayerCombatMultiplier = value);
        }

        [Theory]
        [InlineData(0f)]
        [InlineData(-1f)]
        public void PlayerDefenseMultiplier_WhenSetToZeroOrNegative_ThrowsArgumentOutOfRangeException(float value)
        {
            // Arrange
            var config = new BalanceConfiguration();

            // Act & Assert
            Assert.Throws<ArgumentOutOfRangeException>(() => config.PlayerDefenseMultiplier = value);
        }

        [Fact]
        public void MaxActivePeds_WhenSetToZero_ThrowsArgumentOutOfRangeException()
        {
            // Arrange
            var config = new BalanceConfiguration();

            // Act & Assert
            Assert.Throws<ArgumentOutOfRangeException>(() => config.MaxActivePeds = 0);
        }

        [Fact]
        public void MinPedsPerWave_WhenGreaterThanMax_ValidationFails()
        {
            // Arrange
            var config = new BalanceConfiguration
            {
                MinPedsPerWave = 15,
                MaxPedsPerWave = 10
            };

            // Act
            var result = config.Validate();

            // Assert
            Assert.False(result.IsValid);
            Assert.Contains("MinPedsPerWave", result.Errors[0]);
        }

        [Fact]
        public void Validate_WithValidConfiguration_ReturnsValidResult()
        {
            // Arrange
            var config = new BalanceConfiguration();

            // Act
            var result = config.Validate();

            // Assert
            Assert.True(result.IsValid);
            Assert.Empty(result.Errors);
        }

        #endregion

        #region Clone Tests

        [Fact]
        public void Clone_CreatesIndependentCopy()
        {
            // Arrange
            var original = new BalanceConfiguration
            {
                BaseCashGeneration = 200,
                PlayerResourceMultiplier = 1.5f
            };

            // Act
            var clone = original.Clone();
            clone.BaseCashGeneration = 300;

            // Assert
            Assert.Equal(200, original.BaseCashGeneration);
            Assert.Equal(300, clone.BaseCashGeneration);
        }

        [Fact]
        public void Clone_CopiesAllProperties()
        {
            // Arrange
            var original = new BalanceConfiguration
            {
                BaseCashGeneration = 200,
                BaseRecruitmentGeneration = 20,
                BaseWeaponsGeneration = 10,
                MaxCashStorage = 200000,
                MaxRecruitmentStorage = 2000,
                MaxWeaponsStorage = 1000,
                AttackerVictoryThreshold = 90f,
                DefenderVictoryThreshold = 10f,
                MinimumHoldTimeSeconds = 10f,
                MaxActivePeds = 40,
                ReinforcementCooldownSeconds = 45f,
                MinPedsPerWave = 3,
                MaxPedsPerWave = 8,
                MaxActiveWaves = 4,
                ResourceCostPerPed = 150,
                ResourceTickIntervalSeconds = 180f,
                PlayerResourceMultiplier = 1.5f,
                PlayerCombatMultiplier = 1.2f,
                PlayerDefenseMultiplier = 1.3f
            };

            // Act
            var clone = original.Clone();

            // Assert
            Assert.Equal(original.BaseCashGeneration, clone.BaseCashGeneration);
            Assert.Equal(original.BaseRecruitmentGeneration, clone.BaseRecruitmentGeneration);
            Assert.Equal(original.BaseWeaponsGeneration, clone.BaseWeaponsGeneration);
            Assert.Equal(original.MaxCashStorage, clone.MaxCashStorage);
            Assert.Equal(original.MaxRecruitmentStorage, clone.MaxRecruitmentStorage);
            Assert.Equal(original.MaxWeaponsStorage, clone.MaxWeaponsStorage);
            Assert.Equal(original.AttackerVictoryThreshold, clone.AttackerVictoryThreshold);
            Assert.Equal(original.DefenderVictoryThreshold, clone.DefenderVictoryThreshold);
            Assert.Equal(original.MinimumHoldTimeSeconds, clone.MinimumHoldTimeSeconds);
            Assert.Equal(original.MaxActivePeds, clone.MaxActivePeds);
            Assert.Equal(original.ReinforcementCooldownSeconds, clone.ReinforcementCooldownSeconds);
            Assert.Equal(original.MinPedsPerWave, clone.MinPedsPerWave);
            Assert.Equal(original.MaxPedsPerWave, clone.MaxPedsPerWave);
            Assert.Equal(original.MaxActiveWaves, clone.MaxActiveWaves);
            Assert.Equal(original.ResourceCostPerPed, clone.ResourceCostPerPed);
            Assert.Equal(original.ResourceTickIntervalSeconds, clone.ResourceTickIntervalSeconds);
            Assert.Equal(original.PlayerResourceMultiplier, clone.PlayerResourceMultiplier);
            Assert.Equal(original.PlayerCombatMultiplier, clone.PlayerCombatMultiplier);
            Assert.Equal(original.PlayerDefenseMultiplier, clone.PlayerDefenseMultiplier);
        }

        #endregion

        #region PresetName Tests

        [Fact]
        public void PresetName_DefaultConfiguration_ReturnsCustom()
        {
            // Arrange & Act
            var config = new BalanceConfiguration();

            // Assert
            Assert.Equal("Default", config.PresetName);
        }

        [Fact]
        public void PresetName_CanBeSetExplicitly()
        {
            // Arrange
            var config = new BalanceConfiguration();

            // Act
            config.PresetName = "My Custom Config";

            // Assert
            Assert.Equal("My Custom Config", config.PresetName);
        }

        #endregion

        #region AI Aggression Settings Tests

        [Fact]
        public void Constructor_WithDefaults_SetsReasonableAIAggressionValues()
        {
            // Arrange & Act
            var config = new BalanceConfiguration();

            // Assert - AI aggression defaults
            Assert.Equal(5f, config.AIDecisionIntervalSeconds);
            Assert.Equal(1.0f, config.AIAggressionMultiplier);
            Assert.Equal(30f, config.AIAttackCooldownSeconds);
            Assert.Equal(1.0f, config.AITroopCommitmentMultiplier);
        }

        [Theory]
        [InlineData(0f)]
        [InlineData(-1f)]
        public void AIDecisionIntervalSeconds_WhenSetToZeroOrNegative_ThrowsArgumentOutOfRangeException(float value)
        {
            // Arrange
            var config = new BalanceConfiguration();

            // Act & Assert
            Assert.Throws<ArgumentOutOfRangeException>(() => config.AIDecisionIntervalSeconds = value);
        }

        [Theory]
        [InlineData(0f)]
        [InlineData(-0.5f)]
        public void AIAggressionMultiplier_WhenSetToZeroOrNegative_ThrowsArgumentOutOfRangeException(float value)
        {
            // Arrange
            var config = new BalanceConfiguration();

            // Act & Assert
            Assert.Throws<ArgumentOutOfRangeException>(() => config.AIAggressionMultiplier = value);
        }

        [Theory]
        [InlineData(-1f)]
        [InlineData(-0.1f)]
        public void AIAttackCooldownSeconds_WhenSetToNegative_ThrowsArgumentOutOfRangeException(float value)
        {
            // Arrange
            var config = new BalanceConfiguration();

            // Act & Assert
            Assert.Throws<ArgumentOutOfRangeException>(() => config.AIAttackCooldownSeconds = value);
        }

        [Theory]
        [InlineData(0f)]
        [InlineData(-1f)]
        public void AITroopCommitmentMultiplier_WhenSetToZeroOrNegative_ThrowsArgumentOutOfRangeException(float value)
        {
            // Arrange
            var config = new BalanceConfiguration();

            // Act & Assert
            Assert.Throws<ArgumentOutOfRangeException>(() => config.AITroopCommitmentMultiplier = value);
        }

        [Fact]
        public void Clone_CopiesAIAggressionProperties()
        {
            // Arrange
            var original = new BalanceConfiguration
            {
                AIDecisionIntervalSeconds = 10f,
                AIAggressionMultiplier = 1.5f,
                AIAttackCooldownSeconds = 60f,
                AITroopCommitmentMultiplier = 0.8f
            };

            // Act
            var clone = original.Clone();

            // Assert
            Assert.Equal(original.AIDecisionIntervalSeconds, clone.AIDecisionIntervalSeconds);
            Assert.Equal(original.AIAggressionMultiplier, clone.AIAggressionMultiplier);
            Assert.Equal(original.AIAttackCooldownSeconds, clone.AIAttackCooldownSeconds);
            Assert.Equal(original.AITroopCommitmentMultiplier, clone.AITroopCommitmentMultiplier);
        }

        #endregion
    }
}
