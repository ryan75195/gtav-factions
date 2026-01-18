using FactionWars.Lieutenants.Models;
using System;
using Xunit;

namespace FactionWars.Tests.Unit.Lieutenants
{
    public class LieutenantTests
    {
        #region Constructor and Required Properties

        [Fact]
        public void Lieutenant_ShouldRequireId()
        {
            // Arrange & Act
            var lieutenant = new Lieutenant("lt_001", "Gustavo", "faction_michael");

            // Assert
            Assert.Equal("lt_001", lieutenant.Id);
        }

        [Fact]
        public void Lieutenant_ShouldRequireName()
        {
            // Arrange & Act
            var lieutenant = new Lieutenant("lt_001", "Gustavo", "faction_michael");

            // Assert
            Assert.Equal("Gustavo", lieutenant.Name);
        }

        [Fact]
        public void Lieutenant_ShouldRequireFactionId()
        {
            // Arrange & Act
            var lieutenant = new Lieutenant("lt_001", "Gustavo", "faction_michael");

            // Assert
            Assert.Equal("faction_michael", lieutenant.FactionId);
        }

        [Fact]
        public void Lieutenant_ShouldThrowOnNullId()
        {
            // Arrange, Act & Assert
            Assert.Throws<ArgumentNullException>(() => new Lieutenant(null!, "Gustavo", "faction_michael"));
        }

        [Fact]
        public void Lieutenant_ShouldThrowOnEmptyId()
        {
            // Arrange, Act & Assert
            Assert.Throws<ArgumentException>(() => new Lieutenant("", "Gustavo", "faction_michael"));
        }

        [Fact]
        public void Lieutenant_ShouldThrowOnWhitespaceId()
        {
            // Arrange, Act & Assert
            Assert.Throws<ArgumentException>(() => new Lieutenant("   ", "Gustavo", "faction_michael"));
        }

        [Fact]
        public void Lieutenant_ShouldThrowOnNullName()
        {
            // Arrange, Act & Assert
            Assert.Throws<ArgumentNullException>(() => new Lieutenant("lt_001", null!, "faction_michael"));
        }

        [Fact]
        public void Lieutenant_ShouldThrowOnEmptyName()
        {
            // Arrange, Act & Assert
            Assert.Throws<ArgumentException>(() => new Lieutenant("lt_001", "", "faction_michael"));
        }

        [Fact]
        public void Lieutenant_ShouldThrowOnWhitespaceName()
        {
            // Arrange, Act & Assert
            Assert.Throws<ArgumentException>(() => new Lieutenant("lt_001", "   ", "faction_michael"));
        }

        [Fact]
        public void Lieutenant_ShouldThrowOnNullFactionId()
        {
            // Arrange, Act & Assert
            Assert.Throws<ArgumentNullException>(() => new Lieutenant("lt_001", "Gustavo", null!));
        }

        [Fact]
        public void Lieutenant_ShouldThrowOnEmptyFactionId()
        {
            // Arrange, Act & Assert
            Assert.Throws<ArgumentException>(() => new Lieutenant("lt_001", "Gustavo", ""));
        }

        [Fact]
        public void Lieutenant_ShouldThrowOnWhitespaceFactionId()
        {
            // Arrange, Act & Assert
            Assert.Throws<ArgumentException>(() => new Lieutenant("lt_001", "Gustavo", "   "));
        }

        #endregion

        #region Zone Assignment

        [Fact]
        public void Lieutenant_ShouldHaveNullAssignedZoneByDefault()
        {
            // Arrange & Act
            var lieutenant = new Lieutenant("lt_001", "Gustavo", "faction_michael");

            // Assert
            Assert.Null(lieutenant.AssignedZoneId);
        }

        [Fact]
        public void Lieutenant_AssignToZone_ShouldSetAssignedZoneId()
        {
            // Arrange
            var lieutenant = new Lieutenant("lt_001", "Gustavo", "faction_michael");

            // Act
            lieutenant.AssignToZone("zone_downtown");

            // Assert
            Assert.Equal("zone_downtown", lieutenant.AssignedZoneId);
        }

        [Fact]
        public void Lieutenant_AssignToZone_ShouldAllowReassignment()
        {
            // Arrange
            var lieutenant = new Lieutenant("lt_001", "Gustavo", "faction_michael");
            lieutenant.AssignToZone("zone_downtown");

            // Act
            lieutenant.AssignToZone("zone_vinewood");

            // Assert
            Assert.Equal("zone_vinewood", lieutenant.AssignedZoneId);
        }

        [Fact]
        public void Lieutenant_Unassign_ShouldClearAssignedZoneId()
        {
            // Arrange
            var lieutenant = new Lieutenant("lt_001", "Gustavo", "faction_michael");
            lieutenant.AssignToZone("zone_downtown");

            // Act
            lieutenant.Unassign();

            // Assert
            Assert.Null(lieutenant.AssignedZoneId);
        }

        [Fact]
        public void Lieutenant_IsAssigned_ShouldReturnTrueWhenAssigned()
        {
            // Arrange
            var lieutenant = new Lieutenant("lt_001", "Gustavo", "faction_michael");
            lieutenant.AssignToZone("zone_downtown");

            // Act & Assert
            Assert.True(lieutenant.IsAssigned);
        }

        [Fact]
        public void Lieutenant_IsAssigned_ShouldReturnFalseWhenNotAssigned()
        {
            // Arrange
            var lieutenant = new Lieutenant("lt_001", "Gustavo", "faction_michael");

            // Act & Assert
            Assert.False(lieutenant.IsAssigned);
        }

        #endregion

        #region Loyalty

        [Fact]
        public void Lieutenant_ShouldHaveDefaultLoyalty()
        {
            // Arrange & Act
            var lieutenant = new Lieutenant("lt_001", "Gustavo", "faction_michael");

            // Assert
            Assert.Equal(100, lieutenant.Loyalty);
        }

        [Fact]
        public void Lieutenant_ShouldAllowCustomInitialLoyalty()
        {
            // Arrange & Act
            var lieutenant = new Lieutenant("lt_001", "Gustavo", "faction_michael", loyalty: 75);

            // Assert
            Assert.Equal(75, lieutenant.Loyalty);
        }

        [Fact]
        public void Lieutenant_AdjustLoyalty_ShouldIncreaseLoyalty()
        {
            // Arrange
            var lieutenant = new Lieutenant("lt_001", "Gustavo", "faction_michael", loyalty: 50);

            // Act
            lieutenant.AdjustLoyalty(25);

            // Assert
            Assert.Equal(75, lieutenant.Loyalty);
        }

        [Fact]
        public void Lieutenant_AdjustLoyalty_ShouldDecreaseLoyalty()
        {
            // Arrange
            var lieutenant = new Lieutenant("lt_001", "Gustavo", "faction_michael", loyalty: 50);

            // Act
            lieutenant.AdjustLoyalty(-25);

            // Assert
            Assert.Equal(25, lieutenant.Loyalty);
        }

        [Fact]
        public void Lieutenant_AdjustLoyalty_ShouldCapAtMaximum()
        {
            // Arrange
            var lieutenant = new Lieutenant("lt_001", "Gustavo", "faction_michael", loyalty: 90);

            // Act
            lieutenant.AdjustLoyalty(50);

            // Assert
            Assert.Equal(100, lieutenant.Loyalty);
        }

        [Fact]
        public void Lieutenant_AdjustLoyalty_ShouldCapAtMinimum()
        {
            // Arrange
            var lieutenant = new Lieutenant("lt_001", "Gustavo", "faction_michael", loyalty: 20);

            // Act
            lieutenant.AdjustLoyalty(-50);

            // Assert
            Assert.Equal(0, lieutenant.Loyalty);
        }

        [Fact]
        public void Lieutenant_IsLoyal_ShouldReturnTrueWhenLoyaltyAboveThreshold()
        {
            // Arrange
            var lieutenant = new Lieutenant("lt_001", "Gustavo", "faction_michael", loyalty: 50);

            // Act & Assert
            Assert.True(lieutenant.IsLoyal);
        }

        [Fact]
        public void Lieutenant_IsLoyal_ShouldReturnFalseWhenLoyaltyBelowThreshold()
        {
            // Arrange
            var lieutenant = new Lieutenant("lt_001", "Gustavo", "faction_michael", loyalty: 20);

            // Act & Assert
            Assert.False(lieutenant.IsLoyal);
        }

        [Fact]
        public void Lieutenant_IsAtRiskOfDefection_ShouldReturnTrueWhenLoyaltyLow()
        {
            // Arrange
            var lieutenant = new Lieutenant("lt_001", "Gustavo", "faction_michael", loyalty: 15);

            // Act & Assert
            Assert.True(lieutenant.IsAtRiskOfDefection);
        }

        [Fact]
        public void Lieutenant_IsAtRiskOfDefection_ShouldReturnFalseWhenLoyaltyModerate()
        {
            // Arrange
            var lieutenant = new Lieutenant("lt_001", "Gustavo", "faction_michael", loyalty: 50);

            // Act & Assert
            Assert.False(lieutenant.IsAtRiskOfDefection);
        }

        #endregion

        #region Experience

        [Fact]
        public void Lieutenant_ShouldHaveZeroExperienceByDefault()
        {
            // Arrange & Act
            var lieutenant = new Lieutenant("lt_001", "Gustavo", "faction_michael");

            // Assert
            Assert.Equal(0, lieutenant.Experience);
        }

        [Fact]
        public void Lieutenant_GainExperience_ShouldIncreaseExperience()
        {
            // Arrange
            var lieutenant = new Lieutenant("lt_001", "Gustavo", "faction_michael");

            // Act
            lieutenant.GainExperience(100);

            // Assert
            Assert.Equal(100, lieutenant.Experience);
        }

        [Fact]
        public void Lieutenant_GainExperience_ShouldAccumulate()
        {
            // Arrange
            var lieutenant = new Lieutenant("lt_001", "Gustavo", "faction_michael");

            // Act
            lieutenant.GainExperience(100);
            lieutenant.GainExperience(50);

            // Assert
            Assert.Equal(150, lieutenant.Experience);
        }

        [Fact]
        public void Lieutenant_GainExperience_ShouldIgnoreNegativeValues()
        {
            // Arrange
            var lieutenant = new Lieutenant("lt_001", "Gustavo", "faction_michael");
            lieutenant.GainExperience(100);

            // Act
            lieutenant.GainExperience(-50);

            // Assert
            Assert.Equal(100, lieutenant.Experience);
        }

        [Fact]
        public void Lieutenant_Level_ShouldBeOneByDefault()
        {
            // Arrange & Act
            var lieutenant = new Lieutenant("lt_001", "Gustavo", "faction_michael");

            // Assert
            Assert.Equal(1, lieutenant.Level);
        }

        [Fact]
        public void Lieutenant_Level_ShouldIncreaseWithExperience()
        {
            // Arrange
            var lieutenant = new Lieutenant("lt_001", "Gustavo", "faction_michael");

            // Act - 1000 XP per level
            lieutenant.GainExperience(1000);

            // Assert
            Assert.Equal(2, lieutenant.Level);
        }

        [Fact]
        public void Lieutenant_Level_ShouldCapAtMaximum()
        {
            // Arrange
            var lieutenant = new Lieutenant("lt_001", "Gustavo", "faction_michael");

            // Act - Way more than max level
            lieutenant.GainExperience(100000);

            // Assert - Max level is 10
            Assert.Equal(10, lieutenant.Level);
        }

        #endregion

        #region Status

        [Fact]
        public void Lieutenant_ShouldBeActiveByDefault()
        {
            // Arrange & Act
            var lieutenant = new Lieutenant("lt_001", "Gustavo", "faction_michael");

            // Assert
            Assert.Equal(LieutenantStatus.Active, lieutenant.Status);
        }

        [Fact]
        public void Lieutenant_Kill_ShouldSetStatusToDeceased()
        {
            // Arrange
            var lieutenant = new Lieutenant("lt_001", "Gustavo", "faction_michael");

            // Act
            lieutenant.Kill();

            // Assert
            Assert.Equal(LieutenantStatus.Deceased, lieutenant.Status);
        }

        [Fact]
        public void Lieutenant_Capture_ShouldSetStatusToCaptured()
        {
            // Arrange
            var lieutenant = new Lieutenant("lt_001", "Gustavo", "faction_michael");

            // Act
            lieutenant.Capture("faction_trevor");

            // Assert
            Assert.Equal(LieutenantStatus.Captured, lieutenant.Status);
            Assert.Equal("faction_trevor", lieutenant.CapturedByFactionId);
        }

        [Fact]
        public void Lieutenant_Release_ShouldSetStatusToActive()
        {
            // Arrange
            var lieutenant = new Lieutenant("lt_001", "Gustavo", "faction_michael");
            lieutenant.Capture("faction_trevor");

            // Act
            lieutenant.Release();

            // Assert
            Assert.Equal(LieutenantStatus.Active, lieutenant.Status);
            Assert.Null(lieutenant.CapturedByFactionId);
        }

        [Fact]
        public void Lieutenant_IsAvailable_ShouldReturnTrueWhenActiveAndNotAssigned()
        {
            // Arrange
            var lieutenant = new Lieutenant("lt_001", "Gustavo", "faction_michael");

            // Act & Assert
            Assert.True(lieutenant.IsAvailable);
        }

        [Fact]
        public void Lieutenant_IsAvailable_ShouldReturnFalseWhenAssigned()
        {
            // Arrange
            var lieutenant = new Lieutenant("lt_001", "Gustavo", "faction_michael");
            lieutenant.AssignToZone("zone_downtown");

            // Act & Assert
            Assert.False(lieutenant.IsAvailable);
        }

        [Fact]
        public void Lieutenant_IsAvailable_ShouldReturnFalseWhenCaptured()
        {
            // Arrange
            var lieutenant = new Lieutenant("lt_001", "Gustavo", "faction_michael");
            lieutenant.Capture("faction_trevor");

            // Act & Assert
            Assert.False(lieutenant.IsAvailable);
        }

        [Fact]
        public void Lieutenant_IsAvailable_ShouldReturnFalseWhenDeceased()
        {
            // Arrange
            var lieutenant = new Lieutenant("lt_001", "Gustavo", "faction_michael");
            lieutenant.Kill();

            // Act & Assert
            Assert.False(lieutenant.IsAvailable);
        }

        #endregion

        #region Traits

        [Fact]
        public void Lieutenant_ShouldHaveEmptyTraitsByDefault()
        {
            // Arrange & Act
            var lieutenant = new Lieutenant("lt_001", "Gustavo", "faction_michael");

            // Assert
            Assert.Empty(lieutenant.Traits);
        }

        [Fact]
        public void Lieutenant_AddTrait_ShouldAddTrait()
        {
            // Arrange
            var lieutenant = new Lieutenant("lt_001", "Gustavo", "faction_michael");

            // Act
            lieutenant.AddTrait(LieutenantTrait.Aggressive);

            // Assert
            Assert.Contains(LieutenantTrait.Aggressive, lieutenant.Traits);
        }

        [Fact]
        public void Lieutenant_AddTrait_ShouldNotAddDuplicateTrait()
        {
            // Arrange
            var lieutenant = new Lieutenant("lt_001", "Gustavo", "faction_michael");
            lieutenant.AddTrait(LieutenantTrait.Aggressive);

            // Act
            lieutenant.AddTrait(LieutenantTrait.Aggressive);

            // Assert
            Assert.Single(lieutenant.Traits);
        }

        [Fact]
        public void Lieutenant_RemoveTrait_ShouldRemoveTrait()
        {
            // Arrange
            var lieutenant = new Lieutenant("lt_001", "Gustavo", "faction_michael");
            lieutenant.AddTrait(LieutenantTrait.Aggressive);

            // Act
            lieutenant.RemoveTrait(LieutenantTrait.Aggressive);

            // Assert
            Assert.DoesNotContain(LieutenantTrait.Aggressive, lieutenant.Traits);
        }

        [Fact]
        public void Lieutenant_HasTrait_ShouldReturnTrueWhenTraitExists()
        {
            // Arrange
            var lieutenant = new Lieutenant("lt_001", "Gustavo", "faction_michael");
            lieutenant.AddTrait(LieutenantTrait.Cunning);

            // Act & Assert
            Assert.True(lieutenant.HasTrait(LieutenantTrait.Cunning));
        }

        [Fact]
        public void Lieutenant_HasTrait_ShouldReturnFalseWhenTraitMissing()
        {
            // Arrange
            var lieutenant = new Lieutenant("lt_001", "Gustavo", "faction_michael");

            // Act & Assert
            Assert.False(lieutenant.HasTrait(LieutenantTrait.Cunning));
        }

        #endregion

        #region Defection

        [Fact]
        public void Lieutenant_Defect_ShouldChangeFactionId()
        {
            // Arrange
            var lieutenant = new Lieutenant("lt_001", "Gustavo", "faction_michael");

            // Act
            lieutenant.Defect("faction_trevor");

            // Assert
            Assert.Equal("faction_trevor", lieutenant.FactionId);
        }

        [Fact]
        public void Lieutenant_Defect_ShouldUnassignFromZone()
        {
            // Arrange
            var lieutenant = new Lieutenant("lt_001", "Gustavo", "faction_michael");
            lieutenant.AssignToZone("zone_downtown");

            // Act
            lieutenant.Defect("faction_trevor");

            // Assert
            Assert.Null(lieutenant.AssignedZoneId);
        }

        [Fact]
        public void Lieutenant_Defect_ShouldReduceLoyalty()
        {
            // Arrange
            var lieutenant = new Lieutenant("lt_001", "Gustavo", "faction_michael", loyalty: 100);

            // Act
            lieutenant.Defect("faction_trevor");

            // Assert - Defectors start with reduced loyalty
            Assert.Equal(50, lieutenant.Loyalty);
        }

        [Fact]
        public void Lieutenant_Defect_ShouldThrowWhenDefectingToSameFaction()
        {
            // Arrange
            var lieutenant = new Lieutenant("lt_001", "Gustavo", "faction_michael");

            // Act & Assert
            Assert.Throws<InvalidOperationException>(() => lieutenant.Defect("faction_michael"));
        }

        [Fact]
        public void Lieutenant_Defect_ShouldThrowWhenDeceased()
        {
            // Arrange
            var lieutenant = new Lieutenant("lt_001", "Gustavo", "faction_michael");
            lieutenant.Kill();

            // Act & Assert
            Assert.Throws<InvalidOperationException>(() => lieutenant.Defect("faction_trevor"));
        }

        [Fact]
        public void Lieutenant_Defect_ShouldReleaseIfCaptured()
        {
            // Arrange
            var lieutenant = new Lieutenant("lt_001", "Gustavo", "faction_michael");
            lieutenant.Capture("faction_trevor");

            // Act
            lieutenant.Defect("faction_trevor");

            // Assert
            Assert.Equal(LieutenantStatus.Active, lieutenant.Status);
            Assert.Equal("faction_trevor", lieutenant.FactionId);
        }

        [Fact]
        public void Lieutenant_OriginalFactionId_ShouldTrackOriginalFaction()
        {
            // Arrange
            var lieutenant = new Lieutenant("lt_001", "Gustavo", "faction_michael");

            // Act
            lieutenant.Defect("faction_trevor");

            // Assert
            Assert.Equal("faction_michael", lieutenant.OriginalFactionId);
        }

        [Fact]
        public void Lieutenant_HasDefected_ShouldReturnTrueAfterDefection()
        {
            // Arrange
            var lieutenant = new Lieutenant("lt_001", "Gustavo", "faction_michael");

            // Act
            lieutenant.Defect("faction_trevor");

            // Assert
            Assert.True(lieutenant.HasDefected);
        }

        [Fact]
        public void Lieutenant_HasDefected_ShouldReturnFalseIfNeverDefected()
        {
            // Arrange
            var lieutenant = new Lieutenant("lt_001", "Gustavo", "faction_michael");

            // Act & Assert
            Assert.False(lieutenant.HasDefected);
        }

        #endregion

        #region Equality

        [Fact]
        public void Lieutenant_ShouldBeEqualById()
        {
            // Arrange
            var lieutenant1 = new Lieutenant("lt_001", "Gustavo", "faction_michael");
            var lieutenant2 = new Lieutenant("lt_001", "Different Name", "faction_trevor");

            // Act & Assert - Lieutenants are equal if they have the same ID
            Assert.Equal(lieutenant1, lieutenant2);
        }

        [Fact]
        public void Lieutenant_ShouldNotBeEqualWithDifferentId()
        {
            // Arrange
            var lieutenant1 = new Lieutenant("lt_001", "Gustavo", "faction_michael");
            var lieutenant2 = new Lieutenant("lt_002", "Gustavo", "faction_michael");

            // Act & Assert
            Assert.NotEqual(lieutenant1, lieutenant2);
        }

        [Fact]
        public void Lieutenant_GetHashCode_ShouldBeConsistentWithEquals()
        {
            // Arrange
            var lieutenant1 = new Lieutenant("lt_001", "Gustavo", "faction_michael");
            var lieutenant2 = new Lieutenant("lt_001", "Different Name", "faction_trevor");

            // Act & Assert - Equal objects must have equal hash codes
            Assert.Equal(lieutenant1.GetHashCode(), lieutenant2.GetHashCode());
        }

        [Fact]
        public void Lieutenant_ShouldNotBeEqualToNull()
        {
            // Arrange
            var lieutenant = new Lieutenant("lt_001", "Gustavo", "faction_michael");

            // Act & Assert
            Assert.False(lieutenant.Equals(null));
        }

        [Fact]
        public void Lieutenant_EqualityOperator_ShouldWork()
        {
            // Arrange
            var lieutenant1 = new Lieutenant("lt_001", "Gustavo", "faction_michael");
            var lieutenant2 = new Lieutenant("lt_001", "Different Name", "faction_trevor");

            // Act & Assert
            Assert.True(lieutenant1 == lieutenant2);
        }

        [Fact]
        public void Lieutenant_InequalityOperator_ShouldWork()
        {
            // Arrange
            var lieutenant1 = new Lieutenant("lt_001", "Gustavo", "faction_michael");
            var lieutenant2 = new Lieutenant("lt_002", "Marco", "faction_michael");

            // Act & Assert
            Assert.True(lieutenant1 != lieutenant2);
        }

        [Fact]
        public void Lieutenant_NullEquality_ShouldHandleNullLeft()
        {
            // Arrange
            Lieutenant? lieutenant1 = null;
            var lieutenant2 = new Lieutenant("lt_001", "Gustavo", "faction_michael");

            // Act & Assert
            Assert.True(lieutenant1 != lieutenant2);
            Assert.False(lieutenant1 == lieutenant2);
        }

        [Fact]
        public void Lieutenant_NullEquality_ShouldHandleBothNull()
        {
            // Arrange
            Lieutenant? lieutenant1 = null;
            Lieutenant? lieutenant2 = null;

            // Act & Assert
            Assert.True(lieutenant1 == lieutenant2);
        }

        #endregion

        #region ToString

        [Fact]
        public void Lieutenant_ToString_ShouldReturnReadableFormat()
        {
            // Arrange
            var lieutenant = new Lieutenant("lt_001", "Gustavo", "faction_michael");

            // Act
            var result = lieutenant.ToString();

            // Assert
            Assert.Contains("lt_001", result);
            Assert.Contains("Gustavo", result);
        }

        #endregion
    }
}
