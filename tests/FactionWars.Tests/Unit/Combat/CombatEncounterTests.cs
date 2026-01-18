using FactionWars.Combat.Models;
using FactionWars.Core.Interfaces;
using System;
using Xunit;

namespace FactionWars.Tests.Unit.Combat
{
    public class CombatEncounterTests
    {
        #region Constructor and Properties

        [Fact]
        public void CombatEncounter_ShouldStoreId()
        {
            // Arrange & Act
            var encounter = new CombatEncounter("encounter_1", "zone_vinewood", "faction_michael", "faction_trevor");

            // Assert
            Assert.Equal("encounter_1", encounter.Id);
        }

        [Fact]
        public void CombatEncounter_ShouldStoreZoneId()
        {
            // Arrange & Act
            var encounter = new CombatEncounter("encounter_1", "zone_vinewood", "faction_michael", "faction_trevor");

            // Assert
            Assert.Equal("zone_vinewood", encounter.ZoneId);
        }

        [Fact]
        public void CombatEncounter_ShouldStoreAttackingFactionId()
        {
            // Arrange & Act
            var encounter = new CombatEncounter("encounter_1", "zone_vinewood", "faction_michael", "faction_trevor");

            // Assert
            Assert.Equal("faction_michael", encounter.AttackingFactionId);
        }

        [Fact]
        public void CombatEncounter_ShouldStoreDefendingFactionId()
        {
            // Arrange & Act
            var encounter = new CombatEncounter("encounter_1", "zone_vinewood", "faction_michael", "faction_trevor");

            // Assert
            Assert.Equal("faction_trevor", encounter.DefendingFactionId);
        }

        [Fact]
        public void CombatEncounter_ShouldHaveZeroAttackerControlByDefault()
        {
            // Arrange & Act
            var encounter = new CombatEncounter("encounter_1", "zone_vinewood", "faction_michael", "faction_trevor");

            // Assert
            Assert.Equal(0f, encounter.AttackerControlPercentage);
        }

        [Fact]
        public void CombatEncounter_ShouldHave100DefenderControlByDefault()
        {
            // Arrange & Act
            var encounter = new CombatEncounter("encounter_1", "zone_vinewood", "faction_michael", "faction_trevor");

            // Assert
            Assert.Equal(100f, encounter.DefenderControlPercentage);
        }

        [Fact]
        public void CombatEncounter_ShouldBeActiveByDefault()
        {
            // Arrange & Act
            var encounter = new CombatEncounter("encounter_1", "zone_vinewood", "faction_michael", "faction_trevor");

            // Assert
            Assert.True(encounter.IsActive);
        }

        [Fact]
        public void CombatEncounter_ShouldRecordStartTime()
        {
            // Arrange
            var before = DateTime.UtcNow;

            // Act
            var encounter = new CombatEncounter("encounter_1", "zone_vinewood", "faction_michael", "faction_trevor");
            var after = DateTime.UtcNow;

            // Assert
            Assert.InRange(encounter.StartedAt, before, after);
        }

        #endregion

        #region Validation

        [Fact]
        public void CombatEncounter_ShouldThrowOnNullId()
        {
            // Arrange & Act & Assert
            Assert.Throws<ArgumentNullException>(() =>
                new CombatEncounter(null!, "zone_vinewood", "faction_michael", "faction_trevor"));
        }

        [Fact]
        public void CombatEncounter_ShouldThrowOnEmptyId()
        {
            // Arrange & Act & Assert
            Assert.Throws<ArgumentException>(() =>
                new CombatEncounter("", "zone_vinewood", "faction_michael", "faction_trevor"));
        }

        [Fact]
        public void CombatEncounter_ShouldThrowOnWhitespaceId()
        {
            // Arrange & Act & Assert
            Assert.Throws<ArgumentException>(() =>
                new CombatEncounter("   ", "zone_vinewood", "faction_michael", "faction_trevor"));
        }

        [Fact]
        public void CombatEncounter_ShouldThrowOnNullZoneId()
        {
            // Arrange & Act & Assert
            Assert.Throws<ArgumentNullException>(() =>
                new CombatEncounter("encounter_1", null!, "faction_michael", "faction_trevor"));
        }

        [Fact]
        public void CombatEncounter_ShouldThrowOnEmptyZoneId()
        {
            // Arrange & Act & Assert
            Assert.Throws<ArgumentException>(() =>
                new CombatEncounter("encounter_1", "", "faction_michael", "faction_trevor"));
        }

        [Fact]
        public void CombatEncounter_ShouldThrowOnNullAttackingFactionId()
        {
            // Arrange & Act & Assert
            Assert.Throws<ArgumentNullException>(() =>
                new CombatEncounter("encounter_1", "zone_vinewood", null!, "faction_trevor"));
        }

        [Fact]
        public void CombatEncounter_ShouldThrowOnEmptyAttackingFactionId()
        {
            // Arrange & Act & Assert
            Assert.Throws<ArgumentException>(() =>
                new CombatEncounter("encounter_1", "zone_vinewood", "", "faction_trevor"));
        }

        [Fact]
        public void CombatEncounter_ShouldThrowOnNullDefendingFactionId()
        {
            // Arrange & Act & Assert
            Assert.Throws<ArgumentNullException>(() =>
                new CombatEncounter("encounter_1", "zone_vinewood", "faction_michael", null!));
        }

        [Fact]
        public void CombatEncounter_ShouldThrowOnEmptyDefendingFactionId()
        {
            // Arrange & Act & Assert
            Assert.Throws<ArgumentException>(() =>
                new CombatEncounter("encounter_1", "zone_vinewood", "faction_michael", ""));
        }

        [Fact]
        public void CombatEncounter_ShouldThrowWhenAttackerAndDefenderAreSame()
        {
            // Arrange & Act & Assert
            Assert.Throws<ArgumentException>(() =>
                new CombatEncounter("encounter_1", "zone_vinewood", "faction_michael", "faction_michael"));
        }

        #endregion

        #region Combat State

        [Fact]
        public void CombatEncounter_ShouldHaveZeroAttackerPedsByDefault()
        {
            // Arrange & Act
            var encounter = new CombatEncounter("encounter_1", "zone_vinewood", "faction_michael", "faction_trevor");

            // Assert
            Assert.Equal(0, encounter.AttackerPedCount);
        }

        [Fact]
        public void CombatEncounter_ShouldHaveZeroDefenderPedsByDefault()
        {
            // Arrange & Act
            var encounter = new CombatEncounter("encounter_1", "zone_vinewood", "faction_michael", "faction_trevor");

            // Assert
            Assert.Equal(0, encounter.DefenderPedCount);
        }

        [Fact]
        public void CombatEncounter_SetAttackerPedCount_ShouldUpdateCount()
        {
            // Arrange
            var encounter = new CombatEncounter("encounter_1", "zone_vinewood", "faction_michael", "faction_trevor");

            // Act
            encounter.AttackerPedCount = 10;

            // Assert
            Assert.Equal(10, encounter.AttackerPedCount);
        }

        [Fact]
        public void CombatEncounter_SetDefenderPedCount_ShouldUpdateCount()
        {
            // Arrange
            var encounter = new CombatEncounter("encounter_1", "zone_vinewood", "faction_michael", "faction_trevor");

            // Act
            encounter.DefenderPedCount = 15;

            // Assert
            Assert.Equal(15, encounter.DefenderPedCount);
        }

        [Fact]
        public void CombatEncounter_AttackerPedCount_ShouldClampToZero()
        {
            // Arrange
            var encounter = new CombatEncounter("encounter_1", "zone_vinewood", "faction_michael", "faction_trevor");

            // Act
            encounter.AttackerPedCount = -5;

            // Assert
            Assert.Equal(0, encounter.AttackerPedCount);
        }

        [Fact]
        public void CombatEncounter_DefenderPedCount_ShouldClampToZero()
        {
            // Arrange
            var encounter = new CombatEncounter("encounter_1", "zone_vinewood", "faction_michael", "faction_trevor");

            // Act
            encounter.DefenderPedCount = -5;

            // Assert
            Assert.Equal(0, encounter.DefenderPedCount);
        }

        [Fact]
        public void CombatEncounter_TotalPedCount_ShouldReturnSum()
        {
            // Arrange
            var encounter = new CombatEncounter("encounter_1", "zone_vinewood", "faction_michael", "faction_trevor");
            encounter.AttackerPedCount = 10;
            encounter.DefenderPedCount = 15;

            // Act & Assert
            Assert.Equal(25, encounter.TotalPedCount);
        }

        #endregion

        #region Control Percentage

        [Fact]
        public void CombatEncounter_SetAttackerControlPercentage_ShouldUpdate()
        {
            // Arrange
            var encounter = new CombatEncounter("encounter_1", "zone_vinewood", "faction_michael", "faction_trevor");

            // Act
            encounter.AttackerControlPercentage = 35f;

            // Assert
            Assert.Equal(35f, encounter.AttackerControlPercentage);
        }

        [Fact]
        public void CombatEncounter_SetDefenderControlPercentage_ShouldUpdate()
        {
            // Arrange
            var encounter = new CombatEncounter("encounter_1", "zone_vinewood", "faction_michael", "faction_trevor");

            // Act
            encounter.DefenderControlPercentage = 65f;

            // Assert
            Assert.Equal(65f, encounter.DefenderControlPercentage);
        }

        [Fact]
        public void CombatEncounter_AttackerControlPercentage_ShouldClampToZero()
        {
            // Arrange
            var encounter = new CombatEncounter("encounter_1", "zone_vinewood", "faction_michael", "faction_trevor");

            // Act
            encounter.AttackerControlPercentage = -10f;

            // Assert
            Assert.Equal(0f, encounter.AttackerControlPercentage);
        }

        [Fact]
        public void CombatEncounter_AttackerControlPercentage_ShouldClampTo100()
        {
            // Arrange
            var encounter = new CombatEncounter("encounter_1", "zone_vinewood", "faction_michael", "faction_trevor");

            // Act
            encounter.AttackerControlPercentage = 150f;

            // Assert
            Assert.Equal(100f, encounter.AttackerControlPercentage);
        }

        [Fact]
        public void CombatEncounter_DefenderControlPercentage_ShouldClampToZero()
        {
            // Arrange
            var encounter = new CombatEncounter("encounter_1", "zone_vinewood", "faction_michael", "faction_trevor");

            // Act
            encounter.DefenderControlPercentage = -10f;

            // Assert
            Assert.Equal(0f, encounter.DefenderControlPercentage);
        }

        [Fact]
        public void CombatEncounter_DefenderControlPercentage_ShouldClampTo100()
        {
            // Arrange
            var encounter = new CombatEncounter("encounter_1", "zone_vinewood", "faction_michael", "faction_trevor");

            // Act
            encounter.DefenderControlPercentage = 150f;

            // Assert
            Assert.Equal(100f, encounter.DefenderControlPercentage);
        }

        #endregion

        #region Combat Status

        [Fact]
        public void CombatEncounter_Status_ShouldBeInProgressByDefault()
        {
            // Arrange & Act
            var encounter = new CombatEncounter("encounter_1", "zone_vinewood", "faction_michael", "faction_trevor");

            // Assert
            Assert.Equal(CombatStatus.InProgress, encounter.Status);
        }

        [Fact]
        public void CombatEncounter_End_ShouldSetStatusAndEndTime()
        {
            // Arrange
            var encounter = new CombatEncounter("encounter_1", "zone_vinewood", "faction_michael", "faction_trevor");
            var before = DateTime.UtcNow;

            // Act
            encounter.End(CombatStatus.AttackerVictory);
            var after = DateTime.UtcNow;

            // Assert
            Assert.Equal(CombatStatus.AttackerVictory, encounter.Status);
            Assert.False(encounter.IsActive);
            Assert.NotNull(encounter.EndedAt);
            Assert.InRange(encounter.EndedAt!.Value, before, after);
        }

        [Fact]
        public void CombatEncounter_End_ShouldNotAllowInProgressStatus()
        {
            // Arrange
            var encounter = new CombatEncounter("encounter_1", "zone_vinewood", "faction_michael", "faction_trevor");

            // Act & Assert
            Assert.Throws<ArgumentException>(() => encounter.End(CombatStatus.InProgress));
        }

        [Fact]
        public void CombatEncounter_End_ShouldNotAllowEndingTwice()
        {
            // Arrange
            var encounter = new CombatEncounter("encounter_1", "zone_vinewood", "faction_michael", "faction_trevor");
            encounter.End(CombatStatus.AttackerVictory);

            // Act & Assert
            Assert.Throws<InvalidOperationException>(() => encounter.End(CombatStatus.DefenderVictory));
        }

        [Fact]
        public void CombatEncounter_End_ShouldAllowDefenderVictory()
        {
            // Arrange
            var encounter = new CombatEncounter("encounter_1", "zone_vinewood", "faction_michael", "faction_trevor");

            // Act
            encounter.End(CombatStatus.DefenderVictory);

            // Assert
            Assert.Equal(CombatStatus.DefenderVictory, encounter.Status);
        }

        [Fact]
        public void CombatEncounter_End_ShouldAllowStalemate()
        {
            // Arrange
            var encounter = new CombatEncounter("encounter_1", "zone_vinewood", "faction_michael", "faction_trevor");

            // Act
            encounter.End(CombatStatus.Stalemate);

            // Assert
            Assert.Equal(CombatStatus.Stalemate, encounter.Status);
        }

        [Fact]
        public void CombatEncounter_End_ShouldAllowAborted()
        {
            // Arrange
            var encounter = new CombatEncounter("encounter_1", "zone_vinewood", "faction_michael", "faction_trevor");

            // Act
            encounter.End(CombatStatus.Aborted);

            // Assert
            Assert.Equal(CombatStatus.Aborted, encounter.Status);
        }

        #endregion

        #region Duration

        [Fact]
        public void CombatEncounter_GetDuration_ShouldReturnElapsedTimeWhileActive()
        {
            // Arrange
            var encounter = new CombatEncounter("encounter_1", "zone_vinewood", "faction_michael", "faction_trevor");

            // Act - Small delay to ensure measurable duration
            System.Threading.Thread.Sleep(10);
            var duration = encounter.GetDuration();

            // Assert
            Assert.True(duration >= TimeSpan.FromMilliseconds(10));
        }

        [Fact]
        public void CombatEncounter_GetDuration_ShouldReturnFixedTimeAfterEnded()
        {
            // Arrange
            var encounter = new CombatEncounter("encounter_1", "zone_vinewood", "faction_michael", "faction_trevor");
            System.Threading.Thread.Sleep(10);
            encounter.End(CombatStatus.AttackerVictory);
            var durationAfterEnd = encounter.GetDuration();

            // Act - Wait more time
            System.Threading.Thread.Sleep(20);
            var laterDuration = encounter.GetDuration();

            // Assert - Duration should not change after ending
            Assert.Equal(durationAfterEnd, laterDuration);
        }

        #endregion

        #region Equality

        [Fact]
        public void CombatEncounter_ShouldBeEqualById()
        {
            // Arrange
            var encounter1 = new CombatEncounter("encounter_1", "zone_vinewood", "faction_michael", "faction_trevor");
            var encounter2 = new CombatEncounter("encounter_1", "zone_downtown", "faction_franklin", "faction_michael");

            // Act & Assert
            Assert.Equal(encounter1, encounter2);
        }

        [Fact]
        public void CombatEncounter_ShouldNotBeEqualWithDifferentId()
        {
            // Arrange
            var encounter1 = new CombatEncounter("encounter_1", "zone_vinewood", "faction_michael", "faction_trevor");
            var encounter2 = new CombatEncounter("encounter_2", "zone_vinewood", "faction_michael", "faction_trevor");

            // Act & Assert
            Assert.NotEqual(encounter1, encounter2);
        }

        [Fact]
        public void CombatEncounter_GetHashCode_ShouldBeConsistentWithEquals()
        {
            // Arrange
            var encounter1 = new CombatEncounter("encounter_1", "zone_vinewood", "faction_michael", "faction_trevor");
            var encounter2 = new CombatEncounter("encounter_1", "zone_downtown", "faction_franklin", "faction_michael");

            // Act & Assert
            Assert.Equal(encounter1.GetHashCode(), encounter2.GetHashCode());
        }

        [Fact]
        public void CombatEncounter_ShouldNotBeEqualToNull()
        {
            // Arrange
            var encounter = new CombatEncounter("encounter_1", "zone_vinewood", "faction_michael", "faction_trevor");

            // Act & Assert
            Assert.False(encounter.Equals(null));
        }

        [Fact]
        public void CombatEncounter_EqualityOperator_ShouldWork()
        {
            // Arrange
            var encounter1 = new CombatEncounter("encounter_1", "zone_vinewood", "faction_michael", "faction_trevor");
            var encounter2 = new CombatEncounter("encounter_1", "zone_downtown", "faction_franklin", "faction_michael");

            // Act & Assert
            Assert.True(encounter1 == encounter2);
        }

        [Fact]
        public void CombatEncounter_InequalityOperator_ShouldWork()
        {
            // Arrange
            var encounter1 = new CombatEncounter("encounter_1", "zone_vinewood", "faction_michael", "faction_trevor");
            var encounter2 = new CombatEncounter("encounter_2", "zone_vinewood", "faction_michael", "faction_trevor");

            // Act & Assert
            Assert.True(encounter1 != encounter2);
        }

        [Fact]
        public void CombatEncounter_NullEquality_ShouldHandleNullLeft()
        {
            // Arrange
            CombatEncounter? encounter1 = null;
            var encounter2 = new CombatEncounter("encounter_1", "zone_vinewood", "faction_michael", "faction_trevor");

            // Act & Assert
            Assert.True(encounter1 != encounter2);
            Assert.False(encounter1 == encounter2);
        }

        [Fact]
        public void CombatEncounter_NullEquality_ShouldHandleBothNull()
        {
            // Arrange
            CombatEncounter? encounter1 = null;
            CombatEncounter? encounter2 = null;

            // Act & Assert
            Assert.True(encounter1 == encounter2);
        }

        #endregion

        #region ToString

        [Fact]
        public void CombatEncounter_ToString_ShouldContainId()
        {
            // Arrange
            var encounter = new CombatEncounter("encounter_12345", "zone_vinewood", "faction_michael", "faction_trevor");

            // Act
            var result = encounter.ToString();

            // Assert
            Assert.Contains("encounter_12345", result);
        }

        [Fact]
        public void CombatEncounter_ToString_ShouldContainZoneId()
        {
            // Arrange
            var encounter = new CombatEncounter("encounter_1", "zone_vinewood", "faction_michael", "faction_trevor");

            // Act
            var result = encounter.ToString();

            // Assert
            Assert.Contains("zone_vinewood", result);
        }

        [Fact]
        public void CombatEncounter_ToString_ShouldIndicateStatus()
        {
            // Arrange
            var encounter = new CombatEncounter("encounter_1", "zone_vinewood", "faction_michael", "faction_trevor");

            // Act
            var result = encounter.ToString();

            // Assert
            Assert.Contains("InProgress", result);
        }

        #endregion

        #region Winner Determination

        [Fact]
        public void CombatEncounter_WinnerFactionId_ShouldBeNullWhileActive()
        {
            // Arrange & Act
            var encounter = new CombatEncounter("encounter_1", "zone_vinewood", "faction_michael", "faction_trevor");

            // Assert
            Assert.Null(encounter.WinnerFactionId);
        }

        [Fact]
        public void CombatEncounter_WinnerFactionId_ShouldBeAttackerOnAttackerVictory()
        {
            // Arrange
            var encounter = new CombatEncounter("encounter_1", "zone_vinewood", "faction_michael", "faction_trevor");

            // Act
            encounter.End(CombatStatus.AttackerVictory);

            // Assert
            Assert.Equal("faction_michael", encounter.WinnerFactionId);
        }

        [Fact]
        public void CombatEncounter_WinnerFactionId_ShouldBeDefenderOnDefenderVictory()
        {
            // Arrange
            var encounter = new CombatEncounter("encounter_1", "zone_vinewood", "faction_michael", "faction_trevor");

            // Act
            encounter.End(CombatStatus.DefenderVictory);

            // Assert
            Assert.Equal("faction_trevor", encounter.WinnerFactionId);
        }

        [Fact]
        public void CombatEncounter_WinnerFactionId_ShouldBeNullOnStalemate()
        {
            // Arrange
            var encounter = new CombatEncounter("encounter_1", "zone_vinewood", "faction_michael", "faction_trevor");

            // Act
            encounter.End(CombatStatus.Stalemate);

            // Assert
            Assert.Null(encounter.WinnerFactionId);
        }

        [Fact]
        public void CombatEncounter_WinnerFactionId_ShouldBeNullOnAborted()
        {
            // Arrange
            var encounter = new CombatEncounter("encounter_1", "zone_vinewood", "faction_michael", "faction_trevor");

            // Act
            encounter.End(CombatStatus.Aborted);

            // Assert
            Assert.Null(encounter.WinnerFactionId);
        }

        #endregion
    }
}
