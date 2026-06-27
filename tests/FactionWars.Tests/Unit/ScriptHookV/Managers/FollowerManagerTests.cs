using System;
using System.Collections.Generic;
using FactionWars.Combat.Interfaces;
using FactionWars.Combat.Models;
using FactionWars.Core.Interfaces;
using FactionWars.Core.Models;
using FactionWars.Persistence.Models;
using FactionWars.ScriptHookV.Managers;
using FactionWars.UI.Interfaces;
using Moq;
using Xunit;

namespace FactionWars.Tests.Unit.ScriptHookV.Managers
{
    public class FollowerManagerTests
    {
        private readonly Mock<IGameBridge> _gameBridgeMock;
        private readonly Mock<IFollowerService> _followerServiceMock;
        private readonly Mock<IPedSpawningService> _pedSpawningServiceMock;
        private readonly Mock<IDefenderTierService> _defenderTierServiceMock;
        private readonly Mock<IPedBlipService> _pedBlipServiceMock;
        private readonly Mock<IVehicleSeatPriorityService> _seatPriorityServiceMock;
        private readonly FollowerManager _manager;

        public FollowerManagerTests()
        {
            _gameBridgeMock = new Mock<IGameBridge>();
            _followerServiceMock = new Mock<IFollowerService>();
            _pedSpawningServiceMock = new Mock<IPedSpawningService>();
            _defenderTierServiceMock = new Mock<IDefenderTierService>();
            _pedBlipServiceMock = new Mock<IPedBlipService>();
            _seatPriorityServiceMock = new Mock<IVehicleSeatPriorityService>();

            // Set up default tier configs (tests can override as needed)
            _defenderTierServiceMock.Setup(s => s.GetTierConfig(DefenderTier.Basic))
                .Returns(new DefenderTierConfig(DefenderTier.Basic, 200, 100, 0, "weapon_pistol", 0.4f, 1.0f));
            _defenderTierServiceMock.Setup(s => s.GetTierConfig(DefenderTier.Medium))
                .Returns(new DefenderTierConfig(DefenderTier.Medium, 500, 150, 50, "weapon_pistol", 0.6f, 1.5f));
            _defenderTierServiceMock.Setup(s => s.GetTierConfig(DefenderTier.Heavy))
                .Returns(new DefenderTierConfig(DefenderTier.Heavy, 1000, 200, 100, "weapon_smg", 0.8f, 2.0f));

            // Set up default player money (plenty of cash by default)
            _gameBridgeMock.Setup(g => g.GetPlayerMoney()).Returns(10000);
            _gameBridgeMock.Setup(g => g.IsPedFollowingPlayer(It.IsAny<int>())).Returns(true);

            // Default: return seats as-is, return all followers
            _seatPriorityServiceMock.Setup(s => s.GetPrioritizedFreeSeats(It.IsAny<int>()))
                .Returns(new[] { 1, 2, 3 });
            _seatPriorityServiceMock.Setup(s => s.FilterFollowersByProximity(
                It.IsAny<int[]>(), It.IsAny<int>(), It.IsAny<float>()))
                .Returns<int[], int, float>((handles, v, d) => handles);

            _manager = new FollowerManager(
                _gameBridgeMock.Object,
                _followerServiceMock.Object,
                _pedSpawningServiceMock.Object,
                _defenderTierServiceMock.Object,
                _pedBlipServiceMock.Object,
                _seatPriorityServiceMock.Object);
        }

        #region Constructor Tests

        [Fact]
        public void Constructor_WithNullGameBridge_ShouldThrowArgumentNullException()
        {
            // Act & Assert
            Assert.Throws<ArgumentNullException>(() => new FollowerManager(
                null!,
                _followerServiceMock.Object,
                _pedSpawningServiceMock.Object,
                _defenderTierServiceMock.Object,
                _pedBlipServiceMock.Object,
                _seatPriorityServiceMock.Object));
        }

        [Fact]
        public void Constructor_WithNullFollowerService_ShouldThrowArgumentNullException()
        {
            // Act & Assert
            Assert.Throws<ArgumentNullException>(() => new FollowerManager(
                _gameBridgeMock.Object,
                null!,
                _pedSpawningServiceMock.Object,
                _defenderTierServiceMock.Object,
                _pedBlipServiceMock.Object,
                _seatPriorityServiceMock.Object));
        }

        [Fact]
        public void Constructor_WithNullPedSpawningService_ShouldThrowArgumentNullException()
        {
            // Act & Assert
            Assert.Throws<ArgumentNullException>(() => new FollowerManager(
                _gameBridgeMock.Object,
                _followerServiceMock.Object,
                null!,
                _defenderTierServiceMock.Object,
                _pedBlipServiceMock.Object,
                _seatPriorityServiceMock.Object));
        }

        [Fact]
        public void Constructor_WithNullDefenderTierService_ShouldThrowArgumentNullException()
        {
            // Act & Assert
            Assert.Throws<ArgumentNullException>(() => new FollowerManager(
                _gameBridgeMock.Object,
                _followerServiceMock.Object,
                _pedSpawningServiceMock.Object,
                null!,
                _pedBlipServiceMock.Object,
                _seatPriorityServiceMock.Object));
        }

        [Fact]
        public void Constructor_WithNullPedBlipService_ShouldThrowArgumentNullException()
        {
            // Act & Assert
            Assert.Throws<ArgumentNullException>(() => new FollowerManager(
                _gameBridgeMock.Object,
                _followerServiceMock.Object,
                _pedSpawningServiceMock.Object,
                _defenderTierServiceMock.Object,
                null!,
                _seatPriorityServiceMock.Object));
        }

        [Fact]
        public void Constructor_WithNullSeatPriorityService_ShouldThrowArgumentNullException()
        {
            // Act & Assert
            Assert.Throws<ArgumentNullException>(() => new FollowerManager(
                _gameBridgeMock.Object,
                _followerServiceMock.Object,
                _pedSpawningServiceMock.Object,
                _defenderTierServiceMock.Object,
                _pedBlipServiceMock.Object,
                null!));
        }

        #endregion

        #region RecruitFollower Tests

        [Fact]
        public void RecruitFollower_WhenSuccessful_ShouldSpawnPedAndReturnFollower()
        {
            // Arrange
            var factionId = "blue";
            var tier = DefenderTier.Basic;
            var playerPos = new Vector3(100f, 200f, 30f);
            var spawnPos = new Vector3(102f, 202f, 30f);
            var follower = new Follower(factionId, tier);

            _gameBridgeMock.Setup(g => g.GetPlayerPosition()).Returns(playerPos);
            _followerServiceMock.Setup(s => s.Recruit(factionId, tier))
                .Returns(FollowerRecruitResult.Succeeded(follower));
            _pedSpawningServiceMock.Setup(s => s.CanSpawn()).Returns(true);
            _pedSpawningServiceMock.Setup(s => s.SpawnPed("basic_model", It.IsAny<Vector3>(), factionId, null))
                .Returns(new PedHandle(42, factionId, spawnPos, "basic_model", null));

            // Configure model name for tier
            _manager.SetModelForTier(tier, "basic_model");

            // Act
            var result = _manager.RecruitFollower(factionId, tier);

            // Assert
            Assert.True(result.Success);
            Assert.NotNull(result.Follower);
            Assert.Equal(42, result.Follower!.PedHandle);
        }

        [Fact]
        public void RecruitFollower_WhenMaxFollowersReached_ShouldReturnFailure()
        {
            // Arrange
            var factionId = "blue";
            var tier = DefenderTier.Basic;

            _followerServiceMock.Setup(s => s.Recruit(factionId, tier))
                .Returns(FollowerRecruitResult.Failed(FollowerRecruitFailureReason.MaxFollowersReached));

            // Act
            var result = _manager.RecruitFollower(factionId, tier);

            // Assert
            Assert.False(result.Success);
            Assert.Equal(FollowerRecruitFailureReason.MaxFollowersReached, result.FailureReason);
        }

        [Fact]
        public void RecruitFollower_WhenSpawnFails_ShouldDismissFollowerAndReturnFailure()
        {
            // Arrange
            var factionId = "blue";
            var tier = DefenderTier.Basic;
            var playerPos = new Vector3(100f, 200f, 30f);
            var follower = new Follower(factionId, tier);

            _gameBridgeMock.Setup(g => g.GetPlayerPosition()).Returns(playerPos);
            _followerServiceMock.Setup(s => s.Recruit(factionId, tier))
                .Returns(FollowerRecruitResult.Succeeded(follower));
            _pedSpawningServiceMock.Setup(s => s.CanSpawn()).Returns(false);

            _manager.SetModelForTier(tier, "basic_model");

            // Act
            var result = _manager.RecruitFollower(factionId, tier);

            // Assert
            Assert.False(result.Success);
            Assert.Equal(FollowerRecruitFailureReason.SpawnFailed, result.FailureReason);
            _followerServiceMock.Verify(s => s.DismissFollower(follower.Id), Times.Once);
        }

        [Fact]
        public void RecruitFollower_WithNullFactionId_ShouldThrowArgumentNullException()
        {
            // Act & Assert
            Assert.Throws<ArgumentNullException>(() => _manager.RecruitFollower(null!, DefenderTier.Basic));
        }

        [Fact]
        public void RecruitFollower_ShouldSpawnNearPlayer()
        {
            // Arrange
            var factionId = "blue";
            var tier = DefenderTier.Medium;
            var playerPos = new Vector3(100f, 200f, 30f);
            var follower = new Follower(factionId, tier);

            _gameBridgeMock.Setup(g => g.GetPlayerPosition()).Returns(playerPos);
            _followerServiceMock.Setup(s => s.Recruit(factionId, tier))
                .Returns(FollowerRecruitResult.Succeeded(follower));
            _pedSpawningServiceMock.Setup(s => s.CanSpawn()).Returns(true);
            _pedSpawningServiceMock.Setup(s => s.SpawnPed(It.IsAny<string>(), It.IsAny<Vector3>(), factionId, null))
                .Returns<string, Vector3, string, string?>((model, pos, faction, zone) =>
                    new PedHandle(1, faction, pos, model, zone));

            _manager.SetModelForTier(tier, "medium_model");

            // Act
            _manager.RecruitFollower(factionId, tier);

            // Assert - verify ped was spawned near player position
            _pedSpawningServiceMock.Verify(s => s.SpawnPed(
                "medium_model",
                It.Is<Vector3>(p =>
                    Math.Abs(p.X - playerPos.X) <= 5f &&
                    Math.Abs(p.Y - playerPos.Y) <= 5f),
                factionId,
                null), Times.Once);
        }

        #endregion

        #region DismissFollower Tests

        [Fact]
        public void DismissFollower_WhenFollowerExists_ShouldDespawnAndRemove()
        {
            // Arrange
            var followerId = Guid.NewGuid();
            var follower = CreateFollowerWithPedHandle("blue", DefenderTier.Basic, 42);

            _followerServiceMock.Setup(s => s.GetFollowerById(followerId)).Returns(follower);
            _followerServiceMock.Setup(s => s.DismissFollower(followerId)).Returns(true);

            // Act
            var result = _manager.DismissFollower(followerId);

            // Assert
            Assert.True(result);
            _gameBridgeMock.Verify(g => g.DeletePed(42), Times.Once);
            _followerServiceMock.Verify(s => s.DismissFollower(followerId), Times.Once);
        }

        [Fact]
        public void DismissFollower_WhenFollowerNotFound_ShouldReturnFalse()
        {
            // Arrange
            var followerId = Guid.NewGuid();
            _followerServiceMock.Setup(s => s.GetFollowerById(followerId)).Returns((Follower?)null);

            // Act
            var result = _manager.DismissFollower(followerId);

            // Assert
            Assert.False(result);
            _gameBridgeMock.Verify(g => g.DeletePed(It.IsAny<int>()), Times.Never);
        }

        #endregion

        #region DismissAllFollowers Tests

        [Fact]
        public void DismissAllFollowers_ShouldDespawnAllAndDismissFromService()
        {
            // Arrange
            var factionId = "blue";
            var followers = new List<Follower>
            {
                CreateFollowerWithPedHandle(factionId, DefenderTier.Basic, 1),
                CreateFollowerWithPedHandle(factionId, DefenderTier.Medium, 2),
                CreateFollowerWithPedHandle(factionId, DefenderTier.Heavy, 3)
            };

            _followerServiceMock.Setup(s => s.GetFollowers(factionId)).Returns(followers);

            // Act
            _manager.DismissAllFollowers(factionId);

            // Assert
            _gameBridgeMock.Verify(g => g.DeletePed(1), Times.Once);
            _gameBridgeMock.Verify(g => g.DeletePed(2), Times.Once);
            _gameBridgeMock.Verify(g => g.DeletePed(3), Times.Once);
            _followerServiceMock.Verify(s => s.DismissAllFollowers(factionId), Times.Once);
        }

        [Fact]
        public void DismissAllFollowers_WithNullFactionId_ShouldDoNothing()
        {
            // Act - should not throw
            _manager.DismissAllFollowers(null!);

            // Assert
            _followerServiceMock.Verify(s => s.DismissAllFollowers(It.IsAny<string>()), Times.Never);
        }

        [Fact]
        public void DismissAllFollowers_WithNoFollowers_ShouldStillCallService()
        {
            // Arrange
            var factionId = "blue";
            _followerServiceMock.Setup(s => s.GetFollowers(factionId)).Returns(new List<Follower>());

            // Act
            _manager.DismissAllFollowers(factionId);

            // Assert
            _followerServiceMock.Verify(s => s.DismissAllFollowers(factionId), Times.Once);
        }

        #endregion

        #region Update Tests

        [Fact]
        public void Update_WhenFollowerDies_ShouldHandleDeathAndDespawn()
        {
            // Arrange
            var factionId = "blue";
            var follower = CreateFollowerWithPedHandle(factionId, DefenderTier.Basic, 42);
            var followers = new List<Follower> { follower };

            _followerServiceMock.Setup(s => s.GetFollowers(factionId)).Returns(followers);
            _gameBridgeMock.Setup(g => g.IsPedAlive(42)).Returns(false);

            // Act
            _manager.Update(factionId);

            // Assert
            _followerServiceMock.Verify(s => s.HandleFollowerDeath(follower.Id), Times.Once);
            _gameBridgeMock.Verify(g => g.DeletePed(42), Times.Once);
        }

        [Fact]
        public void Update_WhenFollowerAlive_ShouldNotHandleDeath()
        {
            // Arrange
            var factionId = "blue";
            var follower = CreateFollowerWithPedHandle(factionId, DefenderTier.Basic, 42);
            var followers = new List<Follower> { follower };

            _followerServiceMock.Setup(s => s.GetFollowers(factionId)).Returns(followers);
            _gameBridgeMock.Setup(g => g.IsPedAlive(42)).Returns(true);

            // Act
            _manager.Update(factionId);

            // Assert
            _followerServiceMock.Verify(s => s.HandleFollowerDeath(It.IsAny<Guid>()), Times.Never);
        }

        [Fact]
        public void Update_WithUnspawnedFollower_ShouldSkip()
        {
            // Arrange
            var factionId = "blue";
            var follower = new Follower(factionId, DefenderTier.Basic, -1); // Not spawned (handle = -1)
            var followers = new List<Follower> { follower };

            _followerServiceMock.Setup(s => s.GetFollowers(factionId)).Returns(followers);

            // Act
            _manager.Update(factionId);

            // Assert - should not check ped alive for unspawned follower
            _gameBridgeMock.Verify(g => g.IsPedAlive(It.IsAny<int>()), Times.Never);
        }

        [Fact]
        public void Update_WithNullFactionId_ShouldDoNothing()
        {
            // Act - should not throw
            _manager.Update(null!);

            // Assert
            _followerServiceMock.Verify(s => s.GetFollowers(It.IsAny<string>()), Times.Never);
        }

        [Fact]
        public void Update_ShouldRaiseFollowerDiedEvent()
        {
            // Arrange
            var factionId = "blue";
            var follower = CreateFollowerWithPedHandle(factionId, DefenderTier.Basic, 42);
            var followers = new List<Follower> { follower };

            _followerServiceMock.Setup(s => s.GetFollowers(factionId)).Returns(followers);
            _gameBridgeMock.Setup(g => g.IsPedAlive(42)).Returns(false);

            Follower? diedFollower = null;
            _manager.FollowerDied += (sender, f) => diedFollower = f;

            // Act
            _manager.Update(factionId);

            // Assert
            Assert.NotNull(diedFollower);
            Assert.Equal(follower.Id, diedFollower!.Id);
        }

        #endregion

        #region GetFollowerCount Tests

        [Fact]
        public void GetFollowerCount_ShouldDelegateToService()
        {
            // Arrange
            _followerServiceMock.Setup(s => s.GetFollowerCount("blue")).Returns(3);

            // Act
            var count = _manager.GetFollowerCount("blue");

            // Assert
            Assert.Equal(3, count);
        }

        #endregion

        #region GetMaxFollowers Tests

        [Fact]
        public void GetMaxFollowers_ShouldDelegateToService()
        {
            // Arrange
            _followerServiceMock.Setup(s => s.GetMaxFollowers()).Returns(6);

            // Act
            var max = _manager.GetMaxFollowers();

            // Assert
            Assert.Equal(6, max);
        }

        #endregion

        #region GetFollowers Tests

        [Fact]
        public void GetFollowers_ShouldDelegateToService()
        {
            // Arrange
            var followers = new List<Follower>
            {
                new Follower("blue", DefenderTier.Basic),
                new Follower("blue", DefenderTier.Medium)
            };
            _followerServiceMock.Setup(s => s.GetFollowers("blue")).Returns(followers);

            // Act
            var result = _manager.GetFollowers("blue");

            // Assert
            Assert.Equal(2, result.Count);
        }

        #endregion

        #region GetRecruitCost Tests

        [Fact]
        public void GetRecruitCost_ShouldReturnTierCost()
        {
            // Arrange
            var tierConfig = new DefenderTierConfig(
                DefenderTier.Heavy,
                cost: 1000,
                health: 200,
                armor: 100,
                weapon: "weapon_smg",
                accuracy: 0.8f,
                combatModifier: 2.0f);

            _defenderTierServiceMock.Setup(s => s.GetTierConfig(DefenderTier.Heavy))
                .Returns(tierConfig);

            // Act
            var cost = _manager.GetRecruitCost(DefenderTier.Heavy);

            // Assert
            Assert.Equal(1000, cost);
        }

        #endregion

        #region SetModelForTier Tests

        [Fact]
        public void SetModelForTier_ShouldStoreModel()
        {
            // Arrange & Act
            _manager.SetModelForTier(DefenderTier.Basic, "g_m_y_lost_01");
            _manager.SetModelForTier(DefenderTier.Medium, "g_m_y_lost_02");
            _manager.SetModelForTier(DefenderTier.Heavy, "g_m_y_lost_03");

            // Assert - verify through recruitment
            var follower = new Follower("blue", DefenderTier.Heavy);
            _followerServiceMock.Setup(s => s.Recruit("blue", DefenderTier.Heavy))
                .Returns(FollowerRecruitResult.Succeeded(follower));
            _gameBridgeMock.Setup(g => g.GetPlayerPosition()).Returns(new Vector3(0, 0, 0));
            _pedSpawningServiceMock.Setup(s => s.CanSpawn()).Returns(true);
            _pedSpawningServiceMock.Setup(s => s.SpawnPed(It.IsAny<string>(), It.IsAny<Vector3>(), It.IsAny<string>(), null))
                .Returns(new PedHandle(1, "blue", default, "g_m_y_lost_03", null));

            _manager.RecruitFollower("blue", DefenderTier.Heavy);

            _pedSpawningServiceMock.Verify(s => s.SpawnPed("g_m_y_lost_03", It.IsAny<Vector3>(), "blue", null), Times.Once);
        }

        #endregion

        #region RecruitFollowerWithCost Tests

        [Fact]
        public void RecruitFollower_ShouldDeductCostFromPlayerMoney()
        {
            // Arrange
            var factionId = "blue";
            var tier = DefenderTier.Medium;
            var cost = 500;
            var playerMoney = 1000;
            var playerPos = new Vector3(100f, 200f, 30f);
            var follower = new Follower(factionId, tier);

            _gameBridgeMock.Setup(g => g.GetPlayerMoney()).Returns(playerMoney);
            _gameBridgeMock.Setup(g => g.GetPlayerPosition()).Returns(playerPos);
            _defenderTierServiceMock.Setup(s => s.GetTierConfig(tier))
                .Returns(new DefenderTierConfig(tier, cost, 150, 50, "weapon_pistol", 0.6f, 1.5f));
            _followerServiceMock.Setup(s => s.Recruit(factionId, tier))
                .Returns(FollowerRecruitResult.Succeeded(follower));
            _pedSpawningServiceMock.Setup(s => s.CanSpawn()).Returns(true);
            _pedSpawningServiceMock.Setup(s => s.SpawnPed(It.IsAny<string>(), It.IsAny<Vector3>(), factionId, null))
                .Returns(new PedHandle(42, factionId, playerPos, "medium_model", null));

            _manager.SetModelForTier(tier, "medium_model");

            // Act
            var result = _manager.RecruitFollower(factionId, tier);

            // Assert
            Assert.True(result.Success);
            _gameBridgeMock.Verify(g => g.AddPlayerMoney(-cost), Times.Once);
        }

        [Fact]
        public void RecruitFollower_WithInsufficientFunds_ShouldReturnFailure()
        {
            // Arrange
            var factionId = "blue";
            var tier = DefenderTier.Heavy;
            var cost = 1000;
            var playerMoney = 500; // Not enough

            _gameBridgeMock.Setup(g => g.GetPlayerMoney()).Returns(playerMoney);
            _defenderTierServiceMock.Setup(s => s.GetTierConfig(tier))
                .Returns(new DefenderTierConfig(tier, cost, 200, 100, "weapon_smg", 0.8f, 2.0f));

            // Act
            var result = _manager.RecruitFollower(factionId, tier);

            // Assert
            Assert.False(result.Success);
            Assert.Equal(FollowerRecruitFailureReason.InsufficientFunds, result.FailureReason);
            _gameBridgeMock.Verify(g => g.AddPlayerMoney(It.IsAny<int>()), Times.Never);
        }

        [Fact]
        public void RecruitFollower_ShouldSetPedAsFollower()
        {
            // Arrange
            var factionId = "blue";
            var tier = DefenderTier.Basic;
            var cost = 200;
            var playerMoney = 1000;
            var playerPos = new Vector3(100f, 200f, 30f);
            var follower = new Follower(factionId, tier);
            var pedHandle = 42;

            _gameBridgeMock.Setup(g => g.GetPlayerMoney()).Returns(playerMoney);
            _gameBridgeMock.Setup(g => g.GetPlayerPosition()).Returns(playerPos);
            _defenderTierServiceMock.Setup(s => s.GetTierConfig(tier))
                .Returns(new DefenderTierConfig(tier, cost, 100, 0, "weapon_pistol", 0.4f, 1.0f));
            _followerServiceMock.Setup(s => s.Recruit(factionId, tier))
                .Returns(FollowerRecruitResult.Succeeded(follower));
            _pedSpawningServiceMock.Setup(s => s.CanSpawn()).Returns(true);
            _pedSpawningServiceMock.Setup(s => s.SpawnPed(It.IsAny<string>(), It.IsAny<Vector3>(), factionId, null))
                .Returns(new PedHandle(pedHandle, factionId, playerPos, "basic_model", null));

            _manager.SetModelForTier(tier, "basic_model");

            // Act
            var result = _manager.RecruitFollower(factionId, tier);

            // Assert
            Assert.True(result.Success);
            _gameBridgeMock.Verify(g => g.SetPedAsFollower(pedHandle), Times.Once);
        }

        [Fact]
        public void RecruitFollower_WhenSpawnFails_ShouldNotDeductMoney()
        {
            // Arrange
            var factionId = "blue";
            var tier = DefenderTier.Basic;
            var cost = 200;
            var playerMoney = 1000;
            var playerPos = new Vector3(100f, 200f, 30f);
            var follower = new Follower(factionId, tier);

            _gameBridgeMock.Setup(g => g.GetPlayerMoney()).Returns(playerMoney);
            _gameBridgeMock.Setup(g => g.GetPlayerPosition()).Returns(playerPos);
            _defenderTierServiceMock.Setup(s => s.GetTierConfig(tier))
                .Returns(new DefenderTierConfig(tier, cost, 100, 0, "weapon_pistol", 0.4f, 1.0f));
            _followerServiceMock.Setup(s => s.Recruit(factionId, tier))
                .Returns(FollowerRecruitResult.Succeeded(follower));
            _pedSpawningServiceMock.Setup(s => s.CanSpawn()).Returns(false);

            _manager.SetModelForTier(tier, "basic_model");

            // Act
            var result = _manager.RecruitFollower(factionId, tier);

            // Assert
            Assert.False(result.Success);
            Assert.Equal(FollowerRecruitFailureReason.SpawnFailed, result.FailureReason);
            // Money should not be deducted since spawn failed
            _gameBridgeMock.Verify(g => g.AddPlayerMoney(It.IsAny<int>()), Times.Never);
        }

        [Fact]
        public void CanRecruitWithCost_WhenInsufficientFunds_ShouldReturnFalse()
        {
            // Arrange
            var tier = DefenderTier.Heavy;
            var cost = 1000;
            var playerMoney = 500;

            _followerServiceMock.Setup(s => s.GetFollowerCount("blue")).Returns(0);
            _followerServiceMock.Setup(s => s.GetMaxFollowers()).Returns(6);
            _pedSpawningServiceMock.Setup(s => s.CanSpawn()).Returns(true);
            _gameBridgeMock.Setup(g => g.GetPlayerMoney()).Returns(playerMoney);
            _defenderTierServiceMock.Setup(s => s.GetTierConfig(tier))
                .Returns(new DefenderTierConfig(tier, cost, 200, 100, "weapon_smg", 0.8f, 2.0f));

            // Act
            var canRecruit = _manager.CanRecruitWithCost("blue", tier);

            // Assert
            Assert.False(canRecruit);
        }

        [Fact]
        public void CanRecruitWithCost_WhenSufficientFunds_ShouldReturnTrue()
        {
            // Arrange
            var tier = DefenderTier.Basic;
            var cost = 200;
            var playerMoney = 1000;

            _followerServiceMock.Setup(s => s.GetFollowerCount("blue")).Returns(0);
            _followerServiceMock.Setup(s => s.GetMaxFollowers()).Returns(6);
            _pedSpawningServiceMock.Setup(s => s.CanSpawn()).Returns(true);
            _gameBridgeMock.Setup(g => g.GetPlayerMoney()).Returns(playerMoney);
            _defenderTierServiceMock.Setup(s => s.GetTierConfig(tier))
                .Returns(new DefenderTierConfig(tier, cost, 100, 0, "weapon_pistol", 0.4f, 1.0f));

            // Act
            var canRecruit = _manager.CanRecruitWithCost("blue", tier);

            // Assert
            Assert.True(canRecruit);
        }

        #endregion

        #region CanRecruit Tests

        [Fact]
        public void CanRecruit_WhenBelowMax_ShouldReturnTrue()
        {
            // Arrange
            _followerServiceMock.Setup(s => s.GetFollowerCount("blue")).Returns(3);
            _followerServiceMock.Setup(s => s.GetMaxFollowers()).Returns(6);
            _pedSpawningServiceMock.Setup(s => s.CanSpawn()).Returns(true);

            // Act
            var canRecruit = _manager.CanRecruit("blue");

            // Assert
            Assert.True(canRecruit);
        }

        [Fact]
        public void CanRecruit_WhenAtMax_ShouldReturnFalse()
        {
            // Arrange
            _followerServiceMock.Setup(s => s.GetFollowerCount("blue")).Returns(6);
            _followerServiceMock.Setup(s => s.GetMaxFollowers()).Returns(6);

            // Act
            var canRecruit = _manager.CanRecruit("blue");

            // Assert
            Assert.False(canRecruit);
        }

        [Fact]
        public void CanRecruit_WhenSpawnPoolFull_ShouldReturnFalse()
        {
            // Arrange
            _followerServiceMock.Setup(s => s.GetFollowerCount("blue")).Returns(3);
            _followerServiceMock.Setup(s => s.GetMaxFollowers()).Returns(6);
            _pedSpawningServiceMock.Setup(s => s.CanSpawn()).Returns(false);

            // Act
            var canRecruit = _manager.CanRecruit("blue");

            // Assert
            Assert.False(canRecruit);
        }

        #endregion

        #region Vehicle Behavior Tests

        [Fact]
        public void Update_WhenPlayerEntersVehicle_ShouldOrderFollowersToEnterVehicle()
        {
            // Arrange
            var factionId = "blue";
            var follower = CreateFollowerWithPedHandle(factionId, DefenderTier.Basic, 42);
            var followers = new List<Follower> { follower };
            var vehicleHandle = 100;

            _followerServiceMock.Setup(s => s.GetFollowers(factionId)).Returns(followers);
            _gameBridgeMock.Setup(g => g.IsPedAlive(42)).Returns(true);
            _gameBridgeMock.Setup(g => g.IsPlayerInVehicle()).Returns(true);
            _gameBridgeMock.Setup(g => g.GetPlayerVehicle()).Returns(vehicleHandle);
            _gameBridgeMock.Setup(g => g.IsPedInVehicle(42)).Returns(false);
            _seatPriorityServiceMock.Setup(s => s.GetPrioritizedFreeSeats(vehicleHandle)).Returns(new[] { 1, 2 });
            _seatPriorityServiceMock.Setup(s => s.FilterFollowersByProximity(It.IsAny<int[]>(), vehicleHandle, 15f))
                .Returns(new[] { 42 });

            // Act
            _manager.Update(factionId);

            // Assert
            _gameBridgeMock.Verify(g => g.TaskPedEnterVehicle(42, vehicleHandle, It.IsAny<int>()), Times.Once);
        }

        [Fact]
        public void Update_WhenPlayerExitsVehicle_ShouldOrderFollowersToExitVehicle()
        {
            // Arrange
            var factionId = "blue";
            var follower = CreateFollowerWithPedHandle(factionId, DefenderTier.Basic, 42);
            var followers = new List<Follower> { follower };

            _followerServiceMock.Setup(s => s.GetFollowers(factionId)).Returns(followers);
            _gameBridgeMock.Setup(g => g.IsPedAlive(42)).Returns(true);
            _gameBridgeMock.Setup(g => g.IsPlayerInVehicle()).Returns(false);
            _gameBridgeMock.Setup(g => g.IsPedInVehicle(42)).Returns(true);

            // Act
            _manager.Update(factionId);

            // Assert
            _gameBridgeMock.Verify(g => g.TaskPedLeaveVehicle(42), Times.Once);
        }

        [Fact]
        public void Update_WhenPlayerInVehicleAndFollowerAlreadyInVehicle_ShouldNotEnterAgain()
        {
            // Arrange
            var factionId = "blue";
            var follower = CreateFollowerWithPedHandle(factionId, DefenderTier.Basic, 42);
            var followers = new List<Follower> { follower };
            var vehicleHandle = 100;

            _followerServiceMock.Setup(s => s.GetFollowers(factionId)).Returns(followers);
            _gameBridgeMock.Setup(g => g.IsPedAlive(42)).Returns(true);
            _gameBridgeMock.Setup(g => g.IsPlayerInVehicle()).Returns(true);
            _gameBridgeMock.Setup(g => g.GetPlayerVehicle()).Returns(vehicleHandle);
            _gameBridgeMock.Setup(g => g.IsPedInVehicle(42)).Returns(true); // Already in vehicle
            _seatPriorityServiceMock.Setup(s => s.GetPrioritizedFreeSeats(vehicleHandle)).Returns(new[] { 1, 2 });
            _seatPriorityServiceMock.Setup(s => s.FilterFollowersByProximity(It.IsAny<int[]>(), vehicleHandle, 15f))
                .Returns(new[] { 42 });

            // Act
            _manager.Update(factionId);

            // Assert
            _gameBridgeMock.Verify(g => g.TaskPedEnterVehicle(It.IsAny<int>(), It.IsAny<int>(), It.IsAny<int>()), Times.Never);
        }

        [Fact]
        public void Update_WhenPlayerNotInVehicleAndFollowerNotInVehicle_ShouldNotExitVehicle()
        {
            // Arrange
            var factionId = "blue";
            var follower = CreateFollowerWithPedHandle(factionId, DefenderTier.Basic, 42);
            var followers = new List<Follower> { follower };

            _followerServiceMock.Setup(s => s.GetFollowers(factionId)).Returns(followers);
            _gameBridgeMock.Setup(g => g.IsPedAlive(42)).Returns(true);
            _gameBridgeMock.Setup(g => g.IsPlayerInVehicle()).Returns(false);
            _gameBridgeMock.Setup(g => g.IsPedInVehicle(42)).Returns(false);

            // Act
            _manager.Update(factionId);

            // Assert
            _gameBridgeMock.Verify(g => g.TaskPedLeaveVehicle(It.IsAny<int>()), Times.Never);
        }

        [Fact]
        public void Update_WhenOnFootFollowerLostPlayerGroup_ShouldSetPedAsFollowerAgain()
        {
            // Arrange
            var factionId = "blue";
            var follower = CreateFollowerWithPedHandle(factionId, DefenderTier.Basic, 42);
            var followers = new List<Follower> { follower };

            _followerServiceMock.Setup(s => s.GetFollowers(factionId)).Returns(followers);
            _gameBridgeMock.Setup(g => g.IsPedAlive(42)).Returns(true);
            _gameBridgeMock.Setup(g => g.IsPlayerInVehicle()).Returns(false);
            _gameBridgeMock.Setup(g => g.IsPlayerDead()).Returns(false);
            _gameBridgeMock.Setup(g => g.IsPedInVehicle(42)).Returns(false);
            _gameBridgeMock.Setup(g => g.IsPedFollowingPlayer(42)).Returns(false);

            // Act
            _manager.Update(factionId);

            // Assert
            _gameBridgeMock.Verify(g => g.SetPedAsFollower(42), Times.Once);
        }

        [Fact]
        public void Update_WhenPlayerDead_ShouldNotRepairFollowerGroup()
        {
            // Arrange
            var factionId = "blue";
            var follower = CreateFollowerWithPedHandle(factionId, DefenderTier.Basic, 42);
            var followers = new List<Follower> { follower };

            _followerServiceMock.Setup(s => s.GetFollowers(factionId)).Returns(followers);
            _gameBridgeMock.Setup(g => g.IsPedAlive(42)).Returns(true);
            _gameBridgeMock.Setup(g => g.IsPlayerInVehicle()).Returns(false);
            _gameBridgeMock.Setup(g => g.IsPlayerDead()).Returns(true);
            _gameBridgeMock.Setup(g => g.IsPedFollowingPlayer(42)).Returns(false);

            // Act
            _manager.Update(factionId);

            // Assert
            _gameBridgeMock.Verify(g => g.SetPedAsFollower(It.IsAny<int>()), Times.Never);
        }

        [Fact]
        public void Update_OnFootFollowerStuckNotFollowing_ShouldReassertOncePerInterval()
        {
            // Arrange: a follower that never registers as "following" (as happens in-game
            // when its own combat task detaches it from the player group). Without a
            // throttle this re-runs the full SetPedAsFollower reconfigure every tick.
            var factionId = "blue";
            var follower = CreateFollowerWithPedHandle(factionId, DefenderTier.Basic, 42);
            var followers = new List<Follower> { follower };

            _followerServiceMock.Setup(s => s.GetFollowers(factionId)).Returns(followers);
            _gameBridgeMock.Setup(g => g.IsPedAlive(42)).Returns(true);
            _gameBridgeMock.Setup(g => g.IsPlayerInVehicle()).Returns(false);
            _gameBridgeMock.Setup(g => g.IsPlayerDead()).Returns(false);
            _gameBridgeMock.Setup(g => g.IsPedInVehicle(42)).Returns(false);
            _gameBridgeMock.Setup(g => g.IsPedFollowingPlayer(42)).Returns(false);
            _gameBridgeMock.Setup(g => g.IsPedInCombat(42)).Returns(false);
            _gameBridgeMock.Setup(g => g.GetGameTime()).Returns(1000);

            // Act: three ticks at the same game time (well within the throttle window)
            _manager.Update(factionId);
            _manager.Update(factionId);
            _manager.Update(factionId);

            // Assert: reasserted once, not once per tick
            _gameBridgeMock.Verify(g => g.SetPedAsFollower(42), Times.Once);
        }

        [Fact]
        public void Update_OnFootFollowerInCombat_ShouldNotReassertFollow()
        {
            // Arrange: an out-of-group follower that is actively fighting. Re-asserting
            // would clear its tasks and cancel combat, so it must be left alone.
            var factionId = "blue";
            var follower = CreateFollowerWithPedHandle(factionId, DefenderTier.Basic, 42);
            var followers = new List<Follower> { follower };

            _followerServiceMock.Setup(s => s.GetFollowers(factionId)).Returns(followers);
            _gameBridgeMock.Setup(g => g.IsPedAlive(42)).Returns(true);
            _gameBridgeMock.Setup(g => g.IsPlayerInVehicle()).Returns(false);
            _gameBridgeMock.Setup(g => g.IsPlayerDead()).Returns(false);
            _gameBridgeMock.Setup(g => g.IsPedInVehicle(42)).Returns(false);
            _gameBridgeMock.Setup(g => g.IsPedFollowingPlayer(42)).Returns(false);
            _gameBridgeMock.Setup(g => g.IsPedInCombat(42)).Returns(true);

            // Act
            _manager.Update(factionId);

            // Assert
            _gameBridgeMock.Verify(g => g.SetPedAsFollower(It.IsAny<int>()), Times.Never);
        }

        [Fact]
        public void Update_WhenNoFreeSeatsInVehicle_ShouldNotOrderFollowerToEnter()
        {
            // Arrange
            var factionId = "blue";
            var follower = CreateFollowerWithPedHandle(factionId, DefenderTier.Basic, 42);
            var followers = new List<Follower> { follower };
            var vehicleHandle = 100;

            _followerServiceMock.Setup(s => s.GetFollowers(factionId)).Returns(followers);
            _gameBridgeMock.Setup(g => g.IsPedAlive(42)).Returns(true);
            _gameBridgeMock.Setup(g => g.IsPlayerInVehicle()).Returns(true);
            _gameBridgeMock.Setup(g => g.GetPlayerVehicle()).Returns(vehicleHandle);
            _gameBridgeMock.Setup(g => g.IsPedInVehicle(42)).Returns(false);
            _seatPriorityServiceMock.Setup(s => s.GetPrioritizedFreeSeats(vehicleHandle)).Returns(Array.Empty<int>()); // No free seats
            _seatPriorityServiceMock.Setup(s => s.FilterFollowersByProximity(It.IsAny<int[]>(), vehicleHandle, 15f))
                .Returns(new[] { 42 });

            // Act
            _manager.Update(factionId);

            // Assert
            _gameBridgeMock.Verify(g => g.TaskPedEnterVehicle(It.IsAny<int>(), It.IsAny<int>(), It.IsAny<int>()), Times.Never);
        }

        [Fact]
        public void Update_WithMultipleFollowers_ShouldAssignDifferentSeats()
        {
            // Arrange
            var factionId = "blue";
            var follower1 = CreateFollowerWithPedHandle(factionId, DefenderTier.Basic, 42);
            var follower2 = CreateFollowerWithPedHandle(factionId, DefenderTier.Medium, 43);
            var followers = new List<Follower> { follower1, follower2 };
            var vehicleHandle = 100;

            _followerServiceMock.Setup(s => s.GetFollowers(factionId)).Returns(followers);
            _gameBridgeMock.Setup(g => g.IsPedAlive(42)).Returns(true);
            _gameBridgeMock.Setup(g => g.IsPedAlive(43)).Returns(true);
            _gameBridgeMock.Setup(g => g.IsPlayerInVehicle()).Returns(true);
            _gameBridgeMock.Setup(g => g.GetPlayerVehicle()).Returns(vehicleHandle);
            _gameBridgeMock.Setup(g => g.IsPedInVehicle(42)).Returns(false);
            _gameBridgeMock.Setup(g => g.IsPedInVehicle(43)).Returns(false);
            _seatPriorityServiceMock.Setup(s => s.GetPrioritizedFreeSeats(vehicleHandle)).Returns(new[] { 1, 2 });
            _seatPriorityServiceMock.Setup(s => s.FilterFollowersByProximity(It.IsAny<int[]>(), vehicleHandle, 15f))
                .Returns(new[] { 42, 43 });

            // Act
            _manager.Update(factionId);

            // Assert - both followers should be assigned different seats
            _gameBridgeMock.Verify(g => g.TaskPedEnterVehicle(42, vehicleHandle, 1), Times.Once);
            _gameBridgeMock.Verify(g => g.TaskPedEnterVehicle(43, vehicleHandle, 2), Times.Once);
        }

        [Fact]
        public void Update_WithMoreFollowersThanSeats_ShouldOnlyFillAvailableSeats()
        {
            // Arrange
            var factionId = "blue";
            var follower1 = CreateFollowerWithPedHandle(factionId, DefenderTier.Basic, 42);
            var follower2 = CreateFollowerWithPedHandle(factionId, DefenderTier.Medium, 43);
            var follower3 = CreateFollowerWithPedHandle(factionId, DefenderTier.Heavy, 44);
            var followers = new List<Follower> { follower1, follower2, follower3 };
            var vehicleHandle = 100;

            _followerServiceMock.Setup(s => s.GetFollowers(factionId)).Returns(followers);
            _gameBridgeMock.Setup(g => g.IsPedAlive(It.IsAny<int>())).Returns(true);
            _gameBridgeMock.Setup(g => g.IsPlayerInVehicle()).Returns(true);
            _gameBridgeMock.Setup(g => g.GetPlayerVehicle()).Returns(vehicleHandle);
            _gameBridgeMock.Setup(g => g.IsPedInVehicle(It.IsAny<int>())).Returns(false);
            _seatPriorityServiceMock.Setup(s => s.GetPrioritizedFreeSeats(vehicleHandle)).Returns(new[] { 1 }); // Only one free seat
            _seatPriorityServiceMock.Setup(s => s.FilterFollowersByProximity(It.IsAny<int[]>(), vehicleHandle, 15f))
                .Returns(new[] { 42, 43, 44 });

            // Act
            _manager.Update(factionId);

            // Assert - only one follower should enter
            _gameBridgeMock.Verify(g => g.TaskPedEnterVehicle(It.IsAny<int>(), vehicleHandle, It.IsAny<int>()), Times.Once);
        }

        [Fact]
        public void Update_WhenFollowerTooFarFromVehicle_ShouldNotEnterVehicle()
        {
            // Arrange
            var factionId = "blue";
            var follower = CreateFollowerWithPedHandle(factionId, DefenderTier.Basic, 42);
            var followers = new List<Follower> { follower };
            var vehicleHandle = 100;

            _followerServiceMock.Setup(s => s.GetFollowers(factionId)).Returns(followers);
            _gameBridgeMock.Setup(g => g.IsPedAlive(42)).Returns(true);
            _gameBridgeMock.Setup(g => g.IsPlayerInVehicle()).Returns(true);
            _gameBridgeMock.Setup(g => g.GetPlayerVehicle()).Returns(vehicleHandle);
            _gameBridgeMock.Setup(g => g.IsPedInVehicle(42)).Returns(false);
            _seatPriorityServiceMock.Setup(s => s.GetPrioritizedFreeSeats(vehicleHandle)).Returns(new[] { 1, 2 });
            // Follower is too far, filtered out
            _seatPriorityServiceMock.Setup(s => s.FilterFollowersByProximity(It.IsAny<int[]>(), vehicleHandle, 15f))
                .Returns(Array.Empty<int>());

            // Act
            _manager.Update(factionId);

            // Assert - follower should not enter since they're too far
            _gameBridgeMock.Verify(g => g.TaskPedEnterVehicle(It.IsAny<int>(), It.IsAny<int>(), It.IsAny<int>()), Times.Never);
        }

        [Fact]
        public void Update_ShouldUsePrioritizedSeatsFromService()
        {
            // Arrange
            var factionId = "blue";
            var follower = CreateFollowerWithPedHandle(factionId, DefenderTier.Basic, 42);
            var followers = new List<Follower> { follower };
            var vehicleHandle = 100;

            _followerServiceMock.Setup(s => s.GetFollowers(factionId)).Returns(followers);
            _gameBridgeMock.Setup(g => g.IsPedAlive(42)).Returns(true);
            _gameBridgeMock.Setup(g => g.IsPlayerInVehicle()).Returns(true);
            _gameBridgeMock.Setup(g => g.GetPlayerVehicle()).Returns(vehicleHandle);
            _gameBridgeMock.Setup(g => g.IsPedInVehicle(42)).Returns(false);
            // Return turret seat (8) as highest priority
            _seatPriorityServiceMock.Setup(s => s.GetPrioritizedFreeSeats(vehicleHandle)).Returns(new[] { 8, 1, 2 });
            _seatPriorityServiceMock.Setup(s => s.FilterFollowersByProximity(It.IsAny<int[]>(), vehicleHandle, 15f))
                .Returns(new[] { 42 });

            // Act
            _manager.Update(factionId);

            // Assert - follower should get seat 8 (turret, highest priority)
            _gameBridgeMock.Verify(g => g.TaskPedEnterVehicle(42, vehicleHandle, 8), Times.Once);
        }

        #endregion

        #region Combat Behavior Tests

        [Fact]
        public void RecruitFollower_ShouldGiveTierAppropriateWeapon()
        {
            // Arrange
            var factionId = "blue";
            var tier = DefenderTier.Heavy;
            var playerPos = new Vector3(100f, 200f, 30f);
            var follower = new Follower(factionId, tier);
            var pedHandle = 42;

            _gameBridgeMock.Setup(g => g.GetPlayerMoney()).Returns(10000);
            _gameBridgeMock.Setup(g => g.GetPlayerPosition()).Returns(playerPos);
            _defenderTierServiceMock.Setup(s => s.GetTierConfig(tier))
                .Returns(new DefenderTierConfig(tier, 1000, 200, 100, "weapon_carbinerifle", 0.7f, 2.0f));
            _followerServiceMock.Setup(s => s.Recruit(factionId, tier))
                .Returns(FollowerRecruitResult.Succeeded(follower));
            _pedSpawningServiceMock.Setup(s => s.CanSpawn()).Returns(true);
            _pedSpawningServiceMock.Setup(s => s.SpawnPed(It.IsAny<string>(), It.IsAny<Vector3>(), factionId, null))
                .Returns(new PedHandle(pedHandle, factionId, playerPos, "heavy_model", null));

            _manager.SetModelForTier(tier, "heavy_model");

            // Act
            var result = _manager.RecruitFollower(factionId, tier);

            // Assert
            Assert.True(result.Success);
            _gameBridgeMock.Verify(g => g.GivePedWeapon(pedHandle, "weapon_carbinerifle"), Times.Once);
        }

        [Theory]
        [InlineData(DefenderTier.Basic, "weapon_pistol")]
        [InlineData(DefenderTier.Medium, "weapon_smg")]
        [InlineData(DefenderTier.Heavy, "weapon_carbinerifle")]
        public void RecruitFollower_ShouldGiveCorrectWeaponPerTier(DefenderTier tier, string expectedWeapon)
        {
            // Arrange
            var factionId = "blue";
            var playerPos = new Vector3(100f, 200f, 30f);
            var follower = new Follower(factionId, tier);
            var pedHandle = 42;
            var cost = tier == DefenderTier.Basic ? 200 : tier == DefenderTier.Medium ? 500 : 1000;

            _gameBridgeMock.Setup(g => g.GetPlayerMoney()).Returns(10000);
            _gameBridgeMock.Setup(g => g.GetPlayerPosition()).Returns(playerPos);
            _defenderTierServiceMock.Setup(s => s.GetTierConfig(tier))
                .Returns(new DefenderTierConfig(tier, cost, 100, 0, expectedWeapon, 0.5f, 1.0f));
            _followerServiceMock.Setup(s => s.Recruit(factionId, tier))
                .Returns(FollowerRecruitResult.Succeeded(follower));
            _pedSpawningServiceMock.Setup(s => s.CanSpawn()).Returns(true);
            _pedSpawningServiceMock.Setup(s => s.SpawnPed(It.IsAny<string>(), It.IsAny<Vector3>(), factionId, null))
                .Returns(new PedHandle(pedHandle, factionId, playerPos, "model", null));

            _manager.SetModelForTier(tier, "model");

            // Act
            var result = _manager.RecruitFollower(factionId, tier);

            // Assert - Implementation gives pistol first (for drive-by), then tier weapon
            // For Basic tier, both are pistol so GivePedWeapon is called twice with same weapon
            Assert.True(result.Success);
            _gameBridgeMock.Verify(g => g.GivePedWeapon(pedHandle, expectedWeapon), Times.AtLeastOnce);
        }

        [Fact]
        public void RecruitFollower_ShouldSetTierAppropriateAccuracy()
        {
            // Arrange
            var factionId = "blue";
            var tier = DefenderTier.Heavy;
            var playerPos = new Vector3(100f, 200f, 30f);
            var follower = new Follower(factionId, tier);
            var pedHandle = 42;
            var expectedAccuracy = 0.7f;

            _gameBridgeMock.Setup(g => g.GetPlayerMoney()).Returns(10000);
            _gameBridgeMock.Setup(g => g.GetPlayerPosition()).Returns(playerPos);
            _defenderTierServiceMock.Setup(s => s.GetTierConfig(tier))
                .Returns(new DefenderTierConfig(tier, 1000, 200, 100, "weapon_carbinerifle", expectedAccuracy, 2.0f));
            _followerServiceMock.Setup(s => s.Recruit(factionId, tier))
                .Returns(FollowerRecruitResult.Succeeded(follower));
            _pedSpawningServiceMock.Setup(s => s.CanSpawn()).Returns(true);
            _pedSpawningServiceMock.Setup(s => s.SpawnPed(It.IsAny<string>(), It.IsAny<Vector3>(), factionId, null))
                .Returns(new PedHandle(pedHandle, factionId, playerPos, "heavy_model", null));

            _manager.SetModelForTier(tier, "heavy_model");

            // Act
            var result = _manager.RecruitFollower(factionId, tier);

            // Assert
            Assert.True(result.Success);
            _gameBridgeMock.Verify(g => g.SetPedAccuracy(pedHandle, expectedAccuracy), Times.Once);
        }

        [Theory]
        [InlineData(DefenderTier.Basic, 0.3f)]
        [InlineData(DefenderTier.Medium, 0.5f)]
        [InlineData(DefenderTier.Heavy, 0.7f)]
        public void RecruitFollower_ShouldSetCorrectAccuracyPerTier(DefenderTier tier, float expectedAccuracy)
        {
            // Arrange
            var factionId = "blue";
            var playerPos = new Vector3(100f, 200f, 30f);
            var follower = new Follower(factionId, tier);
            var pedHandle = 42;
            var cost = tier == DefenderTier.Basic ? 200 : tier == DefenderTier.Medium ? 500 : 1000;

            _gameBridgeMock.Setup(g => g.GetPlayerMoney()).Returns(10000);
            _gameBridgeMock.Setup(g => g.GetPlayerPosition()).Returns(playerPos);
            _defenderTierServiceMock.Setup(s => s.GetTierConfig(tier))
                .Returns(new DefenderTierConfig(tier, cost, 100, 50, "weapon_pistol", expectedAccuracy, 1.0f));
            _followerServiceMock.Setup(s => s.Recruit(factionId, tier))
                .Returns(FollowerRecruitResult.Succeeded(follower));
            _pedSpawningServiceMock.Setup(s => s.CanSpawn()).Returns(true);
            _pedSpawningServiceMock.Setup(s => s.SpawnPed(It.IsAny<string>(), It.IsAny<Vector3>(), factionId, null))
                .Returns(new PedHandle(pedHandle, factionId, playerPos, "model", null));

            _manager.SetModelForTier(tier, "model");

            // Act
            var result = _manager.RecruitFollower(factionId, tier);

            // Assert
            Assert.True(result.Success);
            _gameBridgeMock.Verify(g => g.SetPedAccuracy(pedHandle, expectedAccuracy), Times.Once);
        }

        [Fact]
        public void RecruitFollower_ShouldSetTierAppropriateArmor()
        {
            // Arrange
            var factionId = "blue";
            var tier = DefenderTier.Heavy;
            var playerPos = new Vector3(100f, 200f, 30f);
            var follower = new Follower(factionId, tier);
            var pedHandle = 42;
            var expectedArmor = 100;

            _gameBridgeMock.Setup(g => g.GetPlayerMoney()).Returns(10000);
            _gameBridgeMock.Setup(g => g.GetPlayerPosition()).Returns(playerPos);
            _defenderTierServiceMock.Setup(s => s.GetTierConfig(tier))
                .Returns(new DefenderTierConfig(tier, 1000, 200, expectedArmor, "weapon_carbinerifle", 0.7f, 2.0f));
            _followerServiceMock.Setup(s => s.Recruit(factionId, tier))
                .Returns(FollowerRecruitResult.Succeeded(follower));
            _pedSpawningServiceMock.Setup(s => s.CanSpawn()).Returns(true);
            _pedSpawningServiceMock.Setup(s => s.SpawnPed(It.IsAny<string>(), It.IsAny<Vector3>(), factionId, null))
                .Returns(new PedHandle(pedHandle, factionId, playerPos, "heavy_model", null));

            _manager.SetModelForTier(tier, "heavy_model");

            // Act
            var result = _manager.RecruitFollower(factionId, tier);

            // Assert
            Assert.True(result.Success);
            _gameBridgeMock.Verify(g => g.SetPedArmor(pedHandle, expectedArmor), Times.Once);
        }

        [Theory]
        [InlineData(DefenderTier.Basic, 0)]
        [InlineData(DefenderTier.Medium, 50)]
        [InlineData(DefenderTier.Heavy, 100)]
        public void RecruitFollower_ShouldSetCorrectArmorPerTier(DefenderTier tier, int expectedArmor)
        {
            // Arrange
            var factionId = "blue";
            var playerPos = new Vector3(100f, 200f, 30f);
            var follower = new Follower(factionId, tier);
            var pedHandle = 42;
            var cost = tier == DefenderTier.Basic ? 200 : tier == DefenderTier.Medium ? 500 : 1000;

            _gameBridgeMock.Setup(g => g.GetPlayerMoney()).Returns(10000);
            _gameBridgeMock.Setup(g => g.GetPlayerPosition()).Returns(playerPos);
            _defenderTierServiceMock.Setup(s => s.GetTierConfig(tier))
                .Returns(new DefenderTierConfig(tier, cost, 100, expectedArmor, "weapon_pistol", 0.5f, 1.0f));
            _followerServiceMock.Setup(s => s.Recruit(factionId, tier))
                .Returns(FollowerRecruitResult.Succeeded(follower));
            _pedSpawningServiceMock.Setup(s => s.CanSpawn()).Returns(true);
            _pedSpawningServiceMock.Setup(s => s.SpawnPed(It.IsAny<string>(), It.IsAny<Vector3>(), factionId, null))
                .Returns(new PedHandle(pedHandle, factionId, playerPos, "model", null));

            _manager.SetModelForTier(tier, "model");

            // Act
            var result = _manager.RecruitFollower(factionId, tier);

            // Assert
            Assert.True(result.Success);
            _gameBridgeMock.Verify(g => g.SetPedArmor(pedHandle, expectedArmor), Times.Once);
        }

        [Fact]
        public void RecruitFollower_ShouldConfigureCombatBehavior()
        {
            // Arrange
            var factionId = "blue";
            var tier = DefenderTier.Medium;
            var playerPos = new Vector3(100f, 200f, 30f);
            var follower = new Follower(factionId, tier);
            var pedHandle = 42;

            _gameBridgeMock.Setup(g => g.GetPlayerMoney()).Returns(10000);
            _gameBridgeMock.Setup(g => g.GetPlayerPosition()).Returns(playerPos);
            _defenderTierServiceMock.Setup(s => s.GetTierConfig(tier))
                .Returns(new DefenderTierConfig(tier, 500, 150, 50, "weapon_smg", 0.5f, 1.5f));
            _followerServiceMock.Setup(s => s.Recruit(factionId, tier))
                .Returns(FollowerRecruitResult.Succeeded(follower));
            _pedSpawningServiceMock.Setup(s => s.CanSpawn()).Returns(true);
            _pedSpawningServiceMock.Setup(s => s.SpawnPed(It.IsAny<string>(), It.IsAny<Vector3>(), factionId, null))
                .Returns(new PedHandle(pedHandle, factionId, playerPos, "medium_model", null));

            _manager.SetModelForTier(tier, "medium_model");

            // Act
            var result = _manager.RecruitFollower(factionId, tier);

            // Assert
            Assert.True(result.Success);
            // Ped should be configured to take cover and engage enemies
            _gameBridgeMock.Verify(g => g.SetPedCombatAttributes(pedHandle, true, true), Times.Once);
        }

        [Fact]
        public void RecruitFollower_ShouldSetTierAppropriateHealth()
        {
            // Arrange
            var factionId = "blue";
            var tier = DefenderTier.Heavy;
            var playerPos = new Vector3(100f, 200f, 30f);
            var follower = new Follower(factionId, tier);
            var pedHandle = 42;
            var expectedHealth = 200;

            _gameBridgeMock.Setup(g => g.GetPlayerMoney()).Returns(10000);
            _gameBridgeMock.Setup(g => g.GetPlayerPosition()).Returns(playerPos);
            _defenderTierServiceMock.Setup(s => s.GetTierConfig(tier))
                .Returns(new DefenderTierConfig(tier, 1000, expectedHealth, 100, "weapon_carbinerifle", 0.7f, 2.0f));
            _followerServiceMock.Setup(s => s.Recruit(factionId, tier))
                .Returns(FollowerRecruitResult.Succeeded(follower));
            _pedSpawningServiceMock.Setup(s => s.CanSpawn()).Returns(true);
            _pedSpawningServiceMock.Setup(s => s.SpawnPed(It.IsAny<string>(), It.IsAny<Vector3>(), factionId, null))
                .Returns(new PedHandle(pedHandle, factionId, playerPos, "heavy_model", null));

            _manager.SetModelForTier(tier, "heavy_model");

            // Act
            var result = _manager.RecruitFollower(factionId, tier);

            // Assert
            Assert.True(result.Success);
            _gameBridgeMock.Verify(g => g.SetPedHealth(pedHandle, expectedHealth), Times.Once);
            _gameBridgeMock.Verify(g => g.SetPedCriticalHitsEnabled(pedHandle, false), Times.Once);
            _gameBridgeMock.Verify(g => g.SetPedRagdollEnabled(pedHandle, false), Times.Once);
        }

        [Theory]
        [InlineData(DefenderTier.Basic, 100)]
        [InlineData(DefenderTier.Medium, 150)]
        [InlineData(DefenderTier.Heavy, 200)]
        public void RecruitFollower_ShouldSetCorrectHealthPerTier(DefenderTier tier, int expectedHealth)
        {
            // Arrange
            var factionId = "blue";
            var playerPos = new Vector3(100f, 200f, 30f);
            var follower = new Follower(factionId, tier);
            var pedHandle = 42;
            var cost = tier == DefenderTier.Basic ? 200 : tier == DefenderTier.Medium ? 500 : 1000;

            _gameBridgeMock.Setup(g => g.GetPlayerMoney()).Returns(10000);
            _gameBridgeMock.Setup(g => g.GetPlayerPosition()).Returns(playerPos);
            _defenderTierServiceMock.Setup(s => s.GetTierConfig(tier))
                .Returns(new DefenderTierConfig(tier, cost, expectedHealth, 50, "weapon_pistol", 0.5f, 1.0f));
            _followerServiceMock.Setup(s => s.Recruit(factionId, tier))
                .Returns(FollowerRecruitResult.Succeeded(follower));
            _pedSpawningServiceMock.Setup(s => s.CanSpawn()).Returns(true);
            _pedSpawningServiceMock.Setup(s => s.SpawnPed(It.IsAny<string>(), It.IsAny<Vector3>(), factionId, null))
                .Returns(new PedHandle(pedHandle, factionId, playerPos, "model", null));

            _manager.SetModelForTier(tier, "model");

            // Act
            var result = _manager.RecruitFollower(factionId, tier);

            // Assert
            Assert.True(result.Success);
            _gameBridgeMock.Verify(g => g.SetPedHealth(pedHandle, expectedHealth), Times.Once);
        }

        #endregion

        #region RestoreFollowers Tests

        [Fact]
        public void RestoreFollowers_WithSavedFollower_ShouldSpawnAndConfigureFollower()
        {
            // Arrange
            var factionId = "blue";
            var savedFollower = new SavedFollowerState
            {
                FactionId = factionId,
                Tier = DefenderTier.Medium,
                Position = new PlayerPosition { X = 10f, Y = 20f, Z = 30f },
            };
            var follower = new Follower(factionId, DefenderTier.Medium);

            _followerServiceMock.Setup(s => s.GetFollowers(factionId)).Returns(Array.Empty<Follower>());
            _followerServiceMock.Setup(s => s.DismissAllFollowers(factionId));
            _followerServiceMock.Setup(s => s.Recruit(factionId, DefenderTier.Medium))
                .Returns(FollowerRecruitResult.Succeeded(follower));
            _pedSpawningServiceMock.Setup(s => s.SpawnPed(
                    "g_m_y_lost_02",
                    It.Is<Vector3>(p => p.X == 10f && p.Y == 20f && p.Z == 30f),
                    factionId,
                    null))
                .Returns(new PedHandle(42, factionId, new Vector3(10f, 20f, 30f), "g_m_y_lost_02", null));

            // Act
            _manager.RestoreFollowers(factionId, new[] { savedFollower }, -1);

            // Assert
            Assert.Equal(42, follower.PedHandle);
            _gameBridgeMock.Verify(g => g.SetPedAsFollower(42), Times.Once);
            _pedBlipServiceMock.Verify(b => b.CreateBlipForPed(42, BlipColor.Yellow), Times.Once);
        }

        [Fact]
        public void RestoreFollowers_WithVehicleSeat_ShouldRestoreFollowerIntoVehicle()
        {
            // Arrange
            var factionId = "blue";
            var savedFollower = new SavedFollowerState
            {
                FactionId = factionId,
                Tier = DefenderTier.Basic,
                Position = new PlayerPosition { X = 10f, Y = 20f, Z = 30f },
                VehicleSeatIndex = 1,
            };
            var follower = new Follower(factionId, DefenderTier.Basic);

            _followerServiceMock.Setup(s => s.GetFollowers(factionId)).Returns(Array.Empty<Follower>());
            _followerServiceMock.Setup(s => s.Recruit(factionId, DefenderTier.Basic))
                .Returns(FollowerRecruitResult.Succeeded(follower));
            _pedSpawningServiceMock.Setup(s => s.SpawnPed(It.IsAny<string>(), It.IsAny<Vector3>(), factionId, null))
                .Returns(new PedHandle(42, factionId, new Vector3(10f, 20f, 30f), "g_m_y_lost_01", null));

            // Act
            _manager.RestoreFollowers(factionId, new[] { savedFollower }, 100);

            // Assert
            _gameBridgeMock.Verify(g => g.SetPedIntoVehicle(42, 100, 1), Times.Once);
        }

        #endregion

        #region Helper Methods

        private Follower CreateFollowerWithPedHandle(string factionId, DefenderTier tier, int pedHandle)
        {
            var follower = new Follower(factionId, tier, pedHandle);
            return follower;
        }

        #endregion
    }
}
