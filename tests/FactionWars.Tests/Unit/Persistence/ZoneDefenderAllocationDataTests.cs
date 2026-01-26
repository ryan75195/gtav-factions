using FactionWars.Core.Models;
using FactionWars.Persistence.Models;
using Xunit;

namespace FactionWars.Tests.Unit.Persistence
{
    /// <summary>
    /// Tests for ZoneDefenderAllocationData persistence model.
    /// Validates that all defender tiers (Basic, Medium, Heavy, Elite) serialize and deserialize correctly.
    /// </summary>
    public class ZoneDefenderAllocationDataTests
    {
        #region FromAllocation Tests

        [Fact]
        public void FromAllocation_IncludesEliteTier()
        {
            var allocation = new ZoneDefenderAllocation("faction-1", "zone-1");
            allocation.AddTroops(DefenderTier.Elite, 5);

            var data = ZoneDefenderAllocationData.FromAllocation(allocation);

            Assert.True(data.Troops.ContainsKey(DefenderTier.Elite));
            Assert.Equal(5, data.Troops[DefenderTier.Elite]);
        }

        [Fact]
        public void FromAllocation_IncludesAllTiers()
        {
            var allocation = new ZoneDefenderAllocation("faction-1", "zone-1");
            allocation.AddTroops(DefenderTier.Basic, 10);
            allocation.AddTroops(DefenderTier.Medium, 7);
            allocation.AddTroops(DefenderTier.Heavy, 3);
            allocation.AddTroops(DefenderTier.Elite, 1);

            var data = ZoneDefenderAllocationData.FromAllocation(allocation);

            Assert.Equal(10, data.Troops[DefenderTier.Basic]);
            Assert.Equal(7, data.Troops[DefenderTier.Medium]);
            Assert.Equal(3, data.Troops[DefenderTier.Heavy]);
            Assert.Equal(1, data.Troops[DefenderTier.Elite]);
        }

        [Fact]
        public void FromAllocation_PreservesFactionIdAndZoneId()
        {
            var allocation = new ZoneDefenderAllocation("test-faction", "test-zone");
            allocation.AddTroops(DefenderTier.Elite, 2);

            var data = ZoneDefenderAllocationData.FromAllocation(allocation);

            Assert.Equal("test-faction", data.FactionId);
            Assert.Equal("test-zone", data.ZoneId);
        }

        #endregion

        #region ToAllocation Tests

        [Fact]
        public void ToAllocation_RestoresEliteTier()
        {
            var data = new ZoneDefenderAllocationData
            {
                FactionId = "faction-1",
                ZoneId = "zone-1",
                Troops = new System.Collections.Generic.Dictionary<DefenderTier, int>
                {
                    { DefenderTier.Elite, 4 }
                }
            };

            var allocation = data.ToAllocation();

            Assert.Equal(4, allocation.GetTroopCount(DefenderTier.Elite));
        }

        [Fact]
        public void ToAllocation_RestoresAllTiers()
        {
            var data = new ZoneDefenderAllocationData
            {
                FactionId = "faction-1",
                ZoneId = "zone-1",
                Troops = new System.Collections.Generic.Dictionary<DefenderTier, int>
                {
                    { DefenderTier.Basic, 8 },
                    { DefenderTier.Medium, 4 },
                    { DefenderTier.Heavy, 2 },
                    { DefenderTier.Elite, 1 }
                }
            };

            var allocation = data.ToAllocation();

            Assert.Equal(8, allocation.GetTroopCount(DefenderTier.Basic));
            Assert.Equal(4, allocation.GetTroopCount(DefenderTier.Medium));
            Assert.Equal(2, allocation.GetTroopCount(DefenderTier.Heavy));
            Assert.Equal(1, allocation.GetTroopCount(DefenderTier.Elite));
        }

        [Fact]
        public void ToAllocation_PreservesFactionIdAndZoneId()
        {
            var data = new ZoneDefenderAllocationData
            {
                FactionId = "my-faction",
                ZoneId = "my-zone",
                Troops = new System.Collections.Generic.Dictionary<DefenderTier, int>
                {
                    { DefenderTier.Elite, 1 }
                }
            };

            var allocation = data.ToAllocation();

            Assert.Equal("my-faction", allocation.FactionId);
            Assert.Equal("my-zone", allocation.ZoneId);
        }

        [Fact]
        public void ToAllocation_SkipsZeroValues()
        {
            var data = new ZoneDefenderAllocationData
            {
                FactionId = "faction-1",
                ZoneId = "zone-1",
                Troops = new System.Collections.Generic.Dictionary<DefenderTier, int>
                {
                    { DefenderTier.Basic, 0 },
                    { DefenderTier.Elite, 5 }
                }
            };

            var allocation = data.ToAllocation();

            // Zero values should result in zero troop count
            Assert.Equal(0, allocation.GetTroopCount(DefenderTier.Basic));
            Assert.Equal(5, allocation.GetTroopCount(DefenderTier.Elite));
        }

        #endregion

        #region Roundtrip Tests

        [Fact]
        public void Roundtrip_PreservesAllTierData()
        {
            // Create original allocation with all tiers
            var original = new ZoneDefenderAllocation("faction-1", "zone-1");
            original.AddTroops(DefenderTier.Basic, 12);
            original.AddTroops(DefenderTier.Medium, 6);
            original.AddTroops(DefenderTier.Heavy, 3);
            original.AddTroops(DefenderTier.Elite, 1);

            // Convert to data and back
            var data = ZoneDefenderAllocationData.FromAllocation(original);
            var restored = data.ToAllocation();

            // Verify all fields preserved
            Assert.Equal(original.FactionId, restored.FactionId);
            Assert.Equal(original.ZoneId, restored.ZoneId);
            Assert.Equal(original.GetTroopCount(DefenderTier.Basic), restored.GetTroopCount(DefenderTier.Basic));
            Assert.Equal(original.GetTroopCount(DefenderTier.Medium), restored.GetTroopCount(DefenderTier.Medium));
            Assert.Equal(original.GetTroopCount(DefenderTier.Heavy), restored.GetTroopCount(DefenderTier.Heavy));
            Assert.Equal(original.GetTroopCount(DefenderTier.Elite), restored.GetTroopCount(DefenderTier.Elite));
            Assert.Equal(original.TotalTroops, restored.TotalTroops);
        }

        #endregion
    }
}
