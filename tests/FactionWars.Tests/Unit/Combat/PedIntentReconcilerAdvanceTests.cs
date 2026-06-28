using FactionWars.Combat.Models;
using FactionWars.Combat.Services;
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

            reconciler.Submit(50, PedIntent.AdvanceOnTarget(99, 18f));

            Assert.Equal(99, bridge.GetGoToEntityTarget(50)!.Value);
            Assert.Equal(18f, bridge.GetGoToEntityStoppingRange(50)!.Value);
        }

        [Fact]
        public void AdvanceOnTarget_SameTarget_NotReissued()
        {
            var bridge = new MockGameBridge();
            var reconciler = new PedIntentReconciler(bridge);

            reconciler.Submit(50, PedIntent.AdvanceOnTarget(99, 18f));
            int callsAfterFirst = bridge.GetGoToEntityCallCount(50);
            reconciler.Submit(50, PedIntent.AdvanceOnTarget(99, 18f)); // identical -> deduped

            Assert.Equal(callsAfterFirst, bridge.GetGoToEntityCallCount(50));
        }

        [Fact]
        public void AdvanceOnTarget_NewTarget_Reissued()
        {
            var bridge = new MockGameBridge();
            var reconciler = new PedIntentReconciler(bridge);

            reconciler.Submit(50, PedIntent.AdvanceOnTarget(99, 18f));
            reconciler.Submit(50, PedIntent.AdvanceOnTarget(123, 18f)); // different discriminator

            Assert.Equal(123, bridge.GetGoToEntityTarget(50));
        }
    }
}
