using System.Linq;
using FactionWars.Core.Interfaces;
using FactionWars.Core.Utils;
using FactionWars.ScriptHookV;
using FactionWars.Tests.Mocks;
using Xunit;

namespace FactionWars.Tests.Unit.ScriptHookV
{
    /// <summary>
    /// Tests for GameLoopController's character switching detection functionality.
    /// </summary>
    public class GameLoopControllerCharacterSwitchTests
    {
        private MockGameBridge _gameBridge = null!;
        private ServiceContainer _container = null!;

        private void SetupController(string initialCharacterModel = "player_zero")
        {
            _gameBridge = new MockGameBridge();
            _gameBridge.PlayerCharacterModel = initialCharacterModel;
            _container = ServiceContainerFactory.Create(_gameBridge, new MockMenuProvider());
        }

        [Fact]
        public void OnTick_CharacterSwitched_RaisesCharacterSwitchedEvent()
        {
            // Arrange
            SetupController("player_zero"); // Start as Michael
            var controller = new GameLoopController(_container);

            string? raisedOldFaction = null;
            string? raisedNewFaction = null;
            controller.OnCharacterSwitched += (oldFaction, newFaction) =>
            {
                raisedOldFaction = oldFaction;
                raisedNewFaction = newFaction;
            };

            // Initialize by calling first tick
            controller.OnTick();

            // Act - Switch to Trevor
            _gameBridge.PlayerCharacterModel = "player_two";
            controller.OnTick();

            // Assert
            Assert.Equal("michael", raisedOldFaction);
            Assert.Equal("trevor", raisedNewFaction);
        }

        [Fact]
        public void OnTick_NoCharacterSwitch_DoesNotRaiseEvent()
        {
            // Arrange
            SetupController("player_zero"); // Michael
            var controller = new GameLoopController(_container);

            bool eventRaised = false;
            controller.OnCharacterSwitched += (oldFaction, newFaction) =>
            {
                eventRaised = true;
            };

            // Act - Multiple ticks without character change
            controller.OnTick();
            controller.OnTick();
            controller.OnTick();

            // Assert
            Assert.False(eventRaised);
        }

        [Fact]
        public void CurrentPlayerFactionId_AfterInitialization_ReturnsCorrectFaction()
        {
            // Arrange
            SetupController("player_two"); // Trevor
            var controller = new GameLoopController(_container);

            // Act - First tick initializes
            controller.OnTick();

            // Assert
            Assert.Equal("trevor", controller.CurrentPlayerFactionId);
        }

        [Fact]
        public void CurrentPlayerFactionId_AfterSwitch_ReturnsNewFaction()
        {
            // Arrange
            SetupController("player_zero"); // Michael
            var controller = new GameLoopController(_container);
            controller.OnTick();

            // Act
            _gameBridge.PlayerCharacterModel = "player_one"; // Franklin
            controller.OnTick();

            // Assert
            Assert.Equal("franklin", controller.CurrentPlayerFactionId);
        }

        [Fact]
        public void OnTick_SwitchFromMichaelToTrevor_ShowsNotification()
        {
            // Arrange
            SetupController("player_zero"); // Michael
            var controller = new GameLoopController(_container);
            controller.OnTick();
            var notificationCountBeforeSwitch = _gameBridge.NotificationCount;

            // Act
            _gameBridge.PlayerCharacterModel = "player_two"; // Trevor
            controller.OnTick();

            // Assert - Should show character switch notification
            Assert.True(_gameBridge.NotificationCount > notificationCountBeforeSwitch);
            Assert.True(_gameBridge.Notifications.Any(n =>
                n.Contains("Trevor") ||
                n.Contains("switch") ||
                n.Contains("faction")));
        }

        [Fact]
        public void OnTick_SwitchFromMichaelToFranklin_ShowsNotification()
        {
            // Arrange
            SetupController("player_zero"); // Michael
            var controller = new GameLoopController(_container);
            controller.OnTick();
            var notificationCountBeforeSwitch = _gameBridge.NotificationCount;

            // Act
            _gameBridge.PlayerCharacterModel = "player_one"; // Franklin
            controller.OnTick();

            // Assert
            Assert.True(_gameBridge.NotificationCount > notificationCountBeforeSwitch);
            Assert.True(_gameBridge.Notifications.Any(n =>
                n.Contains("Franklin") ||
                n.Contains("switch") ||
                n.Contains("faction")));
        }

        [Fact]
        public void OnTick_MultipleSwitches_RaisesEventsForEach()
        {
            // Arrange
            SetupController("player_zero"); // Michael
            var controller = new GameLoopController(_container);

            int switchCount = 0;
            controller.OnCharacterSwitched += (oldFaction, newFaction) =>
            {
                switchCount++;
            };

            // Act
            controller.OnTick(); // Initialize

            _gameBridge.PlayerCharacterModel = "player_one"; // Franklin
            controller.OnTick();

            _gameBridge.PlayerCharacterModel = "player_two"; // Trevor
            controller.OnTick();

            _gameBridge.PlayerCharacterModel = "player_zero"; // Michael
            controller.OnTick();

            // Assert
            Assert.Equal(3, switchCount);
        }

        [Fact]
        public void OnAbort_AfterSwitch_CleansUpCorrectly()
        {
            // Arrange
            SetupController("player_zero"); // Michael
            var controller = new GameLoopController(_container);
            controller.OnTick();

            _gameBridge.PlayerCharacterModel = "player_one"; // Franklin
            controller.OnTick();

            // Act
            controller.OnAbort();

            // Assert - After abort, IsInitialized should be false
            Assert.False(controller.IsInitialized);
        }
    }
}
