using FactionWars.Core.Interfaces;
using FactionWars.Core.Models;
using FactionWars.Core.Services;
using FactionWars.Factions.Models;
using System;
using System.Collections.Generic;
using Xunit;

namespace FactionWars.Tests.Unit.Core
{
    /// <summary>
    /// Tests for ZoneDefenderAllocationService.
    /// The zone defender allocation system manages the assignment of troops
    /// from a faction's reserve pool to specific zones for defense.
    /// </summary>
    public class ZoneDefenderAllocationServiceTests
    {
        #region Constructor Tests

        [Fact]
        public void Constructor_CreatesInstance()
        {
            var repository = new InMemoryZoneDefenderAllocationRepository();
            var service = new ZoneDefenderAllocationService(repository);

            Assert.NotNull(service);
        }

        [Fact]
        public void Constructor_NullRepository_ThrowsArgumentNullException()
        {
            Assert.Throws<ArgumentNullException>(() => new ZoneDefenderAllocationService(null!));
        }

        [Fact]
        public void Constructor_ImplementsInterface()
        {
            var repository = new InMemoryZoneDefenderAllocationRepository();
            var service = new ZoneDefenderAllocationService(repository);

            Assert.IsAssignableFrom<IZoneDefenderAllocationService>(service);
        }

        #endregion

        #region AllocateTroops - Null Parameter Tests

        [Fact]
        public void AllocateTroops_NullFactionState_ThrowsArgumentNullException()
        {
            var service = CreateService();

            Assert.Throws<ArgumentNullException>(() =>
                service.AllocateTroops(null!, "zone-1", DefenderRole.Grunt, 5));
        }

        [Fact]
        public void AllocateTroops_NullZoneId_ThrowsArgumentNullException()
        {
            var service = CreateService();
            var factionState = CreateFactionStateWithReserves();

            Assert.Throws<ArgumentNullException>(() =>
                service.AllocateTroops(factionState, null!, DefenderRole.Grunt, 5));
        }

        [Fact]
        public void AllocateTroops_EmptyZoneId_ThrowsArgumentException()
        {
            var service = CreateService();
            var factionState = CreateFactionStateWithReserves();

            Assert.Throws<ArgumentException>(() =>
                service.AllocateTroops(factionState, "", DefenderRole.Grunt, 5));
        }

        [Fact]
        public void AllocateTroops_WhitespaceZoneId_ThrowsArgumentException()
        {
            var service = CreateService();
            var factionState = CreateFactionStateWithReserves();

            Assert.Throws<ArgumentException>(() =>
                service.AllocateTroops(factionState, "  ", DefenderRole.Grunt, 5));
        }

        #endregion

        #region AllocateTroops - Count Validation Tests

        [Fact]
        public void AllocateTroops_NegativeCount_ThrowsArgumentOutOfRangeException()
        {
            var service = CreateService();
            var factionState = CreateFactionStateWithReserves();

            Assert.Throws<ArgumentOutOfRangeException>(() =>
                service.AllocateTroops(factionState, "zone-1", DefenderRole.Grunt, -1));
        }

        [Fact]
        public void AllocateTroops_ZeroCount_ThrowsArgumentOutOfRangeException()
        {
            var service = CreateService();
            var factionState = CreateFactionStateWithReserves();

            Assert.Throws<ArgumentOutOfRangeException>(() =>
                service.AllocateTroops(factionState, "zone-1", DefenderRole.Grunt, 0));
        }

        #endregion

        #region AllocateTroops - Success Cases

        [Fact]
        public void AllocateTroops_WithSufficientReserve_ReturnsTrue()
        {
            var service = CreateService();
            var factionState = CreateFactionStateWithReserves(basic: 10);

            var result = service.AllocateTroops(factionState, "zone-1", DefenderRole.Grunt, 5);

            Assert.True(result);
        }

        [Fact]
        public void AllocateTroops_WithSufficientReserve_DeductsFromReserve()
        {
            var service = CreateService();
            var factionState = CreateFactionStateWithReserves(basic: 10);

            service.AllocateTroops(factionState, "zone-1", DefenderRole.Grunt, 5);

            Assert.Equal(5, factionState.GetReserveTroops(DefenderRole.Grunt));
        }

        [Fact]
        public void AllocateTroops_WithSufficientReserve_AddsToZoneAllocation()
        {
            var service = CreateService();
            var factionState = CreateFactionStateWithReserves(basic: 10);

            service.AllocateTroops(factionState, "zone-1", DefenderRole.Grunt, 5);

            var allocation = service.GetAllocation(factionState.FactionId, "zone-1");
            Assert.NotNull(allocation);
            Assert.Equal(5, allocation!.GetTroopCount(DefenderRole.Grunt));
        }

        [Fact]
        public void AllocateTroops_InsufficientReserve_ReturnsFalse()
        {
            var service = CreateService();
            var factionState = CreateFactionStateWithReserves(basic: 3);

            var result = service.AllocateTroops(factionState, "zone-1", DefenderRole.Grunt, 5);

            Assert.False(result);
        }

        [Fact]
        public void AllocateTroops_InsufficientReserve_DoesNotDeductFromReserve()
        {
            var service = CreateService();
            var factionState = CreateFactionStateWithReserves(basic: 3);

            service.AllocateTroops(factionState, "zone-1", DefenderRole.Grunt, 5);

            Assert.Equal(3, factionState.GetReserveTroops(DefenderRole.Grunt));
        }

        [Fact]
        public void AllocateTroops_ExactAmount_SucceedsAndLeavesZeroReserve()
        {
            var service = CreateService();
            var factionState = CreateFactionStateWithReserves(medium: 10);

            var result = service.AllocateTroops(factionState, "zone-1", DefenderRole.Gunner, 10);

            Assert.True(result);
            Assert.Equal(0, factionState.GetReserveTroops(DefenderRole.Gunner));
        }

        [Fact]
        public void AllocateTroops_MultipleTiers_TracksEachSeparately()
        {
            var service = CreateService();
            var factionState = CreateFactionStateWithReserves(basic: 10, medium: 5, heavy: 3);

            service.AllocateTroops(factionState, "zone-1", DefenderRole.Grunt, 3);
            service.AllocateTroops(factionState, "zone-1", DefenderRole.Gunner, 2);
            service.AllocateTroops(factionState, "zone-1", DefenderRole.Rifleman, 1);

            var allocation = service.GetAllocation(factionState.FactionId, "zone-1");
            Assert.NotNull(allocation);
            Assert.Equal(3, allocation!.GetTroopCount(DefenderRole.Grunt));
            Assert.Equal(2, allocation.GetTroopCount(DefenderRole.Gunner));
            Assert.Equal(1, allocation.GetTroopCount(DefenderRole.Rifleman));
        }

        [Fact]
        public void AllocateTroops_SameZoneMultipleTimes_Accumulates()
        {
            var service = CreateService();
            var factionState = CreateFactionStateWithReserves(basic: 20);

            service.AllocateTroops(factionState, "zone-1", DefenderRole.Grunt, 5);
            service.AllocateTroops(factionState, "zone-1", DefenderRole.Grunt, 3);

            var allocation = service.GetAllocation(factionState.FactionId, "zone-1");
            Assert.NotNull(allocation);
            Assert.Equal(8, allocation!.GetTroopCount(DefenderRole.Grunt));
        }

        [Fact]
        public void AllocateTroops_DifferentZones_TracksSeparately()
        {
            var service = CreateService();
            var factionState = CreateFactionStateWithReserves(basic: 20);

            service.AllocateTroops(factionState, "zone-1", DefenderRole.Grunt, 5);
            service.AllocateTroops(factionState, "zone-2", DefenderRole.Grunt, 8);

            var allocation1 = service.GetAllocation(factionState.FactionId, "zone-1");
            var allocation2 = service.GetAllocation(factionState.FactionId, "zone-2");

            Assert.NotNull(allocation1);
            Assert.NotNull(allocation2);
            Assert.Equal(5, allocation1!.GetTroopCount(DefenderRole.Grunt));
            Assert.Equal(8, allocation2!.GetTroopCount(DefenderRole.Grunt));
        }

        [Fact]
        public void AllocateTroops_HeavyTier_Works()
        {
            var service = CreateService();
            var factionState = CreateFactionStateWithReserves(heavy: 5);

            var result = service.AllocateTroops(factionState, "zone-1", DefenderRole.Rifleman, 3);

            Assert.True(result);
            Assert.Equal(2, factionState.GetReserveTroops(DefenderRole.Rifleman));
        }

        [Fact]
        public void AllocateTroops_EliteTier_Works()
        {
            var service = CreateService();
            var factionState = CreateFactionStateWithReserves(elite: 5);

            var result = service.AllocateTroops(factionState, "zone-1", DefenderRole.Rocketeer, 3);

            Assert.True(result);
            Assert.Equal(2, factionState.GetReserveTroops(DefenderRole.Rocketeer));
        }

        [Fact]
        public void AllocateTroops_AllTiers_TracksEachSeparately()
        {
            var service = CreateService();
            var factionState = CreateFactionStateWithReserves(basic: 10, medium: 5, heavy: 3, elite: 2);

            service.AllocateTroops(factionState, "zone-1", DefenderRole.Grunt, 3);
            service.AllocateTroops(factionState, "zone-1", DefenderRole.Gunner, 2);
            service.AllocateTroops(factionState, "zone-1", DefenderRole.Rifleman, 1);
            service.AllocateTroops(factionState, "zone-1", DefenderRole.Rocketeer, 1);

            var allocation = service.GetAllocation(factionState.FactionId, "zone-1");
            Assert.NotNull(allocation);
            Assert.Equal(3, allocation!.GetTroopCount(DefenderRole.Grunt));
            Assert.Equal(2, allocation.GetTroopCount(DefenderRole.Gunner));
            Assert.Equal(1, allocation.GetTroopCount(DefenderRole.Rifleman));
            Assert.Equal(1, allocation.GetTroopCount(DefenderRole.Rocketeer));
        }

        #endregion

        #region GetAllocation Tests

        [Fact]
        public void GetAllocation_NullFactionId_ThrowsArgumentNullException()
        {
            var service = CreateService();

            Assert.Throws<ArgumentNullException>(() => service.GetAllocation(null!, "zone-1"));
        }

        [Fact]
        public void GetAllocation_NullZoneId_ThrowsArgumentNullException()
        {
            var service = CreateService();

            Assert.Throws<ArgumentNullException>(() => service.GetAllocation("faction-1", null!));
        }

        [Fact]
        public void GetAllocation_NoAllocation_ReturnsNull()
        {
            var service = CreateService();

            var allocation = service.GetAllocation("faction-1", "zone-1");

            Assert.Null(allocation);
        }

        [Fact]
        public void GetAllocation_AfterAllocation_ReturnsAllocation()
        {
            var service = CreateService();
            var factionState = CreateFactionStateWithReserves(basic: 10);
            service.AllocateTroops(factionState, "zone-1", DefenderRole.Grunt, 5);

            var allocation = service.GetAllocation(factionState.FactionId, "zone-1");

            Assert.NotNull(allocation);
            Assert.Equal(factionState.FactionId, allocation!.FactionId);
            Assert.Equal("zone-1", allocation.ZoneId);
        }

        #endregion

        #region GetAllocationsForFaction Tests

        [Fact]
        public void GetAllocationsForFaction_NullFactionId_ThrowsArgumentNullException()
        {
            var service = CreateService();

            Assert.Throws<ArgumentNullException>(() => service.GetAllocationsForFaction(null!));
        }

        [Fact]
        public void GetAllocationsForFaction_NoAllocations_ReturnsEmptyList()
        {
            var service = CreateService();

            var allocations = service.GetAllocationsForFaction("faction-1");

            Assert.Empty(allocations);
        }

        [Fact]
        public void GetAllocationsForFaction_WithAllocations_ReturnsAll()
        {
            var service = CreateService();
            var factionState = CreateFactionStateWithReserves(basic: 30);
            service.AllocateTroops(factionState, "zone-1", DefenderRole.Grunt, 5);
            service.AllocateTroops(factionState, "zone-2", DefenderRole.Grunt, 10);

            var allocations = service.GetAllocationsForFaction(factionState.FactionId);

            Assert.Equal(2, allocations.Count);
        }

        #endregion

        #region GetTotalAllocatedTroops Tests

        [Fact]
        public void GetTotalAllocatedTroops_NullFactionId_ThrowsArgumentNullException()
        {
            var service = CreateService();

            Assert.Throws<ArgumentNullException>(() => service.GetTotalAllocatedTroops(null!));
        }

        [Fact]
        public void GetTotalAllocatedTroops_NoAllocations_ReturnsZero()
        {
            var service = CreateService();

            var total = service.GetTotalAllocatedTroops("faction-1");

            Assert.Equal(0, total);
        }

        [Fact]
        public void GetTotalAllocatedTroops_WithAllocations_ReturnsTotalAcrossAllZones()
        {
            var service = CreateService();
            var factionState = CreateFactionStateWithReserves(basic: 30, medium: 10);
            service.AllocateTroops(factionState, "zone-1", DefenderRole.Grunt, 5);
            service.AllocateTroops(factionState, "zone-2", DefenderRole.Grunt, 10);
            service.AllocateTroops(factionState, "zone-1", DefenderRole.Gunner, 3);

            var total = service.GetTotalAllocatedTroops(factionState.FactionId);

            Assert.Equal(18, total); // 5 + 10 + 3
        }

        #endregion

        #region WithdrawTroops - Null Parameter Tests

        [Fact]
        public void WithdrawTroops_NullFactionState_ThrowsArgumentNullException()
        {
            var service = CreateService();

            Assert.Throws<ArgumentNullException>(() =>
                service.WithdrawTroops(null!, "zone-1", DefenderRole.Grunt, 5));
        }

        [Fact]
        public void WithdrawTroops_NullZoneId_ThrowsArgumentNullException()
        {
            var service = CreateService();
            var factionState = CreateFactionStateWithReserves();

            Assert.Throws<ArgumentNullException>(() =>
                service.WithdrawTroops(factionState, null!, DefenderRole.Grunt, 5));
        }

        [Fact]
        public void WithdrawTroops_EmptyZoneId_ThrowsArgumentException()
        {
            var service = CreateService();
            var factionState = CreateFactionStateWithReserves();

            Assert.Throws<ArgumentException>(() =>
                service.WithdrawTroops(factionState, "", DefenderRole.Grunt, 5));
        }

        [Fact]
        public void WithdrawTroops_WhitespaceZoneId_ThrowsArgumentException()
        {
            var service = CreateService();
            var factionState = CreateFactionStateWithReserves();

            Assert.Throws<ArgumentException>(() =>
                service.WithdrawTroops(factionState, "  ", DefenderRole.Grunt, 5));
        }

        #endregion

        #region WithdrawTroops - Count Validation Tests

        [Fact]
        public void WithdrawTroops_NegativeCount_ThrowsArgumentOutOfRangeException()
        {
            var service = CreateService();
            var factionState = CreateFactionStateWithReserves();

            Assert.Throws<ArgumentOutOfRangeException>(() =>
                service.WithdrawTroops(factionState, "zone-1", DefenderRole.Grunt, -1));
        }

        [Fact]
        public void WithdrawTroops_ZeroCount_ThrowsArgumentOutOfRangeException()
        {
            var service = CreateService();
            var factionState = CreateFactionStateWithReserves();

            Assert.Throws<ArgumentOutOfRangeException>(() =>
                service.WithdrawTroops(factionState, "zone-1", DefenderRole.Grunt, 0));
        }

        #endregion

        #region WithdrawTroops - Success Cases

        [Fact]
        public void WithdrawTroops_WithSufficientAllocation_ReturnsTrue()
        {
            var service = CreateService();
            var factionState = CreateFactionStateWithReserves(basic: 10);
            service.AllocateTroops(factionState, "zone-1", DefenderRole.Grunt, 5);

            var result = service.WithdrawTroops(factionState, "zone-1", DefenderRole.Grunt, 3);

            Assert.True(result);
        }

        [Fact]
        public void WithdrawTroops_WithSufficientAllocation_AddsToReserve()
        {
            var service = CreateService();
            var factionState = CreateFactionStateWithReserves(basic: 10);
            service.AllocateTroops(factionState, "zone-1", DefenderRole.Grunt, 5);
            // Reserve is now 5 (10 - 5 allocated)

            service.WithdrawTroops(factionState, "zone-1", DefenderRole.Grunt, 3);

            Assert.Equal(8, factionState.GetReserveTroops(DefenderRole.Grunt)); // 5 + 3 withdrawn
        }

        [Fact]
        public void WithdrawTroops_WithSufficientAllocation_ReducesZoneAllocation()
        {
            var service = CreateService();
            var factionState = CreateFactionStateWithReserves(basic: 10);
            service.AllocateTroops(factionState, "zone-1", DefenderRole.Grunt, 5);

            service.WithdrawTroops(factionState, "zone-1", DefenderRole.Grunt, 3);

            var allocation = service.GetAllocation(factionState.FactionId, "zone-1");
            Assert.NotNull(allocation);
            Assert.Equal(2, allocation!.GetTroopCount(DefenderRole.Grunt));
        }

        [Fact]
        public void WithdrawTroops_NoAllocation_ReturnsFalse()
        {
            var service = CreateService();
            var factionState = CreateFactionStateWithReserves();

            var result = service.WithdrawTroops(factionState, "zone-1", DefenderRole.Grunt, 5);

            Assert.False(result);
        }

        [Fact]
        public void WithdrawTroops_InsufficientAllocation_ReturnsFalse()
        {
            var service = CreateService();
            var factionState = CreateFactionStateWithReserves(basic: 10);
            service.AllocateTroops(factionState, "zone-1", DefenderRole.Grunt, 3);

            var result = service.WithdrawTroops(factionState, "zone-1", DefenderRole.Grunt, 5);

            Assert.False(result);
        }

        [Fact]
        public void WithdrawTroops_InsufficientAllocation_DoesNotAddToReserve()
        {
            var service = CreateService();
            var factionState = CreateFactionStateWithReserves(basic: 10);
            service.AllocateTroops(factionState, "zone-1", DefenderRole.Grunt, 3);
            // Reserve is now 7 (10 - 3 allocated)

            service.WithdrawTroops(factionState, "zone-1", DefenderRole.Grunt, 5);

            Assert.Equal(7, factionState.GetReserveTroops(DefenderRole.Grunt));
        }

        [Fact]
        public void WithdrawTroops_ExactAmount_SucceedsAndLeavesZeroAllocation()
        {
            var service = CreateService();
            var factionState = CreateFactionStateWithReserves(medium: 10);
            service.AllocateTroops(factionState, "zone-1", DefenderRole.Gunner, 5);

            var result = service.WithdrawTroops(factionState, "zone-1", DefenderRole.Gunner, 5);

            Assert.True(result);
            var allocation = service.GetAllocation(factionState.FactionId, "zone-1");
            Assert.NotNull(allocation);
            Assert.Equal(0, allocation!.GetTroopCount(DefenderRole.Gunner));
        }

        [Fact]
        public void WithdrawTroops_MultipleTiers_WithdrawsOnlySpecifiedTier()
        {
            var service = CreateService();
            var factionState = CreateFactionStateWithReserves(basic: 10, medium: 5, heavy: 3);
            service.AllocateTroops(factionState, "zone-1", DefenderRole.Grunt, 3);
            service.AllocateTroops(factionState, "zone-1", DefenderRole.Gunner, 2);
            service.AllocateTroops(factionState, "zone-1", DefenderRole.Rifleman, 1);

            service.WithdrawTroops(factionState, "zone-1", DefenderRole.Gunner, 1);

            var allocation = service.GetAllocation(factionState.FactionId, "zone-1");
            Assert.NotNull(allocation);
            Assert.Equal(3, allocation!.GetTroopCount(DefenderRole.Grunt));
            Assert.Equal(1, allocation.GetTroopCount(DefenderRole.Gunner));
            Assert.Equal(1, allocation.GetTroopCount(DefenderRole.Rifleman));
        }

        [Fact]
        public void WithdrawTroops_WrongTier_ReturnsFalse()
        {
            var service = CreateService();
            var factionState = CreateFactionStateWithReserves(basic: 10);
            service.AllocateTroops(factionState, "zone-1", DefenderRole.Grunt, 5);

            var result = service.WithdrawTroops(factionState, "zone-1", DefenderRole.Rifleman, 1);

            Assert.False(result);
        }

        [Fact]
        public void WithdrawTroops_HeavyTier_Works()
        {
            var service = CreateService();
            var factionState = CreateFactionStateWithReserves(heavy: 5);
            service.AllocateTroops(factionState, "zone-1", DefenderRole.Rifleman, 3);

            var result = service.WithdrawTroops(factionState, "zone-1", DefenderRole.Rifleman, 2);

            Assert.True(result);
            Assert.Equal(4, factionState.GetReserveTroops(DefenderRole.Rifleman)); // 2 remaining + 2 withdrawn
        }

        [Fact]
        public void WithdrawTroops_EliteTier_Works()
        {
            var service = CreateService();
            var factionState = CreateFactionStateWithReserves(elite: 5);
            service.AllocateTroops(factionState, "zone-1", DefenderRole.Rocketeer, 3);

            var result = service.WithdrawTroops(factionState, "zone-1", DefenderRole.Rocketeer, 2);

            Assert.True(result);
            Assert.Equal(4, factionState.GetReserveTroops(DefenderRole.Rocketeer)); // 2 remaining + 2 withdrawn
        }

        [Fact]
        public void WithdrawTroops_UpdatesTotalAllocatedTroops()
        {
            var service = CreateService();
            var factionState = CreateFactionStateWithReserves(basic: 20);
            service.AllocateTroops(factionState, "zone-1", DefenderRole.Grunt, 5);
            service.AllocateTroops(factionState, "zone-2", DefenderRole.Grunt, 5);

            service.WithdrawTroops(factionState, "zone-1", DefenderRole.Grunt, 3);

            var total = service.GetTotalAllocatedTroops(factionState.FactionId);
            Assert.Equal(7, total); // 5 - 3 + 5 = 7
        }

        #endregion

        #region Helper Methods

        private ZoneDefenderAllocationService CreateService()
        {
            var repository = new InMemoryZoneDefenderAllocationRepository();
            return new ZoneDefenderAllocationService(repository);
        }

        private FactionState CreateFactionStateWithReserves(int basic = 0, int medium = 0, int heavy = 0, int elite = 0)
        {
            // After consolidation, initialTroopCount goes to Basic tier, so we don't use it
            var state = new FactionState("test-faction", 10000);
            state.AddReserveTroops(DefenderRole.Grunt, basic);
            state.AddReserveTroops(DefenderRole.Gunner, medium);
            state.AddReserveTroops(DefenderRole.Rifleman, heavy);
            state.AddReserveTroops(DefenderRole.Rocketeer, elite);
            return state;
        }

        #endregion
    }
}
