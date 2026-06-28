using FactionWars.Combat.Models;
using FactionWars.Combat.Services;
using FactionWars.Core.Interfaces;
using Moq;
using Xunit;

namespace FactionWars.Tests.Unit.Combat
{
    public class PedIntentReconcilerTests
    {
        private readonly Mock<IGameBridge> _bridge = new Mock<IGameBridge>();

        private PedIntentReconciler Build() => new PedIntentReconciler(_bridge.Object);

        [Fact]
        public void Submit_CombatTarget_DetachesAndTasksCombat()
        {
            var r = Build();
            r.Submit(10, PedIntent.CombatTarget(55));
            _bridge.Verify(b => b.RemovePedFromFollowerGroup(10), Times.Once);
            _bridge.Verify(b => b.TaskCombatPed(10, 55), Times.Once);
        }

        [Fact]
        public void Submit_IdenticalIntentTwice_AppliesOnce()
        {
            var r = Build();
            r.Submit(10, PedIntent.CombatTarget(55));
            r.Submit(10, PedIntent.CombatTarget(55));
            _bridge.Verify(b => b.TaskCombatPed(10, 55), Times.Once);
        }

        [Fact]
        public void Submit_ChangedTarget_ReappliesForNewTarget()
        {
            var r = Build();
            r.Submit(10, PedIntent.CombatTarget(55));
            r.Submit(10, PedIntent.CombatTarget(66));
            _bridge.Verify(b => b.TaskCombatPed(10, 55), Times.Once);
            _bridge.Verify(b => b.TaskCombatPed(10, 66), Times.Once);
        }

        [Fact]
        public void Submit_FollowPlayer_SetsFollower()
        {
            var r = Build();
            r.Submit(7, PedIntent.FollowPlayer());
            _bridge.Verify(b => b.SetPedAsFollower(7), Times.Once);
        }

        [Fact]
        public void Submit_GuardArea_DetachesAndGuards()
        {
            var r = Build();
            var center = new Vector3(1f, 2f, 3f);
            r.Submit(8, PedIntent.GuardArea(center, 10f, 0));
            _bridge.Verify(b => b.RemovePedFromFollowerGroup(8), Times.Once);
            _bridge.Verify(b => b.TaskGuardArea(8, center, 10f), Times.Once);
        }

        [Fact]
        public void Submit_GoToCoord_ClearsThenGoes()
        {
            var r = Build();
            var dest = new Vector3(5f, 6f, 7f);
            r.Submit(9, PedIntent.GoToCoord(dest));
            _bridge.Verify(b => b.ClearPedTasks(9), Times.Once);
            _bridge.Verify(b => b.TaskGoToCoord(9, dest), Times.Once);
        }

        [Fact]
        public void Submit_SeekHatedTargets_DetachesAndSeeks()
        {
            var r = Build();
            var center = new Vector3(0f, 0f, 0f);
            r.Submit(11, PedIntent.SeekHatedTargets(center, 50f));
            _bridge.Verify(b => b.RemovePedFromFollowerGroup(11), Times.Once);
            _bridge.Verify(b => b.TaskCombatHatedTargetsAroundPed(11, 50f), Times.Once);
        }

        [Fact]
        public void Submit_WanderArea_Wanders()
        {
            var r = Build();
            var center = new Vector3(2f, 2f, 2f);
            r.Submit(12, PedIntent.WanderArea(center, 30f));
            _bridge.Verify(b => b.TaskPedWanderInBoundedArea(12, center, 30f), Times.Once);
        }

        [Fact]
        public void Submit_LeaveVehicle_TasksLeave()
        {
            var r = Build();
            r.Submit(13, PedIntent.LeaveVehicle());
            _bridge.Verify(b => b.TaskPedLeaveVehicle(13), Times.Once);
        }

        [Fact]
        public void Forget_ThenResubmit_Reapplies()
        {
            var r = Build();
            r.Submit(10, PedIntent.CombatTarget(55));
            r.Forget(10);
            r.Submit(10, PedIntent.CombatTarget(55));
            _bridge.Verify(b => b.TaskCombatPed(10, 55), Times.Exactly(2));
        }

        [Fact]
        public void Clear_ThenResubmit_Reapplies()
        {
            var r = Build();
            r.Submit(10, PedIntent.CombatTarget(55));
            r.Clear();
            r.Submit(10, PedIntent.CombatTarget(55));
            _bridge.Verify(b => b.TaskCombatPed(10, 55), Times.Exactly(2));
        }
    }
}
