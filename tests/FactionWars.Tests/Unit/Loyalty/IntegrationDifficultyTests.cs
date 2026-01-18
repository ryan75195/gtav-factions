using FactionWars.Loyalty.Interfaces;
using FactionWars.Loyalty.Models;
using FactionWars.Loyalty.Services;
using Moq;
using System;
using System.Collections.Generic;
using Xunit;

namespace FactionWars.Tests.Unit.Loyalty
{
    /// <summary>
    /// Tests for the integration difficulty system that determines how hard it is
    /// to integrate a captured zone into the controlling faction.
    /// </summary>
    public class IntegrationDifficultyTests
    {
        #region IntegrationDifficulty Enum Tests

        [Fact]
        public void IntegrationDifficulty_ShouldHaveEasyValue()
        {
            // Assert
            Assert.True(Enum.IsDefined(typeof(IntegrationDifficulty), IntegrationDifficulty.Easy));
        }

        [Fact]
        public void IntegrationDifficulty_ShouldHaveModerateValue()
        {
            // Assert
            Assert.True(Enum.IsDefined(typeof(IntegrationDifficulty), IntegrationDifficulty.Moderate));
        }

        [Fact]
        public void IntegrationDifficulty_ShouldHaveChallengingValue()
        {
            // Assert
            Assert.True(Enum.IsDefined(typeof(IntegrationDifficulty), IntegrationDifficulty.Challenging));
        }

        [Fact]
        public void IntegrationDifficulty_ShouldHaveSevereValue()
        {
            // Assert
            Assert.True(Enum.IsDefined(typeof(IntegrationDifficulty), IntegrationDifficulty.Severe));
        }

        [Fact]
        public void IntegrationDifficulty_ShouldHaveExtremeValue()
        {
            // Assert
            Assert.True(Enum.IsDefined(typeof(IntegrationDifficulty), IntegrationDifficulty.Extreme));
        }

        #endregion

        #region ZoneIntegrationState Model Tests

        [Fact]
        public void ZoneIntegrationState_ShouldRequireZoneId()
        {
            // Arrange & Act
            var state = new ZoneIntegrationState("zone_downtown", "faction_michael", "faction_trevor");

            // Assert
            Assert.Equal("zone_downtown", state.ZoneId);
        }

        [Fact]
        public void ZoneIntegrationState_ShouldRequireNewControllerFactionId()
        {
            // Arrange & Act
            var state = new ZoneIntegrationState("zone_downtown", "faction_michael", "faction_trevor");

            // Assert
            Assert.Equal("faction_michael", state.NewControllerFactionId);
        }

        [Fact]
        public void ZoneIntegrationState_ShouldRequirePreviousControllerFactionId()
        {
            // Arrange & Act
            var state = new ZoneIntegrationState("zone_downtown", "faction_michael", "faction_trevor");

            // Assert
            Assert.Equal("faction_trevor", state.PreviousControllerFactionId);
        }

        [Fact]
        public void ZoneIntegrationState_ShouldThrowOnNullZoneId()
        {
            // Arrange, Act & Assert
            Assert.Throws<ArgumentNullException>(() =>
                new ZoneIntegrationState(null!, "faction_michael", "faction_trevor"));
        }

        [Fact]
        public void ZoneIntegrationState_ShouldThrowOnEmptyZoneId()
        {
            // Arrange, Act & Assert
            Assert.Throws<ArgumentException>(() =>
                new ZoneIntegrationState("", "faction_michael", "faction_trevor"));
        }

        [Fact]
        public void ZoneIntegrationState_ShouldThrowOnNullNewControllerFactionId()
        {
            // Arrange, Act & Assert
            Assert.Throws<ArgumentNullException>(() =>
                new ZoneIntegrationState("zone_downtown", null!, "faction_trevor"));
        }

        [Fact]
        public void ZoneIntegrationState_ShouldThrowOnNullPreviousControllerFactionId()
        {
            // Arrange, Act & Assert
            Assert.Throws<ArgumentNullException>(() =>
                new ZoneIntegrationState("zone_downtown", "faction_michael", null!));
        }

        [Fact]
        public void ZoneIntegrationState_ShouldHaveDefaultIntegrationProgressOfZero()
        {
            // Arrange & Act
            var state = new ZoneIntegrationState("zone_downtown", "faction_michael", "faction_trevor");

            // Assert
            Assert.Equal(0, state.IntegrationProgress);
        }

        [Fact]
        public void ZoneIntegrationState_ShouldAllowSettingInitialIntegrationProgress()
        {
            // Arrange & Act
            var state = new ZoneIntegrationState("zone_downtown", "faction_michael", "faction_trevor", initialProgress: 25);

            // Assert
            Assert.Equal(25, state.IntegrationProgress);
        }

        [Fact]
        public void ZoneIntegrationState_ShouldClampProgressToMinimum()
        {
            // Arrange & Act
            var state = new ZoneIntegrationState("zone_downtown", "faction_michael", "faction_trevor", initialProgress: -10);

            // Assert
            Assert.Equal(0, state.IntegrationProgress);
        }

        [Fact]
        public void ZoneIntegrationState_ShouldClampProgressToMaximum()
        {
            // Arrange & Act
            var state = new ZoneIntegrationState("zone_downtown", "faction_michael", "faction_trevor", initialProgress: 150);

            // Assert
            Assert.Equal(100, state.IntegrationProgress);
        }

        [Fact]
        public void ZoneIntegrationState_ShouldTrackDaysSinceCapture()
        {
            // Arrange & Act
            var state = new ZoneIntegrationState("zone_downtown", "faction_michael", "faction_trevor");

            // Assert
            Assert.Equal(0, state.DaysSinceCapture);
        }

        [Fact]
        public void ZoneIntegrationState_AdvanceDay_ShouldIncrementDaysSinceCapture()
        {
            // Arrange
            var state = new ZoneIntegrationState("zone_downtown", "faction_michael", "faction_trevor");

            // Act
            state.AdvanceDay();

            // Assert
            Assert.Equal(1, state.DaysSinceCapture);
        }

        [Fact]
        public void ZoneIntegrationState_AddProgress_ShouldIncreaseProgress()
        {
            // Arrange
            var state = new ZoneIntegrationState("zone_downtown", "faction_michael", "faction_trevor", initialProgress: 20);

            // Act
            state.AddProgress(15);

            // Assert
            Assert.Equal(35, state.IntegrationProgress);
        }

        [Fact]
        public void ZoneIntegrationState_AddProgress_ShouldClampToMaximum()
        {
            // Arrange
            var state = new ZoneIntegrationState("zone_downtown", "faction_michael", "faction_trevor", initialProgress: 90);

            // Act
            state.AddProgress(20);

            // Assert
            Assert.Equal(100, state.IntegrationProgress);
        }

        [Fact]
        public void ZoneIntegrationState_ReduceProgress_ShouldDecreaseProgress()
        {
            // Arrange
            var state = new ZoneIntegrationState("zone_downtown", "faction_michael", "faction_trevor", initialProgress: 50);

            // Act
            state.ReduceProgress(15);

            // Assert
            Assert.Equal(35, state.IntegrationProgress);
        }

        [Fact]
        public void ZoneIntegrationState_ReduceProgress_ShouldClampToMinimum()
        {
            // Arrange
            var state = new ZoneIntegrationState("zone_downtown", "faction_michael", "faction_trevor", initialProgress: 10);

            // Act
            state.ReduceProgress(25);

            // Assert
            Assert.Equal(0, state.IntegrationProgress);
        }

        [Fact]
        public void ZoneIntegrationState_IsFullyIntegrated_ShouldBeTrueAt100Progress()
        {
            // Arrange
            var state = new ZoneIntegrationState("zone_downtown", "faction_michael", "faction_trevor", initialProgress: 100);

            // Assert
            Assert.True(state.IsFullyIntegrated);
        }

        [Fact]
        public void ZoneIntegrationState_IsFullyIntegrated_ShouldBeFalseBelow100Progress()
        {
            // Arrange
            var state = new ZoneIntegrationState("zone_downtown", "faction_michael", "faction_trevor", initialProgress: 99);

            // Assert
            Assert.False(state.IsFullyIntegrated);
        }

        #endregion

        #region ZoneIntegrationState Difficulty Level Tests

        [Fact]
        public void ZoneIntegrationState_ShouldAllowSettingBaseDifficulty()
        {
            // Arrange & Act
            var state = new ZoneIntegrationState("zone_downtown", "faction_michael", "faction_trevor",
                baseDifficulty: IntegrationDifficulty.Challenging);

            // Assert
            Assert.Equal(IntegrationDifficulty.Challenging, state.BaseDifficulty);
        }

        [Fact]
        public void ZoneIntegrationState_ShouldDefaultToModerateDifficulty()
        {
            // Arrange & Act
            var state = new ZoneIntegrationState("zone_downtown", "faction_michael", "faction_trevor");

            // Assert
            Assert.Equal(IntegrationDifficulty.Moderate, state.BaseDifficulty);
        }

        [Fact]
        public void ZoneIntegrationState_ShouldTrackTransferCount()
        {
            // Arrange & Act
            var state = new ZoneIntegrationState("zone_downtown", "faction_michael", "faction_trevor", transferCount: 3);

            // Assert
            Assert.Equal(3, state.TransferCount);
        }

        [Fact]
        public void ZoneIntegrationState_ShouldDefaultToTransferCountOfOne()
        {
            // Arrange & Act
            var state = new ZoneIntegrationState("zone_downtown", "faction_michael", "faction_trevor");

            // Assert
            Assert.Equal(1, state.TransferCount);
        }

        #endregion

        #region IZoneIntegrationService Interface Tests

        [Fact]
        public void ZoneIntegrationService_CalculateDifficulty_ShouldReturnEasyForHighStartingLoyalty()
        {
            // Arrange
            var service = new ZoneIntegrationService();
            var loyalty = new ZoneLoyalty("zone_downtown", "faction_michael", initialLoyalty: 70);

            // Act
            var difficulty = service.CalculateDifficulty(loyalty, transferCount: 1);

            // Assert - High starting loyalty means easier integration
            Assert.Equal(IntegrationDifficulty.Easy, difficulty);
        }

        [Fact]
        public void ZoneIntegrationService_CalculateDifficulty_ShouldReturnModerateForNeutralLoyalty()
        {
            // Arrange
            var service = new ZoneIntegrationService();
            var loyalty = new ZoneLoyalty("zone_downtown", "faction_michael", initialLoyalty: 50);

            // Act
            var difficulty = service.CalculateDifficulty(loyalty, transferCount: 1);

            // Assert
            Assert.Equal(IntegrationDifficulty.Moderate, difficulty);
        }

        [Fact]
        public void ZoneIntegrationService_CalculateDifficulty_ShouldReturnChallengingForResistantLoyalty()
        {
            // Arrange
            var service = new ZoneIntegrationService();
            var loyalty = new ZoneLoyalty("zone_downtown", "faction_michael", initialLoyalty: 30);

            // Act
            var difficulty = service.CalculateDifficulty(loyalty, transferCount: 1);

            // Assert
            Assert.Equal(IntegrationDifficulty.Challenging, difficulty);
        }

        [Fact]
        public void ZoneIntegrationService_CalculateDifficulty_ShouldReturnSevereForHostileLoyalty()
        {
            // Arrange
            var service = new ZoneIntegrationService();
            var loyalty = new ZoneLoyalty("zone_downtown", "faction_michael", initialLoyalty: 15);

            // Act
            var difficulty = service.CalculateDifficulty(loyalty, transferCount: 1);

            // Assert
            Assert.Equal(IntegrationDifficulty.Severe, difficulty);
        }

        [Fact]
        public void ZoneIntegrationService_CalculateDifficulty_ShouldIncreaseDifficultyForMultipleTransfers()
        {
            // Arrange
            var service = new ZoneIntegrationService();
            var loyalty = new ZoneLoyalty("zone_downtown", "faction_michael", initialLoyalty: 50);

            // Act
            var difficultyFirstTransfer = service.CalculateDifficulty(loyalty, transferCount: 1);
            var difficultyThirdTransfer = service.CalculateDifficulty(loyalty, transferCount: 3);

            // Assert - More transfers means harder integration
            Assert.True(difficultyThirdTransfer > difficultyFirstTransfer);
        }

        [Fact]
        public void ZoneIntegrationService_CalculateDifficulty_ShouldReturnExtremeForHostileLoyaltyWithManyTransfers()
        {
            // Arrange
            var service = new ZoneIntegrationService();
            var loyalty = new ZoneLoyalty("zone_downtown", "faction_michael", initialLoyalty: 10);

            // Act
            var difficulty = service.CalculateDifficulty(loyalty, transferCount: 5);

            // Assert
            Assert.Equal(IntegrationDifficulty.Extreme, difficulty);
        }

        #endregion

        #region ZoneIntegrationService Daily Progress Tests

        [Fact]
        public void ZoneIntegrationService_CalculateDailyProgress_ShouldBeHigherForEasyDifficulty()
        {
            // Arrange
            var service = new ZoneIntegrationService();
            var easyState = new ZoneIntegrationState("zone_a", "faction_michael", "faction_trevor",
                baseDifficulty: IntegrationDifficulty.Easy);
            var severeState = new ZoneIntegrationState("zone_b", "faction_michael", "faction_trevor",
                baseDifficulty: IntegrationDifficulty.Severe);

            // Act
            int easyProgress = service.CalculateDailyProgress(easyState);
            int severeProgress = service.CalculateDailyProgress(severeState);

            // Assert
            Assert.True(easyProgress > severeProgress);
        }

        [Fact]
        public void ZoneIntegrationService_CalculateDailyProgress_ShouldAlwaysBePositive()
        {
            // Arrange
            var service = new ZoneIntegrationService();
            var state = new ZoneIntegrationState("zone_downtown", "faction_michael", "faction_trevor",
                baseDifficulty: IntegrationDifficulty.Extreme);

            // Act
            int progress = service.CalculateDailyProgress(state);

            // Assert - Even extreme difficulty should allow some progress
            Assert.True(progress >= 1);
        }

        [Fact]
        public void ZoneIntegrationService_ApplyDailyProgress_ShouldIncrementIntegrationProgress()
        {
            // Arrange
            var service = new ZoneIntegrationService();
            var state = new ZoneIntegrationState("zone_downtown", "faction_michael", "faction_trevor",
                baseDifficulty: IntegrationDifficulty.Moderate);
            var initialProgress = state.IntegrationProgress;

            // Act
            service.ApplyDailyProgress(state);

            // Assert
            Assert.True(state.IntegrationProgress > initialProgress);
        }

        [Fact]
        public void ZoneIntegrationService_ApplyDailyProgress_ShouldAdvanceDayCounter()
        {
            // Arrange
            var service = new ZoneIntegrationService();
            var state = new ZoneIntegrationState("zone_downtown", "faction_michael", "faction_trevor");

            // Act
            service.ApplyDailyProgress(state);

            // Assert
            Assert.Equal(1, state.DaysSinceCapture);
        }

        #endregion

        #region ZoneIntegrationService Integration Penalty Tests

        [Fact]
        public void ZoneIntegrationService_CalculateResourcePenalty_ShouldBeHigherForLessIntegration()
        {
            // Arrange
            var service = new ZoneIntegrationService();
            var lowProgressState = new ZoneIntegrationState("zone_a", "faction_michael", "faction_trevor", initialProgress: 20);
            var highProgressState = new ZoneIntegrationState("zone_b", "faction_michael", "faction_trevor", initialProgress: 80);

            // Act
            float lowProgressPenalty = service.CalculateResourcePenalty(lowProgressState);
            float highProgressPenalty = service.CalculateResourcePenalty(highProgressState);

            // Assert - Lower integration means higher penalty (lower multiplier)
            Assert.True(lowProgressPenalty < highProgressPenalty);
        }

        [Fact]
        public void ZoneIntegrationService_CalculateResourcePenalty_ShouldReturnFullValueForFullyIntegrated()
        {
            // Arrange
            var service = new ZoneIntegrationService();
            var state = new ZoneIntegrationState("zone_downtown", "faction_michael", "faction_trevor", initialProgress: 100);

            // Act
            float penalty = service.CalculateResourcePenalty(state);

            // Assert - No penalty for fully integrated zones
            Assert.Equal(1.0f, penalty, 3);
        }

        [Fact]
        public void ZoneIntegrationService_CalculateResourcePenalty_ShouldReturnMinimumForZeroProgress()
        {
            // Arrange
            var service = new ZoneIntegrationService();
            var state = new ZoneIntegrationState("zone_downtown", "faction_michael", "faction_trevor", initialProgress: 0);

            // Act
            float penalty = service.CalculateResourcePenalty(state);

            // Assert - Maximum penalty at zero integration (minimum 25% production)
            Assert.Equal(0.25f, penalty, 3);
        }

        [Fact]
        public void ZoneIntegrationService_CalculateResourcePenalty_ShouldScaleLinearly()
        {
            // Arrange
            var service = new ZoneIntegrationService();
            var state50 = new ZoneIntegrationState("zone_a", "faction_michael", "faction_trevor", initialProgress: 50);

            // Act
            float penalty50 = service.CalculateResourcePenalty(state50);

            // Assert - 50% progress should give approximately 62.5% production (0.25 + 0.5 * 0.75)
            Assert.True(penalty50 > 0.5f && penalty50 < 0.75f);
        }

        #endregion

        #region ZoneIntegrationService Setback Tests

        [Fact]
        public void ZoneIntegrationService_ApplyInsurgencySetback_ShouldReduceProgress()
        {
            // Arrange
            var service = new ZoneIntegrationService();
            var state = new ZoneIntegrationState("zone_downtown", "faction_michael", "faction_trevor", initialProgress: 50);

            // Act
            service.ApplyInsurgencySetback(state, InsurgencyLevel.Medium);

            // Assert
            Assert.True(state.IntegrationProgress < 50);
        }

        [Fact]
        public void ZoneIntegrationService_ApplyInsurgencySetback_ShouldScaleWithInsurgencyLevel()
        {
            // Arrange
            var service = new ZoneIntegrationService();
            var lowState = new ZoneIntegrationState("zone_a", "faction_michael", "faction_trevor", initialProgress: 50);
            var criticalState = new ZoneIntegrationState("zone_b", "faction_michael", "faction_trevor", initialProgress: 50);

            // Act
            service.ApplyInsurgencySetback(lowState, InsurgencyLevel.Low);
            service.ApplyInsurgencySetback(criticalState, InsurgencyLevel.Critical);

            // Assert - Higher insurgency level causes more setback
            Assert.True(lowState.IntegrationProgress > criticalState.IntegrationProgress);
        }

        [Fact]
        public void ZoneIntegrationService_ApplyInsurgencySetback_ShouldNotReduceBelowZero()
        {
            // Arrange
            var service = new ZoneIntegrationService();
            var state = new ZoneIntegrationState("zone_downtown", "faction_michael", "faction_trevor", initialProgress: 5);

            // Act
            service.ApplyInsurgencySetback(state, InsurgencyLevel.Critical);

            // Assert
            Assert.Equal(0, state.IntegrationProgress);
        }

        [Fact]
        public void ZoneIntegrationService_ApplyInsurgencySetback_ShouldHaveNoEffectForNoneLevel()
        {
            // Arrange
            var service = new ZoneIntegrationService();
            var state = new ZoneIntegrationState("zone_downtown", "faction_michael", "faction_trevor", initialProgress: 50);

            // Act
            service.ApplyInsurgencySetback(state, InsurgencyLevel.None);

            // Assert
            Assert.Equal(50, state.IntegrationProgress);
        }

        #endregion

        #region ZoneIntegrationService Create Integration State Tests

        [Fact]
        public void ZoneIntegrationService_CreateIntegrationState_ShouldCreateFromZoneLoyalty()
        {
            // Arrange
            var service = new ZoneIntegrationService();
            var loyalty = new ZoneLoyalty("zone_downtown", "faction_michael", initialLoyalty: 30, previousFactionId: "faction_trevor");

            // Act
            var state = service.CreateIntegrationState(loyalty);

            // Assert
            Assert.Equal("zone_downtown", state.ZoneId);
            Assert.Equal("faction_michael", state.NewControllerFactionId);
            Assert.Equal("faction_trevor", state.PreviousControllerFactionId);
        }

        [Fact]
        public void ZoneIntegrationService_CreateIntegrationState_ShouldSetDifficultyBasedOnLoyalty()
        {
            // Arrange
            var service = new ZoneIntegrationService();
            var hostileLoyalty = new ZoneLoyalty("zone_downtown", "faction_michael", initialLoyalty: 10, previousFactionId: "faction_trevor");

            // Act
            var state = service.CreateIntegrationState(hostileLoyalty);

            // Assert - Hostile loyalty should result in Severe or higher difficulty
            Assert.True(state.BaseDifficulty >= IntegrationDifficulty.Severe);
        }

        [Fact]
        public void ZoneIntegrationService_CreateIntegrationState_ShouldThrowWhenNoPreviousFaction()
        {
            // Arrange
            var service = new ZoneIntegrationService();
            var loyalty = new ZoneLoyalty("zone_downtown", "faction_michael", initialLoyalty: 50);

            // Act & Assert - Cannot create integration state without previous faction
            Assert.Throws<InvalidOperationException>(() => service.CreateIntegrationState(loyalty));
        }

        [Fact]
        public void ZoneIntegrationService_CreateIntegrationState_ShouldSetTransferCountFromLoyalty()
        {
            // Arrange
            var service = new ZoneIntegrationService();
            var loyalty = new ZoneLoyalty("zone_downtown", "faction_michael", initialLoyalty: 50, previousFactionId: "faction_trevor");
            loyalty.TransferControl("faction_franklin"); // TransferCount = 1
            loyalty.TransferControl("faction_michael");  // TransferCount = 2

            // Act
            var state = service.CreateIntegrationState(loyalty);

            // Assert
            Assert.Equal(2, state.TransferCount);
        }

        #endregion

        #region ZoneIntegrationService Loyalty Sync Tests

        [Fact]
        public void ZoneIntegrationService_UpdateLoyaltyFromIntegration_ShouldIncreaseLoyaltyWithProgress()
        {
            // Arrange
            var service = new ZoneIntegrationService();
            var loyalty = new ZoneLoyalty("zone_downtown", "faction_michael", initialLoyalty: 30);
            var state = new ZoneIntegrationState("zone_downtown", "faction_michael", "faction_trevor", initialProgress: 75);

            // Act
            service.UpdateLoyaltyFromIntegration(loyalty, state);

            // Assert - Higher integration should boost loyalty
            Assert.True(loyalty.LoyaltyValue > 30);
        }

        [Fact]
        public void ZoneIntegrationService_UpdateLoyaltyFromIntegration_ShouldNotChangeLoyaltyForLowProgress()
        {
            // Arrange
            var service = new ZoneIntegrationService();
            var loyalty = new ZoneLoyalty("zone_downtown", "faction_michael", initialLoyalty: 30);
            var state = new ZoneIntegrationState("zone_downtown", "faction_michael", "faction_trevor", initialProgress: 10);

            // Act
            service.UpdateLoyaltyFromIntegration(loyalty, state);

            // Assert - Low integration progress doesn't affect loyalty
            Assert.Equal(30, loyalty.LoyaltyValue);
        }

        [Fact]
        public void ZoneIntegrationService_UpdateLoyaltyFromIntegration_ShouldMaxOutAtSupportiveLevelForFullIntegration()
        {
            // Arrange
            var service = new ZoneIntegrationService();
            var loyalty = new ZoneLoyalty("zone_downtown", "faction_michael", initialLoyalty: 30);
            var state = new ZoneIntegrationState("zone_downtown", "faction_michael", "faction_trevor", initialProgress: 100);

            // Act
            service.UpdateLoyaltyFromIntegration(loyalty, state);

            // Assert - Full integration guarantees at least Supportive level
            Assert.True(loyalty.Level >= LoyaltyLevel.Supportive);
        }

        #endregion

        #region ZoneIntegrationState Equality Tests

        [Fact]
        public void ZoneIntegrationState_ShouldBeEqualByZoneId()
        {
            // Arrange
            var state1 = new ZoneIntegrationState("zone_downtown", "faction_michael", "faction_trevor", initialProgress: 50);
            var state2 = new ZoneIntegrationState("zone_downtown", "faction_franklin", "faction_michael", initialProgress: 80);

            // Act & Assert
            Assert.Equal(state1, state2);
        }

        [Fact]
        public void ZoneIntegrationState_ShouldNotBeEqualWithDifferentZoneId()
        {
            // Arrange
            var state1 = new ZoneIntegrationState("zone_downtown", "faction_michael", "faction_trevor");
            var state2 = new ZoneIntegrationState("zone_vinewood", "faction_michael", "faction_trevor");

            // Act & Assert
            Assert.NotEqual(state1, state2);
        }

        [Fact]
        public void ZoneIntegrationState_GetHashCode_ShouldBeConsistentWithEquals()
        {
            // Arrange
            var state1 = new ZoneIntegrationState("zone_downtown", "faction_michael", "faction_trevor", initialProgress: 50);
            var state2 = new ZoneIntegrationState("zone_downtown", "faction_franklin", "faction_michael", initialProgress: 80);

            // Act & Assert
            Assert.Equal(state1.GetHashCode(), state2.GetHashCode());
        }

        #endregion

        #region Integration Difficulty Progress Rates Tests

        [Theory]
        [InlineData(IntegrationDifficulty.Easy, 8)]
        [InlineData(IntegrationDifficulty.Moderate, 5)]
        [InlineData(IntegrationDifficulty.Challenging, 3)]
        [InlineData(IntegrationDifficulty.Severe, 2)]
        [InlineData(IntegrationDifficulty.Extreme, 1)]
        public void ZoneIntegrationService_CalculateDailyProgress_ShouldMatchExpectedRates(IntegrationDifficulty difficulty, int expectedMinProgress)
        {
            // Arrange
            var service = new ZoneIntegrationService();
            var state = new ZoneIntegrationState("zone_downtown", "faction_michael", "faction_trevor",
                baseDifficulty: difficulty);

            // Act
            int progress = service.CalculateDailyProgress(state);

            // Assert
            Assert.True(progress >= expectedMinProgress);
        }

        #endregion

        #region Integration Bonus Tests

        [Fact]
        public void ZoneIntegrationService_CalculateDefenseBonus_ShouldBeNegativeForLowIntegration()
        {
            // Arrange
            var service = new ZoneIntegrationService();
            var state = new ZoneIntegrationState("zone_downtown", "faction_michael", "faction_trevor", initialProgress: 20);

            // Act
            int bonus = service.CalculateDefenseBonus(state);

            // Assert - Low integration provides a defense penalty
            Assert.True(bonus < 0);
        }

        [Fact]
        public void ZoneIntegrationService_CalculateDefenseBonus_ShouldBePositiveForHighIntegration()
        {
            // Arrange
            var service = new ZoneIntegrationService();
            var state = new ZoneIntegrationState("zone_downtown", "faction_michael", "faction_trevor", initialProgress: 80);

            // Act
            int bonus = service.CalculateDefenseBonus(state);

            // Assert - High integration provides a defense bonus
            Assert.True(bonus > 0);
        }

        [Fact]
        public void ZoneIntegrationService_CalculateDefenseBonus_ShouldBeZeroAtFiftyPercent()
        {
            // Arrange
            var service = new ZoneIntegrationService();
            var state = new ZoneIntegrationState("zone_downtown", "faction_michael", "faction_trevor", initialProgress: 50);

            // Act
            int bonus = service.CalculateDefenseBonus(state);

            // Assert - Neutral integration has no bonus or penalty
            Assert.Equal(0, bonus);
        }

        #endregion
    }
}
