using FactionWars.Combat.Models;
using FactionWars.Combat.Services;
using FactionWars.Core.Interfaces;
using FactionWars.Core.Utils;
using Xunit;

namespace FactionWars.Tests.Unit.Combat
{
    public class PedIntentReconcilerAdvanceTests
    {
        [Fact]
        public void AdvanceOnTarget_TasksGoToEntity_AtStoppingRange()
        {
            var bridge = new MockGameBridge();
            var reconciler = new PedIntentReconciler(bridge);
            int follower = bridge.CreatePed("f", new Vector3(0f, 0f, 0f));
            int target = bridge.CreatePed("e", new Vector3(0f, 0f, 0f));

            reconciler.Submit(follower, PedIntent.AdvanceOnTarget(target, 18f));

            Assert.Equal(target, bridge.GetGoToEntityTarget(follower)!.Value);
            Assert.Equal(18f, bridge.GetGoToEntityStoppingRange(follower)!.Value);
        }

        [Fact]
        public void AdvanceOnTarget_SameTarget_NotReissued()
        {
            var bridge = new MockGameBridge();
            var reconciler = new PedIntentReconciler(bridge);
            int follower = bridge.CreatePed("f", new Vector3(0f, 0f, 0f));
            int target = bridge.CreatePed("e", new Vector3(0f, 0f, 0f));

            reconciler.Submit(follower, PedIntent.AdvanceOnTarget(target, 18f));
            int callsAfterFirst = bridge.GetGoToEntityCallCount(follower);
            reconciler.Submit(follower, PedIntent.AdvanceOnTarget(target, 18f)); // identical -> deduped

            Assert.Equal(callsAfterFirst, bridge.GetGoToEntityCallCount(follower));
        }

        [Fact]
        public void AdvanceOnTarget_SameTarget_DifferentRange_NotReissued()
        {
            var bridge = new MockGameBridge();
            var reconciler = new PedIntentReconciler(bridge);
            int follower = bridge.CreatePed("f", new Vector3(0f, 0f, 0f));
            int target = bridge.CreatePed("e", new Vector3(0f, 0f, 0f));

            reconciler.Submit(follower, PedIntent.AdvanceOnTarget(target, 18f));
            int callsAfterFirst = bridge.GetGoToEntityCallCount(follower);
            reconciler.Submit(follower, PedIntent.AdvanceOnTarget(target, 30f)); // same target, new range -> Radius is not part of equality, so deduped

            Assert.Equal(callsAfterFirst, bridge.GetGoToEntityCallCount(follower));
        }

        [Fact]
        public void AdvanceOnTarget_NewTarget_Reissued()
        {
            var bridge = new MockGameBridge();
            var reconciler = new PedIntentReconciler(bridge);
            int follower = bridge.CreatePed("f", new Vector3(0f, 0f, 0f));
            int target = bridge.CreatePed("e", new Vector3(0f, 0f, 0f));
            int newTarget = bridge.CreatePed("e2", new Vector3(0f, 0f, 0f));

            reconciler.Submit(follower, PedIntent.AdvanceOnTarget(target, 18f));
            reconciler.Submit(follower, PedIntent.AdvanceOnTarget(newTarget, 18f)); // different discriminator

            Assert.Equal(newTarget, bridge.GetGoToEntityTarget(follower));
        }
    }
}
