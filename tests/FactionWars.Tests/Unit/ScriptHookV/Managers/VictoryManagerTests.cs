using System;
using FactionWars.Core.Interfaces;
using FactionWars.Core.Models;
using FactionWars.Factions.Interfaces;
using FactionWars.Factions.Models;
using FactionWars.ScriptHookV.Managers;
using FactionWars.UI.Interfaces;
using FactionWars.UI.Models;
using Moq;
using Xunit;

namespace FactionWars.Tests.Unit.ScriptHookV.Managers
{
    public class VictoryManagerTests
    {
        private readonly Mock<IVictoryConditionService> _victoryConditionServiceMock;
        private readonly Mock<IFactionService> _factionServiceMock;
        private readonly Mock<INotificationService> _notificationServiceMock;
        private readonly VictoryManager _victoryManager;

        public VictoryManagerTests()
        {
            _victoryConditionServiceMock = new Mock<IVictoryConditionService>();
            _factionServiceMock = new Mock<IFactionService>();
            _notificationServiceMock = new Mock<INotificationService>();

            _victoryManager = new VictoryManager(
                _victoryConditionServiceMock.Object,
                _factionServiceMock.Object,
                _notificationServiceMock.Object);
        }

        #region Constructor Tests

        [Fact]
        public void Constructor_WithNullVictoryConditionService_ThrowsArgumentNullException()
        {
            Assert.Throws<ArgumentNullException>(() => new VictoryManager(
                null!,
                _factionServiceMock.Object,
                _notificationServiceMock.Object));
        }

        [Fact]
        public void Constructor_WithNullFactionService_ThrowsArgumentNullException()
        {
            Assert.Throws<ArgumentNullException>(() => new VictoryManager(
                _victoryConditionServiceMock.Object,
                null!,
                _notificationServiceMock.Object));
        }

        [Fact]
        public void Constructor_WithNullNotificationService_ThrowsArgumentNullException()
        {
            Assert.Throws<ArgumentNullException>(() => new VictoryManager(
                _victoryConditionServiceMock.Object,
                _factionServiceMock.Object,
                null!));
        }

        [Fact]
        public void Constructor_WithValidParameters_Succeeds()
        {
            var manager = new VictoryManager(
                _victoryConditionServiceMock.Object,
                _factionServiceMock.Object,
                _notificationServiceMock.Object);

            Assert.NotNull(manager);
            Assert.False(manager.IsVictoryAchieved);
        }

        #endregion

        #region Start/Stop Tests

        [Fact]
        public void Start_SetsIsRunningTrue()
        {
            _victoryManager.Start();

            Assert.True(_victoryManager.IsRunning);
        }

        [Fact]
        public void Stop_SetsIsRunningFalse()
        {
            _victoryManager.Start();
            _victoryManager.Stop();

            Assert.False(_victoryManager.IsRunning);
        }

        #endregion

        #region Update Tests

        [Fact]
        public void Update_WhenNotRunning_DoesNotCheckVictory()
        {
            _victoryManager.Update(1.0f);

            _victoryConditionServiceMock.Verify(v => v.IsGameOver(), Times.Never);
        }

        [Fact]
        public void Update_WhenRunning_ChecksVictoryCondition()
        {
            _victoryConditionServiceMock.Setup(v => v.IsGameOver()).Returns(false);
            _victoryManager.Start();

            _victoryManager.Update(1.0f);

            _victoryConditionServiceMock.Verify(v => v.IsGameOver(), Times.Once);
        }

        [Fact]
        public void Update_WhenVictoryNotAchieved_DoesNotShowNotification()
        {
            _victoryConditionServiceMock.Setup(v => v.IsGameOver()).Returns(false);
            _victoryManager.Start();

            _victoryManager.Update(1.0f);

            _notificationServiceMock.Verify(n => n.ShowSuccess(
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<NotificationPriority>(),
                It.IsAny<float>()), Times.Never);
        }

        [Fact]
        public void Update_WhenVictoryAchieved_ShowsVictoryNotification()
        {
            var faction = new Faction("michael", "Michael's Crew", "Michael De Santa");
            _victoryConditionServiceMock.Setup(v => v.IsGameOver()).Returns(true);
            _victoryConditionServiceMock.Setup(v => v.GetWinningFactionId()).Returns("michael");
            _factionServiceMock.Setup(f => f.GetFaction("michael")).Returns(faction);
            _victoryManager.Start();

            _victoryManager.Update(1.0f);

            _notificationServiceMock.Verify(n => n.ShowSuccess(
                It.Is<string>(s => s.Contains("Victory")),
                It.Is<string>(s => s.Contains("Michael's Crew")),
                It.IsAny<NotificationPriority>(),
                It.IsAny<float>()), Times.Once);
        }

        [Fact]
        public void Update_WhenVictoryAchieved_SetsIsVictoryAchievedTrue()
        {
            var faction = new Faction("michael", "Michael's Crew", "Michael De Santa");
            _victoryConditionServiceMock.Setup(v => v.IsGameOver()).Returns(true);
            _victoryConditionServiceMock.Setup(v => v.GetWinningFactionId()).Returns("michael");
            _factionServiceMock.Setup(f => f.GetFaction("michael")).Returns(faction);
            _victoryManager.Start();

            _victoryManager.Update(1.0f);

            Assert.True(_victoryManager.IsVictoryAchieved);
        }

        [Fact]
        public void Update_WhenVictoryAlreadyAchieved_DoesNotShowNotificationAgain()
        {
            var faction = new Faction("michael", "Michael's Crew", "Michael De Santa");
            _victoryConditionServiceMock.Setup(v => v.IsGameOver()).Returns(true);
            _victoryConditionServiceMock.Setup(v => v.GetWinningFactionId()).Returns("michael");
            _factionServiceMock.Setup(f => f.GetFaction("michael")).Returns(faction);
            _victoryManager.Start();

            // First update triggers victory
            _victoryManager.Update(1.0f);
            // Second update should not show notification again
            _victoryManager.Update(1.0f);

            _notificationServiceMock.Verify(n => n.ShowSuccess(
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<NotificationPriority>(),
                It.IsAny<float>()), Times.Once);
        }

        [Fact]
        public void Update_RaisesOnVictoryEvent_WhenVictoryAchieved()
        {
            var faction = new Faction("michael", "Michael's Crew", "Michael De Santa");
            _victoryConditionServiceMock.Setup(v => v.IsGameOver()).Returns(true);
            _victoryConditionServiceMock.Setup(v => v.GetWinningFactionId()).Returns("michael");
            _factionServiceMock.Setup(f => f.GetFaction("michael")).Returns(faction);
            _victoryManager.Start();

            VictoryEventArgs? receivedArgs = null;
            _victoryManager.OnVictory += (sender, args) => receivedArgs = args;

            _victoryManager.Update(1.0f);

            Assert.NotNull(receivedArgs);
            Assert.Equal("michael", receivedArgs!.WinningFactionId);
            Assert.Equal("Michael's Crew", receivedArgs.WinningFactionName);
        }

        [Fact]
        public void Update_OnlyChecksVictoryAtInterval()
        {
            _victoryConditionServiceMock.Setup(v => v.IsGameOver()).Returns(false);
            _victoryManager.Start();
            _victoryManager.SetCheckInterval(5.0f);

            // Update with less than interval
            _victoryManager.Update(0.5f);
            _victoryManager.Update(0.5f);

            // Should only check once after interval is reached
            _victoryConditionServiceMock.Verify(v => v.IsGameOver(), Times.Never);

            // Now pass the interval
            _victoryManager.Update(5.0f);

            _victoryConditionServiceMock.Verify(v => v.IsGameOver(), Times.Once);
        }

        #endregion

        #region GetWinningFaction Tests

        [Fact]
        public void GetWinningFactionId_BeforeVictory_ReturnsNull()
        {
            var winningFactionId = _victoryManager.GetWinningFactionId();

            Assert.Null(winningFactionId);
        }

        [Fact]
        public void GetWinningFactionId_AfterVictory_ReturnsWinningFactionId()
        {
            var faction = new Faction("michael", "Michael's Crew", "Michael De Santa");
            _victoryConditionServiceMock.Setup(v => v.IsGameOver()).Returns(true);
            _victoryConditionServiceMock.Setup(v => v.GetWinningFactionId()).Returns("michael");
            _factionServiceMock.Setup(f => f.GetFaction("michael")).Returns(faction);
            _victoryManager.Start();

            _victoryManager.Update(1.0f);

            Assert.Equal("michael", _victoryManager.GetWinningFactionId());
        }

        #endregion

        #region CheckInterval Tests

        [Fact]
        public void CheckIntervalSeconds_DefaultValue_IsOne()
        {
            Assert.Equal(1.0f, _victoryManager.CheckIntervalSeconds);
        }

        [Fact]
        public void SetCheckInterval_UpdatesInterval()
        {
            _victoryManager.SetCheckInterval(5.0f);

            Assert.Equal(5.0f, _victoryManager.CheckIntervalSeconds);
        }

        [Fact]
        public void SetCheckInterval_WithNegativeValue_ClampsToMinimum()
        {
            _victoryManager.SetCheckInterval(-1.0f);

            Assert.True(_victoryManager.CheckIntervalSeconds >= 0.1f);
        }

        #endregion

        #region Reset Tests

        [Fact]
        public void Reset_ClearsVictoryState()
        {
            var faction = new Faction("michael", "Michael's Crew", "Michael De Santa");
            _victoryConditionServiceMock.Setup(v => v.IsGameOver()).Returns(true);
            _victoryConditionServiceMock.Setup(v => v.GetWinningFactionId()).Returns("michael");
            _factionServiceMock.Setup(f => f.GetFaction("michael")).Returns(faction);
            _victoryManager.Start();
            _victoryManager.Update(1.0f);

            _victoryManager.Reset();

            Assert.False(_victoryManager.IsVictoryAchieved);
            Assert.Null(_victoryManager.GetWinningFactionId());
        }

        #endregion
    }
}
