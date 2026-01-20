//using System;
//using FactionWars.Core.Interfaces;
//using FactionWars.Core.Models;
//using FactionWars.Factions.Interfaces;
//using FactionWars.Factions.Models;
//using FactionWars.ScriptHookV.Managers;
//using FactionWars.UI.Interfaces;
//using FactionWars.UI.Models;
//using Moq;
//using Xunit;

//namespace FactionWars.Tests.Unit.ScriptHookV
//{
//    /// <summary>
//    /// Tests for the VictoryManager which coordinates victory detection and display.
//    /// </summary>
//    public class VictoryManagerTests
//    {
//        private readonly Mock<IVictoryConditionService> _mockVictoryService;
//        private readonly Mock<INotificationService> _mockNotificationService;
//        private readonly Mock<IFactionService> _mockFactionService;
//        private readonly VictoryManager _victoryManager;

//        public VictoryManagerTests()
//        {
//            _mockVictoryService = new Mock<IVictoryConditionService>();
//            _mockNotificationService = new Mock<INotificationService>();
//            _mockFactionService = new Mock<IFactionService>();
//            _victoryManager = new VictoryManager(
//                _mockVictoryService.Object,
//                _mockNotificationService.Object,
//                _mockFactionService.Object);
//        }

//        #region Constructor Tests

//        [Fact]
//        public void Constructor_WithValidDependencies_CreatesInstance()
//        {
//            // Arrange & Act
//            var manager = new VictoryManager(
//                _mockVictoryService.Object,
//                _mockNotificationService.Object,
//                _mockFactionService.Object);

//            // Assert
//            Assert.NotNull(manager);
//        }

//        [Fact]
//        public void Constructor_WithNullVictoryConditionService_ThrowsArgumentNullException()
//        {
//            // Act & Assert
//            var exception = Assert.Throws<ArgumentNullException>(() =>
//                new VictoryManager(null!, _mockNotificationService.Object, _mockFactionService.Object));
//            Assert.Equal("victoryConditionService", exception.ParamName);
//        }

//        [Fact]
//        public void Constructor_WithNullNotificationService_ThrowsArgumentNullException()
//        {
//            // Act & Assert
//            var exception = Assert.Throws<ArgumentNullException>(() =>
//                new VictoryManager(_mockVictoryService.Object, null!, _mockFactionService.Object));
//            Assert.Equal("notificationService", exception.ParamName);
//        }

//        [Fact]
//        public void Constructor_WithNullFactionService_ThrowsArgumentNullException()
//        {
//            // Act & Assert
//            var exception = Assert.Throws<ArgumentNullException>(() =>
//                new VictoryManager(_mockVictoryService.Object, _mockNotificationService.Object, null!));
//            Assert.Equal("factionService", exception.ParamName);
//        }

//        #endregion

//        #region Start/Stop Tests

//        [Fact]
//        public void Start_SetsIsRunningToTrue()
//        {
//            // Act
//            _victoryManager.Start();

//            // Assert
//            Assert.True(_victoryManager.IsRunning);
//        }

//        [Fact]
//        public void Stop_SetsIsRunningToFalse()
//        {
//            // Arrange
//            _victoryManager.Start();

//            // Act
//            _victoryManager.Stop();

//            // Assert
//            Assert.False(_victoryManager.IsRunning);
//        }

//        [Fact]
//        public void IsRunning_InitiallyFalse()
//        {
//            // Assert
//            Assert.False(_victoryManager.IsRunning);
//        }

//        #endregion

//        #region Update Tests

//        [Fact]
//        public void Update_WhenNotRunning_DoesNotCheckVictoryCondition()
//        {
//            // Arrange
//            float deltaTime = 0.016f;

//            // Act
//            _victoryManager.Update(deltaTime);

//            // Assert
//            _mockVictoryService.Verify(s => s.IsGameOver(), Times.Never);
//        }

//        [Fact]
//        public void Update_WhenRunning_ChecksVictoryConditionPeriodically()
//        {
//            // Arrange
//            _victoryManager.Start();
//            _mockVictoryService.Setup(s => s.IsGameOver()).Returns(false);

//            // Act - Simulate enough updates to trigger a check (default interval is 1 second)
//            for (int i = 0; i < 100; i++)
//            {
//                _victoryManager.Update(0.016f); // ~60 FPS
//            }

//            // Assert - should check at least once after ~1.6 seconds
//            _mockVictoryService.Verify(s => s.IsGameOver(), Times.AtLeastOnce);
//        }

//        [Fact]
//        public void Update_WhenAlreadyVictoryAchieved_DoesNotCheckAgain()
//        {
//            // Arrange
//            _victoryManager.Start();
//            SetupVictoryScenario("michael", "Michael's Crew");

//            // Act - First check triggers victory
//            _victoryManager.Update(2.0f);

//            // Reset verification
//            _mockVictoryService.Invocations.Clear();

//            // Act - Subsequent updates
//            _victoryManager.Update(2.0f);

//            // Assert - should not check again once victory is achieved
//            _mockVictoryService.Verify(s => s.IsGameOver(), Times.Never);
//        }

//        #endregion

//        #region Victory Detection Tests

//        [Fact]
//        public void Update_WhenGameIsOver_SetsIsVictoryAchievedToTrue()
//        {
//            // Arrange
//            _victoryManager.Start();
//            SetupVictoryScenario("michael", "Michael's Crew");

//            // Act
//            _victoryManager.Update(2.0f);

//            // Assert
//            Assert.True(_victoryManager.IsVictoryAchieved);
//        }

//        [Fact]
//        public void Update_WhenGameIsOver_SetsWinningFactionId()
//        {
//            // Arrange
//            _victoryManager.Start();
//            SetupVictoryScenario("michael", "Michael's Crew");

//            // Act
//            _victoryManager.Update(2.0f);

//            // Assert
//            Assert.Equal("michael", _victoryManager.WinningFactionId);
//        }

//        [Fact]
//        public void Update_WhenGameIsNotOver_DoesNotSetVictory()
//        {
//            // Arrange
//            _victoryManager.Start();
//            _mockVictoryService.Setup(s => s.IsGameOver()).Returns(false);

//            // Act
//            _victoryManager.Update(2.0f);

//            // Assert
//            Assert.False(_victoryManager.IsVictoryAchieved);
//            Assert.Null(_victoryManager.WinningFactionId);
//        }

//        [Fact]
//        public void IsVictoryAchieved_InitiallyFalse()
//        {
//            // Assert
//            Assert.False(_victoryManager.IsVictoryAchieved);
//        }

//        [Fact]
//        public void WinningFactionId_InitiallyNull()
//        {
//            // Assert
//            Assert.Null(_victoryManager.WinningFactionId);
//        }

//        #endregion

//        #region Victory Notification Tests

//        [Fact]
//        public void Update_WhenVictoryAchieved_ShowsVictoryNotification()
//        {
//            // Arrange
//            _victoryManager.Start();
//            SetupVictoryScenario("michael", "Michael's Crew");

//            // Act
//            _victoryManager.Update(2.0f);

//            // Assert
//            _mockNotificationService.Verify(
//                s => s.ShowSuccess(
//                    It.Is<string>(t => t.Contains("Victory")),
//                    It.Is<string>(m => m.Contains("Michael's Crew")),
//                    NotificationPriority.Critical,
//                    It.IsAny<float>()),
//                Times.Once);
//        }

//        [Fact]
//        public void Update_WhenVictoryAchieved_NotificationShowsFactionName()
//        {
//            // Arrange
//            _victoryManager.Start();
//            SetupVictoryScenario("trevor", "Trevor's Gang");

//            // Act
//            _victoryManager.Update(2.0f);

//            // Assert
//            _mockNotificationService.Verify(
//                s => s.ShowSuccess(
//                    It.IsAny<string>(),
//                    It.Is<string>(m => m.Contains("Trevor's Gang")),
//                    It.IsAny<NotificationPriority>(),
//                    It.IsAny<float>()),
//                Times.Once);
//        }

//        [Fact]
//        public void Update_WhenNoVictory_DoesNotShowNotification()
//        {
//            // Arrange
//            _victoryManager.Start();
//            _mockVictoryService.Setup(s => s.IsGameOver()).Returns(false);

//            // Act
//            _victoryManager.Update(2.0f);

//            // Assert
//            _mockNotificationService.Verify(
//                s => s.ShowSuccess(
//                    It.IsAny<string>(),
//                    It.IsAny<string>(),
//                    It.IsAny<NotificationPriority>(),
//                    It.IsAny<float>()),
//                Times.Never);
//        }

//        #endregion

//        #region Victory Event Tests

//        [Fact]
//        public void Update_WhenVictoryAchieved_RaisesOnVictoryEvent()
//        {
//            // Arrange
//            _victoryManager.Start();
//            SetupVictoryScenario("franklin", "Franklin's Family");

//            VictoryEventArgs? receivedArgs = null;
//            _victoryManager.OnVictory += (_, args) => receivedArgs = args;

//            // Act
//            _victoryManager.Update(2.0f);

//            // Assert
//            Assert.NotNull(receivedArgs);
//            Assert.Equal("franklin", receivedArgs.WinningFactionId);
//            Assert.Equal("Franklin's Family", receivedArgs.WinningFactionName);
//        }

//        [Fact]
//        public void Update_WhenVictoryAlreadyAchieved_DoesNotRaiseEventAgain()
//        {
//            // Arrange
//            _victoryManager.Start();
//            SetupVictoryScenario("michael", "Michael's Crew");

//            int eventCount = 0;
//            _victoryManager.OnVictory += (_, _) => eventCount++;

//            // Act - trigger victory
//            _victoryManager.Update(2.0f);
//            // Act - update again
//            _victoryManager.Update(2.0f);

//            // Assert
//            Assert.Equal(1, eventCount);
//        }

//        #endregion

//        #region Check Interval Tests

//        [Fact]
//        public void SetCheckInterval_SetsNewInterval()
//        {
//            // Act
//            _victoryManager.SetCheckInterval(5.0f);

//            // Assert
//            Assert.Equal(5.0f, _victoryManager.CheckIntervalSeconds);
//        }

//        [Fact]
//        public void SetCheckInterval_WithValueBelowMinimum_ClampsToMinimum()
//        {
//            // Act
//            _victoryManager.SetCheckInterval(0.1f);

//            // Assert
//            Assert.Equal(0.5f, _victoryManager.CheckIntervalSeconds); // Minimum is 0.5 seconds
//        }

//        [Fact]
//        public void CheckIntervalSeconds_DefaultsToOneSecond()
//        {
//            // Assert
//            Assert.Equal(1.0f, _victoryManager.CheckIntervalSeconds);
//        }

//        #endregion

//        #region Reset Tests

//        [Fact]
//        public void Reset_ClearsVictoryState()
//        {
//            // Arrange
//            _victoryManager.Start();
//            SetupVictoryScenario("michael", "Michael's Crew");
//            _victoryManager.Update(2.0f);

//            // Act
//            _victoryManager.Reset();

//            // Assert
//            Assert.False(_victoryManager.IsVictoryAchieved);
//            Assert.Null(_victoryManager.WinningFactionId);
//        }

//        [Fact]
//        public void Reset_AllowsVictoryCheckAgain()
//        {
//            // Arrange
//            _victoryManager.Start();
//            SetupVictoryScenario("michael", "Michael's Crew");
//            _victoryManager.Update(2.0f);

//            // Reset and setup new victory
//            _victoryManager.Reset();
//            SetupVictoryScenario("trevor", "Trevor's Gang");

//            int eventCount = 0;
//            _victoryManager.OnVictory += (_, _) => eventCount++;

//            // Act
//            _victoryManager.Update(2.0f);

//            // Assert
//            Assert.Equal(1, eventCount);
//            Assert.Equal("trevor", _victoryManager.WinningFactionId);
//        }

//        #endregion

//        #region Force Check Tests

//        [Fact]
//        public void ForceVictoryCheck_ChecksImmediately()
//        {
//            // Arrange
//            _victoryManager.Start();
//            _mockVictoryService.Setup(s => s.IsGameOver()).Returns(false);

//            // Act
//            _victoryManager.ForceVictoryCheck();

//            // Assert
//            _mockVictoryService.Verify(s => s.IsGameOver(), Times.Once);
//        }

//        [Fact]
//        public void ForceVictoryCheck_WhenVictoryFound_TriggersVictory()
//        {
//            // Arrange
//            _victoryManager.Start();
//            SetupVictoryScenario("michael", "Michael's Crew");

//            // Act
//            _victoryManager.ForceVictoryCheck();

//            // Assert
//            Assert.True(_victoryManager.IsVictoryAchieved);
//        }

//        [Fact]
//        public void ForceVictoryCheck_WorksWhenNotRunning()
//        {
//            // Arrange - not started
//            SetupVictoryScenario("michael", "Michael's Crew");

//            // Act
//            _victoryManager.ForceVictoryCheck();

//            // Assert
//            Assert.True(_victoryManager.IsVictoryAchieved);
//        }

//        #endregion

//        #region Player Victory Tests

//        [Fact]
//        public void SetPlayerFactionId_SetsPlayerFaction()
//        {
//            // Act
//            _victoryManager.SetPlayerFactionId("michael");

//            // Assert
//            Assert.Equal("michael", _victoryManager.GetPlayerFactionId());
//        }

//        [Fact]
//        public void IsPlayerVictory_WhenPlayerFactionWins_ReturnsTrue()
//        {
//            // Arrange
//            _victoryManager.Start();
//            _victoryManager.SetPlayerFactionId("michael");
//            SetupVictoryScenario("michael", "Michael's Crew");
//            _victoryManager.Update(2.0f);

//            // Act & Assert
//            Assert.True(_victoryManager.IsPlayerVictory);
//        }

//        [Fact]
//        public void IsPlayerVictory_WhenOtherFactionWins_ReturnsFalse()
//        {
//            // Arrange
//            _victoryManager.Start();
//            _victoryManager.SetPlayerFactionId("michael");
//            SetupVictoryScenario("trevor", "Trevor's Gang");
//            _victoryManager.Update(2.0f);

//            // Act & Assert
//            Assert.False(_victoryManager.IsPlayerVictory);
//        }

//        [Fact]
//        public void IsPlayerVictory_WhenNoVictory_ReturnsFalse()
//        {
//            // Arrange
//            _victoryManager.SetPlayerFactionId("michael");
//            _mockVictoryService.Setup(s => s.IsGameOver()).Returns(false);

//            // Assert
//            Assert.False(_victoryManager.IsPlayerVictory);
//        }

//        #endregion

//        #region Helper Methods

//        private void SetupVictoryScenario(string winningFactionId, string factionName)
//        {
//            _mockVictoryService.Setup(s => s.IsGameOver()).Returns(true);
//            _mockVictoryService.Setup(s => s.GetWinningFactionId()).Returns(winningFactionId);

//            var faction = new Faction(winningFactionId, factionName);
//            _mockFactionService.Setup(s => s.GetFaction(winningFactionId)).Returns(faction);
//        }

//        #endregion
//    }
//}
