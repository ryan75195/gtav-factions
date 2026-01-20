using FactionWars.Core.Interfaces;
using FactionWars.Core.Models;
using FactionWars.Core.Utils;
using FactionWars.ScriptHookV;
using Xunit;

namespace FactionWars.Tests.Unit.ScriptHookV
{
    /// <summary>
    /// Tests for GameLoopController's follower management functionality,
    /// specifically the auto-dismissal of followers when the player switches characters.
    /// </summary>
    public class GameLoopControllerFollowerTests
    {
        private MockGameBridge _gameBridge = null!;
        private ServiceContainer _container = null!;

        private void SetupController(string initialCharacterModel = "player_zero", int playerMoney = 10000)
        {
            _gameBridge = new MockGameBridge();
            _gameBridge.PlayerCharacterModel = initialCharacterModel;
            _gameBridge.PlayerMoney = playerMoney;
            _container = ServiceContainerFactory.Create(_gameBridge);
        }

        [Fact]
        public void OnTick_CharacterSwitched_DismissesAllFollowersForOldFaction()
        {
            // Arrange
            SetupController("player_zero"); // Start as Michael
            var controller = new GameLoopController(_container);

            // Initialize by calling first tick
            controller.OnTick();

            // Recruit some followers for Michael's faction
            var followerService = _container.Resolve<IFollowerService>();
            followerService.Recruit("michael", DefenderTier.Basic);
            followerService.Recruit("michael", DefenderTier.Medium);

            Assert.Equal(2, followerService.GetFollowerCount("michael"));

            // Act - Switch to Trevor
            _gameBridge.PlayerCharacterModel = "player_two";
            controller.OnTick();

            // Assert - Michael's followers should be dismissed
            Assert.Equal(0, followerService.GetFollowerCount("michael"));
        }

        [Fact]
        public void OnTick_CharacterSwitched_DoesNotAffectOtherFactionFollowers()
        {
            // Arrange
            SetupController("player_zero"); // Start as Michael
            var controller = new GameLoopController(_container);
            controller.OnTick();

            var followerService = _container.Resolve<IFollowerService>();

            // Recruit followers for multiple factions (simulating scenario where other factions have followers)
            followerService.Recruit("michael", DefenderTier.Basic);
            followerService.Recruit("trevor", DefenderTier.Basic);

            Assert.Equal(1, followerService.GetFollowerCount("michael"));
            Assert.Equal(1, followerService.GetFollowerCount("trevor"));

            // Act - Switch to Trevor
            _gameBridge.PlayerCharacterModel = "player_two";
            controller.OnTick();

            // Assert - Michael's followers dismissed, Trevor's remain
            Assert.Equal(0, followerService.GetFollowerCount("michael"));
            Assert.Equal(1, followerService.GetFollowerCount("trevor"));
        }

        [Fact]
        public void OnTick_NoCharacterSwitch_FollowersRemain()
        {
            // Arrange
            SetupController("player_zero"); // Michael
            var controller = new GameLoopController(_container);
            controller.OnTick();

            var followerService = _container.Resolve<IFollowerService>();
            followerService.Recruit("michael", DefenderTier.Basic);
            followerService.Recruit("michael", DefenderTier.Heavy);

            var initialCount = followerService.GetFollowerCount("michael");

            // Act - Multiple ticks without character change
            controller.OnTick();
            controller.OnTick();
            controller.OnTick();

            // Assert - Followers should remain unchanged
            Assert.Equal(initialCount, followerService.GetFollowerCount("michael"));
        }

        [Fact]
        public void OnTick_CharacterSwitched_FollowerManagerDespawnsFollowerPeds()
        {
            // Arrange
            SetupController("player_zero"); // Start as Michael
            var controller = new GameLoopController(_container);
            controller.OnTick();

            // Get the follower manager and recruit a follower with an actual ped
            var followerManager = controller.FollowerManager;
            Assert.NotNull(followerManager);

            var result = followerManager!.RecruitFollower("michael", DefenderTier.Basic);
            Assert.True(result.Success);

            var followerPedHandle = result.Follower!.PedHandle;
            Assert.True(followerPedHandle >= 0);
            Assert.True(_gameBridge.PedExists(followerPedHandle));

            // Act - Switch to Franklin
            _gameBridge.PlayerCharacterModel = "player_one";
            controller.OnTick();

            // Assert - The ped should have been deleted
            Assert.False(_gameBridge.PedExists(followerPedHandle));
        }

        [Fact]
        public void OnTick_MultipleSwitches_DismissesFollowersEachTime()
        {
            // Arrange
            SetupController("player_zero"); // Start as Michael
            var controller = new GameLoopController(_container);
            controller.OnTick();

            var followerService = _container.Resolve<IFollowerService>();

            // Recruit followers for Michael
            followerService.Recruit("michael", DefenderTier.Basic);
            Assert.Equal(1, followerService.GetFollowerCount("michael"));

            // Act - Switch to Franklin
            _gameBridge.PlayerCharacterModel = "player_one";
            controller.OnTick();

            Assert.Equal(0, followerService.GetFollowerCount("michael"));

            // Recruit followers for Franklin
            followerService.Recruit("franklin", DefenderTier.Medium);
            Assert.Equal(1, followerService.GetFollowerCount("franklin"));

            // Act - Switch to Trevor
            _gameBridge.PlayerCharacterModel = "player_two";
            controller.OnTick();

            // Assert - Franklin's followers should be dismissed
            Assert.Equal(0, followerService.GetFollowerCount("franklin"));
        }

        [Fact]
        public void OnTick_CharacterSwitched_WithNoFollowers_DoesNotThrow()
        {
            // Arrange
            SetupController("player_zero"); // Start as Michael
            var controller = new GameLoopController(_container);
            controller.OnTick();

            // Don't recruit any followers

            // Act - Switch to Trevor (should not throw)
            _gameBridge.PlayerCharacterModel = "player_two";
            var exception = Record.Exception(() => controller.OnTick());

            // Assert
            Assert.Null(exception);
        }

        [Fact]
        public void FollowerManager_IsAccessibleFromController()
        {
            // Arrange
            SetupController("player_zero");
            var controller = new GameLoopController(_container);
            controller.OnTick();

            // Act
            var followerManager = controller.FollowerManager;

            // Assert
            Assert.NotNull(followerManager);
        }
    }
}
