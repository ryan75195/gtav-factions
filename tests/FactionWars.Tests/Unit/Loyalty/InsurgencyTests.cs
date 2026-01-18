using FactionWars.Loyalty.Interfaces;
using FactionWars.Loyalty.Models;
using FactionWars.Loyalty.Services;
using Moq;
using System;
using System.Collections.Generic;
using Xunit;

namespace FactionWars.Tests.Unit.Loyalty
{
    public class InsurgencyTests
    {
        #region InsurgencyRisk Model Tests

        [Fact]
        public void InsurgencyRisk_ShouldRequireZoneId()
        {
            // Arrange & Act
            var risk = new InsurgencyRisk("zone_downtown", "faction_michael");

            // Assert
            Assert.Equal("zone_downtown", risk.ZoneId);
        }

        [Fact]
        public void InsurgencyRisk_ShouldRequireControllingFactionId()
        {
            // Arrange & Act
            var risk = new InsurgencyRisk("zone_downtown", "faction_michael");

            // Assert
            Assert.Equal("faction_michael", risk.ControllingFactionId);
        }

        [Fact]
        public void InsurgencyRisk_ShouldThrowOnNullZoneId()
        {
            // Arrange, Act & Assert
            Assert.Throws<ArgumentNullException>(() => new InsurgencyRisk(null!, "faction_michael"));
        }

        [Fact]
        public void InsurgencyRisk_ShouldThrowOnEmptyZoneId()
        {
            // Arrange, Act & Assert
            Assert.Throws<ArgumentException>(() => new InsurgencyRisk("", "faction_michael"));
        }

        [Fact]
        public void InsurgencyRisk_ShouldThrowOnNullFactionId()
        {
            // Arrange, Act & Assert
            Assert.Throws<ArgumentNullException>(() => new InsurgencyRisk("zone_downtown", null!));
        }

        [Fact]
        public void InsurgencyRisk_ShouldHaveZeroRiskLevelByDefault()
        {
            // Arrange & Act
            var risk = new InsurgencyRisk("zone_downtown", "faction_michael");

            // Assert
            Assert.Equal(0, risk.RiskLevel);
        }

        [Fact]
        public void InsurgencyRisk_ShouldAllowSettingInitialRiskLevel()
        {
            // Arrange & Act
            var risk = new InsurgencyRisk("zone_downtown", "faction_michael", initialRiskLevel: 50);

            // Assert
            Assert.Equal(50, risk.RiskLevel);
        }

        [Fact]
        public void InsurgencyRisk_ShouldClampRiskLevelToMinimum()
        {
            // Arrange & Act
            var risk = new InsurgencyRisk("zone_downtown", "faction_michael", initialRiskLevel: -10);

            // Assert
            Assert.Equal(0, risk.RiskLevel);
        }

        [Fact]
        public void InsurgencyRisk_ShouldClampRiskLevelToMaximum()
        {
            // Arrange & Act
            var risk = new InsurgencyRisk("zone_downtown", "faction_michael", initialRiskLevel: 150);

            // Assert
            Assert.Equal(100, risk.RiskLevel);
        }

        #endregion

        #region InsurgencyRisk Level Thresholds

        [Fact]
        public void InsurgencyRisk_ShouldBeNoneWhenBelowLowThreshold()
        {
            // Arrange
            var risk = new InsurgencyRisk("zone_downtown", "faction_michael", initialRiskLevel: 10);

            // Assert - Risk below 25 is None
            Assert.Equal(InsurgencyLevel.None, risk.Level);
        }

        [Fact]
        public void InsurgencyRisk_ShouldBeLowWhenInLowRange()
        {
            // Arrange
            var risk = new InsurgencyRisk("zone_downtown", "faction_michael", initialRiskLevel: 35);

            // Assert - Risk 25-49 is Low
            Assert.Equal(InsurgencyLevel.Low, risk.Level);
        }

        [Fact]
        public void InsurgencyRisk_ShouldBeMediumWhenInMediumRange()
        {
            // Arrange
            var risk = new InsurgencyRisk("zone_downtown", "faction_michael", initialRiskLevel: 60);

            // Assert - Risk 50-74 is Medium
            Assert.Equal(InsurgencyLevel.Medium, risk.Level);
        }

        [Fact]
        public void InsurgencyRisk_ShouldBeHighWhenInHighRange()
        {
            // Arrange
            var risk = new InsurgencyRisk("zone_downtown", "faction_michael", initialRiskLevel: 80);

            // Assert - Risk 75-89 is High
            Assert.Equal(InsurgencyLevel.High, risk.Level);
        }

        [Fact]
        public void InsurgencyRisk_ShouldBeCriticalWhenAboveCriticalThreshold()
        {
            // Arrange
            var risk = new InsurgencyRisk("zone_downtown", "faction_michael", initialRiskLevel: 95);

            // Assert - Risk 90+ is Critical
            Assert.Equal(InsurgencyLevel.Critical, risk.Level);
        }

        #endregion

        #region InsurgencyRisk Adjustment

        [Fact]
        public void InsurgencyRisk_AdjustRisk_ShouldIncreaseRisk()
        {
            // Arrange
            var risk = new InsurgencyRisk("zone_downtown", "faction_michael", initialRiskLevel: 30);

            // Act
            risk.AdjustRisk(20);

            // Assert
            Assert.Equal(50, risk.RiskLevel);
        }

        [Fact]
        public void InsurgencyRisk_AdjustRisk_ShouldDecreaseRisk()
        {
            // Arrange
            var risk = new InsurgencyRisk("zone_downtown", "faction_michael", initialRiskLevel: 50);

            // Act
            risk.AdjustRisk(-20);

            // Assert
            Assert.Equal(30, risk.RiskLevel);
        }

        [Fact]
        public void InsurgencyRisk_AdjustRisk_ShouldClampToMaximum()
        {
            // Arrange
            var risk = new InsurgencyRisk("zone_downtown", "faction_michael", initialRiskLevel: 90);

            // Act
            risk.AdjustRisk(50);

            // Assert
            Assert.Equal(100, risk.RiskLevel);
        }

        [Fact]
        public void InsurgencyRisk_AdjustRisk_ShouldClampToMinimum()
        {
            // Arrange
            var risk = new InsurgencyRisk("zone_downtown", "faction_michael", initialRiskLevel: 20);

            // Act
            risk.AdjustRisk(-50);

            // Assert
            Assert.Equal(0, risk.RiskLevel);
        }

        [Fact]
        public void InsurgencyRisk_ResetRisk_ShouldSetToZero()
        {
            // Arrange
            var risk = new InsurgencyRisk("zone_downtown", "faction_michael", initialRiskLevel: 80);

            // Act
            risk.ResetRisk();

            // Assert
            Assert.Equal(0, risk.RiskLevel);
        }

        #endregion

        #region InsurgencyRisk Previous Faction Tracking

        [Fact]
        public void InsurgencyRisk_ShouldAllowSettingPreviousFactionId()
        {
            // Arrange & Act
            var risk = new InsurgencyRisk("zone_downtown", "faction_michael", previousFactionId: "faction_trevor");

            // Assert
            Assert.Equal("faction_trevor", risk.PreviousFactionId);
        }

        [Fact]
        public void InsurgencyRisk_PreviousFactionId_ShouldBeNullByDefault()
        {
            // Arrange & Act
            var risk = new InsurgencyRisk("zone_downtown", "faction_michael");

            // Assert
            Assert.Null(risk.PreviousFactionId);
        }

        [Fact]
        public void InsurgencyRisk_InsurgentFaction_ShouldBePreviousFactionWhenSet()
        {
            // Arrange & Act
            var risk = new InsurgencyRisk("zone_downtown", "faction_michael", previousFactionId: "faction_trevor");

            // Assert - Insurgents support the previous controlling faction
            Assert.Equal("faction_trevor", risk.InsurgentFactionId);
        }

        [Fact]
        public void InsurgencyRisk_InsurgentFaction_ShouldBeNullWhenNoPreviousFaction()
        {
            // Arrange & Act
            var risk = new InsurgencyRisk("zone_downtown", "faction_michael");

            // Assert - No previous faction means no organized insurgency
            Assert.Null(risk.InsurgentFactionId);
        }

        #endregion

        #region InsurgencyRisk Uprising Probability

        [Fact]
        public void InsurgencyRisk_UprisingChance_ShouldBeZeroWhenRiskIsNone()
        {
            // Arrange
            var risk = new InsurgencyRisk("zone_downtown", "faction_michael", initialRiskLevel: 10);

            // Assert
            Assert.Equal(0f, risk.UprisingChance);
        }

        [Fact]
        public void InsurgencyRisk_UprisingChance_ShouldBeLowWhenRiskIsLow()
        {
            // Arrange
            var risk = new InsurgencyRisk("zone_downtown", "faction_michael", initialRiskLevel: 40);

            // Assert - 5% chance at low risk
            Assert.Equal(0.05f, risk.UprisingChance, 3);
        }

        [Fact]
        public void InsurgencyRisk_UprisingChance_ShouldBeMediumWhenRiskIsMedium()
        {
            // Arrange
            var risk = new InsurgencyRisk("zone_downtown", "faction_michael", initialRiskLevel: 60);

            // Assert - 15% chance at medium risk
            Assert.Equal(0.15f, risk.UprisingChance, 3);
        }

        [Fact]
        public void InsurgencyRisk_UprisingChance_ShouldBeHighWhenRiskIsHigh()
        {
            // Arrange
            var risk = new InsurgencyRisk("zone_downtown", "faction_michael", initialRiskLevel: 80);

            // Assert - 30% chance at high risk
            Assert.Equal(0.30f, risk.UprisingChance, 3);
        }

        [Fact]
        public void InsurgencyRisk_UprisingChance_ShouldBeMaxWhenRiskIsCritical()
        {
            // Arrange
            var risk = new InsurgencyRisk("zone_downtown", "faction_michael", initialRiskLevel: 95);

            // Assert - 50% chance at critical risk
            Assert.Equal(0.50f, risk.UprisingChance, 3);
        }

        #endregion

        #region InsurgencyRisk Days Since Last Check

        [Fact]
        public void InsurgencyRisk_DaysSinceLastCheck_ShouldBeZeroByDefault()
        {
            // Arrange & Act
            var risk = new InsurgencyRisk("zone_downtown", "faction_michael");

            // Assert
            Assert.Equal(0, risk.DaysSinceLastCheck);
        }

        [Fact]
        public void InsurgencyRisk_AdvanceDay_ShouldIncrementDaysSinceLastCheck()
        {
            // Arrange
            var risk = new InsurgencyRisk("zone_downtown", "faction_michael");

            // Act
            risk.AdvanceDay();

            // Assert
            Assert.Equal(1, risk.DaysSinceLastCheck);
        }

        [Fact]
        public void InsurgencyRisk_ResetDayCounter_ShouldSetToZero()
        {
            // Arrange
            var risk = new InsurgencyRisk("zone_downtown", "faction_michael");
            risk.AdvanceDay();
            risk.AdvanceDay();

            // Act
            risk.ResetDayCounter();

            // Assert
            Assert.Equal(0, risk.DaysSinceLastCheck);
        }

        #endregion

        #region IInsurgencyService Interface Tests

        [Fact]
        public void InsurgencyService_CalculateRiskFromLoyalty_ShouldReturnHighRiskForHostileLoyalty()
        {
            // Arrange
            var service = new InsurgencyService();
            var loyalty = new ZoneLoyalty("zone_downtown", "faction_michael", initialLoyalty: 10);

            // Act
            int riskIncrease = service.CalculateRiskFromLoyalty(loyalty);

            // Assert - Hostile loyalty (below 20) causes significant risk increase
            Assert.True(riskIncrease >= 15);
        }

        [Fact]
        public void InsurgencyService_CalculateRiskFromLoyalty_ShouldReturnModerateRiskForResistantLoyalty()
        {
            // Arrange
            var service = new InsurgencyService();
            var loyalty = new ZoneLoyalty("zone_downtown", "faction_michael", initialLoyalty: 30);

            // Act
            int riskIncrease = service.CalculateRiskFromLoyalty(loyalty);

            // Assert - Resistant loyalty (20-39) causes moderate risk increase
            Assert.True(riskIncrease >= 5 && riskIncrease < 15);
        }

        [Fact]
        public void InsurgencyService_CalculateRiskFromLoyalty_ShouldReturnNoRiskForNeutralOrHigherLoyalty()
        {
            // Arrange
            var service = new InsurgencyService();
            var loyalty = new ZoneLoyalty("zone_downtown", "faction_michael", initialLoyalty: 50);

            // Act
            int riskIncrease = service.CalculateRiskFromLoyalty(loyalty);

            // Assert - Neutral or higher loyalty does not increase risk
            Assert.Equal(0, riskIncrease);
        }

        [Fact]
        public void InsurgencyService_CalculateRiskReduction_ShouldReduceRiskForHighLoyalty()
        {
            // Arrange
            var service = new InsurgencyService();
            var loyalty = new ZoneLoyalty("zone_downtown", "faction_michael", initialLoyalty: 80);

            // Act
            int riskReduction = service.CalculateRiskReduction(loyalty);

            // Assert - High loyalty (Supportive/Fanatical) reduces insurgency risk
            Assert.True(riskReduction >= 5);
        }

        [Fact]
        public void InsurgencyService_CalculateRiskReduction_ShouldNotReduceRiskForLowLoyalty()
        {
            // Arrange
            var service = new InsurgencyService();
            var loyalty = new ZoneLoyalty("zone_downtown", "faction_michael", initialLoyalty: 30);

            // Act
            int riskReduction = service.CalculateRiskReduction(loyalty);

            // Assert - Low loyalty does not reduce risk
            Assert.Equal(0, riskReduction);
        }

        #endregion

        #region InsurgencyService Daily Update Tests

        [Fact]
        public void InsurgencyService_UpdateDailyRisk_ShouldIncreaseRiskWhenLoyaltyIsHostile()
        {
            // Arrange
            var service = new InsurgencyService();
            var loyalty = new ZoneLoyalty("zone_downtown", "faction_michael", initialLoyalty: 10);
            var risk = new InsurgencyRisk("zone_downtown", "faction_michael", initialRiskLevel: 30);

            // Act
            service.UpdateDailyRisk(risk, loyalty);

            // Assert
            Assert.True(risk.RiskLevel > 30);
        }

        [Fact]
        public void InsurgencyService_UpdateDailyRisk_ShouldDecreaseRiskWhenLoyaltyIsSupportive()
        {
            // Arrange
            var service = new InsurgencyService();
            var loyalty = new ZoneLoyalty("zone_downtown", "faction_michael", initialLoyalty: 70);
            var risk = new InsurgencyRisk("zone_downtown", "faction_michael", initialRiskLevel: 50);

            // Act
            service.UpdateDailyRisk(risk, loyalty);

            // Assert
            Assert.True(risk.RiskLevel < 50);
        }

        [Fact]
        public void InsurgencyService_UpdateDailyRisk_ShouldNotChangeRiskWhenLoyaltyIsNeutral()
        {
            // Arrange
            var service = new InsurgencyService();
            var loyalty = new ZoneLoyalty("zone_downtown", "faction_michael", initialLoyalty: 50);
            var risk = new InsurgencyRisk("zone_downtown", "faction_michael", initialRiskLevel: 30);

            // Act
            service.UpdateDailyRisk(risk, loyalty);

            // Assert - Neutral loyalty maintains current risk level
            Assert.Equal(30, risk.RiskLevel);
        }

        [Fact]
        public void InsurgencyService_UpdateDailyRisk_ShouldAdvanceDayCounter()
        {
            // Arrange
            var service = new InsurgencyService();
            var loyalty = new ZoneLoyalty("zone_downtown", "faction_michael", initialLoyalty: 50);
            var risk = new InsurgencyRisk("zone_downtown", "faction_michael");

            // Act
            service.UpdateDailyRisk(risk, loyalty);

            // Assert
            Assert.Equal(1, risk.DaysSinceLastCheck);
        }

        #endregion

        #region InsurgencyService Uprising Check Tests

        [Fact]
        public void InsurgencyService_CheckForUprising_ShouldReturnFalseWhenRiskIsNone()
        {
            // Arrange
            var service = new InsurgencyService();
            var risk = new InsurgencyRisk("zone_downtown", "faction_michael", initialRiskLevel: 10);

            // Act
            bool uprisingTriggered = service.CheckForUprising(risk, rollValue: 0.01f);

            // Assert - No uprising possible at None risk level
            Assert.False(uprisingTriggered);
        }

        [Fact]
        public void InsurgencyService_CheckForUprising_ShouldReturnTrueWhenRollBelowChance()
        {
            // Arrange
            var service = new InsurgencyService();
            var risk = new InsurgencyRisk("zone_downtown", "faction_michael", initialRiskLevel: 95);

            // Act - 50% chance at critical, roll of 0.20 should trigger
            bool uprisingTriggered = service.CheckForUprising(risk, rollValue: 0.20f);

            // Assert
            Assert.True(uprisingTriggered);
        }

        [Fact]
        public void InsurgencyService_CheckForUprising_ShouldReturnFalseWhenRollAboveChance()
        {
            // Arrange
            var service = new InsurgencyService();
            var risk = new InsurgencyRisk("zone_downtown", "faction_michael", initialRiskLevel: 40);

            // Act - 5% chance at low risk, roll of 0.80 should not trigger
            bool uprisingTriggered = service.CheckForUprising(risk, rollValue: 0.80f);

            // Assert
            Assert.False(uprisingTriggered);
        }

        [Fact]
        public void InsurgencyService_CheckForUprising_ShouldResetDayCounterWhenChecked()
        {
            // Arrange
            var service = new InsurgencyService();
            var risk = new InsurgencyRisk("zone_downtown", "faction_michael", initialRiskLevel: 60);
            risk.AdvanceDay();
            risk.AdvanceDay();

            // Act
            service.CheckForUprising(risk, rollValue: 0.80f);

            // Assert
            Assert.Equal(0, risk.DaysSinceLastCheck);
        }

        #endregion

        #region InsurgencyService Suppression Tests

        [Fact]
        public void InsurgencyService_ApplySuppressionEffect_ShouldReduceRisk()
        {
            // Arrange
            var service = new InsurgencyService();
            var risk = new InsurgencyRisk("zone_downtown", "faction_michael", initialRiskLevel: 60);

            // Act
            service.ApplySuppressionEffect(risk, suppressionStrength: 20);

            // Assert
            Assert.Equal(40, risk.RiskLevel);
        }

        [Fact]
        public void InsurgencyService_ApplySuppressionEffect_ShouldNotReduceBelowZero()
        {
            // Arrange
            var service = new InsurgencyService();
            var risk = new InsurgencyRisk("zone_downtown", "faction_michael", initialRiskLevel: 10);

            // Act
            service.ApplySuppressionEffect(risk, suppressionStrength: 30);

            // Assert
            Assert.Equal(0, risk.RiskLevel);
        }

        [Fact]
        public void InsurgencyService_ApplySuppressionEffect_ShouldThrowOnNegativeStrength()
        {
            // Arrange
            var service = new InsurgencyService();
            var risk = new InsurgencyRisk("zone_downtown", "faction_michael", initialRiskLevel: 60);

            // Act & Assert
            Assert.Throws<ArgumentOutOfRangeException>(() => service.ApplySuppressionEffect(risk, suppressionStrength: -10));
        }

        #endregion

        #region InsurgencyEvent Model Tests

        [Fact]
        public void InsurgencyEvent_ShouldRequireZoneId()
        {
            // Arrange & Act
            var evt = new InsurgencyEvent(
                "zone_downtown",
                "faction_michael",
                "faction_trevor",
                InsurgencyEventType.Uprising);

            // Assert
            Assert.Equal("zone_downtown", evt.ZoneId);
        }

        [Fact]
        public void InsurgencyEvent_ShouldRequireControllingFactionId()
        {
            // Arrange & Act
            var evt = new InsurgencyEvent(
                "zone_downtown",
                "faction_michael",
                "faction_trevor",
                InsurgencyEventType.Uprising);

            // Assert
            Assert.Equal("faction_michael", evt.ControllingFactionId);
        }

        [Fact]
        public void InsurgencyEvent_ShouldRequireInsurgentFactionId()
        {
            // Arrange & Act
            var evt = new InsurgencyEvent(
                "zone_downtown",
                "faction_michael",
                "faction_trevor",
                InsurgencyEventType.Uprising);

            // Assert
            Assert.Equal("faction_trevor", evt.InsurgentFactionId);
        }

        [Fact]
        public void InsurgencyEvent_ShouldHaveEventType()
        {
            // Arrange & Act
            var evt = new InsurgencyEvent(
                "zone_downtown",
                "faction_michael",
                "faction_trevor",
                InsurgencyEventType.Uprising);

            // Assert
            Assert.Equal(InsurgencyEventType.Uprising, evt.EventType);
        }

        [Fact]
        public void InsurgencyEvent_ShouldHaveTimestamp()
        {
            // Arrange
            var before = DateTime.UtcNow;

            // Act
            var evt = new InsurgencyEvent(
                "zone_downtown",
                "faction_michael",
                "faction_trevor",
                InsurgencyEventType.Uprising);

            var after = DateTime.UtcNow;

            // Assert
            Assert.True(evt.Timestamp >= before && evt.Timestamp <= after);
        }

        [Fact]
        public void InsurgencyEvent_ShouldDefaultToNotResolved()
        {
            // Arrange & Act
            var evt = new InsurgencyEvent(
                "zone_downtown",
                "faction_michael",
                "faction_trevor",
                InsurgencyEventType.Uprising);

            // Assert
            Assert.False(evt.IsResolved);
        }

        [Fact]
        public void InsurgencyEvent_MarkResolved_ShouldSetResolvedFlag()
        {
            // Arrange
            var evt = new InsurgencyEvent(
                "zone_downtown",
                "faction_michael",
                "faction_trevor",
                InsurgencyEventType.Uprising);

            // Act
            evt.MarkResolved(InsurgencyOutcome.Suppressed);

            // Assert
            Assert.True(evt.IsResolved);
        }

        [Fact]
        public void InsurgencyEvent_MarkResolved_ShouldSetOutcome()
        {
            // Arrange
            var evt = new InsurgencyEvent(
                "zone_downtown",
                "faction_michael",
                "faction_trevor",
                InsurgencyEventType.Uprising);

            // Act
            evt.MarkResolved(InsurgencyOutcome.ZoneFlipped);

            // Assert
            Assert.Equal(InsurgencyOutcome.ZoneFlipped, evt.Outcome);
        }

        #endregion

        #region InsurgencyEventType Enum Tests

        [Fact]
        public void InsurgencyEventType_ShouldHaveUprisingValue()
        {
            // Assert
            Assert.True(Enum.IsDefined(typeof(InsurgencyEventType), InsurgencyEventType.Uprising));
        }

        [Fact]
        public void InsurgencyEventType_ShouldHaveSabotageValue()
        {
            // Assert
            Assert.True(Enum.IsDefined(typeof(InsurgencyEventType), InsurgencyEventType.Sabotage));
        }

        [Fact]
        public void InsurgencyEventType_ShouldHaveProtestValue()
        {
            // Assert
            Assert.True(Enum.IsDefined(typeof(InsurgencyEventType), InsurgencyEventType.Protest));
        }

        #endregion

        #region InsurgencyOutcome Enum Tests

        [Fact]
        public void InsurgencyOutcome_ShouldHaveSuppressedValue()
        {
            // Assert
            Assert.True(Enum.IsDefined(typeof(InsurgencyOutcome), InsurgencyOutcome.Suppressed));
        }

        [Fact]
        public void InsurgencyOutcome_ShouldHaveZoneFlippedValue()
        {
            // Assert
            Assert.True(Enum.IsDefined(typeof(InsurgencyOutcome), InsurgencyOutcome.ZoneFlipped));
        }

        [Fact]
        public void InsurgencyOutcome_ShouldHaveNegotiatedValue()
        {
            // Assert
            Assert.True(Enum.IsDefined(typeof(InsurgencyOutcome), InsurgencyOutcome.Negotiated));
        }

        #endregion

        #region InsurgencyLevel Enum Tests

        [Fact]
        public void InsurgencyLevel_ShouldHaveNoneValue()
        {
            // Assert
            Assert.True(Enum.IsDefined(typeof(InsurgencyLevel), InsurgencyLevel.None));
        }

        [Fact]
        public void InsurgencyLevel_ShouldHaveLowValue()
        {
            // Assert
            Assert.True(Enum.IsDefined(typeof(InsurgencyLevel), InsurgencyLevel.Low));
        }

        [Fact]
        public void InsurgencyLevel_ShouldHaveMediumValue()
        {
            // Assert
            Assert.True(Enum.IsDefined(typeof(InsurgencyLevel), InsurgencyLevel.Medium));
        }

        [Fact]
        public void InsurgencyLevel_ShouldHaveHighValue()
        {
            // Assert
            Assert.True(Enum.IsDefined(typeof(InsurgencyLevel), InsurgencyLevel.High));
        }

        [Fact]
        public void InsurgencyLevel_ShouldHaveCriticalValue()
        {
            // Assert
            Assert.True(Enum.IsDefined(typeof(InsurgencyLevel), InsurgencyLevel.Critical));
        }

        #endregion

        #region InsurgencyService CreateEvent Tests

        [Fact]
        public void InsurgencyService_CreateUprisingEvent_ShouldReturnEvent()
        {
            // Arrange
            var service = new InsurgencyService();
            var risk = new InsurgencyRisk("zone_downtown", "faction_michael", previousFactionId: "faction_trevor");

            // Act
            var evt = service.CreateUprisingEvent(risk);

            // Assert
            Assert.NotNull(evt);
            Assert.Equal("zone_downtown", evt.ZoneId);
            Assert.Equal("faction_michael", evt.ControllingFactionId);
            Assert.Equal("faction_trevor", evt.InsurgentFactionId);
            Assert.Equal(InsurgencyEventType.Uprising, evt.EventType);
        }

        [Fact]
        public void InsurgencyService_CreateUprisingEvent_ShouldThrowWhenNoInsurgentFaction()
        {
            // Arrange
            var service = new InsurgencyService();
            var risk = new InsurgencyRisk("zone_downtown", "faction_michael");

            // Act & Assert - Cannot create uprising without an insurgent faction
            Assert.Throws<InvalidOperationException>(() => service.CreateUprisingEvent(risk));
        }

        #endregion

        #region InsurgencyService Effect On Zone Control Tests

        [Fact]
        public void InsurgencyService_CalculateUprisingStrength_ShouldScaleWithRiskLevel()
        {
            // Arrange
            var service = new InsurgencyService();
            var lowRisk = new InsurgencyRisk("zone_a", "faction_michael", initialRiskLevel: 40);
            var highRisk = new InsurgencyRisk("zone_b", "faction_michael", initialRiskLevel: 90);

            // Act
            int lowStrength = service.CalculateUprisingStrength(lowRisk);
            int highStrength = service.CalculateUprisingStrength(highRisk);

            // Assert
            Assert.True(highStrength > lowStrength);
        }

        [Fact]
        public void InsurgencyService_CalculateUprisingStrength_ShouldBePositive()
        {
            // Arrange
            var service = new InsurgencyService();
            var risk = new InsurgencyRisk("zone_downtown", "faction_michael", initialRiskLevel: 50);

            // Act
            int strength = service.CalculateUprisingStrength(risk);

            // Assert
            Assert.True(strength > 0);
        }

        [Fact]
        public void InsurgencyService_CalculateUprisingStrength_ShouldHaveMinimumValue()
        {
            // Arrange
            var service = new InsurgencyService();
            var risk = new InsurgencyRisk("zone_downtown", "faction_michael", initialRiskLevel: 25);

            // Act
            int strength = service.CalculateUprisingStrength(risk);

            // Assert - Even low risk uprisings should have some strength
            Assert.True(strength >= 5);
        }

        #endregion

        #region InsurgencyRisk Equality Tests

        [Fact]
        public void InsurgencyRisk_ShouldBeEqualByZoneId()
        {
            // Arrange
            var risk1 = new InsurgencyRisk("zone_downtown", "faction_michael", initialRiskLevel: 50);
            var risk2 = new InsurgencyRisk("zone_downtown", "faction_trevor", initialRiskLevel: 80);

            // Act & Assert
            Assert.Equal(risk1, risk2);
        }

        [Fact]
        public void InsurgencyRisk_ShouldNotBeEqualWithDifferentZoneId()
        {
            // Arrange
            var risk1 = new InsurgencyRisk("zone_downtown", "faction_michael");
            var risk2 = new InsurgencyRisk("zone_vinewood", "faction_michael");

            // Act & Assert
            Assert.NotEqual(risk1, risk2);
        }

        [Fact]
        public void InsurgencyRisk_GetHashCode_ShouldBeConsistentWithEquals()
        {
            // Arrange
            var risk1 = new InsurgencyRisk("zone_downtown", "faction_michael", initialRiskLevel: 50);
            var risk2 = new InsurgencyRisk("zone_downtown", "faction_trevor", initialRiskLevel: 80);

            // Act & Assert
            Assert.Equal(risk1.GetHashCode(), risk2.GetHashCode());
        }

        #endregion
    }
}
