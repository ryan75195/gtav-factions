using FactionWars.Tension.Models;
using Xunit;

namespace FactionWars.Tests.Unit.Tension
{
    /// <summary>
    /// Tests for the AssassinationTargetType enum.
    /// </summary>
    public class AssassinationTargetTypeTests
    {
        [Fact]
        public void AssassinationTargetType_Lieutenant_HasExpectedValue()
        {
            Assert.Equal(0, (int)AssassinationTargetType.Lieutenant);
        }

        [Fact]
        public void AssassinationTargetType_HighValueMember_HasExpectedValue()
        {
            Assert.Equal(1, (int)AssassinationTargetType.HighValueMember);
        }

        [Fact]
        public void AssassinationTargetType_Enforcer_HasExpectedValue()
        {
            Assert.Equal(2, (int)AssassinationTargetType.Enforcer);
        }

        [Fact]
        public void AssassinationTargetType_HasExactlyThreeValues()
        {
            var values = System.Enum.GetValues(typeof(AssassinationTargetType));
            Assert.Equal(3, values.Length);
        }
    }
}
