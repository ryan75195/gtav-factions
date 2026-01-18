using FactionWars.AI.Interfaces;
using FactionWars.AI.Models;
using FactionWars.AI.Services;
using FactionWars.AI.Strategies;
using FactionWars.Factions.Models;
using FactionWars.Territory.Models;
using System;
using System.Collections.Generic;
using Xunit;

namespace FactionWars.Tests.Unit.AI
{
    /// <summary>
    /// Tests for configurable AI difficulty system.
    /// The difficulty system scales AI behavior across multiple dimensions:
    /// - Resource generation rate
    /// - Decision-making quality (attack thresholds, target selection)
    /// - Troop allocation
    /// - Reaction time
    /// </summary>
    public class AIDifficultyTests
    {
        #region AIDifficulty Enum Tests

        [Fact]
        public void AIDifficulty_HasEasyLevel()
        {
            var difficulty = AIDifficulty.Easy;
            Assert.Equal(AIDifficulty.Easy, difficulty);
        }

        [Fact]
        public void AIDifficulty_HasNormalLevel()
        {
            var difficulty = AIDifficulty.Normal;
            Assert.Equal(AIDifficulty.Normal, difficulty);
        }

        [Fact]
        public void AIDifficulty_HasHardLevel()
        {
            var difficulty = AIDifficulty.Hard;
            Assert.Equal(AIDifficulty.Hard, difficulty);
        }

        [Fact]
        public void AIDifficulty_HasVeteranLevel()
        {
            var difficulty = AIDifficulty.Veteran;
            Assert.Equal(AIDifficulty.Veteran, difficulty);
        }

        [Theory]
        [InlineData(AIDifficulty.Easy, 0)]
        [InlineData(AIDifficulty.Normal, 1)]
        [InlineData(AIDifficulty.Hard, 2)]
        [InlineData(AIDifficulty.Veteran, 3)]
        public void AIDifficulty_HasCorrectOrder(AIDifficulty difficulty, int expectedValue)
        {
            Assert.Equal(expectedValue, (int)difficulty);
        }

        #endregion

        #region AIDifficultySettings Tests

        [Fact]
        public void AIDifficultySettings_ForEasy_HasReducedResourceMultiplier()
        {
            var settings = AIDifficultySettings.ForDifficulty(AIDifficulty.Easy);

            // Easy mode: AI generates fewer resources
            Assert.True(settings.ResourceGenerationMultiplier < 1.0f);
            Assert.True(settings.ResourceGenerationMultiplier >= 0.5f);
        }

        [Fact]
        public void AIDifficultySettings_ForNormal_HasStandardResourceMultiplier()
        {
            var settings = AIDifficultySettings.ForDifficulty(AIDifficulty.Normal);

            // Normal mode: 1.0x resources
            Assert.Equal(1.0f, settings.ResourceGenerationMultiplier);
        }

        [Fact]
        public void AIDifficultySettings_ForHard_HasIncreasedResourceMultiplier()
        {
            var settings = AIDifficultySettings.ForDifficulty(AIDifficulty.Hard);

            // Hard mode: AI generates more resources
            Assert.True(settings.ResourceGenerationMultiplier > 1.0f);
            Assert.True(settings.ResourceGenerationMultiplier <= 1.5f);
        }

        [Fact]
        public void AIDifficultySettings_ForVeteran_HasMaxResourceMultiplier()
        {
            var settings = AIDifficultySettings.ForDifficulty(AIDifficulty.Veteran);

            // Veteran mode: Maximum resource generation
            Assert.True(settings.ResourceGenerationMultiplier > 1.25f);
        }

        [Fact]
        public void AIDifficultySettings_Easy_HasLowerAttackThreshold()
        {
            var settings = AIDifficultySettings.ForDifficulty(AIDifficulty.Easy);

            // Easy mode: AI is less aggressive (higher threshold to attack)
            Assert.True(settings.AttackThresholdMultiplier > 1.0f);
        }

        [Fact]
        public void AIDifficultySettings_Hard_HasHigherAttackThreshold()
        {
            var settings = AIDifficultySettings.ForDifficulty(AIDifficulty.Hard);

            // Hard mode: AI attacks more readily (lower threshold)
            Assert.True(settings.AttackThresholdMultiplier < 1.0f);
        }

        [Fact]
        public void AIDifficultySettings_Easy_HasReducedTroopAllocation()
        {
            var settings = AIDifficultySettings.ForDifficulty(AIDifficulty.Easy);

            // Easy mode: AI allocates fewer troops to actions
            Assert.True(settings.TroopAllocationMultiplier < 1.0f);
        }

        [Fact]
        public void AIDifficultySettings_Veteran_HasIncreasedTroopAllocation()
        {
            var settings = AIDifficultySettings.ForDifficulty(AIDifficulty.Veteran);

            // Veteran mode: AI allocates more troops
            Assert.True(settings.TroopAllocationMultiplier > 1.0f);
        }

        [Fact]
        public void AIDifficultySettings_Easy_HasSlowerReactionTime()
        {
            var settings = AIDifficultySettings.ForDifficulty(AIDifficulty.Easy);

            // Easy mode: AI reacts slower (higher reaction delay multiplier)
            Assert.True(settings.ReactionDelayMultiplier > 1.0f);
        }

        [Fact]
        public void AIDifficultySettings_Veteran_HasFasterReactionTime()
        {
            var settings = AIDifficultySettings.ForDifficulty(AIDifficulty.Veteran);

            // Veteran mode: AI reacts faster (lower delay)
            Assert.True(settings.ReactionDelayMultiplier < 1.0f);
        }

        [Fact]
        public void AIDifficultySettings_Easy_HasReducedAggressivenessBonus()
        {
            var settings = AIDifficultySettings.ForDifficulty(AIDifficulty.Easy);

            // Easy mode: AI is less aggressive overall
            Assert.True(settings.AggressivenessBonus <= 0f);
        }

        [Fact]
        public void AIDifficultySettings_Veteran_HasIncreasedAggressivenessBonus()
        {
            var settings = AIDifficultySettings.ForDifficulty(AIDifficulty.Veteran);

            // Veteran mode: AI is more aggressive
            Assert.True(settings.AggressivenessBonus > 0f);
        }

        [Fact]
        public void AIDifficultySettings_AllDifficulties_HaveValidMultipliers()
        {
            foreach (AIDifficulty difficulty in Enum.GetValues(typeof(AIDifficulty)))
            {
                var settings = AIDifficultySettings.ForDifficulty(difficulty);

                // All multipliers should be positive
                Assert.True(settings.ResourceGenerationMultiplier > 0f);
                Assert.True(settings.AttackThresholdMultiplier > 0f);
                Assert.True(settings.TroopAllocationMultiplier > 0f);
                Assert.True(settings.ReactionDelayMultiplier > 0f);

                // Aggressiveness bonus can be negative or positive but bounded
                Assert.InRange(settings.AggressivenessBonus, -0.5f, 0.5f);
            }
        }

        [Fact]
        public void AIDifficultySettings_Difficulty_PropertyReturnsCorrectValue()
        {
            foreach (AIDifficulty difficulty in Enum.GetValues(typeof(AIDifficulty)))
            {
                var settings = AIDifficultySettings.ForDifficulty(difficulty);
                Assert.Equal(difficulty, settings.Difficulty);
            }
        }

        [Fact]
        public void AIDifficultySettings_CustomConstructor_AllowsCustomValues()
        {
            var custom = new AIDifficultySettings(
                difficulty: AIDifficulty.Hard,
                resourceGenerationMultiplier: 1.5f,
                attackThresholdMultiplier: 0.7f,
                troopAllocationMultiplier: 1.3f,
                reactionDelayMultiplier: 0.6f,
                aggressivenessBonus: 0.2f);

            Assert.Equal(AIDifficulty.Hard, custom.Difficulty);
            Assert.Equal(1.5f, custom.ResourceGenerationMultiplier);
            Assert.Equal(0.7f, custom.AttackThresholdMultiplier);
            Assert.Equal(1.3f, custom.TroopAllocationMultiplier);
            Assert.Equal(0.6f, custom.ReactionDelayMultiplier);
            Assert.Equal(0.2f, custom.AggressivenessBonus);
        }

        [Theory]
        [InlineData(0f)]
        [InlineData(-0.1f)]
        public void AIDifficultySettings_InvalidResourceMultiplier_ThrowsException(float invalidMultiplier)
        {
            Assert.Throws<ArgumentOutOfRangeException>(() => new AIDifficultySettings(
                difficulty: AIDifficulty.Normal,
                resourceGenerationMultiplier: invalidMultiplier,
                attackThresholdMultiplier: 1f,
                troopAllocationMultiplier: 1f,
                reactionDelayMultiplier: 1f,
                aggressivenessBonus: 0f));
        }

        [Theory]
        [InlineData(0f)]
        [InlineData(-0.1f)]
        public void AIDifficultySettings_InvalidAttackThreshold_ThrowsException(float invalidMultiplier)
        {
            Assert.Throws<ArgumentOutOfRangeException>(() => new AIDifficultySettings(
                difficulty: AIDifficulty.Normal,
                resourceGenerationMultiplier: 1f,
                attackThresholdMultiplier: invalidMultiplier,
                troopAllocationMultiplier: 1f,
                reactionDelayMultiplier: 1f,
                aggressivenessBonus: 0f));
        }

        #endregion

        #region IAIDifficultyService Interface Tests

        [Fact]
        public void AIDifficultyService_ImplementsInterface()
        {
            var service = new AIDifficultyService();
            Assert.IsAssignableFrom<IAIDifficultyService>(service);
        }

        [Fact]
        public void AIDifficultyService_DefaultDifficulty_IsNormal()
        {
            var service = new AIDifficultyService();

            Assert.Equal(AIDifficulty.Normal, service.CurrentDifficulty);
        }

        [Fact]
        public void AIDifficultyService_SetDifficulty_ChangesCurrentDifficulty()
        {
            var service = new AIDifficultyService();

            service.SetDifficulty(AIDifficulty.Hard);

            Assert.Equal(AIDifficulty.Hard, service.CurrentDifficulty);
        }

        [Fact]
        public void AIDifficultyService_GetSettings_ReturnsSettingsForCurrentDifficulty()
        {
            var service = new AIDifficultyService();
            service.SetDifficulty(AIDifficulty.Veteran);

            var settings = service.GetSettings();

            Assert.Equal(AIDifficulty.Veteran, settings.Difficulty);
        }

        [Fact]
        public void AIDifficultyService_CanBeInitializedWithDifficulty()
        {
            var service = new AIDifficultyService(AIDifficulty.Easy);

            Assert.Equal(AIDifficulty.Easy, service.CurrentDifficulty);
        }

        #endregion

        #region Difficulty Scaling Resource Generation Tests

        [Fact]
        public void AIDifficultyService_ScaleResourceGeneration_AppliesMultiplier()
        {
            var service = new AIDifficultyService(AIDifficulty.Hard);
            var settings = service.GetSettings();

            int baseResources = 100;
            int scaled = service.ScaleResourceGeneration(baseResources);

            Assert.Equal((int)(baseResources * settings.ResourceGenerationMultiplier), scaled);
        }

        [Fact]
        public void AIDifficultyService_ScaleResourceGeneration_EasyGeneratesLess()
        {
            var service = new AIDifficultyService(AIDifficulty.Easy);
            int baseResources = 100;

            int scaled = service.ScaleResourceGeneration(baseResources);

            Assert.True(scaled < baseResources);
        }

        [Fact]
        public void AIDifficultyService_ScaleResourceGeneration_VeteranGeneratesMore()
        {
            var service = new AIDifficultyService(AIDifficulty.Veteran);
            int baseResources = 100;

            int scaled = service.ScaleResourceGeneration(baseResources);

            Assert.True(scaled > baseResources);
        }

        [Fact]
        public void AIDifficultyService_ScaleResourceGeneration_NeverReturnsNegative()
        {
            var service = new AIDifficultyService(AIDifficulty.Easy);

            int scaled = service.ScaleResourceGeneration(0);

            Assert.True(scaled >= 0);
        }

        #endregion

        #region Difficulty Scaling Troop Allocation Tests

        [Fact]
        public void AIDifficultyService_ScaleTroopAllocation_AppliesMultiplier()
        {
            var service = new AIDifficultyService(AIDifficulty.Hard);
            var settings = service.GetSettings();

            int baseTroops = 50;
            int scaled = service.ScaleTroopAllocation(baseTroops);

            Assert.Equal((int)(baseTroops * settings.TroopAllocationMultiplier), scaled);
        }

        [Fact]
        public void AIDifficultyService_ScaleTroopAllocation_EasyAllocatesLess()
        {
            var service = new AIDifficultyService(AIDifficulty.Easy);
            int baseTroops = 50;

            int scaled = service.ScaleTroopAllocation(baseTroops);

            Assert.True(scaled < baseTroops);
        }

        [Fact]
        public void AIDifficultyService_ScaleTroopAllocation_RespectsMinimum()
        {
            var service = new AIDifficultyService(AIDifficulty.Easy);

            // Even with scaling, minimum of 1 should be returned if input is positive
            int scaled = service.ScaleTroopAllocation(3, minimumTroops: 3);

            Assert.True(scaled >= 3);
        }

        #endregion

        #region Difficulty Scaling Aggressiveness Tests

        [Fact]
        public void AIDifficultyService_ScaleAggressiveness_AppliesBonus()
        {
            var service = new AIDifficultyService(AIDifficulty.Veteran);
            var settings = service.GetSettings();

            float baseAggressiveness = 0.5f;
            float scaled = service.ScaleAggressiveness(baseAggressiveness);

            Assert.Equal(
                Math.Min(1f, Math.Max(0f, baseAggressiveness + settings.AggressivenessBonus)),
                scaled);
        }

        [Fact]
        public void AIDifficultyService_ScaleAggressiveness_EasyReducesAggressiveness()
        {
            var service = new AIDifficultyService(AIDifficulty.Easy);
            float baseAggressiveness = 0.5f;

            float scaled = service.ScaleAggressiveness(baseAggressiveness);

            Assert.True(scaled <= baseAggressiveness);
        }

        [Fact]
        public void AIDifficultyService_ScaleAggressiveness_ClampsToValidRange()
        {
            var service = new AIDifficultyService(AIDifficulty.Veteran);

            // Very high base + veteran bonus should still clamp to 1.0
            float scaled = service.ScaleAggressiveness(0.95f);

            Assert.InRange(scaled, 0f, 1f);
        }

        [Fact]
        public void AIDifficultyService_ScaleAggressiveness_ClampsMinimum()
        {
            var service = new AIDifficultyService(AIDifficulty.Easy);

            // Very low base + easy reduction should still clamp to 0
            float scaled = service.ScaleAggressiveness(0.1f);

            Assert.InRange(scaled, 0f, 1f);
        }

        #endregion

        #region Difficulty Scaling Attack Threshold Tests

        [Fact]
        public void AIDifficultyService_ScaleAttackThreshold_AppliesMultiplier()
        {
            var service = new AIDifficultyService(AIDifficulty.Hard);
            var settings = service.GetSettings();

            float baseThreshold = 0.5f;
            float scaled = service.ScaleAttackThreshold(baseThreshold);

            Assert.Equal(baseThreshold * settings.AttackThresholdMultiplier, scaled);
        }

        [Fact]
        public void AIDifficultyService_ScaleAttackThreshold_EasyMakesHarderToAttack()
        {
            var service = new AIDifficultyService(AIDifficulty.Easy);
            float baseThreshold = 0.5f;

            float scaled = service.ScaleAttackThreshold(baseThreshold);

            // Higher threshold means harder to meet = less attacks
            Assert.True(scaled > baseThreshold);
        }

        [Fact]
        public void AIDifficultyService_ScaleAttackThreshold_VeteranMakesEasierToAttack()
        {
            var service = new AIDifficultyService(AIDifficulty.Veteran);
            float baseThreshold = 0.5f;

            float scaled = service.ScaleAttackThreshold(baseThreshold);

            // Lower threshold means easier to meet = more attacks
            Assert.True(scaled < baseThreshold);
        }

        #endregion

        #region Difficulty Scaling Reaction Time Tests

        [Fact]
        public void AIDifficultyService_ScaleReactionDelay_AppliesMultiplier()
        {
            var service = new AIDifficultyService(AIDifficulty.Hard);
            var settings = service.GetSettings();

            int baseDelayMs = 1000;
            int scaled = service.ScaleReactionDelay(baseDelayMs);

            Assert.Equal((int)(baseDelayMs * settings.ReactionDelayMultiplier), scaled);
        }

        [Fact]
        public void AIDifficultyService_ScaleReactionDelay_EasyIsSlow()
        {
            var service = new AIDifficultyService(AIDifficulty.Easy);
            int baseDelayMs = 1000;

            int scaled = service.ScaleReactionDelay(baseDelayMs);

            Assert.True(scaled > baseDelayMs);
        }

        [Fact]
        public void AIDifficultyService_ScaleReactionDelay_VeteranIsFast()
        {
            var service = new AIDifficultyService(AIDifficulty.Veteran);
            int baseDelayMs = 1000;

            int scaled = service.ScaleReactionDelay(baseDelayMs);

            Assert.True(scaled < baseDelayMs);
        }

        [Fact]
        public void AIDifficultyService_ScaleReactionDelay_HasMinimum()
        {
            var service = new AIDifficultyService(AIDifficulty.Veteran);

            // Even veteran difficulty should have some minimum delay
            int scaled = service.ScaleReactionDelay(100);

            Assert.True(scaled >= 50);
        }

        #endregion

        #region Difficulty Description Tests

        [Theory]
        [InlineData(AIDifficulty.Easy, "Easy")]
        [InlineData(AIDifficulty.Normal, "Normal")]
        [InlineData(AIDifficulty.Hard, "Hard")]
        [InlineData(AIDifficulty.Veteran, "Veteran")]
        public void AIDifficultySettings_GetDisplayName_ReturnsCorrectName(AIDifficulty difficulty, string expectedName)
        {
            var settings = AIDifficultySettings.ForDifficulty(difficulty);

            Assert.Equal(expectedName, settings.DisplayName);
        }

        [Fact]
        public void AIDifficultySettings_HasDescription_ForEachLevel()
        {
            foreach (AIDifficulty difficulty in Enum.GetValues(typeof(AIDifficulty)))
            {
                var settings = AIDifficultySettings.ForDifficulty(difficulty);

                Assert.False(string.IsNullOrWhiteSpace(settings.Description));
            }
        }

        #endregion

        #region Comparative Scaling Tests

        [Fact]
        public void Difficulty_ProgressivelyIncreases_ResourceGeneration()
        {
            var easy = AIDifficultySettings.ForDifficulty(AIDifficulty.Easy);
            var normal = AIDifficultySettings.ForDifficulty(AIDifficulty.Normal);
            var hard = AIDifficultySettings.ForDifficulty(AIDifficulty.Hard);
            var veteran = AIDifficultySettings.ForDifficulty(AIDifficulty.Veteran);

            Assert.True(easy.ResourceGenerationMultiplier < normal.ResourceGenerationMultiplier);
            Assert.True(normal.ResourceGenerationMultiplier < hard.ResourceGenerationMultiplier);
            Assert.True(hard.ResourceGenerationMultiplier < veteran.ResourceGenerationMultiplier);
        }

        [Fact]
        public void Difficulty_ProgressivelyDecreases_AttackThreshold()
        {
            var easy = AIDifficultySettings.ForDifficulty(AIDifficulty.Easy);
            var normal = AIDifficultySettings.ForDifficulty(AIDifficulty.Normal);
            var hard = AIDifficultySettings.ForDifficulty(AIDifficulty.Hard);
            var veteran = AIDifficultySettings.ForDifficulty(AIDifficulty.Veteran);

            Assert.True(easy.AttackThresholdMultiplier > normal.AttackThresholdMultiplier);
            Assert.True(normal.AttackThresholdMultiplier > hard.AttackThresholdMultiplier);
            Assert.True(hard.AttackThresholdMultiplier > veteran.AttackThresholdMultiplier);
        }

        [Fact]
        public void Difficulty_ProgressivelyDecreases_ReactionDelay()
        {
            var easy = AIDifficultySettings.ForDifficulty(AIDifficulty.Easy);
            var normal = AIDifficultySettings.ForDifficulty(AIDifficulty.Normal);
            var hard = AIDifficultySettings.ForDifficulty(AIDifficulty.Hard);
            var veteran = AIDifficultySettings.ForDifficulty(AIDifficulty.Veteran);

            Assert.True(easy.ReactionDelayMultiplier > normal.ReactionDelayMultiplier);
            Assert.True(normal.ReactionDelayMultiplier > hard.ReactionDelayMultiplier);
            Assert.True(hard.ReactionDelayMultiplier > veteran.ReactionDelayMultiplier);
        }

        [Fact]
        public void Difficulty_ProgressivelyIncreases_Aggressiveness()
        {
            var easy = AIDifficultySettings.ForDifficulty(AIDifficulty.Easy);
            var normal = AIDifficultySettings.ForDifficulty(AIDifficulty.Normal);
            var hard = AIDifficultySettings.ForDifficulty(AIDifficulty.Hard);
            var veteran = AIDifficultySettings.ForDifficulty(AIDifficulty.Veteran);

            Assert.True(easy.AggressivenessBonus < normal.AggressivenessBonus);
            Assert.True(normal.AggressivenessBonus < hard.AggressivenessBonus);
            Assert.True(hard.AggressivenessBonus < veteran.AggressivenessBonus);
        }

        #endregion
    }
}
