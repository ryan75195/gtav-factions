using FactionWars.Combat.Interfaces;
using FactionWars.Combat.Models;
using FactionWars.Combat.Services;
using FactionWars.Core.Models;
using Xunit;

namespace FactionWars.Tests.Unit.Combat
{
    public class SquadEngagementResolverTests
    {
        // Grunt range = 18; hysteresis band = 18 * 1.3 = 23.4; sustained-LOS-loss threshold = 1500ms;
        // LOS reposition stop range = 3.
        private readonly ISquadEngagementResolver _resolver =
            new SquadEngagementResolver(new EngageRangeProvider());

        private EngageDecision Resolve(float dist, bool los, EngagePhase phase, int msSinceLos)
            => _resolver.Resolve(dist, los, DefenderRole.Grunt, phase, msSinceLos);

        [Fact]
        public void OutOfRange_Advances()
        {
            var d = Resolve(40f, true, EngagePhase.Advance, 0);
            Assert.Equal(EngagePhase.Advance, d.Phase);
            Assert.Equal(18f, d.AdvanceStopRange); // LOS present -> close to engage range
        }

        [Fact]
        public void InRangeWithLos_Engages()
        {
            var d = Resolve(15f, true, EngagePhase.Advance, 0);
            Assert.Equal(EngagePhase.Engage, d.Phase);
        }

        [Fact]
        public void InRangeNoLos_AdvancesToReposition()
        {
            // In range but the sight line is blocked: push right up to the target for a new vantage.
            var d = Resolve(15f, false, EngagePhase.Advance, 0);
            Assert.Equal(EngagePhase.Advance, d.Phase);
            Assert.Equal(3f, d.AdvanceStopRange);
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
            Assert.Equal(18f, d.AdvanceStopRange); // out of range, not a LOS reposition
        }

        [Fact]
        public void Engaging_BriefLosLoss_StaysEngaged()
        {
            // LOS lost only briefly (500ms < 1500ms threshold): stay engaged so the phase
            // doesn't flicker on transient occlusion (peeking, smoke, a passing body).
            var d = Resolve(15f, false, EngagePhase.Engage, 500);
            Assert.Equal(EngagePhase.Engage, d.Phase);
        }

        [Fact]
        public void Engaging_SustainedLosLoss_RepositionsToEdge()
        {
            // LOS lost for a sustained period (2000ms >= 1500ms) while engaged: drop to advance and
            // push toward the target (to the building edge) to regain line of sight, then re-engage.
            var d = Resolve(15f, false, EngagePhase.Engage, 2000);
            Assert.Equal(EngagePhase.Advance, d.Phase);
            Assert.Equal(3f, d.AdvanceStopRange);
        }

        [Fact]
        public void Engaging_LosRegained_StaysEngaged()
        {
            var d = Resolve(15f, true, EngagePhase.Engage, 0);
            Assert.Equal(EngagePhase.Engage, d.Phase);
        }
    }
}
