using System;
using System.Collections.Generic;
using FactionWars.Lieutenants.Interfaces;
using FactionWars.Lieutenants.Models;
using FactionWars.Lieutenants.Services;
using Moq;
using Xunit;

namespace FactionWars.Tests.Unit.Lieutenants
{
    /// <summary>
    /// Tests for the FlipMissionService class.
    /// </summary>
    public class FlipMissionServiceTests
    {
        #region Constructor

        [Fact]
        public void Constructor_WithNullDefectionService_ThrowsArgumentNullException()
        {
            // Arrange
            var mockRandom = new Mock<IRandomProvider>();

            // Act & Assert
            var ex = Assert.Throws<ArgumentNullException>(() =>
                new FlipMissionService(null!, mockRandom.Object));
            Assert.Equal("defectionService", ex.ParamName);
        }

        [Fact]
        public void Constructor_WithNullRandomProvider_ThrowsArgumentNullException()
        {
            // Arrange
            var mockDefection = new Mock<IDefectionService>();

            // Act & Assert
            var ex = Assert.Throws<ArgumentNullException>(() =>
                new FlipMissionService(mockDefection.Object, null!));
            Assert.Equal("randomProvider", ex.ParamName);
        }

        [Fact]
        public void Constructor_WithValidDependencies_CreatesInstance()
        {
            // Arrange
            var mockDefection = new Mock<IDefectionService>();
            var mockRandom = new Mock<IRandomProvider>();

            // Act
            var service = new FlipMissionService(mockDefection.Object, mockRandom.Object);

            // Assert
            Assert.NotNull(service);
        }

        #endregion

        #region CreateMission

        [Fact]
        public void CreateMission_WithNullLieutenant_ThrowsArgumentNullException()
        {
            // Arrange
            var mockDefection = new Mock<IDefectionService>();
            var mockRandom = new Mock<IRandomProvider>();
            var service = new FlipMissionService(mockDefection.Object, mockRandom.Object);

            // Act & Assert
            var ex = Assert.Throws<ArgumentNullException>(() =>
                service.CreateMission(null!, "faction_player"));
            Assert.Equal("targetLieutenant", ex.ParamName);
        }

        [Fact]
        public void CreateMission_WithNullInitiatorFaction_ThrowsArgumentNullException()
        {
            // Arrange
            var mockDefection = new Mock<IDefectionService>();
            var mockRandom = new Mock<IRandomProvider>();
            var service = new FlipMissionService(mockDefection.Object, mockRandom.Object);
            var lieutenant = new Lieutenant("lt_001", "Gustavo", "faction_enemy");

            // Act & Assert
            var ex = Assert.Throws<ArgumentNullException>(() =>
                service.CreateMission(lieutenant, null!));
            Assert.Equal("initiatorFactionId", ex.ParamName);
        }

        [Fact]
        public void CreateMission_WithValidParameters_ReturnsMission()
        {
            // Arrange
            var mockDefection = new Mock<IDefectionService>();
            var mockRandom = new Mock<IRandomProvider>();
            var service = new FlipMissionService(mockDefection.Object, mockRandom.Object);
            var lieutenant = new Lieutenant("lt_001", "Gustavo", "faction_enemy");

            // Act
            var mission = service.CreateMission(lieutenant, "faction_player");

            // Assert
            Assert.NotNull(mission);
            Assert.Equal("lt_001", mission.TargetLieutenantId);
            Assert.Equal("faction_player", mission.InitiatorFactionId);
        }

        [Fact]
        public void CreateMission_WithBribeAmount_SetsBribeOnMission()
        {
            // Arrange
            var mockDefection = new Mock<IDefectionService>();
            var mockRandom = new Mock<IRandomProvider>();
            var service = new FlipMissionService(mockDefection.Object, mockRandom.Object);
            var lieutenant = new Lieutenant("lt_001", "Gustavo", "faction_enemy");

            // Act
            var mission = service.CreateMission(lieutenant, "faction_player", bribeAmount: 50000);

            // Assert
            Assert.Equal(50000, mission.BribeAmount);
        }

        [Fact]
        public void CreateMission_TracksMission()
        {
            // Arrange
            var mockDefection = new Mock<IDefectionService>();
            var mockRandom = new Mock<IRandomProvider>();
            var service = new FlipMissionService(mockDefection.Object, mockRandom.Object);
            var lieutenant = new Lieutenant("lt_001", "Gustavo", "faction_enemy");

            // Act
            var mission = service.CreateMission(lieutenant, "faction_player");
            var activeMissions = service.GetActiveMissions();

            // Assert
            Assert.Contains(mission, activeMissions);
        }

        #endregion

        #region StartMission

        [Fact]
        public void StartMission_WithNullMission_ThrowsArgumentNullException()
        {
            // Arrange
            var mockDefection = new Mock<IDefectionService>();
            var mockRandom = new Mock<IRandomProvider>();
            var service = new FlipMissionService(mockDefection.Object, mockRandom.Object);

            // Act & Assert
            var ex = Assert.Throws<ArgumentNullException>(() =>
                service.StartMission(null!));
            Assert.Equal("mission", ex.ParamName);
        }

        [Fact]
        public void StartMission_WithValidMission_SetsMissionToInProgress()
        {
            // Arrange
            var mockDefection = new Mock<IDefectionService>();
            var mockRandom = new Mock<IRandomProvider>();
            var service = new FlipMissionService(mockDefection.Object, mockRandom.Object);
            var lieutenant = new Lieutenant("lt_001", "Gustavo", "faction_enemy");
            var mission = service.CreateMission(lieutenant, "faction_player");

            // Act
            service.StartMission(mission);

            // Assert
            Assert.Equal(FlipMissionStatus.InProgress, mission.Status);
        }

        #endregion

        #region ExecuteMission

        [Fact]
        public void ExecuteMission_WithNullMission_ThrowsArgumentNullException()
        {
            // Arrange
            var mockDefection = new Mock<IDefectionService>();
            var mockRandom = new Mock<IRandomProvider>();
            var service = new FlipMissionService(mockDefection.Object, mockRandom.Object);
            var lieutenant = new Lieutenant("lt_001", "Gustavo", "faction_enemy");

            // Act & Assert
            var ex = Assert.Throws<ArgumentNullException>(() =>
                service.ExecuteMission(null!, lieutenant));
            Assert.Equal("mission", ex.ParamName);
        }

        [Fact]
        public void ExecuteMission_WithNullLieutenant_ThrowsArgumentNullException()
        {
            // Arrange
            var mockDefection = new Mock<IDefectionService>();
            var mockRandom = new Mock<IRandomProvider>();
            var service = new FlipMissionService(mockDefection.Object, mockRandom.Object);
            var lieutenant = new Lieutenant("lt_001", "Gustavo", "faction_enemy");
            var mission = service.CreateMission(lieutenant, "faction_player");
            service.StartMission(mission);

            // Act & Assert
            var ex = Assert.Throws<ArgumentNullException>(() =>
                service.ExecuteMission(mission, null!));
            Assert.Equal("targetLieutenant", ex.ParamName);
        }

        [Fact]
        public void ExecuteMission_WhenNotStarted_ThrowsInvalidOperationException()
        {
            // Arrange
            var mockDefection = new Mock<IDefectionService>();
            var mockRandom = new Mock<IRandomProvider>();
            var service = new FlipMissionService(mockDefection.Object, mockRandom.Object);
            var lieutenant = new Lieutenant("lt_001", "Gustavo", "faction_enemy");
            var mission = service.CreateMission(lieutenant, "faction_player");

            // Act & Assert
            Assert.Throws<InvalidOperationException>(() =>
                service.ExecuteMission(mission, lieutenant));
        }

        [Fact]
        public void ExecuteMission_WithMismatchedLieutenant_ThrowsArgumentException()
        {
            // Arrange
            var mockDefection = new Mock<IDefectionService>();
            var mockRandom = new Mock<IRandomProvider>();
            var service = new FlipMissionService(mockDefection.Object, mockRandom.Object);
            var lieutenant1 = new Lieutenant("lt_001", "Gustavo", "faction_enemy");
            var lieutenant2 = new Lieutenant("lt_002", "Carlos", "faction_enemy");
            var mission = service.CreateMission(lieutenant1, "faction_player");
            service.StartMission(mission);

            // Act & Assert
            var ex = Assert.Throws<ArgumentException>(() =>
                service.ExecuteMission(mission, lieutenant2));
            Assert.Contains("does not match", ex.Message);
        }

        [Fact]
        public void ExecuteMission_Success_ReturnsTrueOutcome()
        {
            // Arrange
            var mockDefection = new Mock<IDefectionService>();
            var mockRandom = new Mock<IRandomProvider>();

            // Configure defection to succeed
            mockDefection.Setup(d => d.AttemptDefection(
                It.IsAny<Lieutenant>(),
                It.IsAny<string>(),
                It.IsAny<int>()))
                .Returns(DefectionResult.Succeeded(0.8, 0.5));

            // Detection roll fails (not detected)
            mockRandom.Setup(r => r.NextDouble()).Returns(0.9);

            var service = new FlipMissionService(mockDefection.Object, mockRandom.Object);
            var lieutenant = new Lieutenant("lt_001", "Gustavo", "faction_enemy");
            var mission = service.CreateMission(lieutenant, "faction_player");
            service.StartMission(mission);

            // Act
            var outcome = service.ExecuteMission(mission, lieutenant);

            // Assert
            Assert.True(outcome.Success);
            Assert.Equal(FlipMissionStatus.Succeeded, mission.Status);
        }

        [Fact]
        public void ExecuteMission_DefectionFails_ReturnsFalseOutcome()
        {
            // Arrange
            var mockDefection = new Mock<IDefectionService>();
            var mockRandom = new Mock<IRandomProvider>();

            // Configure defection to fail
            mockDefection.Setup(d => d.AttemptDefection(
                It.IsAny<Lieutenant>(),
                It.IsAny<string>(),
                It.IsAny<int>()))
                .Returns(DefectionResult.Failed(0.3, 0.5, "Lieutenant refused"));

            // Detection roll fails (not detected)
            mockRandom.Setup(r => r.NextDouble()).Returns(0.9);

            var service = new FlipMissionService(mockDefection.Object, mockRandom.Object);
            var lieutenant = new Lieutenant("lt_001", "Gustavo", "faction_enemy");
            var mission = service.CreateMission(lieutenant, "faction_player");
            service.StartMission(mission);

            // Act
            var outcome = service.ExecuteMission(mission, lieutenant);

            // Assert
            Assert.False(outcome.Success);
            Assert.Equal(FlipMissionStatus.Failed, mission.Status);
        }

        [Fact]
        public void ExecuteMission_Detected_SetsDetectedStatus()
        {
            // Arrange
            var mockDefection = new Mock<IDefectionService>();
            var mockRandom = new Mock<IRandomProvider>();

            // Configure defection to fail
            mockDefection.Setup(d => d.AttemptDefection(
                It.IsAny<Lieutenant>(),
                It.IsAny<string>(),
                It.IsAny<int>()))
                .Returns(DefectionResult.Failed(0.3, 0.5, "Lieutenant refused"));

            // Detection roll succeeds (detected) - low roll below detection chance
            mockRandom.Setup(r => r.NextDouble()).Returns(0.1);

            var service = new FlipMissionService(mockDefection.Object, mockRandom.Object);
            var lieutenant = new Lieutenant("lt_001", "Gustavo", "faction_enemy");
            var mission = service.CreateMission(lieutenant, "faction_player");
            service.StartMission(mission);

            // Act
            var outcome = service.ExecuteMission(mission, lieutenant);

            // Assert
            Assert.True(outcome.Detected);
            Assert.Equal(FlipMissionStatus.Detected, mission.Status);
        }

        [Fact]
        public void ExecuteMission_SuccessButDetected_SetsBothFlags()
        {
            // Arrange
            var mockDefection = new Mock<IDefectionService>();
            var mockRandom = new Mock<IRandomProvider>();

            // Configure defection to succeed
            mockDefection.Setup(d => d.AttemptDefection(
                It.IsAny<Lieutenant>(),
                It.IsAny<string>(),
                It.IsAny<int>()))
                .Returns(DefectionResult.Succeeded(0.8, 0.5));

            // Detection roll succeeds (detected)
            mockRandom.Setup(r => r.NextDouble()).Returns(0.1);

            var service = new FlipMissionService(mockDefection.Object, mockRandom.Object);
            var lieutenant = new Lieutenant("lt_001", "Gustavo", "faction_enemy");
            var mission = service.CreateMission(lieutenant, "faction_player");
            service.StartMission(mission);

            // Act
            var outcome = service.ExecuteMission(mission, lieutenant);

            // Assert
            Assert.True(outcome.Success);
            Assert.True(outcome.Detected);
            Assert.True(mission.WasSuccessful);
            Assert.True(mission.WasDetected);
        }

        [Fact]
        public void ExecuteMission_UsesBribeAmountInDefectionAttempt()
        {
            // Arrange
            var mockDefection = new Mock<IDefectionService>();
            var mockRandom = new Mock<IRandomProvider>();

            mockDefection.Setup(d => d.AttemptDefection(
                It.IsAny<Lieutenant>(),
                "faction_player",
                50000))
                .Returns(DefectionResult.Succeeded(0.95, 0.5));

            mockRandom.Setup(r => r.NextDouble()).Returns(0.9);

            var service = new FlipMissionService(mockDefection.Object, mockRandom.Object);
            var lieutenant = new Lieutenant("lt_001", "Gustavo", "faction_enemy");
            var mission = service.CreateMission(lieutenant, "faction_player", bribeAmount: 50000);
            service.StartMission(mission);

            // Act
            service.ExecuteMission(mission, lieutenant);

            // Assert
            mockDefection.Verify(d => d.AttemptDefection(
                lieutenant,
                "faction_player",
                50000), Times.Once);
        }

        [Fact]
        public void ExecuteMission_RemovesMissionFromActive()
        {
            // Arrange
            var mockDefection = new Mock<IDefectionService>();
            var mockRandom = new Mock<IRandomProvider>();

            mockDefection.Setup(d => d.AttemptDefection(
                It.IsAny<Lieutenant>(),
                It.IsAny<string>(),
                It.IsAny<int>()))
                .Returns(DefectionResult.Succeeded(0.8, 0.5));

            mockRandom.Setup(r => r.NextDouble()).Returns(0.9);

            var service = new FlipMissionService(mockDefection.Object, mockRandom.Object);
            var lieutenant = new Lieutenant("lt_001", "Gustavo", "faction_enemy");
            var mission = service.CreateMission(lieutenant, "faction_player");
            service.StartMission(mission);

            // Act
            service.ExecuteMission(mission, lieutenant);
            var activeMissions = service.GetActiveMissions();

            // Assert
            Assert.DoesNotContain(mission, activeMissions);
        }

        [Fact]
        public void ExecuteMission_AddsToCompletedMissions()
        {
            // Arrange
            var mockDefection = new Mock<IDefectionService>();
            var mockRandom = new Mock<IRandomProvider>();

            mockDefection.Setup(d => d.AttemptDefection(
                It.IsAny<Lieutenant>(),
                It.IsAny<string>(),
                It.IsAny<int>()))
                .Returns(DefectionResult.Succeeded(0.8, 0.5));

            mockRandom.Setup(r => r.NextDouble()).Returns(0.9);

            var service = new FlipMissionService(mockDefection.Object, mockRandom.Object);
            var lieutenant = new Lieutenant("lt_001", "Gustavo", "faction_enemy");
            var mission = service.CreateMission(lieutenant, "faction_player");
            service.StartMission(mission);

            // Act
            service.ExecuteMission(mission, lieutenant);
            var completedMissions = service.GetCompletedMissions();

            // Assert
            Assert.Contains(mission, completedMissions);
        }

        #endregion

        #region CancelMission

        [Fact]
        public void CancelMission_WithNullMission_ThrowsArgumentNullException()
        {
            // Arrange
            var mockDefection = new Mock<IDefectionService>();
            var mockRandom = new Mock<IRandomProvider>();
            var service = new FlipMissionService(mockDefection.Object, mockRandom.Object);

            // Act & Assert
            var ex = Assert.Throws<ArgumentNullException>(() =>
                service.CancelMission(null!));
            Assert.Equal("mission", ex.ParamName);
        }

        [Fact]
        public void CancelMission_WhenPending_CancelsMission()
        {
            // Arrange
            var mockDefection = new Mock<IDefectionService>();
            var mockRandom = new Mock<IRandomProvider>();
            var service = new FlipMissionService(mockDefection.Object, mockRandom.Object);
            var lieutenant = new Lieutenant("lt_001", "Gustavo", "faction_enemy");
            var mission = service.CreateMission(lieutenant, "faction_player");

            // Act
            service.CancelMission(mission);

            // Assert
            Assert.Equal(FlipMissionStatus.Cancelled, mission.Status);
        }

        [Fact]
        public void CancelMission_WhenInProgress_CancelsMission()
        {
            // Arrange
            var mockDefection = new Mock<IDefectionService>();
            var mockRandom = new Mock<IRandomProvider>();
            var service = new FlipMissionService(mockDefection.Object, mockRandom.Object);
            var lieutenant = new Lieutenant("lt_001", "Gustavo", "faction_enemy");
            var mission = service.CreateMission(lieutenant, "faction_player");
            service.StartMission(mission);

            // Act
            service.CancelMission(mission);

            // Assert
            Assert.Equal(FlipMissionStatus.Cancelled, mission.Status);
        }

        [Fact]
        public void CancelMission_RemovesMissionFromActive()
        {
            // Arrange
            var mockDefection = new Mock<IDefectionService>();
            var mockRandom = new Mock<IRandomProvider>();
            var service = new FlipMissionService(mockDefection.Object, mockRandom.Object);
            var lieutenant = new Lieutenant("lt_001", "Gustavo", "faction_enemy");
            var mission = service.CreateMission(lieutenant, "faction_player");

            // Act
            service.CancelMission(mission);
            var activeMissions = service.GetActiveMissions();

            // Assert
            Assert.DoesNotContain(mission, activeMissions);
        }

        #endregion

        #region CanCreateMission

        [Fact]
        public void CanCreateMission_WithNullLieutenant_ReturnsFalse()
        {
            // Arrange
            var mockDefection = new Mock<IDefectionService>();
            var mockRandom = new Mock<IRandomProvider>();
            var service = new FlipMissionService(mockDefection.Object, mockRandom.Object);

            // Act
            var result = service.CanCreateMission(null!, "faction_player");

            // Assert
            Assert.False(result);
        }

        [Fact]
        public void CanCreateMission_WithNullFaction_ReturnsFalse()
        {
            // Arrange
            var mockDefection = new Mock<IDefectionService>();
            var mockRandom = new Mock<IRandomProvider>();
            var service = new FlipMissionService(mockDefection.Object, mockRandom.Object);
            var lieutenant = new Lieutenant("lt_001", "Gustavo", "faction_enemy");

            // Act
            var result = service.CanCreateMission(lieutenant, null!);

            // Assert
            Assert.False(result);
        }

        [Fact]
        public void CanCreateMission_WithSameFaction_ReturnsFalse()
        {
            // Arrange
            var mockDefection = new Mock<IDefectionService>();
            var mockRandom = new Mock<IRandomProvider>();
            var service = new FlipMissionService(mockDefection.Object, mockRandom.Object);
            var lieutenant = new Lieutenant("lt_001", "Gustavo", "faction_player");

            // Act
            var result = service.CanCreateMission(lieutenant, "faction_player");

            // Assert
            Assert.False(result);
        }

        [Fact]
        public void CanCreateMission_WithDeceasedLieutenant_ReturnsFalse()
        {
            // Arrange
            var mockDefection = new Mock<IDefectionService>();
            var mockRandom = new Mock<IRandomProvider>();
            var service = new FlipMissionService(mockDefection.Object, mockRandom.Object);
            var lieutenant = new Lieutenant("lt_001", "Gustavo", "faction_enemy");
            lieutenant.Kill();

            // Act
            var result = service.CanCreateMission(lieutenant, "faction_player");

            // Assert
            Assert.False(result);
        }

        [Fact]
        public void CanCreateMission_WithExistingActiveMission_ReturnsFalse()
        {
            // Arrange
            var mockDefection = new Mock<IDefectionService>();
            var mockRandom = new Mock<IRandomProvider>();
            var service = new FlipMissionService(mockDefection.Object, mockRandom.Object);
            var lieutenant = new Lieutenant("lt_001", "Gustavo", "faction_enemy");

            // Create first mission
            service.CreateMission(lieutenant, "faction_player");

            // Act - try to create another mission for the same lieutenant
            var result = service.CanCreateMission(lieutenant, "faction_player");

            // Assert
            Assert.False(result);
        }

        [Fact]
        public void CanCreateMission_WithValidParameters_ReturnsTrue()
        {
            // Arrange
            var mockDefection = new Mock<IDefectionService>();
            var mockRandom = new Mock<IRandomProvider>();
            var service = new FlipMissionService(mockDefection.Object, mockRandom.Object);
            var lieutenant = new Lieutenant("lt_001", "Gustavo", "faction_enemy");

            // Act
            var result = service.CanCreateMission(lieutenant, "faction_player");

            // Assert
            Assert.True(result);
        }

        [Fact]
        public void CanCreateMission_WithCapturedLieutenantByInitiator_ReturnsTrue()
        {
            // Arrange
            var mockDefection = new Mock<IDefectionService>();
            var mockRandom = new Mock<IRandomProvider>();
            var service = new FlipMissionService(mockDefection.Object, mockRandom.Object);
            var lieutenant = new Lieutenant("lt_001", "Gustavo", "faction_enemy");
            lieutenant.Capture("faction_player");

            // Act
            var result = service.CanCreateMission(lieutenant, "faction_player");

            // Assert
            Assert.True(result);
        }

        [Fact]
        public void CanCreateMission_WithCapturedLieutenantByOtherFaction_ReturnsFalse()
        {
            // Arrange
            var mockDefection = new Mock<IDefectionService>();
            var mockRandom = new Mock<IRandomProvider>();
            var service = new FlipMissionService(mockDefection.Object, mockRandom.Object);
            var lieutenant = new Lieutenant("lt_001", "Gustavo", "faction_enemy");
            lieutenant.Capture("faction_third");

            // Act
            var result = service.CanCreateMission(lieutenant, "faction_player");

            // Assert
            Assert.False(result);
        }

        #endregion

        #region GetMissionsForFaction

        [Fact]
        public void GetMissionsForFaction_WithNullFactionId_ReturnsEmptyList()
        {
            // Arrange
            var mockDefection = new Mock<IDefectionService>();
            var mockRandom = new Mock<IRandomProvider>();
            var service = new FlipMissionService(mockDefection.Object, mockRandom.Object);

            // Act
            var result = service.GetMissionsForFaction(null!);

            // Assert
            Assert.Empty(result);
        }

        [Fact]
        public void GetMissionsForFaction_ReturnsOnlyMissionsInvolvingFaction()
        {
            // Arrange
            var mockDefection = new Mock<IDefectionService>();
            var mockRandom = new Mock<IRandomProvider>();
            var service = new FlipMissionService(mockDefection.Object, mockRandom.Object);

            var lieutenant1 = new Lieutenant("lt_001", "Gustavo", "faction_enemy1");
            var lieutenant2 = new Lieutenant("lt_002", "Carlos", "faction_enemy2");

            var mission1 = service.CreateMission(lieutenant1, "faction_player");
            var mission2 = service.CreateMission(lieutenant2, "faction_other");

            // Act
            var result = service.GetMissionsForFaction("faction_player");

            // Assert
            Assert.Single(result);
            Assert.Contains(mission1, result);
            Assert.DoesNotContain(mission2, result);
        }

        [Fact]
        public void GetMissionsForFaction_IncludesTargetFactionMissions()
        {
            // Arrange
            var mockDefection = new Mock<IDefectionService>();
            var mockRandom = new Mock<IRandomProvider>();
            var service = new FlipMissionService(mockDefection.Object, mockRandom.Object);

            var lieutenant = new Lieutenant("lt_001", "Gustavo", "faction_target");
            var mission = service.CreateMission(lieutenant, "faction_player");

            // Act - query from target faction's perspective
            var result = service.GetMissionsForFaction("faction_target");

            // Assert
            Assert.Single(result);
            Assert.Contains(mission, result);
        }

        #endregion

        #region GetMissionByLieutenant

        [Fact]
        public void GetMissionByLieutenant_WithNullLieutenantId_ReturnsNull()
        {
            // Arrange
            var mockDefection = new Mock<IDefectionService>();
            var mockRandom = new Mock<IRandomProvider>();
            var service = new FlipMissionService(mockDefection.Object, mockRandom.Object);

            // Act
            var result = service.GetMissionByLieutenant(null!);

            // Assert
            Assert.Null(result);
        }

        [Fact]
        public void GetMissionByLieutenant_WithNoActiveMission_ReturnsNull()
        {
            // Arrange
            var mockDefection = new Mock<IDefectionService>();
            var mockRandom = new Mock<IRandomProvider>();
            var service = new FlipMissionService(mockDefection.Object, mockRandom.Object);

            // Act
            var result = service.GetMissionByLieutenant("lt_001");

            // Assert
            Assert.Null(result);
        }

        [Fact]
        public void GetMissionByLieutenant_WithActiveMission_ReturnsMission()
        {
            // Arrange
            var mockDefection = new Mock<IDefectionService>();
            var mockRandom = new Mock<IRandomProvider>();
            var service = new FlipMissionService(mockDefection.Object, mockRandom.Object);
            var lieutenant = new Lieutenant("lt_001", "Gustavo", "faction_enemy");
            var mission = service.CreateMission(lieutenant, "faction_player");

            // Act
            var result = service.GetMissionByLieutenant("lt_001");

            // Assert
            Assert.Same(mission, result);
        }

        #endregion

        #region EstimateMissionCost

        [Fact]
        public void EstimateMissionCost_WithNullLieutenant_ThrowsArgumentNullException()
        {
            // Arrange
            var mockDefection = new Mock<IDefectionService>();
            var mockRandom = new Mock<IRandomProvider>();
            var service = new FlipMissionService(mockDefection.Object, mockRandom.Object);

            // Act & Assert
            var ex = Assert.Throws<ArgumentNullException>(() =>
                service.EstimateMissionCost(null!));
            Assert.Equal("lieutenant", ex.ParamName);
        }

        [Fact]
        public void EstimateMissionCost_ReturnsPositiveValue()
        {
            // Arrange
            var mockDefection = new Mock<IDefectionService>();
            var mockRandom = new Mock<IRandomProvider>();
            var service = new FlipMissionService(mockDefection.Object, mockRandom.Object);
            var lieutenant = new Lieutenant("lt_001", "Gustavo", "faction_enemy");

            // Act
            var cost = service.EstimateMissionCost(lieutenant);

            // Assert
            Assert.True(cost > 0);
        }

        [Fact]
        public void EstimateMissionCost_WithHighLevelLieutenant_ReturnsHigherCost()
        {
            // Arrange
            var mockDefection = new Mock<IDefectionService>();
            var mockRandom = new Mock<IRandomProvider>();
            var service = new FlipMissionService(mockDefection.Object, mockRandom.Object);

            var lowLevelLieutenant = new Lieutenant("lt_001", "Gustavo", "faction_enemy");
            var highLevelLieutenant = new Lieutenant("lt_002", "Carlos", "faction_enemy");
            highLevelLieutenant.GainExperience(5000); // Level up

            // Act
            var lowLevelCost = service.EstimateMissionCost(lowLevelLieutenant);
            var highLevelCost = service.EstimateMissionCost(highLevelLieutenant);

            // Assert
            Assert.True(highLevelCost > lowLevelCost);
        }

        #endregion

        #region GetRecommendedBribe

        [Fact]
        public void GetRecommendedBribe_WithNullLieutenant_ThrowsArgumentNullException()
        {
            // Arrange
            var mockDefection = new Mock<IDefectionService>();
            var mockRandom = new Mock<IRandomProvider>();
            var service = new FlipMissionService(mockDefection.Object, mockRandom.Object);

            // Act & Assert
            var ex = Assert.Throws<ArgumentNullException>(() =>
                service.GetRecommendedBribe(null!));
            Assert.Equal("lieutenant", ex.ParamName);
        }

        [Fact]
        public void GetRecommendedBribe_CallsDefectionService()
        {
            // Arrange
            var mockDefection = new Mock<IDefectionService>();
            var mockRandom = new Mock<IRandomProvider>();

            mockDefection.Setup(d => d.GetRequiredBribeForGuaranteedDefection(It.IsAny<Lieutenant>()))
                .Returns(100000);

            var service = new FlipMissionService(mockDefection.Object, mockRandom.Object);
            var lieutenant = new Lieutenant("lt_001", "Gustavo", "faction_enemy");

            // Act
            var bribe = service.GetRecommendedBribe(lieutenant);

            // Assert
            mockDefection.Verify(d => d.GetRequiredBribeForGuaranteedDefection(lieutenant), Times.Once);
            Assert.Equal(100000, bribe);
        }

        #endregion
    }
}
