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

            Assert.Equal(0, allocation.GetTroopCount(DefenderTier.Elite));
        }

        [Fact]
        public void Constructor_InitializesAllTiersToZero()
        {
            var allocation = new ZoneDefenderAllocation("faction-1", "zone-1");

            Assert.Equal(0, allocation.GetTroopCount(DefenderTier.Basic));
            Assert.Equal(0, allocation.GetTroopCount(DefenderTier.Medium));
            Assert.Equal(0, allocation.GetTroopCount(DefenderTier.Heavy));
            Assert.Equal(0, allocation.GetTroopCount(DefenderTier.Elite));
        }

        #endregion

        #region AddTroops Tests

        [Fact]
        public void AddTroops_EliteTier_IncreasesTroopCount()
        {
            var allocation = new ZoneDefenderAllocation("faction-1", "zone-1");

            allocation.AddTroops(DefenderTier.Elite, 5);

            Assert.Equal(5, allocation.GetTroopCount(DefenderTier.Elite));
        }

        [Fact]
        public void AddTroops_EliteTier_IncludedInTotalTroops()
        {
            var allocation = new ZoneDefenderAllocation("faction-1", "zone-1");
            allocation.AddTroops(DefenderTier.Basic, 3);
            allocation.AddTroops(DefenderTier.Elite, 2);

            Assert.Equal(5, allocation.TotalTroops);
        }

        #endregion

        #region RemoveTroops Tests

        [Fact]
        public void RemoveTroops_EliteTier_DecreasesTroopCount()
        {
            var allocation = new ZoneDefenderAllocation("faction-1", "zone-1");
            allocation.AddTroops(DefenderTier.Elite, 5);

            var result = allocation.RemoveTroops(DefenderTier.Elite, 2);

            Assert.True(result);
            Assert.Equal(3, allocation.GetTroopCount(DefenderTier.Elite));
        }

        #endregion

        #region HasTroops Tests

        [Fact]
        public void HasTroops_EliteTier_ReturnsTrueWhenSufficient()
        {
            var allocation = new ZoneDefenderAllocation("faction-1", "zone-1");
            allocation.AddTroops(DefenderTier.Elite, 5);

            Assert.True(allocation.HasTroops(DefenderTier.Elite, 3));
        }

        [Fact]
        public void HasTroops_EliteTier_ReturnsFalseWhenInsufficient()
        {
            var allocation = new ZoneDefenderAllocation("faction-1", "zone-1");
            allocation.AddTroops(DefenderTier.Elite, 2);

            Assert.False(allocation.HasTroops(DefenderTier.Elite, 5));
        }

        #endregion

        #region GetTroopsCopy Tests

        [Fact]
        public void GetTroopsCopy_IncludesEliteTier()
        {
            var allocation = new ZoneDefenderAllocation("faction-1", "zone-1");
            allocation.AddTroops(DefenderTier.Elite, 7);

            var troops = allocation.GetTroopsCopy();

            Assert.True(troops.ContainsKey(DefenderTier.Elite));
            Assert.Equal(7, troops[DefenderTier.Elite]);
        }

        #endregion

        #region ToString Tests

        [Fact]
        public void ToString_IncludesEliteCount()
        {
            var allocation = new ZoneDefenderAllocation("faction-1", "zone-1");
            allocation.AddTroops(DefenderTier.Elite, 3);

            var result = allocation.ToString();

            Assert.Contains("Elite=3", result);
        }

        [Fact]
        public void ToString_IncludesAllTiers()
        {
            var allocation = new ZoneDefenderAllocation("faction-1", "zone-1");
            allocation.AddTroops(DefenderTier.Basic, 10);
            allocation.AddTroops(DefenderTier.Medium, 5);
            allocation.AddTroops(DefenderTier.Heavy, 2);
            allocation.AddTroops(DefenderTier.Elite, 1);

            var result = allocation.ToString();

            Assert.Contains("Basic=10", result);
            Assert.Contains("Medium=5", result);
            Assert.Contains("Heavy=2", result);
            Assert.Contains("Elite=1", result);
        }

        #endregion
    }
}
