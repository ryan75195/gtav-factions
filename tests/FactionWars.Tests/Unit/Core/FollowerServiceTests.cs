using System;
using System.Collections.Generic;
using FactionWars.Core.Interfaces;
using FactionWars.Core.Models;
using FactionWars.Core.Services;
using Xunit;

namespace FactionWars.Tests.Unit.Core
{
    public class FollowerServiceTests
    {
        private const string TestFactionId = "test_faction";
        private const int DefaultMaxFollowers = 6;

        private static FollowerService CreateService(int maxFollowers = DefaultMaxFollowers)
        {
            return new FollowerService(maxFollowers);
        }

        #region Interface Existence Tests

        [Fact]
        public void IFollowerService_Interface_Exists()
        {
            // Assert - verify the interface exists and can be referenced
            var interfaceType = typeof(IFollowerService);
            Assert.NotNull(interfaceType);
            Assert.True(interfaceType.IsInterface);
        }

        [Fact]
        public void IFollowerService_HasRecruitMethod()
        {
            // Assert - verify the Recruit method exists with correct signature
            var method = typeof(IFollowerService).GetMethod("Recruit");
            Assert.NotNull(method);
            Assert.Equal(typeof(FollowerRecruitResult), method!.ReturnType);

            var parameters = method.GetParameters();
            Assert.Equal(2, parameters.Length);
            Assert.Equal(typeof(string), parameters[0].ParameterType); // factionId
            Assert.Equal(typeof(DefenderTier), parameters[1].ParameterType); // tier
        }

        [Fact]
        public void IFollowerService_HasGetFollowersMethod()
        {
            // Assert - verify the GetFollowers method exists
            var method = typeof(IFollowerService).GetMethod("GetFollowers");
            Assert.NotNull(method);
            Assert.Equal(typeof(IReadOnlyList<Follower>), method!.ReturnType);

            var parameters = method.GetParameters();
            Assert.Single(parameters);
            Assert.Equal(typeof(string), parameters[0].ParameterType); // factionId
        }

        [Fact]
        public void IFollowerService_HasGetFollowerCountMethod()
        {
            // Assert - verify the GetFollowerCount method exists
            var method = typeof(IFollowerService).GetMethod("GetFollowerCount");
            Assert.NotNull(method);
            Assert.Equal(typeof(int), method!.ReturnType);

            var parameters = method.GetParameters();
            Assert.Single(parameters);
            Assert.Equal(typeof(string), parameters[0].ParameterType); // factionId
        }

        [Fact]
        public void IFollowerService_HasGetMaxFollowersMethod()
        {
            // Assert - verify the GetMaxFollowers method exists
            var method = typeof(IFollowerService).GetMethod("GetMaxFollowers");
            Assert.NotNull(method);
            Assert.Equal(typeof(int), method!.ReturnType);
            Assert.Empty(method.GetParameters());
        }

        [Fact]
        public void IFollowerService_HasDismissFollowerMethod()
        {
            // Assert - verify the DismissFollower method exists
            var method = typeof(IFollowerService).GetMethod("DismissFollower");
            Assert.NotNull(method);
            Assert.Equal(typeof(bool), method!.ReturnType);

            var parameters = method.GetParameters();
            Assert.Single(parameters);
            Assert.Equal(typeof(Guid), parameters[0].ParameterType); // followerId
        }

        [Fact]
        public void IFollowerService_HasDismissAllFollowersMethod()
        {
            // Assert - verify the DismissAllFollowers method exists
            var method = typeof(IFollowerService).GetMethod("DismissAllFollowers");
            Assert.NotNull(method);
            Assert.Equal(typeof(void), method!.ReturnType);

            var parameters = method.GetParameters();
            Assert.Single(parameters);
            Assert.Equal(typeof(string), parameters[0].ParameterType); // factionId
        }

        [Fact]
        public void IFollowerService_HasHandleFollowerDeathMethod()
        {
            // Assert - verify the HandleFollowerDeath method exists
            var method = typeof(IFollowerService).GetMethod("HandleFollowerDeath");
            Assert.NotNull(method);
            Assert.Equal(typeof(void), method!.ReturnType);

            var parameters = method.GetParameters();
            Assert.Single(parameters);
            Assert.Equal(typeof(Guid), parameters[0].ParameterType); // followerId
        }

        [Fact]
        public void IFollowerService_HasGetFollowerByIdMethod()
        {
            // Assert - verify the GetFollowerById method exists
            var method = typeof(IFollowerService).GetMethod("GetFollowerById");
            Assert.NotNull(method);
            Assert.Equal(typeof(Follower), method!.ReturnType);

            var parameters = method.GetParameters();
            Assert.Single(parameters);
            Assert.Equal(typeof(Guid), parameters[0].ParameterType); // followerId
        }

        [Fact]
        public void Follower_Model_HasRequiredProperties()
        {
            // Assert - verify the Follower model has required properties
            var type = typeof(Follower);
            Assert.NotNull(type);

            // Check required properties
            Assert.NotNull(type.GetProperty("Id"));
            Assert.NotNull(type.GetProperty("FactionId"));
            Assert.NotNull(type.GetProperty("Tier"));
            Assert.NotNull(type.GetProperty("PedHandle"));
            Assert.NotNull(type.GetProperty("IsAlive"));
        }

        [Fact]
        public void FollowerRecruitResult_Model_HasRequiredProperties()
        {
            // Assert - verify the FollowerRecruitResult model has required properties
            var type = typeof(FollowerRecruitResult);
            Assert.NotNull(type);

            // Check required properties
            Assert.NotNull(type.GetProperty("Success"));
            Assert.NotNull(type.GetProperty("Follower"));
            Assert.NotNull(type.GetProperty("FailureReason"));
        }

        #endregion

        #region Implementation Tests - Constructor & GetMaxFollowers

        [Fact]
        public void FollowerService_ImplementsIFollowerService()
        {
            // Arrange & Act
            var service = CreateService();

            // Assert
            Assert.IsAssignableFrom<IFollowerService>(service);
        }

        [Fact]
        public void GetMaxFollowers_ReturnsConfiguredLimit()
        {
            // Arrange
            var service = CreateService(maxFollowers: 4);

            // Act
            var max = service.GetMaxFollowers();

            // Assert
            Assert.Equal(4, max);
        }

        [Theory]
        [InlineData(1)]
        [InlineData(4)]
        [InlineData(6)]
        [InlineData(10)]
        public void GetMaxFollowers_ReturnsCorrectValueForDifferentLimits(int limit)
        {
            // Arrange
            var service = CreateService(maxFollowers: limit);

            // Act & Assert
            Assert.Equal(limit, service.GetMaxFollowers());
        }

        #endregion

        #region Implementation Tests - Recruit

        [Fact]
        public void Recruit_WithValidFaction_ReturnsSuccessWithFollower()
        {
            // Arrange
            var service = CreateService();

            // Act
            var result = service.Recruit(TestFactionId, DefenderTier.Basic);

            // Assert
            Assert.True(result.Success);
            Assert.NotNull(result.Follower);
            Assert.Null(result.FailureReason);
            Assert.Equal(TestFactionId, result.Follower!.FactionId);
            Assert.Equal(DefenderTier.Basic, result.Follower.Tier);
        }

        [Theory]
        [InlineData(DefenderTier.Basic)]
        [InlineData(DefenderTier.Medium)]
        [InlineData(DefenderTier.Heavy)]
        public void Recruit_WithDifferentTiers_CreatesFollowerWithCorrectTier(DefenderTier tier)
        {
            // Arrange
            var service = CreateService();

            // Act
            var result = service.Recruit(TestFactionId, tier);

            // Assert
            Assert.True(result.Success);
            Assert.Equal(tier, result.Follower!.Tier);
        }

        [Fact]
        public void Recruit_WithNullFactionId_ReturnsInvalidFactionFailure()
        {
            // Arrange
            var service = CreateService();

            // Act
            var result = service.Recruit(null!, DefenderTier.Basic);

            // Assert
            Assert.False(result.Success);
            Assert.Null(result.Follower);
            Assert.Equal(FollowerRecruitFailureReason.InvalidFaction, result.FailureReason);
        }

        [Fact]
        public void Recruit_WithEmptyFactionId_ReturnsInvalidFactionFailure()
        {
            // Arrange
            var service = CreateService();

            // Act
            var result = service.Recruit(string.Empty, DefenderTier.Basic);

            // Assert
            Assert.False(result.Success);
            Assert.Equal(FollowerRecruitFailureReason.InvalidFaction, result.FailureReason);
        }

        [Fact]
        public void Recruit_WhenAtMaxFollowers_ReturnsMaxFollowersReachedFailure()
        {
            // Arrange
            var service = CreateService(maxFollowers: 2);
            service.Recruit(TestFactionId, DefenderTier.Basic);
            service.Recruit(TestFactionId, DefenderTier.Basic);

            // Act
            var result = service.Recruit(TestFactionId, DefenderTier.Basic);

            // Assert
            Assert.False(result.Success);
            Assert.Equal(FollowerRecruitFailureReason.MaxFollowersReached, result.FailureReason);
        }

        [Fact]
        public void Recruit_CountsFollowersAcrossAllFactions()
        {
            // Arrange - max followers is a global limit, not per-faction
            var service = CreateService(maxFollowers: 2);
            service.Recruit("faction_a", DefenderTier.Basic);
            service.Recruit("faction_b", DefenderTier.Basic);

            // Act - third recruit should fail regardless of faction
            var result = service.Recruit("faction_c", DefenderTier.Basic);

            // Assert
            Assert.False(result.Success);
            Assert.Equal(FollowerRecruitFailureReason.MaxFollowersReached, result.FailureReason);
        }

        [Fact]
        public void Recruit_AssignsUniqueIds()
        {
            // Arrange
            var service = CreateService();

            // Act
            var result1 = service.Recruit(TestFactionId, DefenderTier.Basic);
            var result2 = service.Recruit(TestFactionId, DefenderTier.Basic);

            // Assert
            Assert.NotEqual(result1.Follower!.Id, result2.Follower!.Id);
        }

        [Fact]
        public void Recruit_CreatesFollowerInAliveState()
        {
            // Arrange
            var service = CreateService();

            // Act
            var result = service.Recruit(TestFactionId, DefenderTier.Basic);

            // Assert
            Assert.True(result.Follower!.IsAlive);
        }

        #endregion

        #region Implementation Tests - GetFollowers & GetFollowerCount

        [Fact]
        public void GetFollowers_WithNoFollowers_ReturnsEmptyList()
        {
            // Arrange
            var service = CreateService();

            // Act
            var followers = service.GetFollowers(TestFactionId);

            // Assert
            Assert.Empty(followers);
        }

        [Fact]
        public void GetFollowers_ReturnsOnlyFollowersForSpecifiedFaction()
        {
            // Arrange
            var service = CreateService();
            service.Recruit("faction_a", DefenderTier.Basic);
            service.Recruit("faction_b", DefenderTier.Medium);
            service.Recruit("faction_a", DefenderTier.Heavy);

            // Act
            var factionAFollowers = service.GetFollowers("faction_a");
            var factionBFollowers = service.GetFollowers("faction_b");

            // Assert
            Assert.Equal(2, factionAFollowers.Count);
            Assert.Single(factionBFollowers);
            Assert.All(factionAFollowers, f => Assert.Equal("faction_a", f.FactionId));
            Assert.All(factionBFollowers, f => Assert.Equal("faction_b", f.FactionId));
        }

        [Fact]
        public void GetFollowerCount_WithNoFollowers_ReturnsZero()
        {
            // Arrange
            var service = CreateService();

            // Act
            var count = service.GetFollowerCount(TestFactionId);

            // Assert
            Assert.Equal(0, count);
        }

        [Fact]
        public void GetFollowerCount_ReturnsCorrectCountForFaction()
        {
            // Arrange
            var service = CreateService();
            service.Recruit("faction_a", DefenderTier.Basic);
            service.Recruit("faction_a", DefenderTier.Basic);
            service.Recruit("faction_b", DefenderTier.Basic);

            // Act
            var countA = service.GetFollowerCount("faction_a");
            var countB = service.GetFollowerCount("faction_b");

            // Assert
            Assert.Equal(2, countA);
            Assert.Equal(1, countB);
        }

        #endregion

        #region Implementation Tests - GetFollowerById

        [Fact]
        public void GetFollowerById_WithValidId_ReturnsFollower()
        {
            // Arrange
            var service = CreateService();
            var recruitResult = service.Recruit(TestFactionId, DefenderTier.Basic);
            var followerId = recruitResult.Follower!.Id;

            // Act
            var follower = service.GetFollowerById(followerId);

            // Assert
            Assert.NotNull(follower);
            Assert.Equal(followerId, follower!.Id);
        }

        [Fact]
        public void GetFollowerById_WithInvalidId_ReturnsNull()
        {
            // Arrange
            var service = CreateService();
            service.Recruit(TestFactionId, DefenderTier.Basic);

            // Act
            var follower = service.GetFollowerById(Guid.NewGuid());

            // Assert
            Assert.Null(follower);
        }

        #endregion

        #region Implementation Tests - DismissFollower

        [Fact]
        public void DismissFollower_WithValidId_ReturnsTrue()
        {
            // Arrange
            var service = CreateService();
            var recruitResult = service.Recruit(TestFactionId, DefenderTier.Basic);
            var followerId = recruitResult.Follower!.Id;

            // Act
            var dismissed = service.DismissFollower(followerId);

            // Assert
            Assert.True(dismissed);
        }

        [Fact]
        public void DismissFollower_WithValidId_RemovesFollowerFromList()
        {
            // Arrange
            var service = CreateService();
            var recruitResult = service.Recruit(TestFactionId, DefenderTier.Basic);
            var followerId = recruitResult.Follower!.Id;

            // Act
            service.DismissFollower(followerId);

            // Assert
            Assert.Null(service.GetFollowerById(followerId));
            Assert.Equal(0, service.GetFollowerCount(TestFactionId));
        }

        [Fact]
        public void DismissFollower_WithInvalidId_ReturnsFalse()
        {
            // Arrange
            var service = CreateService();
            service.Recruit(TestFactionId, DefenderTier.Basic);

            // Act
            var dismissed = service.DismissFollower(Guid.NewGuid());

            // Assert
            Assert.False(dismissed);
        }

        [Fact]
        public void DismissFollower_DecreasesFollowerCount()
        {
            // Arrange
            var service = CreateService();
            service.Recruit(TestFactionId, DefenderTier.Basic);
            var result = service.Recruit(TestFactionId, DefenderTier.Basic);

            // Act
            service.DismissFollower(result.Follower!.Id);

            // Assert
            Assert.Equal(1, service.GetFollowerCount(TestFactionId));
        }

        [Fact]
        public void DismissFollower_AllowsNewRecruitmentAfterDismissal()
        {
            // Arrange
            var service = CreateService(maxFollowers: 1);
            var result = service.Recruit(TestFactionId, DefenderTier.Basic);
            service.DismissFollower(result.Follower!.Id);

            // Act
            var newResult = service.Recruit(TestFactionId, DefenderTier.Medium);

            // Assert
            Assert.True(newResult.Success);
        }

        #endregion

        #region Implementation Tests - DismissAllFollowers

        [Fact]
        public void DismissAllFollowers_RemovesAllFollowersForFaction()
        {
            // Arrange
            var service = CreateService();
            service.Recruit("faction_a", DefenderTier.Basic);
            service.Recruit("faction_a", DefenderTier.Medium);
            service.Recruit("faction_b", DefenderTier.Basic);

            // Act
            service.DismissAllFollowers("faction_a");

            // Assert
            Assert.Equal(0, service.GetFollowerCount("faction_a"));
            Assert.Equal(1, service.GetFollowerCount("faction_b"));
        }

        [Fact]
        public void DismissAllFollowers_DoesNotAffectOtherFactions()
        {
            // Arrange
            var service = CreateService();
            service.Recruit("faction_a", DefenderTier.Basic);
            var factionBResult = service.Recruit("faction_b", DefenderTier.Basic);

            // Act
            service.DismissAllFollowers("faction_a");

            // Assert
            Assert.NotNull(service.GetFollowerById(factionBResult.Follower!.Id));
        }

        [Fact]
        public void DismissAllFollowers_WithNoFollowers_DoesNotThrow()
        {
            // Arrange
            var service = CreateService();

            // Act & Assert - should not throw
            var exception = Record.Exception(() => service.DismissAllFollowers(TestFactionId));
            Assert.Null(exception);
        }

        [Fact]
        public void DismissAllFollowers_WithNullFactionId_DoesNotThrow()
        {
            // Arrange
            var service = CreateService();
            service.Recruit(TestFactionId, DefenderTier.Basic);

            // Act & Assert - should handle gracefully
            var exception = Record.Exception(() => service.DismissAllFollowers(null!));
            Assert.Null(exception);
        }

        #endregion

        #region Implementation Tests - HandleFollowerDeath

        [Fact]
        public void HandleFollowerDeath_MarksFollowerAsDead()
        {
            // Arrange
            var service = CreateService();
            var result = service.Recruit(TestFactionId, DefenderTier.Basic);
            var followerId = result.Follower!.Id;

            // Act
            service.HandleFollowerDeath(followerId);

            // Assert - follower is removed from active list after death
            Assert.Null(service.GetFollowerById(followerId));
        }

        [Fact]
        public void HandleFollowerDeath_RemovesFollowerFromActiveList()
        {
            // Arrange
            var service = CreateService();
            service.Recruit(TestFactionId, DefenderTier.Basic);
            var result = service.Recruit(TestFactionId, DefenderTier.Medium);

            // Act
            service.HandleFollowerDeath(result.Follower!.Id);

            // Assert
            Assert.Equal(1, service.GetFollowerCount(TestFactionId));
        }

        [Fact]
        public void HandleFollowerDeath_WithInvalidId_DoesNotThrow()
        {
            // Arrange
            var service = CreateService();

            // Act & Assert
            var exception = Record.Exception(() => service.HandleFollowerDeath(Guid.NewGuid()));
            Assert.Null(exception);
        }

        [Fact]
        public void HandleFollowerDeath_AllowsNewRecruitment()
        {
            // Arrange
            var service = CreateService(maxFollowers: 1);
            var result = service.Recruit(TestFactionId, DefenderTier.Basic);
            service.HandleFollowerDeath(result.Follower!.Id);

            // Act
            var newResult = service.Recruit(TestFactionId, DefenderTier.Heavy);

            // Assert
            Assert.True(newResult.Success);
        }

        #endregion

        #region Implementation Tests - Edge Cases

        [Fact]
        public void Service_MaintainsStateAcrossMultipleOperations()
        {
            // Arrange
            var service = CreateService(maxFollowers: 3);

            // Act - series of operations
            var r1 = service.Recruit("faction_a", DefenderTier.Basic);
            var r2 = service.Recruit("faction_a", DefenderTier.Medium);
            var r3 = service.Recruit("faction_b", DefenderTier.Heavy);
            service.DismissFollower(r2.Follower!.Id);
            var r4 = service.Recruit("faction_c", DefenderTier.Basic);
            service.HandleFollowerDeath(r1.Follower!.Id);

            // Assert
            Assert.Equal(0, service.GetFollowerCount("faction_a"));
            Assert.Equal(1, service.GetFollowerCount("faction_b"));
            Assert.Equal(1, service.GetFollowerCount("faction_c"));
            Assert.NotNull(service.GetFollowerById(r3.Follower!.Id));
            Assert.NotNull(service.GetFollowerById(r4.Follower!.Id));
        }

        [Fact]
        public void GetFollowers_ReturnsReadOnlyList()
        {
            // Arrange
            var service = CreateService();
            service.Recruit(TestFactionId, DefenderTier.Basic);

            // Act
            var followers = service.GetFollowers(TestFactionId);

            // Assert - verify it's a read-only list type
            Assert.IsAssignableFrom<IReadOnlyList<Follower>>(followers);
        }

        #endregion
    }
}
