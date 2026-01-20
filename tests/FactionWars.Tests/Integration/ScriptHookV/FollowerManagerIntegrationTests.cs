using System;
using FactionWars.Combat.Interfaces;
using FactionWars.Combat.Pools;
using FactionWars.Combat.Services;
using FactionWars.Core.Interfaces;
using FactionWars.Core.Models;
using FactionWars.Core.Services;
using FactionWars.Core.Utils;
using FactionWars.ScriptHookV.Managers;
using FactionWars.UI.Interfaces;
using FactionWars.UI.Services;
using Xunit;

namespace FactionWars.Tests.Integration.ScriptHookV
{
    /// <summary>
    /// Integration tests verifying that followers follow the player, fight, and enter vehicles.
    /// Tests the full flow from FollowerManager through to game bridge interactions.
    /// </summary>
    public class FollowerManagerIntegrationTests
    {
        private const string MichaelFactionId = "faction-michael";
        private const string TrevorFactionId = "faction-trevor";

        private readonly MockGameBridge _gameBridge;
        private readonly InMemoryPedPool _pedPool;
        private readonly IPedSpawningService _pedSpawningService;
        private readonly IFollowerService _followerService;
        private readonly IDefenderTierService _defenderTierService;
        private readonly IPedBlipService _pedBlipService;
        private readonly FollowerManager _followerManager;

        public FollowerManagerIntegrationTests()
        {
            _gameBridge = new MockGameBridge
            {
                PlayerPosition = new Vector3(100, 100, 0),
                PlayerMoney = 10000
            };

            _pedPool = new InMemoryPedPool(30);
            _pedSpawningService = new PedSpawningService(_gameBridge, _pedPool);
            _followerService = new FollowerService(maxFollowers: 6);
            _defenderTierService = new DefenderTierService();
            _pedBlipService = new PedBlipService(_gameBridge);

            _followerManager = new FollowerManager(
                _gameBridge,
                _followerService,
                _pedSpawningService,
                _defenderTierService,
                _pedBlipService);
        }

        #region Following Behavior Tests

        [Fact]
        public void RecruitFollower_SetsUpPedToFollowPlayer()
        {
            // Act
            var result = _followerManager.RecruitFollower(MichaelFactionId, DefenderTier.Basic);

            // Assert
            Assert.True(result.Success);
            Assert.NotNull(result.Follower);

            // Verify the ped was set to follow the player
            var pedHandle = result.Follower!.PedHandle;
            Assert.Contains(pedHandle, _gameBridge.FollowingPeds);
        }

        [Fact]
        public void RecruitFollower_MultipleFollowers_AllFollowPlayer()
        {
            // Act
            var result1 = _followerManager.RecruitFollower(MichaelFactionId, DefenderTier.Basic);
            var result2 = _followerManager.RecruitFollower(MichaelFactionId, DefenderTier.Medium);
            var result3 = _followerManager.RecruitFollower(MichaelFactionId, DefenderTier.Heavy);

            // Assert
            Assert.True(result1.Success);
            Assert.True(result2.Success);
            Assert.True(result3.Success);

            // Verify all peds were set to follow
            Assert.Contains(result1.Follower!.PedHandle, _gameBridge.FollowingPeds);
            Assert.Contains(result2.Follower!.PedHandle, _gameBridge.FollowingPeds);
            Assert.Contains(result3.Follower!.PedHandle, _gameBridge.FollowingPeds);
        }

        #endregion

        #region Combat Configuration Tests

        [Fact]
        public void RecruitFollower_BasicTier_ConfiguredWithCorrectWeapon()
        {
            // Act
            var result = _followerManager.RecruitFollower(MichaelFactionId, DefenderTier.Basic);

            // Assert
            Assert.True(result.Success);
            var pedHandle = result.Follower!.PedHandle;

            // Basic tier should have a pistol
            var weapon = _gameBridge.GetPedWeapon(pedHandle);
            Assert.Equal("Pistol", weapon);
        }

        [Fact]
        public void RecruitFollower_MediumTier_ConfiguredWithCorrectWeapon()
        {
            // Act
            var result = _followerManager.RecruitFollower(MichaelFactionId, DefenderTier.Medium);

            // Assert
            Assert.True(result.Success);
            var pedHandle = result.Follower!.PedHandle;

            // Medium tier should have an SMG
            var weapon = _gameBridge.GetPedWeapon(pedHandle);
            Assert.Equal("SMG", weapon);
        }

        [Fact]
        public void RecruitFollower_HeavyTier_ConfiguredWithCorrectWeapon()
        {
            // Act
            var result = _followerManager.RecruitFollower(MichaelFactionId, DefenderTier.Heavy);

            // Assert
            Assert.True(result.Success);
            var pedHandle = result.Follower!.PedHandle;

            // Heavy tier should have a Carbine
            var weapon = _gameBridge.GetPedWeapon(pedHandle);
            Assert.Equal("Carbine", weapon);
        }

        [Fact]
        public void RecruitFollower_ConfiguresAccuracyBasedOnTier()
        {
            // Act
            var basicResult = _followerManager.RecruitFollower(MichaelFactionId, DefenderTier.Basic);
            var mediumResult = _followerManager.RecruitFollower(MichaelFactionId, DefenderTier.Medium);
            var heavyResult = _followerManager.RecruitFollower(MichaelFactionId, DefenderTier.Heavy);

            // Assert
            Assert.Equal(0.3f, _gameBridge.GetPedAccuracy(basicResult.Follower!.PedHandle), 0.01);
            Assert.Equal(0.5f, _gameBridge.GetPedAccuracy(mediumResult.Follower!.PedHandle), 0.01);
            Assert.Equal(0.7f, _gameBridge.GetPedAccuracy(heavyResult.Follower!.PedHandle), 0.01);
        }

        [Fact]
        public void RecruitFollower_ConfiguresArmorBasedOnTier()
        {
            // Act
            var basicResult = _followerManager.RecruitFollower(MichaelFactionId, DefenderTier.Basic);
            var mediumResult = _followerManager.RecruitFollower(MichaelFactionId, DefenderTier.Medium);
            var heavyResult = _followerManager.RecruitFollower(MichaelFactionId, DefenderTier.Heavy);

            // Assert
            Assert.Equal(0, _gameBridge.GetPedArmor(basicResult.Follower!.PedHandle));
            Assert.Equal(50, _gameBridge.GetPedArmor(mediumResult.Follower!.PedHandle));
            Assert.Equal(100, _gameBridge.GetPedArmor(heavyResult.Follower!.PedHandle));
        }

        [Fact]
        public void RecruitFollower_ConfiguresHealthBasedOnTier()
        {
            // Act
            var basicResult = _followerManager.RecruitFollower(MichaelFactionId, DefenderTier.Basic);
            var mediumResult = _followerManager.RecruitFollower(MichaelFactionId, DefenderTier.Medium);
            var heavyResult = _followerManager.RecruitFollower(MichaelFactionId, DefenderTier.Heavy);

            // Assert
            Assert.Equal(100, _gameBridge.GetPedHealth(basicResult.Follower!.PedHandle));
            Assert.Equal(150, _gameBridge.GetPedHealth(mediumResult.Follower!.PedHandle));
            Assert.Equal(200, _gameBridge.GetPedHealth(heavyResult.Follower!.PedHandle));
        }

        [Fact]
        public void RecruitFollower_ConfiguresCombatBehavior()
        {
            // Act
            var result = _followerManager.RecruitFollower(MichaelFactionId, DefenderTier.Basic);

            // Assert
            Assert.True(result.Success);
            var pedHandle = result.Follower!.PedHandle;

            // Verify combat behavior is configured (can use cover and will fight armed peds)
            Assert.True(_gameBridge.GetPedCanUseCover(pedHandle));
            Assert.True(_gameBridge.GetPedWillFightArmedPeds(pedHandle));
        }

        #endregion

        #region Vehicle Behavior Tests

        [Fact]
        public void Update_PlayerEntersVehicle_FollowersOrderedToEnter()
        {
            // Arrange: Recruit a follower
            var result = _followerManager.RecruitFollower(MichaelFactionId, DefenderTier.Basic);
            Assert.True(result.Success);
            var pedHandle = result.Follower!.PedHandle;

            // Follower is not in a vehicle initially
            Assert.False(_gameBridge.IsPedInVehicle(pedHandle));

            // Act: Player gets in a vehicle
            var vehicleHandle = _gameBridge.SetPlayerInVehicle(4);
            _followerManager.Update(MichaelFactionId);

            // Assert: Follower should now be in the vehicle (mock immediately executes the task)
            Assert.True(_gameBridge.IsPedInVehicle(pedHandle));
        }

        [Fact]
        public void Update_PlayerExitsVehicle_FollowersOrderedToExit()
        {
            // Arrange: Recruit a follower and put both player and follower in vehicle
            var result = _followerManager.RecruitFollower(MichaelFactionId, DefenderTier.Basic);
            Assert.True(result.Success);
            var pedHandle = result.Follower!.PedHandle;

            // Player enters vehicle
            var vehicleHandle = _gameBridge.SetPlayerInVehicle(4);
            _followerManager.Update(MichaelFactionId);

            // Verify follower is in vehicle
            Assert.True(_gameBridge.IsPedInVehicle(pedHandle));

            // Act: Player exits vehicle
            _gameBridge.IsPlayerInVehicleValue = false;
            _gameBridge.PlayerVehicleHandle = -1;
            _followerManager.Update(MichaelFactionId);

            // Assert: Follower should exit vehicle
            Assert.False(_gameBridge.IsPedInVehicle(pedHandle));
        }

        [Fact]
        public void Update_MultipleFollowers_AllEnterVehicleWithPlayer()
        {
            // Arrange: Recruit multiple followers
            var result1 = _followerManager.RecruitFollower(MichaelFactionId, DefenderTier.Basic);
            var result2 = _followerManager.RecruitFollower(MichaelFactionId, DefenderTier.Medium);
            Assert.True(result1.Success);
            Assert.True(result2.Success);

            // Act: Player enters a 4-seat vehicle (driver + 3 passengers)
            var vehicleHandle = _gameBridge.SetPlayerInVehicle(4);
            _followerManager.Update(MichaelFactionId);

            // Assert: Both followers should be in the vehicle
            Assert.True(_gameBridge.IsPedInVehicle(result1.Follower!.PedHandle));
            Assert.True(_gameBridge.IsPedInVehicle(result2.Follower!.PedHandle));
        }

        [Fact]
        public void Update_VehicleFullOfFollowers_NoMoreFollowersCanEnter()
        {
            // Arrange: Recruit more followers than available seats (4 followers for 3 passenger seats)
            var followers = new FollowerRecruitResult[4];
            for (int i = 0; i < 4; i++)
            {
                followers[i] = _followerManager.RecruitFollower(MichaelFactionId, DefenderTier.Basic);
                Assert.True(followers[i].Success);
            }

            // Act: Player enters a 4-seat vehicle (1 driver + 3 passengers, so 3 seats available)
            var vehicleHandle = _gameBridge.SetPlayerInVehicle(4);
            _followerManager.Update(MichaelFactionId);

            // Assert: Only 3 followers should be in the vehicle (no 4th seat available)
            int followersInVehicle = 0;
            foreach (var fr in followers)
            {
                if (_gameBridge.IsPedInVehicle(fr.Follower!.PedHandle))
                {
                    followersInVehicle++;
                }
            }

            Assert.Equal(3, followersInVehicle);
        }

        #endregion

        #region Death Handling Tests

        [Fact]
        public void Update_FollowerDies_RaisesFollowerDiedEvent()
        {
            // Arrange
            Follower? diedFollower = null;
            _followerManager.FollowerDied += (sender, follower) => diedFollower = follower;

            var result = _followerManager.RecruitFollower(MichaelFactionId, DefenderTier.Basic);
            Assert.True(result.Success);
            var pedHandle = result.Follower!.PedHandle;

            // Act: Kill the follower
            _gameBridge.KillPed(pedHandle);
            _followerManager.Update(MichaelFactionId);

            // Assert: Event should have been raised
            Assert.NotNull(diedFollower);
            Assert.Equal(result.Follower.Id, diedFollower!.Id);
        }

        [Fact]
        public void Update_FollowerDies_RemovedFromService()
        {
            // Arrange
            var result = _followerManager.RecruitFollower(MichaelFactionId, DefenderTier.Basic);
            Assert.True(result.Success);
            var followerId = result.Follower!.Id;
            var pedHandle = result.Follower.PedHandle;

            Assert.Equal(1, _followerManager.GetFollowerCount(MichaelFactionId));

            // Act: Kill the follower
            _gameBridge.KillPed(pedHandle);
            _followerManager.Update(MichaelFactionId);

            // Assert: Follower should be removed
            Assert.Equal(0, _followerManager.GetFollowerCount(MichaelFactionId));
        }

        [Fact]
        public void Update_FollowerDies_PedDeletedFromGame()
        {
            // Arrange
            var result = _followerManager.RecruitFollower(MichaelFactionId, DefenderTier.Basic);
            Assert.True(result.Success);
            var pedHandle = result.Follower!.PedHandle;

            Assert.True(_gameBridge.PedExists(pedHandle));

            // Act: Kill the follower
            _gameBridge.KillPed(pedHandle);
            _followerManager.Update(MichaelFactionId);

            // Assert: Ped should be deleted from game
            Assert.False(_gameBridge.PedExists(pedHandle));
        }

        #endregion

        #region Full Flow Integration Tests

        [Fact]
        public void FullFlow_RecruitFollower_FollowsPlayer_EntersVehicle_DiesInCombat()
        {
            // Step 1: Recruit a heavy-tier follower
            _gameBridge.PlayerMoney = 10000;
            var result = _followerManager.RecruitFollower(MichaelFactionId, DefenderTier.Heavy);

            Assert.True(result.Success);
            Assert.NotNull(result.Follower);
            var follower = result.Follower!;
            var pedHandle = follower.PedHandle;

            // Verify recruitment deducted cost and follower is configured
            Assert.Equal(9000, _gameBridge.PlayerMoney); // Heavy costs 1000
            Assert.Contains(pedHandle, _gameBridge.FollowingPeds);
            Assert.Equal("Carbine", _gameBridge.GetPedWeapon(pedHandle));
            Assert.Equal(200, _gameBridge.GetPedHealth(pedHandle));
            Assert.Equal(100, _gameBridge.GetPedArmor(pedHandle));

            // Step 2: Player enters vehicle, follower follows
            var vehicleHandle = _gameBridge.SetPlayerInVehicle(4);
            _followerManager.Update(MichaelFactionId);

            Assert.True(_gameBridge.IsPedInVehicle(pedHandle));

            // Step 3: Player exits vehicle, follower follows
            _gameBridge.IsPlayerInVehicleValue = false;
            _gameBridge.PlayerVehicleHandle = -1;
            _followerManager.Update(MichaelFactionId);

            Assert.False(_gameBridge.IsPedInVehicle(pedHandle));

            // Step 4: Follower dies in combat
            Follower? diedFollower = null;
            _followerManager.FollowerDied += (s, f) => diedFollower = f;

            _gameBridge.KillPed(pedHandle);
            _followerManager.Update(MichaelFactionId);

            // Verify death was handled
            Assert.NotNull(diedFollower);
            Assert.Equal(follower.Id, diedFollower!.Id);
            Assert.Equal(0, _followerManager.GetFollowerCount(MichaelFactionId));
            Assert.False(_gameBridge.PedExists(pedHandle));
        }

        [Fact]
        public void FullFlow_DismissAllFollowers_OnCharacterSwitch()
        {
            // Arrange: Recruit multiple followers for Michael
            var result1 = _followerManager.RecruitFollower(MichaelFactionId, DefenderTier.Basic);
            var result2 = _followerManager.RecruitFollower(MichaelFactionId, DefenderTier.Medium);
            var result3 = _followerManager.RecruitFollower(MichaelFactionId, DefenderTier.Heavy);

            Assert.True(result1.Success);
            Assert.True(result2.Success);
            Assert.True(result3.Success);
            Assert.Equal(3, _followerManager.GetFollowerCount(MichaelFactionId));

            // Verify peds exist
            Assert.True(_gameBridge.PedExists(result1.Follower!.PedHandle));
            Assert.True(_gameBridge.PedExists(result2.Follower!.PedHandle));
            Assert.True(_gameBridge.PedExists(result3.Follower!.PedHandle));

            // Act: Simulate character switch by dismissing all followers
            _followerManager.DismissAllFollowers(MichaelFactionId);

            // Assert: All followers removed and peds deleted
            Assert.Equal(0, _followerManager.GetFollowerCount(MichaelFactionId));
            Assert.False(_gameBridge.PedExists(result1.Follower.PedHandle));
            Assert.False(_gameBridge.PedExists(result2.Follower.PedHandle));
            Assert.False(_gameBridge.PedExists(result3.Follower.PedHandle));
        }

        #endregion
    }
}
