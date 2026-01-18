using System;
using FactionWars.Combat.Interfaces;
using FactionWars.Combat.Models;
using FactionWars.Combat.Services;
using Xunit;

namespace FactionWars.Tests.Unit.Combat
{
    /// <summary>
    /// Tests for the TakeoverDetector service which determines when
    /// a zone takeover threshold has been reached during combat.
    /// </summary>
    public class TakeoverDetectorTests
    {
        #region TakeoverThresholdConfig Tests

        [Fact]
        public void TakeoverThresholdConfig_DefaultValues_AreCorrect()
        {
            var config = new TakeoverThresholdConfig();

            Assert.Equal(100f, config.AttackerVictoryThreshold);
            Assert.Equal(0f, config.DefenderVictoryThreshold);
            Assert.Equal(5f, config.MinimumHoldTime);
        }

        [Theory]
        [InlineData(75f)]
        [InlineData(80f)]
        [InlineData(95f)]
        [InlineData(100f)]
        public void TakeoverThresholdConfig_AttackerVictoryThreshold_AcceptsValidValues(float threshold)
        {
            var config = new TakeoverThresholdConfig { AttackerVictoryThreshold = threshold };

            Assert.Equal(threshold, config.AttackerVictoryThreshold);
        }

        [Theory]
        [InlineData(-1f)]
        [InlineData(101f)]
        public void TakeoverThresholdConfig_AttackerVictoryThreshold_RejectsInvalidValues(float threshold)
        {
            var config = new TakeoverThresholdConfig();

            Assert.Throws<ArgumentOutOfRangeException>(() => config.AttackerVictoryThreshold = threshold);
        }

        [Theory]
        [InlineData(0f)]
        [InlineData(10f)]
        [InlineData(25f)]
        public void TakeoverThresholdConfig_DefenderVictoryThreshold_AcceptsValidValues(float threshold)
        {
            var config = new TakeoverThresholdConfig { DefenderVictoryThreshold = threshold };

            Assert.Equal(threshold, config.DefenderVictoryThreshold);
        }

        [Theory]
        [InlineData(-1f)]
        [InlineData(101f)]
        public void TakeoverThresholdConfig_DefenderVictoryThreshold_RejectsInvalidValues(float threshold)
        {
            var config = new TakeoverThresholdConfig();

            Assert.Throws<ArgumentOutOfRangeException>(() => config.DefenderVictoryThreshold = threshold);
        }

        [Theory]
        [InlineData(0f)]
        [InlineData(5f)]
        [InlineData(30f)]
        public void TakeoverThresholdConfig_MinimumHoldTime_AcceptsValidValues(float holdTime)
        {
            var config = new TakeoverThresholdConfig { MinimumHoldTime = holdTime };

            Assert.Equal(holdTime, config.MinimumHoldTime);
        }

        [Fact]
        public void TakeoverThresholdConfig_MinimumHoldTime_RejectsNegativeValue()
        {
            var config = new TakeoverThresholdConfig();

            Assert.Throws<ArgumentOutOfRangeException>(() => config.MinimumHoldTime = -1f);
        }

        #endregion

        #region TakeoverResult Tests

        [Fact]
        public void TakeoverResult_InProgress_HasCorrectProperties()
        {
            var result = TakeoverResult.InProgress(50f, 50f);

            Assert.Equal(TakeoverStatus.InProgress, result.Status);
            Assert.False(result.IsTakeoverComplete);
            Assert.Equal(50f, result.AttackerControlPercentage);
            Assert.Equal(50f, result.DefenderControlPercentage);
            Assert.Null(result.WinnerFactionId);
        }

        [Fact]
        public void TakeoverResult_AttackerVictory_HasCorrectProperties()
        {
            var result = TakeoverResult.AttackerVictory("faction_trevor", 100f, 0f);

            Assert.Equal(TakeoverStatus.AttackerVictory, result.Status);
            Assert.True(result.IsTakeoverComplete);
            Assert.Equal(100f, result.AttackerControlPercentage);
            Assert.Equal(0f, result.DefenderControlPercentage);
            Assert.Equal("faction_trevor", result.WinnerFactionId);
        }

        [Fact]
        public void TakeoverResult_DefenderVictory_HasCorrectProperties()
        {
            var result = TakeoverResult.DefenderVictory("faction_michael", 5f, 95f);

            Assert.Equal(TakeoverStatus.DefenderVictory, result.Status);
            Assert.True(result.IsTakeoverComplete);
            Assert.Equal(5f, result.AttackerControlPercentage);
            Assert.Equal(95f, result.DefenderControlPercentage);
            Assert.Equal("faction_michael", result.WinnerFactionId);
        }

        [Fact]
        public void TakeoverResult_AttackerVictory_RequiresFactionId()
        {
            Assert.Throws<ArgumentNullException>(() => TakeoverResult.AttackerVictory(null!, 100f, 0f));
            Assert.Throws<ArgumentException>(() => TakeoverResult.AttackerVictory("", 100f, 0f));
            Assert.Throws<ArgumentException>(() => TakeoverResult.AttackerVictory("  ", 100f, 0f));
        }

        [Fact]
        public void TakeoverResult_DefenderVictory_RequiresFactionId()
        {
            Assert.Throws<ArgumentNullException>(() => TakeoverResult.DefenderVictory(null!, 0f, 100f));
            Assert.Throws<ArgumentException>(() => TakeoverResult.DefenderVictory("", 0f, 100f));
            Assert.Throws<ArgumentException>(() => TakeoverResult.DefenderVictory("  ", 0f, 100f));
        }

        #endregion

        #region TakeoverDetector Construction Tests

        [Fact]
        public void TakeoverDetector_Constructor_DefaultConfig_DoesNotThrow()
        {
            var detector = new TakeoverDetector();

            Assert.NotNull(detector);
        }

        [Fact]
        public void TakeoverDetector_Constructor_WithConfig_DoesNotThrow()
        {
            var config = new TakeoverThresholdConfig { AttackerVictoryThreshold = 80f };
            var detector = new TakeoverDetector(config);

            Assert.NotNull(detector);
        }

        [Fact]
        public void TakeoverDetector_Constructor_NullConfig_ThrowsArgumentNullException()
        {
            Assert.Throws<ArgumentNullException>(() => new TakeoverDetector(null!));
        }

        #endregion

        #region CheckTakeover (raw values) Tests

        [Fact]
        public void CheckTakeover_AttackerAt100Percent_ReturnsAttackerVictory()
        {
            var detector = new TakeoverDetector();

            var result = detector.CheckTakeover(100f, 0f, "attacker_faction", "defender_faction");

            Assert.Equal(TakeoverStatus.AttackerVictory, result.Status);
            Assert.Equal("attacker_faction", result.WinnerFactionId);
        }

        [Fact]
        public void CheckTakeover_AttackerAt0Percent_ReturnsDefenderVictory()
        {
            var detector = new TakeoverDetector();

            var result = detector.CheckTakeover(0f, 100f, "attacker_faction", "defender_faction");

            Assert.Equal(TakeoverStatus.DefenderVictory, result.Status);
            Assert.Equal("defender_faction", result.WinnerFactionId);
        }

        [Theory]
        [InlineData(50f, 50f)]
        [InlineData(25f, 75f)]
        [InlineData(75f, 25f)]
        [InlineData(99f, 1f)]
        [InlineData(1f, 99f)]
        public void CheckTakeover_NeitherThresholdMet_ReturnsInProgress(float attackerPercent, float defenderPercent)
        {
            var detector = new TakeoverDetector();

            var result = detector.CheckTakeover(attackerPercent, defenderPercent, "attacker", "defender");

            Assert.Equal(TakeoverStatus.InProgress, result.Status);
            Assert.False(result.IsTakeoverComplete);
            Assert.Null(result.WinnerFactionId);
        }

        [Fact]
        public void CheckTakeover_CustomAttackerThreshold_TriggersAtCorrectValue()
        {
            var config = new TakeoverThresholdConfig { AttackerVictoryThreshold = 80f };
            var detector = new TakeoverDetector(config);

            // 79% should still be in progress
            var result79 = detector.CheckTakeover(79f, 21f, "attacker", "defender");
            Assert.Equal(TakeoverStatus.InProgress, result79.Status);

            // 80% should trigger victory
            var result80 = detector.CheckTakeover(80f, 20f, "attacker", "defender");
            Assert.Equal(TakeoverStatus.AttackerVictory, result80.Status);
        }

        [Fact]
        public void CheckTakeover_CustomDefenderThreshold_TriggersAtCorrectValue()
        {
            var config = new TakeoverThresholdConfig { DefenderVictoryThreshold = 10f };
            var detector = new TakeoverDetector(config);

            // 11% attacker should still be in progress
            var result11 = detector.CheckTakeover(11f, 89f, "attacker", "defender");
            Assert.Equal(TakeoverStatus.InProgress, result11.Status);

            // 10% attacker should trigger defender victory
            var result10 = detector.CheckTakeover(10f, 90f, "attacker", "defender");
            Assert.Equal(TakeoverStatus.DefenderVictory, result10.Status);
        }

        [Theory]
        [InlineData(-5f, 100f)]
        [InlineData(105f, 0f)]
        [InlineData(50f, -10f)]
        [InlineData(50f, 110f)]
        public void CheckTakeover_InvalidPercentages_ThrowsArgumentOutOfRangeException(float attackerPercent, float defenderPercent)
        {
            var detector = new TakeoverDetector();

            Assert.Throws<ArgumentOutOfRangeException>(() =>
                detector.CheckTakeover(attackerPercent, defenderPercent, "attacker", "defender"));
        }

        [Fact]
        public void CheckTakeover_NullAttackerFactionId_ThrowsArgumentNullException()
        {
            var detector = new TakeoverDetector();

            Assert.Throws<ArgumentNullException>(() =>
                detector.CheckTakeover(50f, 50f, null!, "defender"));
        }

        [Fact]
        public void CheckTakeover_NullDefenderFactionId_ThrowsArgumentNullException()
        {
            var detector = new TakeoverDetector();

            Assert.Throws<ArgumentNullException>(() =>
                detector.CheckTakeover(50f, 50f, "attacker", null!));
        }

        [Fact]
        public void CheckTakeover_EmptyAttackerFactionId_ThrowsArgumentException()
        {
            var detector = new TakeoverDetector();

            Assert.Throws<ArgumentException>(() =>
                detector.CheckTakeover(50f, 50f, "", "defender"));
        }

        [Fact]
        public void CheckTakeover_EmptyDefenderFactionId_ThrowsArgumentException()
        {
            var detector = new TakeoverDetector();

            Assert.Throws<ArgumentException>(() =>
                detector.CheckTakeover(50f, 50f, "attacker", ""));
        }

        #endregion

        #region CheckTakeover (CombatEncounter) Tests

        [Fact]
        public void CheckTakeoverForEncounter_NullEncounter_ThrowsArgumentNullException()
        {
            var detector = new TakeoverDetector();

            Assert.Throws<ArgumentNullException>(() => detector.CheckTakeover(null!));
        }

        [Fact]
        public void CheckTakeoverForEncounter_AttackerAt100Percent_ReturnsAttackerVictory()
        {
            var detector = new TakeoverDetector();
            var encounter = new CombatEncounter("enc1", "zone1", "trevor_faction", "michael_faction");
            encounter.AttackerControlPercentage = 100f;
            encounter.DefenderControlPercentage = 0f;

            var result = detector.CheckTakeover(encounter);

            Assert.Equal(TakeoverStatus.AttackerVictory, result.Status);
            Assert.Equal("trevor_faction", result.WinnerFactionId);
        }

        [Fact]
        public void CheckTakeoverForEncounter_AttackerAt0Percent_ReturnsDefenderVictory()
        {
            var detector = new TakeoverDetector();
            var encounter = new CombatEncounter("enc1", "zone1", "trevor_faction", "michael_faction");
            encounter.AttackerControlPercentage = 0f;
            encounter.DefenderControlPercentage = 100f;

            var result = detector.CheckTakeover(encounter);

            Assert.Equal(TakeoverStatus.DefenderVictory, result.Status);
            Assert.Equal("michael_faction", result.WinnerFactionId);
        }

        [Fact]
        public void CheckTakeoverForEncounter_InProgress_ReturnsInProgress()
        {
            var detector = new TakeoverDetector();
            var encounter = new CombatEncounter("enc1", "zone1", "trevor_faction", "michael_faction");
            encounter.AttackerControlPercentage = 60f;
            encounter.DefenderControlPercentage = 40f;

            var result = detector.CheckTakeover(encounter);

            Assert.Equal(TakeoverStatus.InProgress, result.Status);
            Assert.Null(result.WinnerFactionId);
        }

        [Fact]
        public void CheckTakeoverForEncounter_EndedEncounter_ThrowsInvalidOperationException()
        {
            var detector = new TakeoverDetector();
            var encounter = new CombatEncounter("enc1", "zone1", "trevor_faction", "michael_faction");
            encounter.End(CombatStatus.Aborted);

            Assert.Throws<InvalidOperationException>(() => detector.CheckTakeover(encounter));
        }

        #endregion

        #region IsAttackerVictory Tests

        [Theory]
        [InlineData(100f, true)]
        [InlineData(99.9f, false)]
        [InlineData(50f, false)]
        [InlineData(0f, false)]
        public void IsAttackerVictory_DefaultThreshold_ReturnsCorrectResult(float attackerPercent, bool expected)
        {
            var detector = new TakeoverDetector();

            var result = detector.IsAttackerVictory(attackerPercent);

            Assert.Equal(expected, result);
        }

        [Theory]
        [InlineData(80f, true)]
        [InlineData(79.9f, false)]
        [InlineData(100f, true)]
        public void IsAttackerVictory_CustomThreshold_ReturnsCorrectResult(float attackerPercent, bool expected)
        {
            var config = new TakeoverThresholdConfig { AttackerVictoryThreshold = 80f };
            var detector = new TakeoverDetector(config);

            var result = detector.IsAttackerVictory(attackerPercent);

            Assert.Equal(expected, result);
        }

        #endregion

        #region IsDefenderVictory Tests

        [Theory]
        [InlineData(0f, true)]
        [InlineData(0.1f, false)]
        [InlineData(50f, false)]
        [InlineData(100f, false)]
        public void IsDefenderVictory_DefaultThreshold_ReturnsCorrectResult(float attackerPercent, bool expected)
        {
            var detector = new TakeoverDetector();

            var result = detector.IsDefenderVictory(attackerPercent);

            Assert.Equal(expected, result);
        }

        [Theory]
        [InlineData(10f, true)]
        [InlineData(9.9f, true)]
        [InlineData(10.1f, false)]
        [InlineData(0f, true)]
        public void IsDefenderVictory_CustomThreshold_ReturnsCorrectResult(float attackerPercent, bool expected)
        {
            var config = new TakeoverThresholdConfig { DefenderVictoryThreshold = 10f };
            var detector = new TakeoverDetector(config);

            var result = detector.IsDefenderVictory(attackerPercent);

            Assert.Equal(expected, result);
        }

        #endregion

        #region GetProgressToVictory Tests

        [Theory]
        [InlineData(0f, 0f)]
        [InlineData(50f, 50f)]
        [InlineData(100f, 100f)]
        public void GetProgressToAttackerVictory_DefaultThreshold_ReturnsCorrectProgress(float attackerPercent, float expected)
        {
            var detector = new TakeoverDetector();

            var progress = detector.GetProgressToAttackerVictory(attackerPercent);

            Assert.Equal(expected, progress);
        }

        [Theory]
        [InlineData(0f, 0f)]
        [InlineData(40f, 50f)]
        [InlineData(80f, 100f)]
        public void GetProgressToAttackerVictory_CustomThreshold_ReturnsCorrectProgress(float attackerPercent, float expected)
        {
            var config = new TakeoverThresholdConfig { AttackerVictoryThreshold = 80f };
            var detector = new TakeoverDetector(config);

            var progress = detector.GetProgressToAttackerVictory(attackerPercent);

            Assert.Equal(expected, progress);
        }

        [Theory]
        [InlineData(100f, 0f)]
        [InlineData(50f, 50f)]
        [InlineData(0f, 100f)]
        public void GetProgressToDefenderVictory_DefaultThreshold_ReturnsCorrectProgress(float attackerPercent, float expected)
        {
            var detector = new TakeoverDetector();

            var progress = detector.GetProgressToDefenderVictory(attackerPercent);

            Assert.Equal(expected, progress);
        }

        [Theory]
        [InlineData(100f, 0f)]
        [InlineData(55f, 50f)]
        [InlineData(10f, 100f)]
        [InlineData(0f, 100f)]
        public void GetProgressToDefenderVictory_CustomThreshold_ReturnsCorrectProgress(float attackerPercent, float expected)
        {
            // With defender victory at 10%, progress is calculated from 100% down to 10%
            // Range is 100 - 10 = 90 points
            var config = new TakeoverThresholdConfig { DefenderVictoryThreshold = 10f };
            var detector = new TakeoverDetector(config);

            var progress = detector.GetProgressToDefenderVictory(attackerPercent);

            Assert.Equal(expected, progress);
        }

        #endregion

        #region GetCurrentThresholdConfig Tests

        [Fact]
        public void GetCurrentConfig_ReturnsConfigCopy()
        {
            var config = new TakeoverThresholdConfig
            {
                AttackerVictoryThreshold = 85f,
                DefenderVictoryThreshold = 15f,
                MinimumHoldTime = 10f
            };
            var detector = new TakeoverDetector(config);

            var currentConfig = detector.GetCurrentConfig();

            Assert.Equal(85f, currentConfig.AttackerVictoryThreshold);
            Assert.Equal(15f, currentConfig.DefenderVictoryThreshold);
            Assert.Equal(10f, currentConfig.MinimumHoldTime);
        }

        #endregion
    }
}
