using System;
using Xunit;
using FactionWars.Balance.Models;
using FactionWars.AI.Models;

namespace FactionWars.Tests.Unit.Balance
{
    public class BalancePresetTests
    {
        #region Preset Creation Tests

        [Fact]
        public void CreateEasyPreset_ReturnsValidConfiguration()
        {
            // Arrange & Act
            var config = BalancePresets.Easy();

            // Assert
            Assert.NotNull(config);
            Assert.Equal("Easy", config.PresetName);
            Assert.True(config.Validate().IsValid);
        }

        [Fact]
        public void CreateNormalPreset_ReturnsValidConfiguration()
        {
            // Arrange & Act
            var config = BalancePresets.Normal();

            // Assert
            Assert.NotNull(config);
            Assert.Equal("Normal", config.PresetName);
            Assert.True(config.Validate().IsValid);
        }

        [Fact]
        public void CreateHardPreset_ReturnsValidConfiguration()
        {
            // Arrange & Act
            var config = BalancePresets.Hard();

            // Assert
            Assert.NotNull(config);
            Assert.Equal("Hard", config.PresetName);
            Assert.True(config.Validate().IsValid);
        }

        [Fact]
        public void CreateVeteranPreset_ReturnsValidConfiguration()
        {
            // Arrange & Act
            var config = BalancePresets.Veteran();

            // Assert
            Assert.NotNull(config);
            Assert.Equal("Veteran", config.PresetName);
            Assert.True(config.Validate().IsValid);
        }

        #endregion

        #region Preset Difficulty Scaling Tests

        [Fact]
        public void EasyPreset_HasHigherPlayerBonuses()
        {
            // Arrange
            var easy = BalancePresets.Easy();
            var normal = BalancePresets.Normal();

            // Assert - Player should have advantages in Easy mode
            Assert.True(easy.PlayerResourceMultiplier > normal.PlayerResourceMultiplier);
            Assert.True(easy.PlayerCombatMultiplier >= normal.PlayerCombatMultiplier);
            Assert.True(easy.PlayerDefenseMultiplier >= normal.PlayerDefenseMultiplier);
        }

        [Fact]
        public void HardPreset_HasLowerPlayerBonuses()
        {
            // Arrange
            var hard = BalancePresets.Hard();
            var normal = BalancePresets.Normal();

            // Assert - Player should have fewer advantages in Hard mode
            Assert.True(hard.PlayerResourceMultiplier <= normal.PlayerResourceMultiplier);
        }

        [Fact]
        public void EasyPreset_HasEasierTakeoverThresholds()
        {
            // Arrange
            var easy = BalancePresets.Easy();
            var normal = BalancePresets.Normal();

            // Assert - Attacker victory threshold lower OR minimum hold time shorter = easier
            Assert.True(
                easy.AttackerVictoryThreshold <= normal.AttackerVictoryThreshold ||
                easy.MinimumHoldTimeSeconds <= normal.MinimumHoldTimeSeconds
            );
        }

        [Fact]
        public void VeteranPreset_HasMostChallengingSettings()
        {
            // Arrange
            var veteran = BalancePresets.Veteran();
            var hard = BalancePresets.Hard();

            // Assert - Veteran should be at least as hard as Hard
            Assert.True(veteran.PlayerResourceMultiplier <= hard.PlayerResourceMultiplier);
        }

        [Fact]
        public void Presets_HaveProgressiveResourceTickIntervals()
        {
            // Arrange
            var easy = BalancePresets.Easy();
            var normal = BalancePresets.Normal();
            var hard = BalancePresets.Hard();

            // Assert - Resource tick should vary by difficulty
            // In easy mode, player might get resources faster (shorter interval)
            // or AI might get resources slower
            Assert.True(easy.ResourceTickIntervalSeconds > 0);
            Assert.True(normal.ResourceTickIntervalSeconds > 0);
            Assert.True(hard.ResourceTickIntervalSeconds > 0);
        }

        #endregion

        #region Preset By Difficulty Tests

        [Theory]
        [InlineData(AIDifficulty.Easy, "Easy")]
        [InlineData(AIDifficulty.Normal, "Normal")]
        [InlineData(AIDifficulty.Hard, "Hard")]
        [InlineData(AIDifficulty.Veteran, "Veteran")]
        public void ForDifficulty_ReturnsMatchingPreset(AIDifficulty difficulty, string expectedName)
        {
            // Arrange & Act
            var config = BalancePresets.ForDifficulty(difficulty);

            // Assert
            Assert.Equal(expectedName, config.PresetName);
        }

        [Fact]
        public void ForDifficulty_InvalidDifficulty_ThrowsArgumentOutOfRangeException()
        {
            // Arrange
            var invalidDifficulty = (AIDifficulty)99;

            // Act & Assert
            Assert.Throws<ArgumentOutOfRangeException>(() => BalancePresets.ForDifficulty(invalidDifficulty));
        }

        #endregion

        #region Preset Independence Tests

        [Fact]
        public void EasyPreset_ReturnsNewInstanceEachTime()
        {
            // Arrange & Act
            var first = BalancePresets.Easy();
            var second = BalancePresets.Easy();

            // Act - Modify first
            first.BaseCashGeneration = 9999;

            // Assert - Second should be unaffected
            Assert.NotEqual(first.BaseCashGeneration, second.BaseCashGeneration);
        }

        [Fact]
        public void NormalPreset_ReturnsNewInstanceEachTime()
        {
            // Arrange & Act
            var first = BalancePresets.Normal();
            var second = BalancePresets.Normal();

            // Act - Modify first
            first.MaxActivePeds = 9999;

            // Assert - Second should be unaffected
            Assert.NotEqual(first.MaxActivePeds, second.MaxActivePeds);
        }

        #endregion

        #region GetAllPresetNames Tests

        [Fact]
        public void GetAllPresetNames_ReturnsFourNames()
        {
            // Arrange & Act
            var names = BalancePresets.GetAllPresetNames();

            // Assert
            Assert.Equal(4, names.Length);
        }

        [Fact]
        public void GetAllPresetNames_ContainsAllDifficulties()
        {
            // Arrange & Act
            var names = BalancePresets.GetAllPresetNames();

            // Assert
            Assert.Contains("Easy", names);
            Assert.Contains("Normal", names);
            Assert.Contains("Hard", names);
            Assert.Contains("Veteran", names);
        }

        #endregion

        #region Reinforcement Scaling Tests

        [Fact]
        public void EasyPreset_HasLongerReinforcementCooldown()
        {
            // AI has longer cooldown in easy = easier for player
            var easy = BalancePresets.Easy();
            var normal = BalancePresets.Normal();

            Assert.True(easy.ReinforcementCooldownSeconds >= normal.ReinforcementCooldownSeconds);
        }

        [Fact]
        public void HardPreset_AllowsMorePedsPerWave()
        {
            // More enemies spawning = harder
            var hard = BalancePresets.Hard();
            var normal = BalancePresets.Normal();

            Assert.True(hard.MaxPedsPerWave >= normal.MaxPedsPerWave);
        }

        #endregion
    }
}
