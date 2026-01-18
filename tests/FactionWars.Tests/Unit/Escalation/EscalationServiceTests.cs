using FactionWars.Escalation.Interfaces;
using FactionWars.Escalation.Models;
using FactionWars.Escalation.Repositories;
using FactionWars.Escalation.Services;
using Moq;
using System;
using System.Collections.Generic;
using Xunit;

namespace FactionWars.Tests.Unit.Escalation
{
    /// <summary>
    /// Tests for IEscalationService interface behavior and EscalationService implementation.
    /// </summary>
    public class EscalationServiceTests
    {
        #region Constructor Tests

        [Fact]
        public void Constructor_WithNullRepository_ThrowsArgumentNullException()
        {
            Assert.Throws<ArgumentNullException>(() => new EscalationService(null!));
        }

        [Fact]
        public void Constructor_WithValidRepository_CreatesInstance()
        {
            var repository = new Mock<IEscalationRepository>();
            var service = new EscalationService(repository.Object);
            Assert.NotNull(service);
        }

        #endregion

        #region GetEscalation Tests

        [Fact]
        public void GetEscalation_WithNullFactionId_ThrowsArgumentNullException()
        {
            var service = CreateService();
            Assert.Throws<ArgumentNullException>(() => service.GetEscalation(null!));
        }

        [Fact]
        public void GetEscalation_WithEmptyFactionId_ThrowsArgumentException()
        {
            var service = CreateService();
            Assert.Throws<ArgumentException>(() => service.GetEscalation(""));
        }

        [Fact]
        public void GetEscalation_WithWhitespaceFactionId_ThrowsArgumentException()
        {
            var service = CreateService();
            Assert.Throws<ArgumentException>(() => service.GetEscalation("   "));
        }

        [Fact]
        public void GetEscalation_WithValidId_ReturnsEscalation()
        {
            var repository = new InMemoryEscalationRepository();
            var escalation = new FactionEscalation("faction1", 500);
            repository.Add(escalation);
            var service = new EscalationService(repository);

            var result = service.GetEscalation("faction1");

            Assert.NotNull(result);
            Assert.Equal("faction1", result.FactionId);
            Assert.Equal(500, result.Points);
        }

        [Fact]
        public void GetEscalation_WithNonExistentId_ReturnsNull()
        {
            var service = CreateService();
            var result = service.GetEscalation("nonexistent");
            Assert.Null(result);
        }

        #endregion

        #region GetOrCreateEscalation Tests

        [Fact]
        public void GetOrCreateEscalation_WithNullFactionId_ThrowsArgumentNullException()
        {
            var service = CreateService();
            Assert.Throws<ArgumentNullException>(() => service.GetOrCreateEscalation(null!));
        }

        [Fact]
        public void GetOrCreateEscalation_WithExistingFaction_ReturnsExisting()
        {
            var repository = new InMemoryEscalationRepository();
            var existing = new FactionEscalation("faction1", 1500);
            repository.Add(existing);
            var service = new EscalationService(repository);

            var result = service.GetOrCreateEscalation("faction1");

            Assert.Equal(1500, result.Points);
        }

        [Fact]
        public void GetOrCreateEscalation_WithNewFaction_CreatesNew()
        {
            var service = CreateService();

            var result = service.GetOrCreateEscalation("newfaction");

            Assert.NotNull(result);
            Assert.Equal("newfaction", result.FactionId);
            Assert.Equal(0, result.Points);
            Assert.Equal(EscalationTier.Tier1, result.CurrentTier);
        }

        #endregion

        #region GetCurrentTier Tests

        [Fact]
        public void GetCurrentTier_WithNullFactionId_ThrowsArgumentNullException()
        {
            var service = CreateService();
            Assert.Throws<ArgumentNullException>(() => service.GetCurrentTier(null!));
        }

        [Fact]
        public void GetCurrentTier_WithValidFactionAtTier1_ReturnsTier1()
        {
            var repository = new InMemoryEscalationRepository();
            repository.Add(new FactionEscalation("faction1", 0));
            var service = new EscalationService(repository);

            var result = service.GetCurrentTier("faction1");

            Assert.Equal(EscalationTier.Tier1, result);
        }

        [Fact]
        public void GetCurrentTier_WithValidFactionAtTier3_ReturnsTier3()
        {
            var repository = new InMemoryEscalationRepository();
            repository.Add(new FactionEscalation("faction1", 3500));
            var service = new EscalationService(repository);

            var result = service.GetCurrentTier("faction1");

            Assert.Equal(EscalationTier.Tier3, result);
        }

        [Fact]
        public void GetCurrentTier_WithNonExistentFaction_ReturnsTier1()
        {
            var service = CreateService();
            var result = service.GetCurrentTier("nonexistent");
            Assert.Equal(EscalationTier.Tier1, result);
        }

        #endregion

        #region AddEscalationPoints Tests

        [Fact]
        public void AddEscalationPoints_WithNullFactionId_ThrowsArgumentNullException()
        {
            var service = CreateService();
            Assert.Throws<ArgumentNullException>(() => service.AddEscalationPoints(null!, 100));
        }

        [Fact]
        public void AddEscalationPoints_WithNegativeAmount_ThrowsArgumentOutOfRangeException()
        {
            var service = CreateService();
            Assert.Throws<ArgumentOutOfRangeException>(() => service.AddEscalationPoints("faction1", -100));
        }

        [Fact]
        public void AddEscalationPoints_WithValidInput_AddsPoints()
        {
            var repository = new InMemoryEscalationRepository();
            repository.Add(new FactionEscalation("faction1", 500));
            var service = new EscalationService(repository);

            var result = service.AddEscalationPoints("faction1", 300);

            Assert.True(result.Success);
            Assert.False(result.TierChanged);
            Assert.Equal(EscalationTier.Tier1, result.NewTier);
            Assert.Equal(800, repository.GetByFactionId("faction1")!.Points);
        }

        [Fact]
        public void AddEscalationPoints_WhenCrossingTierThreshold_ReturnsTierChanged()
        {
            var repository = new InMemoryEscalationRepository();
            repository.Add(new FactionEscalation("faction1", 900));
            var service = new EscalationService(repository);

            var result = service.AddEscalationPoints("faction1", 200);

            Assert.True(result.Success);
            Assert.True(result.TierChanged);
            Assert.Equal(EscalationTier.Tier1, result.OldTier);
            Assert.Equal(EscalationTier.Tier2, result.NewTier);
        }

        [Fact]
        public void AddEscalationPoints_WhenFactionNotFound_ReturnsFailure()
        {
            var service = CreateService();

            var result = service.AddEscalationPoints("nonexistent", 100);

            Assert.False(result.Success);
        }

        [Fact]
        public void AddEscalationPoints_CreatesEscalationIfNotExists_WhenAutoCreateEnabled()
        {
            var service = CreateService(autoCreateEscalation: true);

            var result = service.AddEscalationPoints("newfaction", 500);

            Assert.True(result.Success);
            Assert.Equal(500, service.GetEscalation("newfaction")!.Points);
        }

        #endregion

        #region RemoveEscalationPoints Tests

        [Fact]
        public void RemoveEscalationPoints_WithNullFactionId_ThrowsArgumentNullException()
        {
            var service = CreateService();
            Assert.Throws<ArgumentNullException>(() => service.RemoveEscalationPoints(null!, 100));
        }

        [Fact]
        public void RemoveEscalationPoints_WithNegativeAmount_ThrowsArgumentOutOfRangeException()
        {
            var service = CreateService();
            Assert.Throws<ArgumentOutOfRangeException>(() => service.RemoveEscalationPoints("faction1", -100));
        }

        [Fact]
        public void RemoveEscalationPoints_WithValidInput_RemovesPoints()
        {
            var repository = new InMemoryEscalationRepository();
            repository.Add(new FactionEscalation("faction1", 1500));
            var service = new EscalationService(repository);

            var result = service.RemoveEscalationPoints("faction1", 200);

            Assert.True(result.Success);
            Assert.False(result.TierChanged);
            Assert.Equal(1300, repository.GetByFactionId("faction1")!.Points);
        }

        [Fact]
        public void RemoveEscalationPoints_WhenDroppingTier_ReturnsTierChanged()
        {
            var repository = new InMemoryEscalationRepository();
            repository.Add(new FactionEscalation("faction1", 1050));
            var service = new EscalationService(repository);

            var result = service.RemoveEscalationPoints("faction1", 100);

            Assert.True(result.Success);
            Assert.True(result.TierChanged);
            Assert.Equal(EscalationTier.Tier2, result.OldTier);
            Assert.Equal(EscalationTier.Tier1, result.NewTier);
        }

        [Fact]
        public void RemoveEscalationPoints_WhenFactionNotFound_ReturnsFailure()
        {
            var service = CreateService();

            var result = service.RemoveEscalationPoints("nonexistent", 100);

            Assert.False(result.Success);
        }

        [Fact]
        public void RemoveEscalationPoints_DoesNotGoBelowZero()
        {
            var repository = new InMemoryEscalationRepository();
            repository.Add(new FactionEscalation("faction1", 50));
            var service = new EscalationService(repository);

            var result = service.RemoveEscalationPoints("faction1", 100);

            Assert.True(result.Success);
            Assert.Equal(0, repository.GetByFactionId("faction1")!.Points);
        }

        #endregion

        #region SetEscalationPoints Tests

        [Fact]
        public void SetEscalationPoints_WithNullFactionId_ThrowsArgumentNullException()
        {
            var service = CreateService();
            Assert.Throws<ArgumentNullException>(() => service.SetEscalationPoints(null!, 1000));
        }

        [Fact]
        public void SetEscalationPoints_WithValidInput_SetsPoints()
        {
            var repository = new InMemoryEscalationRepository();
            repository.Add(new FactionEscalation("faction1", 500));
            var service = new EscalationService(repository);

            var result = service.SetEscalationPoints("faction1", 3500);

            Assert.True(result);
            Assert.Equal(3500, repository.GetByFactionId("faction1")!.Points);
        }

        [Fact]
        public void SetEscalationPoints_WhenFactionNotFound_ReturnsFalse()
        {
            var service = CreateService();
            var result = service.SetEscalationPoints("nonexistent", 1000);
            Assert.False(result);
        }

        [Fact]
        public void SetEscalationPoints_ClampsToMaxPoints()
        {
            var repository = new InMemoryEscalationRepository();
            repository.Add(new FactionEscalation("faction1", 500));
            var service = new EscalationService(repository);

            service.SetEscalationPoints("faction1", 50000);

            Assert.Equal(FactionEscalation.MaxPoints, repository.GetByFactionId("faction1")!.Points);
        }

        [Fact]
        public void SetEscalationPoints_ClampsToMinPoints()
        {
            var repository = new InMemoryEscalationRepository();
            repository.Add(new FactionEscalation("faction1", 500));
            var service = new EscalationService(repository);

            service.SetEscalationPoints("faction1", -1000);

            Assert.Equal(FactionEscalation.MinPoints, repository.GetByFactionId("faction1")!.Points);
        }

        #endregion

        #region GetAllEscalations Tests

        [Fact]
        public void GetAllEscalations_WithEmptyRepository_ReturnsEmpty()
        {
            var service = CreateService();
            var result = service.GetAllEscalations();
            Assert.Empty(result);
        }

        [Fact]
        public void GetAllEscalations_WithData_ReturnsAll()
        {
            var repository = new InMemoryEscalationRepository();
            repository.Add(new FactionEscalation("faction1", 500));
            repository.Add(new FactionEscalation("faction2", 1500));
            repository.Add(new FactionEscalation("faction3", 3500));
            var service = new EscalationService(repository);

            var result = service.GetAllEscalations();

            Assert.Equal(3, ((List<FactionEscalation>)result).Count);
        }

        #endregion

        #region GetFactionsAtTier Tests

        [Fact]
        public void GetFactionsAtTier_ReturnsFactionssAtSpecifiedTier()
        {
            var repository = new InMemoryEscalationRepository();
            repository.Add(new FactionEscalation("faction1", 500));  // Tier1
            repository.Add(new FactionEscalation("faction2", 1500)); // Tier2
            repository.Add(new FactionEscalation("faction3", 1200)); // Tier2
            repository.Add(new FactionEscalation("faction4", 3500)); // Tier3
            var service = new EscalationService(repository);

            var result = service.GetFactionsAtTier(EscalationTier.Tier2);

            Assert.Equal(2, ((List<FactionEscalation>)result).Count);
        }

        [Fact]
        public void GetFactionsAtTier_WithNoMatchingFactions_ReturnsEmpty()
        {
            var repository = new InMemoryEscalationRepository();
            repository.Add(new FactionEscalation("faction1", 500));
            var service = new EscalationService(repository);

            var result = service.GetFactionsAtTier(EscalationTier.Tier5);

            Assert.Empty(result);
        }

        #endregion

        #region GetProgressToNextTier Tests

        [Fact]
        public void GetProgressToNextTier_WithNullFactionId_ThrowsArgumentNullException()
        {
            var service = CreateService();
            Assert.Throws<ArgumentNullException>(() => service.GetProgressToNextTier(null!));
        }

        [Fact]
        public void GetProgressToNextTier_WithValidFaction_ReturnsProgress()
        {
            var repository = new InMemoryEscalationRepository();
            repository.Add(new FactionEscalation("faction1", 500));
            var service = new EscalationService(repository);

            var result = service.GetProgressToNextTier("faction1");

            // 500 / 1000 = 50%
            Assert.Equal(50f, result, 0.01f);
        }

        [Fact]
        public void GetProgressToNextTier_WithFactionAtMaxTier_Returns100()
        {
            var repository = new InMemoryEscalationRepository();
            repository.Add(new FactionEscalation("faction1", 9500));
            var service = new EscalationService(repository);

            var result = service.GetProgressToNextTier("faction1");

            Assert.Equal(100f, result);
        }

        [Fact]
        public void GetProgressToNextTier_WithNonExistentFaction_ReturnsZero()
        {
            var service = CreateService();
            var result = service.GetProgressToNextTier("nonexistent");
            Assert.Equal(0f, result);
        }

        #endregion

        #region GetPointsToNextTier Tests

        [Fact]
        public void GetPointsToNextTier_WithNullFactionId_ThrowsArgumentNullException()
        {
            var service = CreateService();
            Assert.Throws<ArgumentNullException>(() => service.GetPointsToNextTier(null!));
        }

        [Fact]
        public void GetPointsToNextTier_WithValidFaction_ReturnsPoints()
        {
            var repository = new InMemoryEscalationRepository();
            repository.Add(new FactionEscalation("faction1", 500));
            var service = new EscalationService(repository);

            var result = service.GetPointsToNextTier("faction1");

            // 1000 - 500 = 500
            Assert.Equal(500, result);
        }

        [Fact]
        public void GetPointsToNextTier_WithFactionAtMaxTier_ReturnsZero()
        {
            var repository = new InMemoryEscalationRepository();
            repository.Add(new FactionEscalation("faction1", 9500));
            var service = new EscalationService(repository);

            var result = service.GetPointsToNextTier("faction1");

            Assert.Equal(0, result);
        }

        [Fact]
        public void GetPointsToNextTier_WithNonExistentFaction_ReturnsMaxInt()
        {
            var service = CreateService();
            var result = service.GetPointsToNextTier("nonexistent");
            Assert.Equal(int.MaxValue, result);
        }

        #endregion

        #region ResetEscalation Tests

        [Fact]
        public void ResetEscalation_WithNullFactionId_ThrowsArgumentNullException()
        {
            var service = CreateService();
            Assert.Throws<ArgumentNullException>(() => service.ResetEscalation(null!));
        }

        [Fact]
        public void ResetEscalation_WithValidFaction_ResetsToZero()
        {
            var repository = new InMemoryEscalationRepository();
            repository.Add(new FactionEscalation("faction1", 5000));
            var service = new EscalationService(repository);

            var result = service.ResetEscalation("faction1");

            Assert.True(result);
            Assert.Equal(0, repository.GetByFactionId("faction1")!.Points);
            Assert.Equal(EscalationTier.Tier1, repository.GetByFactionId("faction1")!.CurrentTier);
        }

        [Fact]
        public void ResetEscalation_WithNonExistentFaction_ReturnsFalse()
        {
            var service = CreateService();
            var result = service.ResetEscalation("nonexistent");
            Assert.False(result);
        }

        #endregion

        #region InitializeEscalation Tests

        [Fact]
        public void InitializeEscalation_WithNullFactionId_ThrowsArgumentNullException()
        {
            var service = CreateService();
            Assert.Throws<ArgumentNullException>(() => service.InitializeEscalation(null!));
        }

        [Fact]
        public void InitializeEscalation_WithNewFaction_CreatesEscalation()
        {
            var repository = new InMemoryEscalationRepository();
            var service = new EscalationService(repository);

            var result = service.InitializeEscalation("newfaction");

            Assert.True(result);
            Assert.NotNull(repository.GetByFactionId("newfaction"));
            Assert.Equal(0, repository.GetByFactionId("newfaction")!.Points);
        }

        [Fact]
        public void InitializeEscalation_WithExistingFaction_ReturnsFalse()
        {
            var repository = new InMemoryEscalationRepository();
            repository.Add(new FactionEscalation("faction1", 500));
            var service = new EscalationService(repository);

            var result = service.InitializeEscalation("faction1");

            Assert.False(result);
            Assert.Equal(500, repository.GetByFactionId("faction1")!.Points);
        }

        [Fact]
        public void InitializeEscalation_WithInitialPoints_CreatesWithPoints()
        {
            var repository = new InMemoryEscalationRepository();
            var service = new EscalationService(repository);

            var result = service.InitializeEscalation("newfaction", 1500);

            Assert.True(result);
            Assert.Equal(1500, repository.GetByFactionId("newfaction")!.Points);
            Assert.Equal(EscalationTier.Tier2, repository.GetByFactionId("newfaction")!.CurrentTier);
        }

        #endregion

        #region RemoveEscalation Tests

        [Fact]
        public void RemoveEscalation_WithNullFactionId_ThrowsArgumentNullException()
        {
            var service = CreateService();
            Assert.Throws<ArgumentNullException>(() => service.RemoveEscalation(null!));
        }

        [Fact]
        public void RemoveEscalation_WithExistingFaction_RemovesAndReturnsTrue()
        {
            var repository = new InMemoryEscalationRepository();
            repository.Add(new FactionEscalation("faction1", 500));
            var service = new EscalationService(repository);

            var result = service.RemoveEscalation("faction1");

            Assert.True(result);
            Assert.Null(repository.GetByFactionId("faction1"));
        }

        [Fact]
        public void RemoveEscalation_WithNonExistentFaction_ReturnsFalse()
        {
            var service = CreateService();
            var result = service.RemoveEscalation("nonexistent");
            Assert.False(result);
        }

        #endregion

        #region Helper Methods

        private EscalationService CreateService(bool autoCreateEscalation = false)
        {
            var repository = new InMemoryEscalationRepository();
            return new EscalationService(repository, autoCreateEscalation);
        }

        #endregion
    }
}
