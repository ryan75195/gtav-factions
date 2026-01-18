using FactionWars.Tension.Models;
using Xunit;

namespace FactionWars.Tests.Unit.Tension
{
    /// <summary>
    /// Tests for the SabotageTargetType enum.
    /// </summary>
    public class SabotageTargetTypeTests
    {
        [Fact]
        public void SabotageTargetType_ResourceProduction_HasExpectedValue()
        {
            Assert.Equal(0, (int)SabotageTargetType.ResourceProduction);
        }

        [Fact]
        public void SabotageTargetType_DefenseRating_HasExpectedValue()
        {
            Assert.Equal(1, (int)SabotageTargetType.DefenseRating);
        }

        [Fact]
        public void SabotageTargetType_RecruitmentRate_HasExpectedValue()
        {
            Assert.Equal(2, (int)SabotageTargetType.RecruitmentRate);
        }

        [Fact]
        public void SabotageTargetType_SupplyLine_HasExpectedValue()
        {
            Assert.Equal(3, (int)SabotageTargetType.SupplyLine);
        }

        [Fact]
        public void SabotageTargetType_HasExactlyFourValues()
        {
            var values = System.Enum.GetValues(typeof(SabotageTargetType));
            Assert.Equal(4, values.Length);
        }
    }
}
