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
        private readonly Mock<IDefenderRoleService> _defenderRoleServiceMock;
        private readonly Mock<IPedBlipService> _pedBlipServiceMock;
        private readonly Mock<IVehicleSeatPriorityService> _seatPriorityServiceMock;
        private readonly FollowerManager _manager;

        public FollowerManagerTests()
        {
            _gameBridgeMock = new Mock<IGameBridge>();
            _followerServiceMock = new Mock<IFollowerService>();
            _pedSpawningServiceMock = new Mock<IPedSpawningService>();
            _defenderRoleServiceMock = new Mock<IDefenderRoleService>();
            _pedBlipServiceMock = new Mock<IPedBlipService>();
            _seatPriorityServiceMock = new Mock<IVehicleSeatPriorityService>();

            // Set up default tier configs (tests can override as needed)
            _defenderRoleServiceMock.Setup(s => s.GetRoleConfig(DefenderRole.Grunt))
                .Returns(new DefenderRoleConfig(DefenderRole.Grunt, 200, 100, 0, "weapon_pistol", 0.4f, 1.0f));
            _defenderRoleServiceMock.Setup(s => s.GetRoleConfig(DefenderRole.Gunner))
                .Returns(new DefenderRoleConfig(DefenderRole.Gunner, 500, 150, 50, "weapon_pistol", 0.6f, 1.5f));
            _defenderRoleServiceMock.Setup(s => s.GetRoleConfig(DefenderRole.Rifleman))
                .Returns(new DefenderRoleConfig(DefenderRole.Rifleman, 1000, 200, 100, "weapon_smg", 0.8f, 2.0f));

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
                _defenderRoleServiceMock.Object,
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
                _defenderRoleServiceMock.Object,
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
                _defenderRoleServiceMock.Object,
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
                _defenderRoleServiceMock.Object,
                _pedBlipServiceMock.Object,
                _seatPriorityServiceMock.Object));
        }

        [Fact]
        public void Constructor_WithNullDefenderRoleService_ShouldThrowArgumentNullException()
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
                _defenderRoleServiceMock.Object,
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
                _defenderRoleServiceMock.Object,
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
            var tier = DefenderRole.Grunt;
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
        public void RecruitFollower_WithSniper_ShouldSetProfessionalCombatAbilityAndFarRange()
        {
            // Arrange — snipers aim but never fire when left at the model's default
            // (Poor) combat ability; they must be set Professional ability + Far range
            // like every other combatant so they commit to firing the scoped rifle.
            var factionId = "blue";
            var tier = DefenderRole.Sniper;
            var follower = new Follower(factionId, tier);

            _defenderRoleServiceMock.Setup(s => s.GetRoleConfig(DefenderRole.Sniper))
                .Returns(new DefenderRoleConfig(DefenderRole.Sniper, 1500, 275, 50, "WEAPON_SNIPERRIFLE", 0.8f, 2.2f));
            _gameBridgeMock.Setup(g => g.GetPlayerPosition()).Returns(new Vector3(0f, 0f, 0f));
            _followerServiceMock.Setup(s => s.Recruit(factionId, tier))
                .Returns(FollowerRecruitResult.Succeeded(follower));
            _pedSpawningServiceMock.Setup(s => s.CanSpawn()).Returns(true);
            _pedSpawningServiceMock.Setup(s => s.SpawnPed(It.IsAny<string>(), It.IsAny<Vector3>(), factionId, null))
                .Returns(new PedHandle(77, factionId, new Vector3(0f, 0f, 0f), "g_m_y_lost_01", null));

            // Act
            _manager.RecruitFollower(factionId, tier);

            // Assert — Professional ability (2), Far range (2)
            _gameBridgeMock.Verify(g => g.SetPedCombatProfile(77, 2, 2), Times.Once);
        }

        [Fact]
        public void RecruitFollower_WithNonSniper_ShouldNotSetCombatProfile()
        {
            // Arrange — only snipers get the profile change; keep grunt/gunner/rifleman
            // behaviour unchanged to avoid them charging distant enemies.
            var factionId = "blue";
            var tier = DefenderRole.Grunt;
            var follower = new Follower(factionId, tier);

            _gameBridgeMock.Setup(g => g.GetPlayerPosition()).Returns(new Vector3(0f, 0f, 0f));
            _followerServiceMock.Setup(s => s.Recruit(factionId, tier))
                .Returns(FollowerRecruitResult.Succeeded(follower));
            _pedSpawningServiceMock.Setup(s => s.CanSpawn()).Returns(true);
            _pedSpawningServiceMock.Setup(s => s.SpawnPed(It.IsAny<string>(), It.IsAny<Vector3>(), factionId, null))
                .Returns(new PedHandle(88, factionId, new Vector3(0f, 0f, 0f), "g_m_y_lost_01", null));

            // Act
            _manager.RecruitFollower(factionId, tier);

            // Assert
            _gameBridgeMock.Verify(g => g.SetPedCombatProfile(It.IsAny<int>(), It.IsAny<int>(), It.IsAny<int>()), Times.Never);
        }

        [Fact]
        public void RecruitFollower_WhenMaxFollowersReached_ShouldReturnFailure()
        {
            // Arrange
            var factionId = "blue";
            var tier = DefenderRole.Grunt;

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
            var tier = DefenderRole.Grunt;
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
            Assert.Throws<ArgumentNullException>(() => _manager.RecruitFollower(null!, DefenderRole.Grunt));
        }

        [Fact]
        public void RecruitFollower_ShouldSpawnNearPlayer()
        {
            // Arrange
            var factionId = "blue";
            var tier = DefenderRole.Gunner;
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
            var follower = CreateFollowerWithPedHandle("blue", DefenderRole.Grunt, 42);

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
                CreateFollowerWithPedHandle(factionId, DefenderRole.Grunt, 1),
                CreateFollowerWithPedHandle(factionId, DefenderRole.Gunner, 2),
                CreateFollowerWithPedHandle(factionId, DefenderRole.Rifleman, 3)
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
            var follower = CreateFollowerWithPedHandle(factionId, DefenderRole.Grunt, 42);
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
            var follower = CreateFollowerWithPedHandle(factionId, DefenderRole.Grunt, 42);
            var followers = new List<Follower> { follower };

            _followerServiceMock.Setup(s => s.GetFollowers(factionId)).Returns(followers);
            _gameBridgeMock.Setup(g => g.IsPedAlive(42)).Returns(true);

            // Act
            _manager.Update(factionId);

            // Assert
            _followerServiceMock.Verify(s => s.HandleFollowerDeath(It.IsAny<Guid>()), Times.Never);
        }

        [Fact]
        public void Update_OnFoot_ShouldExposeOnlyAliveSniperBodyguardHandles()
        {
            // Arrange — only sniper bodyguards need the close-range sidearm switch,
            // so the manager must surface their handles separately from the rest.
            var factionId = "blue";
            var sniper = CreateFollowerWithPedHandle(factionId, DefenderRole.Sniper, 50);
            var grunt = CreateFollowerWithPedHandle(factionId, DefenderRole.Grunt, 60);
            var followers = new List<Follower> { sniper, grunt };

            _followerServiceMock.Setup(s => s.GetFollowers(factionId)).Returns(followers);
            _gameBridgeMock.Setup(g => g.IsPedAlive(50)).Returns(true);
            _gameBridgeMock.Setup(g => g.IsPedAlive(60)).Returns(true);

            // Act
            _manager.Update(factionId);

            // Assert
            Assert.Equal(new[] { 50 }, _manager.SniperBodyguardHandles);
        }

        [Fact]
        public void Update_WhenPlayerInVehicle_ShouldClearSniperBodyguardHandles()
        {
            // Arrange — in a vehicle snipers do drive-bys; no on-foot close defense.
            var factionId = "blue";
            var sniper = CreateFollowerWithPedHandle(factionId, DefenderRole.Sniper, 50);
            var followers = new List<Follower> { sniper };

            _followerServiceMock.Setup(s => s.GetFollowers(factionId)).Returns(followers);
            _gameBridgeMock.Setup(g => g.IsPedAlive(50)).Returns(true);
            _gameBridgeMock.Setup(g => g.IsPlayerInVehicle()).Returns(true);
            _gameBridgeMock.Setup(g => g.GetPlayerVehicle()).Returns(900);

            // Act
            _manager.Update(factionId);

            // Assert
            Assert.Empty(_manager.SniperBodyguardHandles);
        }

        [Fact]
        public void Update_WithUnspawnedFollower_ShouldSkip()
        {
            // Arrange
            var factionId = "blue";
            var follower = new Follower(factionId, DefenderRole.Grunt, -1); // Not spawned (handle = -1)
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
            var follower = CreateFollowerWithPedHandle(factionId, DefenderRole.Grunt, 42);
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
                new Follower("blue", DefenderRole.Grunt),
                new Follower("blue", DefenderRole.Gunner)
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
            var roleConfig = new DefenderRoleConfig(
                DefenderRole.Rifleman,
                cost: 1000,
                health: 200,
                armor: 100,
                weapon: "weapon_smg",
                accuracy: 0.8f,
                combatModifier: 2.0f);

            _defenderRoleServiceMock.Setup(s => s.GetRoleConfig(DefenderRole.Rifleman))
                .Returns(roleConfig);

            // Act
            var cost = _manager.GetRecruitCost(DefenderRole.Rifleman);

            // Assert
            Assert.Equal(1000, cost);
        }

        #endregion

        #region SetModelForTier Tests

        [Fact]
        public void SetModelForTier_ShouldStoreModel()
        {
            // Arrange & Act
            _manager.SetModelForTier(DefenderRole.Grunt, "g_m_y_lost_01");
            _manager.SetModelForTier(DefenderRole.Gunner, "g_m_y_lost_02");
            _manager.SetModelForTier(DefenderRole.Rifleman, "g_m_y_lost_03");

            // Assert - verify through recruitment
            var follower = new Follower("blue", DefenderRole.Rifleman);
            _followerServiceMock.Setup(s => s.Recruit("blue", DefenderRole.Rifleman))
                .Returns(FollowerRecruitResult.Succeeded(follower));
            _gameBridgeMock.Setup(g => g.GetPlayerPosition()).Returns(new Vector3(0, 0, 0));
            _pedSpawningServiceMock.Setup(s => s.CanSpawn()).Returns(true);
            _pedSpawningServiceMock.Setup(s => s.SpawnPed(It.IsAny<string>(), It.IsAny<Vector3>(), It.IsAny<string>(), null))
                .Returns(new PedHandle(1, "blue", default, "g_m_y_lost_03", null));

            _manager.RecruitFollower("blue", DefenderRole.Rifleman);

            _pedSpawningServiceMock.Verify(s => s.SpawnPed("g_m_y_lost_03", It.IsAny<Vector3>(), "blue", null), Times.Once);
        }

        #endregion

        #region RecruitFollowerWithCost Tests

        [Fact]
        public void RecruitFollower_ShouldDeductCostFromPlayerMoney()
        {
            // Arrange
            var factionId = "blue";
            var tier = DefenderRole.Gunner;
            var cost = 500;
            var playerMoney = 1000;
            var playerPos = new Vector3(100f, 200f, 30f);
            var follower = new Follower(factionId, tier);

            _gameBridgeMock.Setup(g => g.GetPlayerMoney()).Returns(playerMoney);
            _gameBridgeMock.Setup(g => g.GetPlayerPosition()).Returns(playerPos);
            _defenderRoleServiceMock.Setup(s => s.GetRoleConfig(tier))
                .Returns(new DefenderRoleConfig(tier, cost, 150, 50, "weapon_pistol", 0.6f, 1.5f));
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
            var tier = DefenderRole.Rifleman;
            var cost = 1000;
            var playerMoney = 500; // Not enough

            _gameBridgeMock.Setup(g => g.GetPlayerMoney()).Returns(playerMoney);
            _defenderRoleServiceMock.Setup(s => s.GetRoleConfig(tier))
                .Returns(new DefenderRoleConfig(tier, cost, 200, 100, "weapon_smg", 0.8f, 2.0f));

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
            var tier = DefenderRole.Grunt;
            var cost = 200;
            var playerMoney = 1000;
            var playerPos = new Vector3(100f, 200f, 30f);
            var follower = new Follower(factionId, tier);
            var pedHandle = 42;

            _gameBridgeMock.Setup(g => g.GetPlayerMoney()).Returns(playerMoney);
            _gameBridgeMock.Setup(g => g.GetPlayerPosition()).Returns(playerPos);
            _defenderRoleServiceMock.Setup(s => s.GetRoleConfig(tier))
                .Returns(new DefenderRoleConfig(tier, cost, 100, 0, "weapon_pistol", 0.4f, 1.0f));
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
            var tier = DefenderRole.Grunt;
            var cost = 200;
            var playerMoney = 1000;
            var playerPos = new Vector3(100f, 200f, 30f);
            var follower = new Follower(factionId, tier);

            _gameBridgeMock.Setup(g => g.GetPlayerMoney()).Returns(playerMoney);
            _gameBridgeMock.Setup(g => g.GetPlayerPosition()).Returns(playerPos);
            _defenderRoleServiceMock.Setup(s => s.GetRoleConfig(tier))
                .Returns(new DefenderRoleConfig(tier, cost, 100, 0, "weapon_pistol", 0.4f, 1.0f));
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
            var tier = DefenderRole.Rifleman;
            var cost = 1000;
            var playerMoney = 500;

            _followerServiceMock.Setup(s => s.GetFollowerCount("blue")).Returns(0);
            _followerServiceMock.Setup(s => s.GetMaxFollowers()).Returns(6);
            _pedSpawningServiceMock.Setup(s => s.CanSpawn()).Returns(true);
            _gameBridgeMock.Setup(g => g.GetPlayerMoney()).Returns(playerMoney);
            _defenderRoleServiceMock.Setup(s => s.GetRoleConfig(tier))
                .Returns(new DefenderRoleConfig(tier, cost, 200, 100, "weapon_smg", 0.8f, 2.0f));

            // Act
            var canRecruit = _manager.CanRecruitWithCost("blue", tier);

            // Assert
            Assert.False(canRecruit);
        }

        [Fact]
        public void CanRecruitWithCost_WhenSufficientFunds_ShouldReturnTrue()
        {
            // Arrange
            var tier = DefenderRole.Grunt;
            var cost = 200;
            var playerMoney = 1000;

            _followerServiceMock.Setup(s => s.GetFollowerCount("blue")).Returns(0);
            _followerServiceMock.Setup(s => s.GetMaxFollowers()).Returns(6);
            _pedSpawningServiceMock.Setup(s => s.CanSpawn()).Returns(true);
            _gameBridgeMock.Setup(g => g.GetPlayerMoney()).Returns(playerMoney);
            _defenderRoleServiceMock.Setup(s => s.GetRoleConfig(tier))
                .Returns(new DefenderRoleConfig(tier, cost, 100, 0, "weapon_pistol", 0.4f, 1.0f));

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
            var follower = CreateFollowerWithPedHandle(factionId, DefenderRole.Grunt, 42);
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
        public void Update_WhenFollowerInCombat_ShouldClearCombatThenBoard()
        {
            // The player wants to flee with the squad: a fighting follower must break off combat
            // and board. Clearing the combat task first stops native combat AI from aborting the
            // enter task (which previously caused rapid enter/exit oscillation).
            var factionId = "blue";
            var follower = CreateFollowerWithPedHandle(factionId, DefenderRole.Grunt, 42);
            var followers = new List<Follower> { follower };
            var vehicleHandle = 100;

            _followerServiceMock.Setup(s => s.GetFollowers(factionId)).Returns(followers);
            _gameBridgeMock.Setup(g => g.IsPedAlive(42)).Returns(true);
            _gameBridgeMock.Setup(g => g.IsPlayerInVehicle()).Returns(true);
            _gameBridgeMock.Setup(g => g.GetPlayerVehicle()).Returns(vehicleHandle);
            _gameBridgeMock.Setup(g => g.IsPedInVehicle(42)).Returns(false);
            _gameBridgeMock.Setup(g => g.IsPedInCombat(42)).Returns(true); // engaged in battle
            _seatPriorityServiceMock.Setup(s => s.GetPrioritizedFreeSeats(vehicleHandle)).Returns(new[] { 1, 2 });
            _seatPriorityServiceMock.Setup(s => s.FilterFollowersByProximity(It.IsAny<int[]>(), vehicleHandle, 15f))
                .Returns(new[] { 42 });

            // Act
            _manager.Update(factionId);

            // Assert - combat is cleared and the follower is tasked to board
            _gameBridgeMock.Verify(g => g.ClearPedTasks(42), Times.Once);
            _gameBridgeMock.Verify(g => g.TaskPedEnterVehicle(42, vehicleHandle, It.IsAny<int>()), Times.Once);
        }

        [Fact]
        public void Update_WhenBoardOrderRecentlyIssued_ShouldNotReissueWithinCooldown()
        {
            // Re-issuing the board order every tick is exactly what caused the thrash. Once a
            // follower has been ordered to board, hold off re-issuing for the cooldown window.
            var factionId = "blue";
            var follower = CreateFollowerWithPedHandle(factionId, DefenderRole.Grunt, 42);
            var followers = new List<Follower> { follower };
            var vehicleHandle = 100;

            _followerServiceMock.Setup(s => s.GetFollowers(factionId)).Returns(followers);
            _gameBridgeMock.Setup(g => g.IsPedAlive(42)).Returns(true);
            _gameBridgeMock.Setup(g => g.IsPlayerInVehicle()).Returns(true);
            _gameBridgeMock.Setup(g => g.GetPlayerVehicle()).Returns(vehicleHandle);
            _gameBridgeMock.Setup(g => g.IsPedInVehicle(42)).Returns(false);
            _gameBridgeMock.Setup(g => g.IsPedInCombat(42)).Returns(true);
            _gameBridgeMock.Setup(g => g.GetGameTime()).Returns(1000); // same tick time both calls
            _seatPriorityServiceMock.Setup(s => s.GetPrioritizedFreeSeats(vehicleHandle)).Returns(new[] { 1, 2 });
            _seatPriorityServiceMock.Setup(s => s.FilterFollowersByProximity(It.IsAny<int[]>(), vehicleHandle, 15f))
                .Returns(new[] { 42 });

            // Act - two updates within the cooldown window
            _manager.Update(factionId);
            _manager.Update(factionId);

            // Assert - the board order is issued once, not re-spammed every tick
            _gameBridgeMock.Verify(g => g.TaskPedEnterVehicle(42, vehicleHandle, It.IsAny<int>()), Times.Once);
        }

        [Fact]
        public void Update_WhenPlayerInVehicleAndFollowerAlreadyInVehicle_ShouldNotEnterAgain()
        {
            // Arrange
            var factionId = "blue";
            var follower = CreateFollowerWithPedHandle(factionId, DefenderRole.Grunt, 42);
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
            var follower = CreateFollowerWithPedHandle(factionId, DefenderRole.Grunt, 42);
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
        public void Update_PlayerOnFoot_ExposesAliveHandlesWithoutTasking()
        {
            // Arrange
            var factionId = "blue";
            var follower = CreateFollowerWithPedHandle(factionId, DefenderRole.Grunt, 42);
            var followers = new List<Follower> { follower };

            _followerServiceMock.Setup(s => s.GetFollowers(factionId)).Returns(followers);
            _gameBridgeMock.Setup(g => g.IsPedAlive(42)).Returns(true);
            _gameBridgeMock.Setup(g => g.IsPlayerInVehicle()).Returns(false);

            // Act
            _manager.Update(factionId);

            // Assert — FollowerManager exposes the handle; on-foot tasking belongs to SquadStanceController.
            Assert.Single(_manager.OnFootBodyguardHandles);
            Assert.Equal(42, _manager.OnFootBodyguardHandles[0]);
            _gameBridgeMock.Verify(g => g.SetPedAsFollower(It.IsAny<int>()), Times.Never);
        }

        [Fact]
        public void Update_WhenNoFreeSeatsInVehicle_ShouldNotOrderFollowerToEnter()
        {
            // Arrange
            var factionId = "blue";
            var follower = CreateFollowerWithPedHandle(factionId, DefenderRole.Grunt, 42);
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
            var follower1 = CreateFollowerWithPedHandle(factionId, DefenderRole.Grunt, 42);
            var follower2 = CreateFollowerWithPedHandle(factionId, DefenderRole.Gunner, 43);
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
            var follower1 = CreateFollowerWithPedHandle(factionId, DefenderRole.Grunt, 42);
            var follower2 = CreateFollowerWithPedHandle(factionId, DefenderRole.Gunner, 43);
            var follower3 = CreateFollowerWithPedHandle(factionId, DefenderRole.Rifleman, 44);
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
            var follower = CreateFollowerWithPedHandle(factionId, DefenderRole.Grunt, 42);
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
            var follower = CreateFollowerWithPedHandle(factionId, DefenderRole.Grunt, 42);
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
            var tier = DefenderRole.Rifleman;
            var playerPos = new Vector3(100f, 200f, 30f);
            var follower = new Follower(factionId, tier);
            var pedHandle = 42;

            _gameBridgeMock.Setup(g => g.GetPlayerMoney()).Returns(10000);
            _gameBridgeMock.Setup(g => g.GetPlayerPosition()).Returns(playerPos);
            _defenderRoleServiceMock.Setup(s => s.GetRoleConfig(tier))
                .Returns(new DefenderRoleConfig(tier, 1000, 200, 100, "weapon_carbinerifle", 0.7f, 2.0f));
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
        [InlineData(DefenderRole.Grunt, "weapon_pistol")]
        [InlineData(DefenderRole.Gunner, "weapon_smg")]
        [InlineData(DefenderRole.Rifleman, "weapon_carbinerifle")]
        public void RecruitFollower_ShouldGiveCorrectWeaponPerTier(DefenderRole tier, string expectedWeapon)
        {
            // Arrange
            var factionId = "blue";
            var playerPos = new Vector3(100f, 200f, 30f);
            var follower = new Follower(factionId, tier);
            var pedHandle = 42;
            var cost = tier == DefenderRole.Grunt ? 200 : tier == DefenderRole.Gunner ? 500 : 1000;

            _gameBridgeMock.Setup(g => g.GetPlayerMoney()).Returns(10000);
            _gameBridgeMock.Setup(g => g.GetPlayerPosition()).Returns(playerPos);
            _defenderRoleServiceMock.Setup(s => s.GetRoleConfig(tier))
                .Returns(new DefenderRoleConfig(tier, cost, 100, 0, expectedWeapon, 0.5f, 1.0f));
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
            var tier = DefenderRole.Rifleman;
            var playerPos = new Vector3(100f, 200f, 30f);
            var follower = new Follower(factionId, tier);
            var pedHandle = 42;
            var expectedAccuracy = 0.7f;

            _gameBridgeMock.Setup(g => g.GetPlayerMoney()).Returns(10000);
            _gameBridgeMock.Setup(g => g.GetPlayerPosition()).Returns(playerPos);
            _defenderRoleServiceMock.Setup(s => s.GetRoleConfig(tier))
                .Returns(new DefenderRoleConfig(tier, 1000, 200, 100, "weapon_carbinerifle", expectedAccuracy, 2.0f));
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
        [InlineData(DefenderRole.Grunt, 0.3f)]
        [InlineData(DefenderRole.Gunner, 0.5f)]
        [InlineData(DefenderRole.Rifleman, 0.7f)]
        public void RecruitFollower_ShouldSetCorrectAccuracyPerTier(DefenderRole tier, float expectedAccuracy)
        {
            // Arrange
            var factionId = "blue";
            var playerPos = new Vector3(100f, 200f, 30f);
            var follower = new Follower(factionId, tier);
            var pedHandle = 42;
            var cost = tier == DefenderRole.Grunt ? 200 : tier == DefenderRole.Gunner ? 500 : 1000;

            _gameBridgeMock.Setup(g => g.GetPlayerMoney()).Returns(10000);
            _gameBridgeMock.Setup(g => g.GetPlayerPosition()).Returns(playerPos);
            _defenderRoleServiceMock.Setup(s => s.GetRoleConfig(tier))
                .Returns(new DefenderRoleConfig(tier, cost, 100, 50, "weapon_pistol", expectedAccuracy, 1.0f));
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
            var tier = DefenderRole.Rifleman;
            var playerPos = new Vector3(100f, 200f, 30f);
            var follower = new Follower(factionId, tier);
            var pedHandle = 42;
            var expectedArmor = 100;

            _gameBridgeMock.Setup(g => g.GetPlayerMoney()).Returns(10000);
            _gameBridgeMock.Setup(g => g.GetPlayerPosition()).Returns(playerPos);
            _defenderRoleServiceMock.Setup(s => s.GetRoleConfig(tier))
                .Returns(new DefenderRoleConfig(tier, 1000, 200, expectedArmor, "weapon_carbinerifle", 0.7f, 2.0f));
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
        [InlineData(DefenderRole.Grunt, 0)]
        [InlineData(DefenderRole.Gunner, 50)]
        [InlineData(DefenderRole.Rifleman, 100)]
        public void RecruitFollower_ShouldSetCorrectArmorPerTier(DefenderRole tier, int expectedArmor)
        {
            // Arrange
            var factionId = "blue";
            var playerPos = new Vector3(100f, 200f, 30f);
            var follower = new Follower(factionId, tier);
            var pedHandle = 42;
            var cost = tier == DefenderRole.Grunt ? 200 : tier == DefenderRole.Gunner ? 500 : 1000;

            _gameBridgeMock.Setup(g => g.GetPlayerMoney()).Returns(10000);
            _gameBridgeMock.Setup(g => g.GetPlayerPosition()).Returns(playerPos);
            _defenderRoleServiceMock.Setup(s => s.GetRoleConfig(tier))
                .Returns(new DefenderRoleConfig(tier, cost, 100, expectedArmor, "weapon_pistol", 0.5f, 1.0f));
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
            var tier = DefenderRole.Gunner;
            var playerPos = new Vector3(100f, 200f, 30f);
            var follower = new Follower(factionId, tier);
            var pedHandle = 42;

            _gameBridgeMock.Setup(g => g.GetPlayerMoney()).Returns(10000);
            _gameBridgeMock.Setup(g => g.GetPlayerPosition()).Returns(playerPos);
            _defenderRoleServiceMock.Setup(s => s.GetRoleConfig(tier))
                .Returns(new DefenderRoleConfig(tier, 500, 150, 50, "weapon_smg", 0.5f, 1.5f));
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
            var tier = DefenderRole.Rifleman;
            var playerPos = new Vector3(100f, 200f, 30f);
            var follower = new Follower(factionId, tier);
            var pedHandle = 42;
            var expectedHealth = 200;

            _gameBridgeMock.Setup(g => g.GetPlayerMoney()).Returns(10000);
            _gameBridgeMock.Setup(g => g.GetPlayerPosition()).Returns(playerPos);
            _defenderRoleServiceMock.Setup(s => s.GetRoleConfig(tier))
                .Returns(new DefenderRoleConfig(tier, 1000, expectedHealth, 100, "weapon_carbinerifle", 0.7f, 2.0f));
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
        [InlineData(DefenderRole.Grunt, 100)]
        [InlineData(DefenderRole.Gunner, 150)]
        [InlineData(DefenderRole.Rifleman, 200)]
        public void RecruitFollower_ShouldSetCorrectHealthPerTier(DefenderRole tier, int expectedHealth)
        {
            // Arrange
            var factionId = "blue";
            var playerPos = new Vector3(100f, 200f, 30f);
            var follower = new Follower(factionId, tier);
            var pedHandle = 42;
            var cost = tier == DefenderRole.Grunt ? 200 : tier == DefenderRole.Gunner ? 500 : 1000;

            _gameBridgeMock.Setup(g => g.GetPlayerMoney()).Returns(10000);
            _gameBridgeMock.Setup(g => g.GetPlayerPosition()).Returns(playerPos);
            _defenderRoleServiceMock.Setup(s => s.GetRoleConfig(tier))
                .Returns(new DefenderRoleConfig(tier, cost, expectedHealth, 50, "weapon_pistol", 0.5f, 1.0f));
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
                Role = DefenderRole.Gunner,
                Position = new PlayerPosition { X = 10f, Y = 20f, Z = 30f },
            };
            var follower = new Follower(factionId, DefenderRole.Gunner);

            _followerServiceMock.Setup(s => s.GetFollowers(factionId)).Returns(Array.Empty<Follower>());
            _followerServiceMock.Setup(s => s.DismissAllFollowers(factionId));
            _followerServiceMock.Setup(s => s.Recruit(factionId, DefenderRole.Gunner))
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
                Role = DefenderRole.Grunt,
                Position = new PlayerPosition { X = 10f, Y = 20f, Z = 30f },
                VehicleSeatIndex = 1,
            };
            var follower = new Follower(factionId, DefenderRole.Grunt);

            _followerServiceMock.Setup(s => s.GetFollowers(factionId)).Returns(Array.Empty<Follower>());
            _followerServiceMock.Setup(s => s.Recruit(factionId, DefenderRole.Grunt))
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

        private Follower CreateFollowerWithPedHandle(string factionId, DefenderRole tier, int pedHandle)
        {
            var follower = new Follower(factionId, tier, pedHandle);
            return follower;
        }

        #endregion
    }
}
