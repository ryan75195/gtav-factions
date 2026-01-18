using Xunit;
using FactionWars.Escalation.Models;

namespace FactionWars.Tests.Unit.Escalation
{
    /// <summary>
    /// Tests for the EscalationTier enum which defines the escalation levels for factions.
    /// Escalation tiers determine what weapons and vehicles are available to a faction.
    /// </summary>
    public class EscalationTierTests
    {
        [Fact]
        public void EscalationTier_HasTier1Value()
        {
            var tier = EscalationTier.Tier1;

            Assert.Equal(0, (int)tier);
        }

        [Fact]
        public void EscalationTier_HasTier2Value()
        {
            var tier = EscalationTier.Tier2;

            Assert.Equal(1, (int)tier);
        }

        [Fact]
        public void EscalationTier_HasTier3Value()
        {
            var tier = EscalationTier.Tier3;

            Assert.Equal(2, (int)tier);
        }

        [Fact]
        public void EscalationTier_HasTier4Value()
        {
            var tier = EscalationTier.Tier4;

            Assert.Equal(3, (int)tier);
        }

        [Fact]
        public void EscalationTier_HasTier5Value()
        {
            var tier = EscalationTier.Tier5;

            Assert.Equal(4, (int)tier);
        }

        [Fact]
        public void EscalationTier_HasCorrectOrdering()
        {
            Assert.True(EscalationTier.Tier1 < EscalationTier.Tier2);
            Assert.True(EscalationTier.Tier2 < EscalationTier.Tier3);
            Assert.True(EscalationTier.Tier3 < EscalationTier.Tier4);
            Assert.True(EscalationTier.Tier4 < EscalationTier.Tier5);
        }

        [Fact]
        public void EscalationTier_HasFiveValues()
        {
            var values = System.Enum.GetValues(typeof(EscalationTier));

            Assert.Equal(5, values.Length);
        }
    }
}
