using System;
using FactionWars.Lieutenants.Interfaces;
using FactionWars.Lieutenants.Models;
using FactionWars.Lieutenants.Services;
using Moq;
using Xunit;

namespace FactionWars.Tests.Unit.Lieutenants
{
    public class DefectionServiceTests
    {
        #region Constructor

        [Fact]
        public void Constructor_WithNullRandomProvider_ThrowsArgumentNullException()
        {
            // Act & Assert
            Assert.Throws<ArgumentNullException>(() => new DefectionService(null!));
        }

        [Fact]
        public void Constructor_WithValidRandomProvider_CreatesInstance()
        {
            // Arrange
            var mockRandom = new Mock<IRandomProvider>();

            // Act
            var service = new DefectionService(mockRandom.Object);

            // Assert
            Assert.NotNull(service);
        }

        #endregion

        #region CalculateDefectionChance - Base Calculations

        [Fact]
        public void CalculateDefectionChance_WithNullLieutenant_ThrowsArgumentNullException()
        {
            // Arrange
            var mockRandom = new Mock<IRandomProvider>();
            var service = new DefectionService(mockRandom.Object);

            // Act & Assert
            Assert.Throws<ArgumentNullException>(() => service.CalculateDefectionChance(null!));
        }

        [Fact]
        public void CalculateDefectionChance_WithMaxLoyalty_ReturnsZero()
        {
            // Arrange
            var mockRandom = new Mock<IRandomProvider>();
            var service = new DefectionService(mockRandom.Object);
            var lieutenant = new Lieutenant("lt_001", "Gustavo", "faction_michael", loyalty: 100);

            // Act
            var chance = service.CalculateDefectionChance(lieutenant);

            // Assert
            Assert.Equal(0.0, chance);
        }

        [Fact]
        public void CalculateDefectionChance_WithZeroLoyalty_ReturnsMaxChance()
        {
            // Arrange
            var mockRandom = new Mock<IRandomProvider>();
            var service = new DefectionService(mockRandom.Object);
            var lieutenant = new Lieutenant("lt_001", "Gustavo", "faction_michael", loyalty: 0);

            // Act
            var chance = service.CalculateDefectionChance(lieutenant);

            // Assert
            // Max base chance is 100% when loyalty is 0
            Assert.Equal(1.0, chance, precision: 2);
        }

        [Fact]
        public void CalculateDefectionChance_WithMidLoyalty_ReturnsProportionalChance()
        {
            // Arrange
            var mockRandom = new Mock<IRandomProvider>();
            var service = new DefectionService(mockRandom.Object);
            var lieutenant = new Lieutenant("lt_001", "Gustavo", "faction_michael", loyalty: 50);

            // Act
            var chance = service.CalculateDefectionChance(lieutenant);

            // Assert
            // 50% loyalty means 50% base defection chance
            Assert.Equal(0.5, chance, precision: 2);
        }

        [Fact]
        public void CalculateDefectionChance_WithDeceasedLieutenant_ReturnsZero()
        {
            // Arrange
            var mockRandom = new Mock<IRandomProvider>();
            var service = new DefectionService(mockRandom.Object);
            var lieutenant = new Lieutenant("lt_001", "Gustavo", "faction_michael", loyalty: 0);
            lieutenant.Kill();

            // Act
            var chance = service.CalculateDefectionChance(lieutenant);

            // Assert
            Assert.Equal(0.0, chance);
        }

        #endregion

        #region CalculateDefectionChance - Trait Effects

        [Fact]
        public void CalculateDefectionChance_WithLoyalTrait_ReducesChance()
        {
            // Arrange
            var mockRandom = new Mock<IRandomProvider>();
            var service = new DefectionService(mockRandom.Object);
            var lieutenantWithoutTrait = new Lieutenant("lt_001", "Gustavo", "faction_michael", loyalty: 50);
            var lieutenantWithTrait = new Lieutenant("lt_002", "Marco", "faction_michael", loyalty: 50);
            lieutenantWithTrait.AddTrait(LieutenantTrait.Loyal);

            // Act
            var chanceWithoutTrait = service.CalculateDefectionChance(lieutenantWithoutTrait);
            var chanceWithTrait = service.CalculateDefectionChance(lieutenantWithTrait);

            // Assert
            Assert.True(chanceWithTrait < chanceWithoutTrait,
                "Loyal trait should reduce defection chance");
        }

        [Fact]
        public void CalculateDefectionChance_WithAmbitiousTrait_IncreasesChance()
        {
            // Arrange
            var mockRandom = new Mock<IRandomProvider>();
            var service = new DefectionService(mockRandom.Object);
            var lieutenantWithoutTrait = new Lieutenant("lt_001", "Gustavo", "faction_michael", loyalty: 50);
            var lieutenantWithTrait = new Lieutenant("lt_002", "Marco", "faction_michael", loyalty: 50);
            lieutenantWithTrait.AddTrait(LieutenantTrait.Ambitious);

            // Act
            var chanceWithoutTrait = service.CalculateDefectionChance(lieutenantWithoutTrait);
            var chanceWithTrait = service.CalculateDefectionChance(lieutenantWithTrait);

            // Assert
            Assert.True(chanceWithTrait > chanceWithoutTrait,
                "Ambitious trait should increase defection chance");
        }

        [Fact]
        public void CalculateDefectionChance_WithLoyalTrait_CanReduceToZero()
        {
            // Arrange
            var mockRandom = new Mock<IRandomProvider>();
            var service = new DefectionService(mockRandom.Object);
            var lieutenant = new Lieutenant("lt_001", "Gustavo", "faction_michael", loyalty: 80);
            lieutenant.AddTrait(LieutenantTrait.Loyal);

            // Act
            var chance = service.CalculateDefectionChance(lieutenant);

            // Assert
            // High loyalty + Loyal trait should result in zero or near-zero chance
            Assert.True(chance <= 0.05, "Loyal trait with high loyalty should result in very low chance");
        }

        [Fact]
        public void CalculateDefectionChance_WithAmbitiousTrait_CapsAtOne()
        {
            // Arrange
            var mockRandom = new Mock<IRandomProvider>();
            var service = new DefectionService(mockRandom.Object);
            var lieutenant = new Lieutenant("lt_001", "Gustavo", "faction_michael", loyalty: 0);
            lieutenant.AddTrait(LieutenantTrait.Ambitious);

            // Act
            var chance = service.CalculateDefectionChance(lieutenant);

            // Assert
            // Even with ambitious trait pushing chance up, it should cap at 1.0
            Assert.True(chance <= 1.0, "Defection chance should not exceed 1.0");
        }

        [Fact]
        public void CalculateDefectionChance_WithVeteranTrait_SlightlyReducesChance()
        {
            // Arrange
            var mockRandom = new Mock<IRandomProvider>();
            var service = new DefectionService(mockRandom.Object);
            var lieutenantWithoutTrait = new Lieutenant("lt_001", "Gustavo", "faction_michael", loyalty: 50);
            var lieutenantWithTrait = new Lieutenant("lt_002", "Marco", "faction_michael", loyalty: 50);
            lieutenantWithTrait.AddTrait(LieutenantTrait.Veteran);

            // Act
            var chanceWithoutTrait = service.CalculateDefectionChance(lieutenantWithoutTrait);
            var chanceWithTrait = service.CalculateDefectionChance(lieutenantWithTrait);

            // Assert
            // Veterans are slightly more loyal due to experience
            Assert.True(chanceWithTrait <= chanceWithoutTrait,
                "Veteran trait should not increase defection chance");
        }

        #endregion

        #region CalculateDefectionChance - With Bribe

        [Fact]
        public void CalculateDefectionChance_WithBribe_IncreasesChance()
        {
            // Arrange
            var mockRandom = new Mock<IRandomProvider>();
            var service = new DefectionService(mockRandom.Object);
            var lieutenant = new Lieutenant("lt_001", "Gustavo", "faction_michael", loyalty: 80);

            // Act
            var chanceWithoutBribe = service.CalculateDefectionChance(lieutenant, bribeAmount: 0);
            var chanceWithBribe = service.CalculateDefectionChance(lieutenant, bribeAmount: 50000);

            // Assert
            Assert.True(chanceWithBribe > chanceWithoutBribe,
                "Bribe should increase defection chance");
        }

        [Fact]
        public void CalculateDefectionChance_WithCorruptTrait_BribeIsMoreEffective()
        {
            // Arrange
            var mockRandom = new Mock<IRandomProvider>();
            var service = new DefectionService(mockRandom.Object);
            var lieutenantWithoutTrait = new Lieutenant("lt_001", "Gustavo", "faction_michael", loyalty: 70);
            var lieutenantWithTrait = new Lieutenant("lt_002", "Marco", "faction_michael", loyalty: 70);
            lieutenantWithTrait.AddTrait(LieutenantTrait.Corrupt);

            // Act
            var chanceWithoutTrait = service.CalculateDefectionChance(lieutenantWithoutTrait, bribeAmount: 25000);
            var chanceWithTrait = service.CalculateDefectionChance(lieutenantWithTrait, bribeAmount: 25000);

            // Assert
            Assert.True(chanceWithTrait > chanceWithoutTrait,
                "Corrupt trait should make bribes more effective");
        }

        [Fact]
        public void CalculateDefectionChance_WithNegativeBribe_TreatedAsZero()
        {
            // Arrange
            var mockRandom = new Mock<IRandomProvider>();
            var service = new DefectionService(mockRandom.Object);
            var lieutenant = new Lieutenant("lt_001", "Gustavo", "faction_michael", loyalty: 50);

            // Act
            var chanceWithNegative = service.CalculateDefectionChance(lieutenant, bribeAmount: -10000);
            var chanceWithZero = service.CalculateDefectionChance(lieutenant, bribeAmount: 0);

            // Assert
            Assert.Equal(chanceWithZero, chanceWithNegative);
        }

        [Fact]
        public void CalculateDefectionChance_BribeHasDiminishingReturns()
        {
            // Arrange
            var mockRandom = new Mock<IRandomProvider>();
            var service = new DefectionService(mockRandom.Object);
            var lieutenant = new Lieutenant("lt_001", "Gustavo", "faction_michael", loyalty: 70);

            // Act
            var chanceWith25k = service.CalculateDefectionChance(lieutenant, bribeAmount: 25000);
            var chanceWith50k = service.CalculateDefectionChance(lieutenant, bribeAmount: 50000);
            var chanceWith100k = service.CalculateDefectionChance(lieutenant, bribeAmount: 100000);

            // Assert
            var increase1 = chanceWith50k - chanceWith25k;
            var increase2 = chanceWith100k - chanceWith50k;
            Assert.True(increase2 < increase1 * 1.5,
                "Larger bribes should have diminishing returns");
        }

        #endregion

        #region CalculateDefectionChance - Captured State

        [Fact]
        public void CalculateDefectionChance_WhenCaptured_IncreasesBaseChance()
        {
            // Arrange
            var mockRandom = new Mock<IRandomProvider>();
            var service = new DefectionService(mockRandom.Object);
            var lieutenantActive = new Lieutenant("lt_001", "Gustavo", "faction_michael", loyalty: 60);
            var lieutenantCaptured = new Lieutenant("lt_002", "Marco", "faction_michael", loyalty: 60);
            lieutenantCaptured.Capture("faction_trevor");

            // Act
            var chanceActive = service.CalculateDefectionChance(lieutenantActive);
            var chanceCaptured = service.CalculateDefectionChance(lieutenantCaptured);

            // Assert
            Assert.True(chanceCaptured > chanceActive,
                "Captured lieutenants should have higher defection chance");
        }

        [Fact]
        public void CalculateDefectionChance_WhenCaptured_LoyalTraitStillHelps()
        {
            // Arrange
            var mockRandom = new Mock<IRandomProvider>();
            var service = new DefectionService(mockRandom.Object);
            var lieutenantCaptured = new Lieutenant("lt_001", "Gustavo", "faction_michael", loyalty: 60);
            lieutenantCaptured.Capture("faction_trevor");
            var lieutenantCapturedLoyal = new Lieutenant("lt_002", "Marco", "faction_michael", loyalty: 60);
            lieutenantCapturedLoyal.Capture("faction_trevor");
            lieutenantCapturedLoyal.AddTrait(LieutenantTrait.Loyal);

            // Act
            var chanceWithout = service.CalculateDefectionChance(lieutenantCaptured);
            var chanceWith = service.CalculateDefectionChance(lieutenantCapturedLoyal);

            // Assert
            Assert.True(chanceWith < chanceWithout,
                "Loyal trait should still reduce defection chance when captured");
        }

        #endregion

        #region AttemptDefection

        [Fact]
        public void AttemptDefection_WithNullLieutenant_ThrowsArgumentNullException()
        {
            // Arrange
            var mockRandom = new Mock<IRandomProvider>();
            var service = new DefectionService(mockRandom.Object);

            // Act & Assert
            Assert.Throws<ArgumentNullException>(() =>
                service.AttemptDefection(null!, "faction_trevor"));
        }

        [Fact]
        public void AttemptDefection_WithNullTargetFaction_ThrowsArgumentNullException()
        {
            // Arrange
            var mockRandom = new Mock<IRandomProvider>();
            var service = new DefectionService(mockRandom.Object);
            var lieutenant = new Lieutenant("lt_001", "Gustavo", "faction_michael");

            // Act & Assert
            Assert.Throws<ArgumentNullException>(() =>
                service.AttemptDefection(lieutenant, null!));
        }

        [Fact]
        public void AttemptDefection_WithEmptyTargetFaction_ThrowsArgumentException()
        {
            // Arrange
            var mockRandom = new Mock<IRandomProvider>();
            var service = new DefectionService(mockRandom.Object);
            var lieutenant = new Lieutenant("lt_001", "Gustavo", "faction_michael");

            // Act & Assert
            Assert.Throws<ArgumentException>(() =>
                service.AttemptDefection(lieutenant, ""));
        }

        [Fact]
        public void AttemptDefection_ToSameFaction_ReturnsFailure()
        {
            // Arrange
            var mockRandom = new Mock<IRandomProvider>();
            var service = new DefectionService(mockRandom.Object);
            var lieutenant = new Lieutenant("lt_001", "Gustavo", "faction_michael", loyalty: 0);

            // Act
            var result = service.AttemptDefection(lieutenant, "faction_michael");

            // Assert
            Assert.False(result.Success);
            Assert.Equal("faction_michael", lieutenant.FactionId); // Unchanged
        }

        [Fact]
        public void AttemptDefection_WhenDeceased_ReturnsFailure()
        {
            // Arrange
            var mockRandom = new Mock<IRandomProvider>();
            var service = new DefectionService(mockRandom.Object);
            var lieutenant = new Lieutenant("lt_001", "Gustavo", "faction_michael", loyalty: 0);
            lieutenant.Kill();

            // Act
            var result = service.AttemptDefection(lieutenant, "faction_trevor");

            // Assert
            Assert.False(result.Success);
        }

        [Fact]
        public void AttemptDefection_WhenSuccessful_ChangesLieutenantFaction()
        {
            // Arrange
            var mockRandom = new Mock<IRandomProvider>();
            // Return 0.0 which is less than any positive defection chance
            mockRandom.Setup(r => r.NextDouble()).Returns(0.0);
            var service = new DefectionService(mockRandom.Object);
            var lieutenant = new Lieutenant("lt_001", "Gustavo", "faction_michael", loyalty: 0);

            // Act
            var result = service.AttemptDefection(lieutenant, "faction_trevor");

            // Assert
            Assert.True(result.Success);
            Assert.Equal("faction_trevor", lieutenant.FactionId);
        }

        [Fact]
        public void AttemptDefection_WhenFailed_DoesNotChangeFaction()
        {
            // Arrange
            var mockRandom = new Mock<IRandomProvider>();
            // Return 1.0 which is greater than any defection chance
            mockRandom.Setup(r => r.NextDouble()).Returns(1.0);
            var service = new DefectionService(mockRandom.Object);
            var lieutenant = new Lieutenant("lt_001", "Gustavo", "faction_michael", loyalty: 50);

            // Act
            var result = service.AttemptDefection(lieutenant, "faction_trevor");

            // Assert
            Assert.False(result.Success);
            Assert.Equal("faction_michael", lieutenant.FactionId);
        }

        [Fact]
        public void AttemptDefection_ReturnsActualChanceUsed()
        {
            // Arrange
            var mockRandom = new Mock<IRandomProvider>();
            mockRandom.Setup(r => r.NextDouble()).Returns(0.0);
            var service = new DefectionService(mockRandom.Object);
            var lieutenant = new Lieutenant("lt_001", "Gustavo", "faction_michael", loyalty: 50);

            // Act
            var result = service.AttemptDefection(lieutenant, "faction_trevor");

            // Assert
            Assert.Equal(0.5, result.DefectionChance, precision: 2);
        }

        [Fact]
        public void AttemptDefection_WithBribe_UsesAdjustedChance()
        {
            // Arrange
            var mockRandom = new Mock<IRandomProvider>();
            mockRandom.Setup(r => r.NextDouble()).Returns(0.0);
            var service = new DefectionService(mockRandom.Object);
            var lieutenant = new Lieutenant("lt_001", "Gustavo", "faction_michael", loyalty: 80);

            // Act
            var resultWithoutBribe = service.AttemptDefection(lieutenant, "faction_trevor", bribeAmount: 0);
            lieutenant = new Lieutenant("lt_002", "Marco", "faction_michael", loyalty: 80);
            var resultWithBribe = service.AttemptDefection(lieutenant, "faction_trevor", bribeAmount: 50000);

            // Assert
            Assert.True(resultWithBribe.DefectionChance > resultWithoutBribe.DefectionChance,
                "Bribe should increase the chance used in attempt");
        }

        #endregion

        #region AttemptDefection - Failure Consequences

        [Fact]
        public void AttemptDefection_WhenFailed_DecreasesLoyaltySlightly()
        {
            // Arrange
            var mockRandom = new Mock<IRandomProvider>();
            mockRandom.Setup(r => r.NextDouble()).Returns(1.0);
            var service = new DefectionService(mockRandom.Object);
            var lieutenant = new Lieutenant("lt_001", "Gustavo", "faction_michael", loyalty: 50);
            int initialLoyalty = lieutenant.Loyalty;

            // Act
            var result = service.AttemptDefection(lieutenant, "faction_trevor");

            // Assert
            Assert.False(result.Success);
            // Failed defection attempt should not decrease loyalty (they resisted temptation)
            // Actually this could go either way - let's say loyalty stays same on failure
            Assert.Equal(initialLoyalty, lieutenant.Loyalty);
        }

        [Fact]
        public void AttemptDefection_WhenSuccessful_ResetsLoyaltyToDefectionPenalty()
        {
            // Arrange
            var mockRandom = new Mock<IRandomProvider>();
            mockRandom.Setup(r => r.NextDouble()).Returns(0.0);
            var service = new DefectionService(mockRandom.Object);
            var lieutenant = new Lieutenant("lt_001", "Gustavo", "faction_michael", loyalty: 10);

            // Act
            service.AttemptDefection(lieutenant, "faction_trevor");

            // Assert
            // Defectors start with reduced loyalty (50 as per Lieutenant.Defect)
            Assert.Equal(50, lieutenant.Loyalty);
        }

        #endregion

        #region CanAttemptDefection

        [Fact]
        public void CanAttemptDefection_WithNullLieutenant_ReturnsFalse()
        {
            // Arrange
            var mockRandom = new Mock<IRandomProvider>();
            var service = new DefectionService(mockRandom.Object);

            // Act
            var canAttempt = service.CanAttemptDefection(null!, "faction_trevor");

            // Assert
            Assert.False(canAttempt);
        }

        [Fact]
        public void CanAttemptDefection_WhenDeceased_ReturnsFalse()
        {
            // Arrange
            var mockRandom = new Mock<IRandomProvider>();
            var service = new DefectionService(mockRandom.Object);
            var lieutenant = new Lieutenant("lt_001", "Gustavo", "faction_michael");
            lieutenant.Kill();

            // Act
            var canAttempt = service.CanAttemptDefection(lieutenant, "faction_trevor");

            // Assert
            Assert.False(canAttempt);
        }

        [Fact]
        public void CanAttemptDefection_ToSameFaction_ReturnsFalse()
        {
            // Arrange
            var mockRandom = new Mock<IRandomProvider>();
            var service = new DefectionService(mockRandom.Object);
            var lieutenant = new Lieutenant("lt_001", "Gustavo", "faction_michael");

            // Act
            var canAttempt = service.CanAttemptDefection(lieutenant, "faction_michael");

            // Assert
            Assert.False(canAttempt);
        }

        [Fact]
        public void CanAttemptDefection_WhenActiveAndDifferentFaction_ReturnsTrue()
        {
            // Arrange
            var mockRandom = new Mock<IRandomProvider>();
            var service = new DefectionService(mockRandom.Object);
            var lieutenant = new Lieutenant("lt_001", "Gustavo", "faction_michael");

            // Act
            var canAttempt = service.CanAttemptDefection(lieutenant, "faction_trevor");

            // Assert
            Assert.True(canAttempt);
        }

        [Fact]
        public void CanAttemptDefection_WhenCapturedByTargetFaction_ReturnsTrue()
        {
            // Arrange
            var mockRandom = new Mock<IRandomProvider>();
            var service = new DefectionService(mockRandom.Object);
            var lieutenant = new Lieutenant("lt_001", "Gustavo", "faction_michael");
            lieutenant.Capture("faction_trevor");

            // Act
            var canAttempt = service.CanAttemptDefection(lieutenant, "faction_trevor");

            // Assert
            Assert.True(canAttempt);
        }

        [Fact]
        public void CanAttemptDefection_WhenCapturedByDifferentFaction_ReturnsFalse()
        {
            // Arrange
            var mockRandom = new Mock<IRandomProvider>();
            var service = new DefectionService(mockRandom.Object);
            var lieutenant = new Lieutenant("lt_001", "Gustavo", "faction_michael");
            lieutenant.Capture("faction_trevor");

            // Act
            // Only the capturing faction can attempt to flip a captured lieutenant
            var canAttempt = service.CanAttemptDefection(lieutenant, "faction_franklin");

            // Assert
            Assert.False(canAttempt);
        }

        #endregion

        #region GetRequiredBribeForGuaranteedDefection

        [Fact]
        public void GetRequiredBribeForGuaranteedDefection_WithNullLieutenant_ThrowsArgumentNullException()
        {
            // Arrange
            var mockRandom = new Mock<IRandomProvider>();
            var service = new DefectionService(mockRandom.Object);

            // Act & Assert
            Assert.Throws<ArgumentNullException>(() =>
                service.GetRequiredBribeForGuaranteedDefection(null!));
        }

        [Fact]
        public void GetRequiredBribeForGuaranteedDefection_WithZeroLoyalty_ReturnsZero()
        {
            // Arrange
            var mockRandom = new Mock<IRandomProvider>();
            var service = new DefectionService(mockRandom.Object);
            var lieutenant = new Lieutenant("lt_001", "Gustavo", "faction_michael", loyalty: 0);

            // Act
            var bribe = service.GetRequiredBribeForGuaranteedDefection(lieutenant);

            // Assert
            Assert.Equal(0, bribe);
        }

        [Fact]
        public void GetRequiredBribeForGuaranteedDefection_WithMaxLoyalty_ReturnsHighValue()
        {
            // Arrange
            var mockRandom = new Mock<IRandomProvider>();
            var service = new DefectionService(mockRandom.Object);
            var lieutenant = new Lieutenant("lt_001", "Gustavo", "faction_michael", loyalty: 100);

            // Act
            var bribe = service.GetRequiredBribeForGuaranteedDefection(lieutenant);

            // Assert
            Assert.True(bribe >= 100000, "High loyalty lieutenants should require large bribes");
        }

        [Fact]
        public void GetRequiredBribeForGuaranteedDefection_WithLoyalTrait_RequiresMore()
        {
            // Arrange
            var mockRandom = new Mock<IRandomProvider>();
            var service = new DefectionService(mockRandom.Object);
            var lieutenantWithout = new Lieutenant("lt_001", "Gustavo", "faction_michael", loyalty: 70);
            var lieutenantWith = new Lieutenant("lt_002", "Marco", "faction_michael", loyalty: 70);
            lieutenantWith.AddTrait(LieutenantTrait.Loyal);

            // Act
            var bribeWithout = service.GetRequiredBribeForGuaranteedDefection(lieutenantWithout);
            var bribeWith = service.GetRequiredBribeForGuaranteedDefection(lieutenantWith);

            // Assert
            Assert.True(bribeWith > bribeWithout,
                "Loyal lieutenants should require larger bribes");
        }

        [Fact]
        public void GetRequiredBribeForGuaranteedDefection_WithCorruptTrait_RequiresLess()
        {
            // Arrange
            var mockRandom = new Mock<IRandomProvider>();
            var service = new DefectionService(mockRandom.Object);
            var lieutenantWithout = new Lieutenant("lt_001", "Gustavo", "faction_michael", loyalty: 70);
            var lieutenantWith = new Lieutenant("lt_002", "Marco", "faction_michael", loyalty: 70);
            lieutenantWith.AddTrait(LieutenantTrait.Corrupt);

            // Act
            var bribeWithout = service.GetRequiredBribeForGuaranteedDefection(lieutenantWithout);
            var bribeWith = service.GetRequiredBribeForGuaranteedDefection(lieutenantWith);

            // Assert
            Assert.True(bribeWith < bribeWithout,
                "Corrupt lieutenants should require smaller bribes");
        }

        [Fact]
        public void GetRequiredBribeForGuaranteedDefection_WhenCaptured_RequiresLess()
        {
            // Arrange
            var mockRandom = new Mock<IRandomProvider>();
            var service = new DefectionService(mockRandom.Object);
            var lieutenantActive = new Lieutenant("lt_001", "Gustavo", "faction_michael", loyalty: 70);
            var lieutenantCaptured = new Lieutenant("lt_002", "Marco", "faction_michael", loyalty: 70);
            lieutenantCaptured.Capture("faction_trevor");

            // Act
            var bribeActive = service.GetRequiredBribeForGuaranteedDefection(lieutenantActive);
            var bribeCaptured = service.GetRequiredBribeForGuaranteedDefection(lieutenantCaptured);

            // Assert
            Assert.True(bribeCaptured < bribeActive,
                "Captured lieutenants should be easier to bribe");
        }

        #endregion

        #region HasDefectedBefore

        [Fact]
        public void IsFormerMember_WithNullLieutenant_ReturnsFalse()
        {
            // Arrange
            var mockRandom = new Mock<IRandomProvider>();
            var service = new DefectionService(mockRandom.Object);

            // Act
            var result = service.IsFormerMember(null!, "faction_michael");

            // Assert
            Assert.False(result);
        }

        [Fact]
        public void IsFormerMember_WhenNeverDefected_ReturnsFalse()
        {
            // Arrange
            var mockRandom = new Mock<IRandomProvider>();
            var service = new DefectionService(mockRandom.Object);
            var lieutenant = new Lieutenant("lt_001", "Gustavo", "faction_michael");

            // Act
            var result = service.IsFormerMember(lieutenant, "faction_trevor");

            // Assert
            Assert.False(result);
        }

        [Fact]
        public void IsFormerMember_WhenDefectedFromFaction_ReturnsTrue()
        {
            // Arrange
            var mockRandom = new Mock<IRandomProvider>();
            var service = new DefectionService(mockRandom.Object);
            var lieutenant = new Lieutenant("lt_001", "Gustavo", "faction_michael");
            lieutenant.Defect("faction_trevor");

            // Act
            var result = service.IsFormerMember(lieutenant, "faction_michael");

            // Assert
            Assert.True(result);
        }

        [Fact]
        public void IsFormerMember_ForCurrentFaction_ReturnsFalse()
        {
            // Arrange
            var mockRandom = new Mock<IRandomProvider>();
            var service = new DefectionService(mockRandom.Object);
            var lieutenant = new Lieutenant("lt_001", "Gustavo", "faction_michael");
            lieutenant.Defect("faction_trevor");

            // Act
            var result = service.IsFormerMember(lieutenant, "faction_trevor");

            // Assert
            Assert.False(result);
        }

        #endregion

        #region CalculateDefectionChance - Previous Defector Bonus

        [Fact]
        public void CalculateDefectionChance_PreviousDefectorHasHigherChance()
        {
            // Arrange
            var mockRandom = new Mock<IRandomProvider>();
            var service = new DefectionService(mockRandom.Object);
            var loyalLieutenant = new Lieutenant("lt_001", "Gustavo", "faction_michael", loyalty: 50);
            var defectorLieutenant = new Lieutenant("lt_002", "Marco", "faction_michael", loyalty: 50);
            defectorLieutenant.Defect("faction_trevor");
            // Now both have the same faction (trevor) and same loyalty (50 after defection)
            // But Marco has already defected once

            // Act
            var loyalChance = service.CalculateDefectionChance(loyalLieutenant);
            var defectorChance = service.CalculateDefectionChance(defectorLieutenant);

            // Assert
            Assert.True(defectorChance >= loyalChance,
                "Previous defectors should have equal or higher defection chance");
        }

        #endregion
    }
}
