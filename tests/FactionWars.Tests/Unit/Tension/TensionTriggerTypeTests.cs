using Xunit;
using FactionWars.Tension.Models;

namespace FactionWars.Tests.Unit.Tension
{
    /// <summary>
    /// Tests for the TensionTriggerType enum which defines the types of events
    /// that can cause tension to escalate between factions.
    /// </summary>
    public class TensionTriggerTypeTests
    {
        #region Enum Value Tests

        [Fact]
        public void BorderIncursion_HasCorrectValue()
        {
            Assert.Equal(0, (int)TensionTriggerType.BorderIncursion);
        }

        [Fact]
        public void ZoneAttack_HasCorrectValue()
        {
            Assert.Equal(1, (int)TensionTriggerType.ZoneAttack);
        }

        [Fact]
        public void ZoneCapture_HasCorrectValue()
        {
            Assert.Equal(2, (int)TensionTriggerType.ZoneCapture);
        }

        [Fact]
        public void MemberKilled_HasCorrectValue()
        {
            Assert.Equal(3, (int)TensionTriggerType.MemberKilled);
        }

        [Fact]
        public void LeaderKilled_HasCorrectValue()
        {
            Assert.Equal(4, (int)TensionTriggerType.LeaderKilled);
        }

        [Fact]
        public void ResourceRaided_HasCorrectValue()
        {
            Assert.Equal(5, (int)TensionTriggerType.ResourceRaided);
        }

        [Fact]
        public void Sabotage_HasCorrectValue()
        {
            Assert.Equal(6, (int)TensionTriggerType.Sabotage);
        }

        [Fact]
        public void TerritoryThreat_HasCorrectValue()
        {
            Assert.Equal(7, (int)TensionTriggerType.TerritoryThreat);
        }

        [Fact]
        public void RepeatedAggression_HasCorrectValue()
        {
            Assert.Equal(8, (int)TensionTriggerType.RepeatedAggression);
        }

        [Fact]
        public void AllyAttacked_HasCorrectValue()
        {
            Assert.Equal(9, (int)TensionTriggerType.AllyAttacked);
        }

        #endregion

        #region Enum Completeness

        [Fact]
        public void TensionTriggerType_HasExpectedNumberOfValues()
        {
            var values = System.Enum.GetValues(typeof(TensionTriggerType));
            Assert.Equal(10, values.Length);
        }

        #endregion
    }
}
