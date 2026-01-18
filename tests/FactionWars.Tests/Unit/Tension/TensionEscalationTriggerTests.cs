using System;
using Xunit;
using FactionWars.Tension.Models;

namespace FactionWars.Tests.Unit.Tension
{
    /// <summary>
    /// Tests for the TensionEscalationTrigger model which represents an event
    /// that causes tension to escalate between factions.
    /// </summary>
    public class TensionEscalationTriggerTests
    {
        private const string FactionMichael = "faction-michael";
        private const string FactionTrevor = "faction-trevor";
        private const string FactionFranklin = "faction-franklin";
        private const string TestZoneId = "zone-downtown";

        #region Construction Tests

        [Fact]
        public void Constructor_WithValidParameters_CreatesTrigger()
        {
            var trigger = new TensionEscalationTrigger(
                TensionTriggerType.ZoneAttack,
                FactionMichael,
                FactionTrevor);

            Assert.Equal(TensionTriggerType.ZoneAttack, trigger.TriggerType);
            Assert.Equal(FactionMichael, trigger.AggressorFactionId);
            Assert.Equal(FactionTrevor, trigger.TargetFactionId);
        }

        [Fact]
        public void Constructor_WithNullAggressor_ThrowsArgumentNullException()
        {
            Assert.Throws<ArgumentNullException>(() => new TensionEscalationTrigger(
                TensionTriggerType.ZoneAttack,
                null!,
                FactionTrevor));
        }

        [Fact]
        public void Constructor_WithNullTarget_ThrowsArgumentNullException()
        {
            Assert.Throws<ArgumentNullException>(() => new TensionEscalationTrigger(
                TensionTriggerType.ZoneAttack,
                FactionMichael,
                null!));
        }

        [Fact]
        public void Constructor_WithEmptyAggressor_ThrowsArgumentException()
        {
            Assert.Throws<ArgumentException>(() => new TensionEscalationTrigger(
                TensionTriggerType.ZoneAttack,
                "",
                FactionTrevor));
        }

        [Fact]
        public void Constructor_WithEmptyTarget_ThrowsArgumentException()
        {
            Assert.Throws<ArgumentException>(() => new TensionEscalationTrigger(
                TensionTriggerType.ZoneAttack,
                FactionMichael,
                ""));
        }

        [Fact]
        public void Constructor_WithWhitespaceAggressor_ThrowsArgumentException()
        {
            Assert.Throws<ArgumentException>(() => new TensionEscalationTrigger(
                TensionTriggerType.ZoneAttack,
                "   ",
                FactionTrevor));
        }

        [Fact]
        public void Constructor_WithWhitespaceTarget_ThrowsArgumentException()
        {
            Assert.Throws<ArgumentException>(() => new TensionEscalationTrigger(
                TensionTriggerType.ZoneAttack,
                FactionMichael,
                "   "));
        }

        [Fact]
        public void Constructor_WithSameFactions_ThrowsArgumentException()
        {
            Assert.Throws<ArgumentException>(() => new TensionEscalationTrigger(
                TensionTriggerType.ZoneAttack,
                FactionMichael,
                FactionMichael));
        }

        [Fact]
        public void Constructor_SetsTimestampToNow()
        {
            var before = DateTime.UtcNow;
            var trigger = new TensionEscalationTrigger(
                TensionTriggerType.ZoneAttack,
                FactionMichael,
                FactionTrevor);
            var after = DateTime.UtcNow;

            Assert.InRange(trigger.Timestamp, before, after);
        }

        [Fact]
        public void Constructor_DefaultZoneId_IsNull()
        {
            var trigger = new TensionEscalationTrigger(
                TensionTriggerType.MemberKilled,
                FactionMichael,
                FactionTrevor);

            Assert.Null(trigger.ZoneId);
        }

        [Fact]
        public void Constructor_DefaultSeverity_IsNormal()
        {
            var trigger = new TensionEscalationTrigger(
                TensionTriggerType.ZoneAttack,
                FactionMichael,
                FactionTrevor);

            Assert.Equal(TriggerSeverity.Normal, trigger.Severity);
        }

        #endregion

        #region Optional Properties Tests

        [Fact]
        public void ZoneId_CanBeSet()
        {
            var trigger = new TensionEscalationTrigger(
                TensionTriggerType.ZoneCapture,
                FactionMichael,
                FactionTrevor,
                zoneId: TestZoneId);

            Assert.Equal(TestZoneId, trigger.ZoneId);
        }

        [Fact]
        public void Severity_CanBeSetToMinor()
        {
            var trigger = new TensionEscalationTrigger(
                TensionTriggerType.BorderIncursion,
                FactionMichael,
                FactionTrevor,
                severity: TriggerSeverity.Minor);

            Assert.Equal(TriggerSeverity.Minor, trigger.Severity);
        }

        [Fact]
        public void Severity_CanBeSetToMajor()
        {
            var trigger = new TensionEscalationTrigger(
                TensionTriggerType.ZoneCapture,
                FactionMichael,
                FactionTrevor,
                severity: TriggerSeverity.Major);

            Assert.Equal(TriggerSeverity.Major, trigger.Severity);
        }

        [Fact]
        public void Severity_CanBeSetToCritical()
        {
            var trigger = new TensionEscalationTrigger(
                TensionTriggerType.LeaderKilled,
                FactionMichael,
                FactionTrevor,
                severity: TriggerSeverity.Critical);

            Assert.Equal(TriggerSeverity.Critical, trigger.Severity);
        }

        [Fact]
        public void Metadata_CanBeSet()
        {
            var trigger = new TensionEscalationTrigger(
                TensionTriggerType.ResourceRaided,
                FactionMichael,
                FactionTrevor,
                metadata: "Raided 5000 cash");

            Assert.Equal("Raided 5000 cash", trigger.Metadata);
        }

        [Fact]
        public void Metadata_DefaultIsNull()
        {
            var trigger = new TensionEscalationTrigger(
                TensionTriggerType.Sabotage,
                FactionMichael,
                FactionTrevor);

            Assert.Null(trigger.Metadata);
        }

        #endregion

        #region BaseTensionIncrease Tests

        [Theory]
        [InlineData(TensionTriggerType.BorderIncursion, 5)]
        [InlineData(TensionTriggerType.ZoneAttack, 15)]
        [InlineData(TensionTriggerType.ZoneCapture, 25)]
        [InlineData(TensionTriggerType.MemberKilled, 10)]
        [InlineData(TensionTriggerType.LeaderKilled, 30)]
        [InlineData(TensionTriggerType.ResourceRaided, 8)]
        [InlineData(TensionTriggerType.Sabotage, 12)]
        [InlineData(TensionTriggerType.TerritoryThreat, 7)]
        [InlineData(TensionTriggerType.RepeatedAggression, 20)]
        [InlineData(TensionTriggerType.AllyAttacked, 10)]
        public void BaseTensionIncrease_ReturnsExpectedValue(TensionTriggerType type, int expected)
        {
            var trigger = new TensionEscalationTrigger(
                type,
                FactionMichael,
                FactionTrevor);

            Assert.Equal(expected, trigger.BaseTensionIncrease);
        }

        #endregion

        #region GetEffectiveTensionIncrease Tests

        [Fact]
        public void GetEffectiveTensionIncrease_MinorSeverity_ReducesIncrease()
        {
            var trigger = new TensionEscalationTrigger(
                TensionTriggerType.ZoneAttack, // Base 15
                FactionMichael,
                FactionTrevor,
                severity: TriggerSeverity.Minor);

            // Minor = 0.5x multiplier, so 15 * 0.5 = 7.5 -> 7 (truncated)
            Assert.Equal(7, trigger.GetEffectiveTensionIncrease());
        }

        [Fact]
        public void GetEffectiveTensionIncrease_NormalSeverity_ReturnsBase()
        {
            var trigger = new TensionEscalationTrigger(
                TensionTriggerType.ZoneAttack, // Base 15
                FactionMichael,
                FactionTrevor,
                severity: TriggerSeverity.Normal);

            Assert.Equal(15, trigger.GetEffectiveTensionIncrease());
        }

        [Fact]
        public void GetEffectiveTensionIncrease_MajorSeverity_IncreasesMultiplier()
        {
            var trigger = new TensionEscalationTrigger(
                TensionTriggerType.ZoneAttack, // Base 15
                FactionMichael,
                FactionTrevor,
                severity: TriggerSeverity.Major);

            // Major = 1.5x multiplier, so 15 * 1.5 = 22.5 -> 22 (truncated)
            Assert.Equal(22, trigger.GetEffectiveTensionIncrease());
        }

        [Fact]
        public void GetEffectiveTensionIncrease_CriticalSeverity_DoublesIncrease()
        {
            var trigger = new TensionEscalationTrigger(
                TensionTriggerType.ZoneAttack, // Base 15
                FactionMichael,
                FactionTrevor,
                severity: TriggerSeverity.Critical);

            // Critical = 2.0x multiplier, so 15 * 2.0 = 30
            Assert.Equal(30, trigger.GetEffectiveTensionIncrease());
        }

        [Theory]
        [InlineData(TensionTriggerType.LeaderKilled, TriggerSeverity.Critical, 60)] // 30 * 2.0
        [InlineData(TensionTriggerType.BorderIncursion, TriggerSeverity.Minor, 2)]  // 5 * 0.5 = 2.5 -> 2
        [InlineData(TensionTriggerType.ZoneCapture, TriggerSeverity.Major, 37)]     // 25 * 1.5 = 37.5 -> 37
        public void GetEffectiveTensionIncrease_CombinesTriggerTypeAndSeverity(
            TensionTriggerType type, TriggerSeverity severity, int expected)
        {
            var trigger = new TensionEscalationTrigger(
                type,
                FactionMichael,
                FactionTrevor,
                severity: severity);

            Assert.Equal(expected, trigger.GetEffectiveTensionIncrease());
        }

        #endregion

        #region InvolvesFaction Tests

        [Fact]
        public void InvolvesFaction_WithAggressor_ReturnsTrue()
        {
            var trigger = new TensionEscalationTrigger(
                TensionTriggerType.ZoneAttack,
                FactionMichael,
                FactionTrevor);

            Assert.True(trigger.InvolvesFaction(FactionMichael));
        }

        [Fact]
        public void InvolvesFaction_WithTarget_ReturnsTrue()
        {
            var trigger = new TensionEscalationTrigger(
                TensionTriggerType.ZoneAttack,
                FactionMichael,
                FactionTrevor);

            Assert.True(trigger.InvolvesFaction(FactionTrevor));
        }

        [Fact]
        public void InvolvesFaction_WithOtherFaction_ReturnsFalse()
        {
            var trigger = new TensionEscalationTrigger(
                TensionTriggerType.ZoneAttack,
                FactionMichael,
                FactionTrevor);

            Assert.False(trigger.InvolvesFaction(FactionFranklin));
        }

        [Fact]
        public void InvolvesFaction_WithNull_ReturnsFalse()
        {
            var trigger = new TensionEscalationTrigger(
                TensionTriggerType.ZoneAttack,
                FactionMichael,
                FactionTrevor);

            Assert.False(trigger.InvolvesFaction(null!));
        }

        #endregion

        #region ToString Tests

        [Fact]
        public void ToString_ContainsTriggerType()
        {
            var trigger = new TensionEscalationTrigger(
                TensionTriggerType.ZoneCapture,
                FactionMichael,
                FactionTrevor);

            var result = trigger.ToString();

            Assert.Contains("ZoneCapture", result);
        }

        [Fact]
        public void ToString_ContainsFactionIds()
        {
            var trigger = new TensionEscalationTrigger(
                TensionTriggerType.ZoneCapture,
                FactionMichael,
                FactionTrevor);

            var result = trigger.ToString();

            Assert.Contains(FactionMichael, result);
            Assert.Contains(FactionTrevor, result);
        }

        [Fact]
        public void ToString_ContainsSeverity()
        {
            var trigger = new TensionEscalationTrigger(
                TensionTriggerType.LeaderKilled,
                FactionMichael,
                FactionTrevor,
                severity: TriggerSeverity.Critical);

            var result = trigger.ToString();

            Assert.Contains("Critical", result);
        }

        #endregion
    }
}
