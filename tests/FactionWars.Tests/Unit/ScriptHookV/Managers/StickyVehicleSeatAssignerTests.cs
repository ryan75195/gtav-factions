using System.Collections.Generic;
using FactionWars.ScriptHookV.Managers;
using Xunit;

namespace FactionWars.Tests.Unit.ScriptHookV.Managers
{
    public class StickyVehicleSeatAssignerTests
    {
        private static HashSet<int> None => new HashSet<int>();

        [Fact]
        public void Sync_FirstPass_AssignsPrioritizedSeatsInOrder()
        {
            var assigner = new StickyVehicleSeatAssigner();

            assigner.Sync(new[] { 10, 20 }, new[] { 2, 3, 1 }, None);

            Assert.True(assigner.TryGetSeat(10, out var s10));
            Assert.True(assigner.TryGetSeat(20, out var s20));
            Assert.Equal(2, s10);
            Assert.Equal(3, s20);
        }

        [Fact]
        public void Sync_KeepsCommittedSeat_WhenFreeSeatsShiftAsOthersBoard()
        {
            // The bug: a still-approaching follower's seat changed every tick because the free-seat
            // list shifted as others boarded. The committed seat must stay put.
            var assigner = new StickyVehicleSeatAssigner();

            assigner.Sync(new[] { 10, 20 }, new[] { 2, 3, 1 }, None);
            Assert.True(assigner.TryGetSeat(20, out var seatBefore)); // 3

            // Ped 10 boarded (seat 2 now occupied); prioritized free seats shift to [3, 1].
            assigner.Sync(new[] { 10, 20 }, new[] { 3, 1 }, new HashSet<int> { 10 });

            Assert.True(assigner.TryGetSeat(20, out var seatAfter));
            Assert.Equal(seatBefore, seatAfter); // unchanged despite the shift
        }

        [Fact]
        public void Sync_DoesNotDoubleBookACommittedSeatForANewFollower()
        {
            var assigner = new StickyVehicleSeatAssigner();

            assigner.Sync(new[] { 10 }, new[] { 2, 3, 1 }, None); // 10 -> 2
            assigner.Sync(new[] { 10, 20 }, new[] { 2, 3, 1 }, None); // 20 must NOT also get 2

            Assert.True(assigner.TryGetSeat(10, out var s10));
            Assert.True(assigner.TryGetSeat(20, out var s20));
            Assert.Equal(2, s10);
            Assert.NotEqual(s10, s20);
        }

        [Fact]
        public void Sync_ReleasesCommitment_WhenFollowerBoards()
        {
            var assigner = new StickyVehicleSeatAssigner();

            assigner.Sync(new[] { 10 }, new[] { 2, 3, 1 }, None);
            assigner.Sync(new[] { 10 }, new[] { 3, 1 }, new HashSet<int> { 10 }); // 10 boarded

            Assert.False(assigner.TryGetSeat(10, out _));
        }

        [Fact]
        public void Sync_ReleasesCommitment_WhenFollowerLeavesProximity()
        {
            var assigner = new StickyVehicleSeatAssigner();

            assigner.Sync(new[] { 10 }, new[] { 2, 3, 1 }, None);
            assigner.Sync(new int[0], new[] { 2, 3, 1 }, None); // 10 no longer nearby

            Assert.False(assigner.TryGetSeat(10, out _));
        }

        [Fact]
        public void Sync_AssignsNothing_WhenNoSeatsAvailable()
        {
            var assigner = new StickyVehicleSeatAssigner();

            assigner.Sync(new[] { 10, 20 }, new int[0], None);

            Assert.False(assigner.TryGetSeat(10, out _));
            Assert.False(assigner.TryGetSeat(20, out _));
        }
    }
}
