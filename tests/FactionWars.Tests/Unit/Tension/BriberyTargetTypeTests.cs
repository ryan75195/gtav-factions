using FactionWars.Tension.Models;
using Xunit;

namespace FactionWars.Tests.Unit.Tension
{
    /// <summary>
    /// Tests for the BriberyTargetType enum.
    /// </summary>
    public class BriberyTargetTypeTests
    {
        [Fact]
        public void BriberyTargetType_IntelligenceAsset_HasExpectedValue()
        {
            Assert.Equal(0, (int)BriberyTargetType.IntelligenceAsset);
        }

        [Fact]
        public void BriberyTargetType_ResourceDiversion_HasExpectedValue()
        {
            Assert.Equal(1, (int)BriberyTargetType.ResourceDiversion);
        }

        [Fact]
        public void BriberyTargetType_DefectorRecruitment_HasExpectedValue()
        {
            Assert.Equal(2, (int)BriberyTargetType.DefectorRecruitment);
        }

        [Fact]
        public void BriberyTargetType_TensionReduction_HasExpectedValue()
        {
            Assert.Equal(3, (int)BriberyTargetType.TensionReduction);
        }

        [Fact]
        public void BriberyTargetType_HasExactlyFourValues()
        {
            var values = System.Enum.GetValues(typeof(BriberyTargetType));
            Assert.Equal(4, values.Length);
        }
    }
}
