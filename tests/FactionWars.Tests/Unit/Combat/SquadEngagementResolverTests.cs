using FactionWars.Combat.Interfaces;
using FactionWars.Combat.Models;
using FactionWars.Combat.Services;
using FactionWars.Core.Models;
using Xunit;

namespace FactionWars.Tests.Unit.Combat
{
    public class SquadEngagementResolverTests
    {
        // Grunt range = 18; hysteresis band = 18 * 1.3 = 23.4; LOS grace = 2.
        private readonly ISquadEngagementResolver _resolver =
            new SquadEngagementResolver(new EngageRangeProvider());

        private EngageDecision Resolve(float dist, bool los, EngagePhase phase, int losMisses)
            => _resolver.Resolve(dist, los, DefenderRole.Grunt, phase, losMisses);

        [Fact]
        public void OutOfRange_Advances()
        {
            var d = Resolve(40f, true, EngagePhase.Advance, 0);
            Assert.Equal(EngagePhase.Advance, d.Phase);
            Assert.Equal(18f, d.EngageRange);
            Assert.Equal(0, d.ConsecutiveLosMisses); // LOS present -> counter stays cleared
        }

        [Fact]
        public void InRangeWithLos_Engages()
        {
            var d = Resolve(15f, true, EngagePhase.Advance, 0);
            Assert.Equal(EngagePhase.Engage, d.Phase);
            Assert.Equal(0, d.ConsecutiveLosMisses);
        }

        [Fact]
        public void InRangeNoLos_StaysAdvance()
        {
            var d = Resolve(15f, false, EngagePhase.Advance, 0);
            Assert.Equal(EngagePhase.Advance, d.Phase);
            Assert.Equal(1, d.ConsecutiveLosMisses); // miss counter increments even while advancing
        }

        [Fact]
        public void Engaging_WithinHysteresisBand_StaysEngaged()
        {
            // 20m is > range(18) but <= 18*1.3 (23.4): keep engaging.
            var d = Resolve(20f, true, EngagePhase.Engage, 0);
            Assert.Equal(EngagePhase.Engage, d.Phase);
        }

        [Fact]
        public void Engaging_PastHysteresisBand_DropsToAdvance()
        {
            var d = Resolve(30f, true, EngagePhase.Engage, 0);
            Assert.Equal(EngagePhase.Advance, d.Phase);
        }

        [Fact]
        public void Engaging_FirstLosMiss_StaysEngaged_CounterIncrements()
        {
            var d = Resolve(15f, false, EngagePhase.Engage, 0);
            Assert.Equal(EngagePhase.Engage, d.Phase);
            Assert.Equal(1, d.ConsecutiveLosMisses);
        }

        [Fact]
        public void Engaging_SecondLosMiss_DropsToAdvance()
        {
            var d = Resolve(15f, false, EngagePhase.Engage, 1);
            Assert.Equal(EngagePhase.Advance, d.Phase);
            Assert.Equal(2, d.ConsecutiveLosMisses);
        }

        [Fact]
        public void Engaging_LosRegained_ResetsMissCounter()
        {
            var d = Resolve(15f, true, EngagePhase.Engage, 1);
            Assert.Equal(EngagePhase.Engage, d.Phase);
            Assert.Equal(0, d.ConsecutiveLosMisses);
        }
    }
}
