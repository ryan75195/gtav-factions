using System;
using FactionWars.Core.Interfaces;
using FactionWars.Economy.Interfaces;
using FactionWars.ScriptHookV.Managers;
using Moq;
using Xunit;

namespace FactionWars.Tests.Unit.ScriptHookV
{
    /// <summary>
    /// Tests for the EconomyManager which coordinates resource ticks with the game loop.
    /// </summary>
    public class EconomyManagerTests
    {
        private readonly Mock<IResourceTickService> _mockResourceTickService;
        private readonly Mock<IGameBridge> _mockGameBridge;
        private readonly EconomyManager _economyManager;

        public EconomyManagerTests()
        {
            _mockResourceTickService = new Mock<IResourceTickService>();
            _mockGameBridge = new Mock<IGameBridge>();
            _economyManager = new EconomyManager(_mockResourceTickService.Object, _mockGameBridge.Object);
        }

        #region Constructor Tests

        [Fact]
        public void Constructor_WithValidDependencies_CreatesInstance()
        {
            // Arrange & Act
            var manager = new EconomyManager(_mockResourceTickService.Object, _mockGameBridge.Object);

            // Assert
            Assert.NotNull(manager);
        }

        [Fact]
        public void Constructor_WithNullResourceTickService_ThrowsArgumentNullException()
        {
            // Act & Assert
            var exception = Assert.Throws<ArgumentNullException>(() =>
                new EconomyManager(null!, _mockGameBridge.Object));
            Assert.Equal("resourceTickService", exception.ParamName);
        }

        [Fact]
        public void Constructor_WithNullGameBridge_ThrowsArgumentNullException()
        {
            // Act & Assert
            var exception = Assert.Throws<ArgumentNullException>(() =>
                new EconomyManager(_mockResourceTickService.Object, null!));
            Assert.Equal("gameBridge", exception.ParamName);
        }

        #endregion

        #region Start/Stop Tests

        [Fact]
        public void Start_StartsResourceTickService()
        {
            // Act
            _economyManager.Start();

            // Assert
            _mockResourceTickService.Verify(s => s.Start(), Times.Once);
        }

        [Fact]
        public void Start_SetsIsRunningToTrue()
        {
            // Act
            _economyManager.Start();

            // Assert
            Assert.True(_economyManager.IsRunning);
        }

        [Fact]
        public void Stop_StopsResourceTickService()
        {
            // Arrange
            _economyManager.Start();

            // Act
            _economyManager.Stop();

            // Assert
            _mockResourceTickService.Verify(s => s.Stop(), Times.Once);
        }

        [Fact]
        public void Stop_SetsIsRunningToFalse()
        {
            // Arrange
            _economyManager.Start();

            // Act
            _economyManager.Stop();

            // Assert
            Assert.False(_economyManager.IsRunning);
        }

        [Fact]
        public void IsRunning_InitiallyFalse()
        {
            // Assert
            Assert.False(_economyManager.IsRunning);
        }

        #endregion

        #region Update Tests

        [Fact]
        public void Update_WhenRunning_CallsResourceTickServiceUpdate()
        {
            // Arrange
            _economyManager.Start();
            float deltaTime = 0.016f; // ~60 FPS

            // Act
            _economyManager.Update(deltaTime);

            // Assert
            _mockResourceTickService.Verify(s => s.Update(deltaTime), Times.Once);
        }

        [Fact]
        public void Update_WhenNotRunning_DoesNotCallResourceTickServiceUpdate()
        {
            // Arrange
            float deltaTime = 0.016f;

            // Act
            _economyManager.Update(deltaTime);

            // Assert
            _mockResourceTickService.Verify(s => s.Update(It.IsAny<float>()), Times.Never);
        }

        [Fact]
        public void Update_WithZeroDeltaTime_StillCallsResourceTickService()
        {
            // Arrange
            _economyManager.Start();

            // Act
            _economyManager.Update(0f);

            // Assert
            _mockResourceTickService.Verify(s => s.Update(0f), Times.Once);
        }

        [Fact]
        public void Update_WithNegativeDeltaTime_StillCallsResourceTickService()
        {
            // Arrange
            _economyManager.Start();

            // Act - negative delta times are handled by ResourceTickService
            _economyManager.Update(-0.016f);

            // Assert
            _mockResourceTickService.Verify(s => s.Update(-0.016f), Times.Once);
        }

        #endregion

        #region Tick Progress Property Tests

        [Fact]
        public void TickProgress_ReturnsResourceTickServiceProgress()
        {
            // Arrange
            _mockResourceTickService.Setup(s => s.TickProgress).Returns(50f);

            // Act
            var progress = _economyManager.TickProgress;

            // Assert
            Assert.Equal(50f, progress);
        }

        [Fact]
        public void TimeUntilNextTick_ReturnsResourceTickServiceValue()
        {
            // Arrange
            _mockResourceTickService.Setup(s => s.TimeUntilNextTick).Returns(150f);

            // Act
            var time = _economyManager.TimeUntilNextTick;

            // Assert
            Assert.Equal(150f, time);
        }

        [Fact]
        public void TickIntervalSeconds_ReturnsResourceTickServiceValue()
        {
            // Arrange
            _mockResourceTickService.Setup(s => s.TickIntervalSeconds).Returns(300);

            // Act
            var interval = _economyManager.TickIntervalSeconds;

            // Assert
            Assert.Equal(300, interval);
        }

        #endregion

        #region ForceTick Tests

        [Fact]
        public void ForceTick_CallsResourceTickServiceForceTick()
        {
            // Act
            _economyManager.ForceTick();

            // Assert
            _mockResourceTickService.Verify(s => s.ForceTick(), Times.Once);
        }

        [Fact]
        public void ForceTick_WorksWhenNotRunning()
        {
            // Act (not started)
            _economyManager.ForceTick();

            // Assert
            _mockResourceTickService.Verify(s => s.ForceTick(), Times.Once);
        }

        #endregion

        #region Resource Tick Event Forwarding Tests

        [Fact]
        public void OnResourceTick_RaisedWhenResourceTickServiceRaisesEvent()
        {
            // Arrange
            var eventRaised = false;
            _economyManager.OnResourceTick += (_, _) => eventRaised = true;

            // Act - simulate ResourceTickService raising an event
            _mockResourceTickService.Raise(
                s => s.OnResourceTick += null,
                new ResourceTickEventArgs("faction1", 100, 10, 5));

            // Assert
            Assert.True(eventRaised);
        }

        [Fact]
        public void OnResourceTick_PassesEventArgsCorrectly()
        {
            // Arrange
            ResourceTickEventArgs? receivedArgs = null;
            _economyManager.OnResourceTick += (_, args) => receivedArgs = args;

            var expectedArgs = new ResourceTickEventArgs("faction-michael", 500, 25, 10);

            // Act
            _mockResourceTickService.Raise(
                s => s.OnResourceTick += null,
                expectedArgs);

            // Assert
            Assert.NotNull(receivedArgs);
            Assert.Equal("faction-michael", receivedArgs.FactionId);
            Assert.Equal(500, receivedArgs.CashGenerated);
            Assert.Equal(25, receivedArgs.RecruitmentGenerated);
            Assert.Equal(10, receivedArgs.WeaponsGenerated);
        }

        #endregion

        #region Reset Tests

        [Fact]
        public void Reset_CallsResourceTickServiceReset()
        {
            // Act
            _economyManager.Reset();

            // Assert
            _mockResourceTickService.Verify(s => s.Reset(), Times.Once);
        }

        #endregion

        #region GTA V Cash Integration Tests

        [Fact]
        public void OnResourceTick_WhenPlayerFactionReceivesCash_AddsToPlayerMoney()
        {
            // Arrange
            var playerFactionId = "michael";
            _economyManager.SetPlayerFactionId(playerFactionId);

            var tickArgs = new ResourceTickEventArgs(playerFactionId, 500, 25, 10);

            // Act - simulate ResourceTickService raising an event for player's faction
            _mockResourceTickService.Raise(
                s => s.OnResourceTick += null,
                tickArgs);

            // Assert - should add cash to player's GTA V money
            _mockGameBridge.Verify(g => g.AddPlayerMoney(500), Times.Once);
        }

        [Fact]
        public void OnResourceTick_WhenNonPlayerFactionReceivesCash_DoesNotAddToPlayerMoney()
        {
            // Arrange
            var playerFactionId = "michael";
            _economyManager.SetPlayerFactionId(playerFactionId);

            var tickArgs = new ResourceTickEventArgs("trevor", 500, 25, 10);

            // Act - simulate ResourceTickService raising an event for non-player faction
            _mockResourceTickService.Raise(
                s => s.OnResourceTick += null,
                tickArgs);

            // Assert - should NOT add cash to player's money
            _mockGameBridge.Verify(g => g.AddPlayerMoney(It.IsAny<int>()), Times.Never);
        }

        [Fact]
        public void OnResourceTick_WhenPlayerFactionNotSet_DoesNotAddToPlayerMoney()
        {
            // Arrange - no player faction set
            var tickArgs = new ResourceTickEventArgs("michael", 500, 25, 10);

            // Act
            _mockResourceTickService.Raise(
                s => s.OnResourceTick += null,
                tickArgs);

            // Assert
            _mockGameBridge.Verify(g => g.AddPlayerMoney(It.IsAny<int>()), Times.Never);
        }

        [Fact]
        public void OnResourceTick_WhenCashIsZero_DoesNotAddToPlayerMoney()
        {
            // Arrange
            var playerFactionId = "michael";
            _economyManager.SetPlayerFactionId(playerFactionId);

            var tickArgs = new ResourceTickEventArgs(playerFactionId, 0, 25, 10);

            // Act
            _mockResourceTickService.Raise(
                s => s.OnResourceTick += null,
                tickArgs);

            // Assert - should not call AddPlayerMoney for zero amount
            _mockGameBridge.Verify(g => g.AddPlayerMoney(It.IsAny<int>()), Times.Never);
        }

        [Fact]
        public void SetPlayerFactionId_SetsPlayerFactionCorrectly()
        {
            // Act
            _economyManager.SetPlayerFactionId("franklin");

            // Assert - verify by triggering a tick for franklin
            var tickArgs = new ResourceTickEventArgs("franklin", 300, 10, 5);
            _mockResourceTickService.Raise(
                s => s.OnResourceTick += null,
                tickArgs);

            _mockGameBridge.Verify(g => g.AddPlayerMoney(300), Times.Once);
        }

        [Fact]
        public void SetPlayerFactionId_WithNull_ClearsPlayerFaction()
        {
            // Arrange
            _economyManager.SetPlayerFactionId("michael");

            // Act
            _economyManager.SetPlayerFactionId(null);

            // Assert - verify no money is added when player faction is cleared
            var tickArgs = new ResourceTickEventArgs("michael", 500, 25, 10);
            _mockResourceTickService.Raise(
                s => s.OnResourceTick += null,
                tickArgs);

            _mockGameBridge.Verify(g => g.AddPlayerMoney(It.IsAny<int>()), Times.Never);
        }

        [Fact]
        public void GetPlayerFactionId_ReturnsSetFactionId()
        {
            // Arrange
            _economyManager.SetPlayerFactionId("trevor");

            // Act
            var factionId = _economyManager.GetPlayerFactionId();

            // Assert
            Assert.Equal("trevor", factionId);
        }

        [Fact]
        public void GetPlayerFactionId_WhenNotSet_ReturnsNull()
        {
            // Act
            var factionId = _economyManager.GetPlayerFactionId();

            // Assert
            Assert.Null(factionId);
        }

        #endregion
    }
}
