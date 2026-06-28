using FactionWars.Core.Models;
using Xunit;

namespace FactionWars.Tests.Unit.Core.Models
{
    /// <summary>
    /// Tests for ZoneDefenderAllocation model.
    /// Validates that all defender tiers (Basic, Medium, Heavy, Elite) are properly tracked.
    /// </summary>
    public class ZoneDefenderAllocationTests
    {
        #region Constructor Tests

        [Fact]
        public void Constructor_InitializesEliteTierToZero()
        {
            var allocation = new ZoneDefenderAllocation("faction-1", "zone-1");

            Assert.Equal(0, allocation.GetTroopCount(DefenderRole.Rocketeer));
        }

        [Fact]
        public void Constructor_InitializesAllTiersToZero()
        {
            var allocation = new ZoneDefenderAllocation("faction-1", "zone-1");

            Assert.Equal(0, allocation.GetTroopCount(DefenderRole.Grunt));
            Assert.Equal(0, allocation.GetTroopCount(DefenderRole.Gunner));
            Assert.Equal(0, allocation.GetTroopCount(DefenderRole.Rifleman));
            Assert.Equal(0, allocation.GetTroopCount(DefenderRole.Rocketeer));
        }

        #endregion

        #region AddTroops Tests

        [Fact]
        public void AddTroops_EliteTier_IncreasesTroopCount()
        {
            var allocation = new ZoneDefenderAllocation("faction-1", "zone-1");

            allocation.AddTroops(DefenderRole.Rocketeer, 5);

            Assert.Equal(5, allocation.GetTroopCount(DefenderRole.Rocketeer));
        }

        [Fact]
        public void AddTroops_EliteTier_IncludedInTotalTroops()
        {
            var allocation = new ZoneDefenderAllocation("faction-1", "zone-1");
            allocation.AddTroops(DefenderRole.Grunt, 3);
            allocation.AddTroops(DefenderRole.Rocketeer, 2);

            Assert.Equal(5, allocation.TotalTroops);
        }

        #endregion

        #region RemoveTroops Tests

        [Fact]
        public void RemoveTroops_EliteTier_DecreasesTroopCount()
        {
            var allocation = new ZoneDefenderAllocation("faction-1", "zone-1");
            allocation.AddTroops(DefenderRole.Rocketeer, 5);

            var result = allocation.RemoveTroops(DefenderRole.Rocketeer, 2);

            Assert.True(result);
            Assert.Equal(3, allocation.GetTroopCount(DefenderRole.Rocketeer));
        }

        #endregion

        #region HasTroops Tests

        [Fact]
        public void HasTroops_EliteTier_ReturnsTrueWhenSufficient()
        {
            var allocation = new ZoneDefenderAllocation("faction-1", "zone-1");
            allocation.AddTroops(DefenderRole.Rocketeer, 5);

            Assert.True(allocation.HasTroops(DefenderRole.Rocketeer, 3));
        }

        [Fact]
        public void HasTroops_EliteTier_ReturnsFalseWhenInsufficient()
        {
            var allocation = new ZoneDefenderAllocation("faction-1", "zone-1");
            allocation.AddTroops(DefenderRole.Rocketeer, 2);

            Assert.False(allocation.HasTroops(DefenderRole.Rocketeer, 5));
        }

        #endregion

        #region GetTroopsCopy Tests

        [Fact]
        public void GetTroopsCopy_IncludesEliteTier()
        {
            var allocation = new ZoneDefenderAllocation("faction-1", "zone-1");
            allocation.AddTroops(DefenderRole.Rocketeer, 7);

            var troops = allocation.GetTroopsCopy();

            Assert.True(troops.ContainsKey(DefenderRole.Rocketeer));
            Assert.Equal(7, troops[DefenderRole.Rocketeer]);
        }

        #endregion

        #region ToString Tests

        [Fact]
        public void ToString_IncludesEliteCount()
        {
            var allocation = new ZoneDefenderAllocation("faction-1", "zone-1");
            allocation.AddTroops(DefenderRole.Rocketeer, 3);

            var result = allocation.ToString();

            Assert.Contains("Elite=3", result);
        }

        [Fact]
        public void ToString_IncludesAllTiers()
        {
            var allocation = new ZoneDefenderAllocation("faction-1", "zone-1");
            allocation.AddTroops(DefenderRole.Grunt, 10);
            allocation.AddTroops(DefenderRole.Gunner, 5);
            allocation.AddTroops(DefenderRole.Rifleman, 2);
            allocation.AddTroops(DefenderRole.Rocketeer, 1);

            var result = allocation.ToString();

            Assert.Contains("Basic=10", result);
            Assert.Contains("Medium=5", result);
            Assert.Contains("Heavy=2", result);
            Assert.Contains("Elite=1", result);
        }

        #endregion
    }
}
