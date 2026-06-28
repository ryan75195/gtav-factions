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
            allocation.AddTroops(DefenderRole.Rocketeer, 5);

            var data = ZoneDefenderAllocationData.FromAllocation(allocation);

            Assert.True(data.Troops.ContainsKey(DefenderRole.Rocketeer));
            Assert.Equal(5, data.Troops[DefenderRole.Rocketeer]);
        }

        [Fact]
        public void FromAllocation_IncludesAllTiers()
        {
            var allocation = new ZoneDefenderAllocation("faction-1", "zone-1");
            allocation.AddTroops(DefenderRole.Grunt, 10);
            allocation.AddTroops(DefenderRole.Gunner, 7);
            allocation.AddTroops(DefenderRole.Rifleman, 3);
            allocation.AddTroops(DefenderRole.Rocketeer, 1);

            var data = ZoneDefenderAllocationData.FromAllocation(allocation);

            Assert.Equal(10, data.Troops[DefenderRole.Grunt]);
            Assert.Equal(7, data.Troops[DefenderRole.Gunner]);
            Assert.Equal(3, data.Troops[DefenderRole.Rifleman]);
            Assert.Equal(1, data.Troops[DefenderRole.Rocketeer]);
        }

        [Fact]
        public void FromAllocation_PreservesFactionIdAndZoneId()
        {
            var allocation = new ZoneDefenderAllocation("test-faction", "test-zone");
            allocation.AddTroops(DefenderRole.Rocketeer, 2);

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
                Troops = new System.Collections.Generic.Dictionary<DefenderRole, int>
                {
                    { DefenderRole.Rocketeer, 4 }
                }
            };

            var allocation = data.ToAllocation();

            Assert.Equal(4, allocation.GetTroopCount(DefenderRole.Rocketeer));
        }

        [Fact]
        public void ToAllocation_RestoresAllTiers()
        {
            var data = new ZoneDefenderAllocationData
            {
                FactionId = "faction-1",
                ZoneId = "zone-1",
                Troops = new System.Collections.Generic.Dictionary<DefenderRole, int>
                {
                    { DefenderRole.Grunt, 8 },
                    { DefenderRole.Gunner, 4 },
                    { DefenderRole.Rifleman, 2 },
                    { DefenderRole.Rocketeer, 1 }
                }
            };

            var allocation = data.ToAllocation();

            Assert.Equal(8, allocation.GetTroopCount(DefenderRole.Grunt));
            Assert.Equal(4, allocation.GetTroopCount(DefenderRole.Gunner));
            Assert.Equal(2, allocation.GetTroopCount(DefenderRole.Rifleman));
            Assert.Equal(1, allocation.GetTroopCount(DefenderRole.Rocketeer));
        }

        [Fact]
        public void ToAllocation_PreservesFactionIdAndZoneId()
        {
            var data = new ZoneDefenderAllocationData
            {
                FactionId = "my-faction",
                ZoneId = "my-zone",
                Troops = new System.Collections.Generic.Dictionary<DefenderRole, int>
                {
                    { DefenderRole.Rocketeer, 1 }
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
                Troops = new System.Collections.Generic.Dictionary<DefenderRole, int>
                {
                    { DefenderRole.Grunt, 0 },
                    { DefenderRole.Rocketeer, 5 }
                }
            };

            var allocation = data.ToAllocation();

            // Zero values should result in zero troop count
            Assert.Equal(0, allocation.GetTroopCount(DefenderRole.Grunt));
            Assert.Equal(5, allocation.GetTroopCount(DefenderRole.Rocketeer));
        }

        #endregion

        #region Roundtrip Tests

        [Fact]
        public void Roundtrip_PreservesAllTierData()
        {
            // Create original allocation with all tiers
            var original = new ZoneDefenderAllocation("faction-1", "zone-1");
            original.AddTroops(DefenderRole.Grunt, 12);
            original.AddTroops(DefenderRole.Gunner, 6);
            original.AddTroops(DefenderRole.Rifleman, 3);
            original.AddTroops(DefenderRole.Rocketeer, 1);

            // Convert to data and back
            var data = ZoneDefenderAllocationData.FromAllocation(original);
            var restored = data.ToAllocation();

            // Verify all fields preserved
            Assert.Equal(original.FactionId, restored.FactionId);
            Assert.Equal(original.ZoneId, restored.ZoneId);
            Assert.Equal(original.GetTroopCount(DefenderRole.Grunt), restored.GetTroopCount(DefenderRole.Grunt));
            Assert.Equal(original.GetTroopCount(DefenderRole.Gunner), restored.GetTroopCount(DefenderRole.Gunner));
            Assert.Equal(original.GetTroopCount(DefenderRole.Rifleman), restored.GetTroopCount(DefenderRole.Rifleman));
            Assert.Equal(original.GetTroopCount(DefenderRole.Rocketeer), restored.GetTroopCount(DefenderRole.Rocketeer));
            Assert.Equal(original.TotalTroops, restored.TotalTroops);
        }

        #endregion
    }
}
