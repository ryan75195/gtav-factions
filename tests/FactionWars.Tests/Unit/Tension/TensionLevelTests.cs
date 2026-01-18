using Xunit;
using FactionWars.Tension.Models;

namespace FactionWars.Tests.Unit.Tension
{
    /// <summary>
    /// Tests for the TensionLevel enum which defines the tension states between factions.
    /// </summary>
    public class TensionLevelTests
    {
        [Fact]
        public void TensionLevel_HasCalmValue()
        {
            var level = TensionLevel.Calm;

            Assert.Equal(0, (int)level);
        }

        [Fact]
        public void TensionLevel_HasUneasyValue()
        {
            var level = TensionLevel.Uneasy;

            Assert.Equal(1, (int)level);
        }

        [Fact]
        public void TensionLevel_HasTenseValue()
        {
            var level = TensionLevel.Tense;

            Assert.Equal(2, (int)level);
        }

        [Fact]
        public void TensionLevel_HasVolatileValue()
        {
            var level = TensionLevel.Volatile;

            Assert.Equal(3, (int)level);
        }

        [Fact]
        public void TensionLevel_HasCriticalValue()
        {
            var level = TensionLevel.Critical;

            Assert.Equal(4, (int)level);
        }

        [Fact]
        public void TensionLevel_HasCorrectOrdering()
        {
            Assert.True(TensionLevel.Calm < TensionLevel.Uneasy);
            Assert.True(TensionLevel.Uneasy < TensionLevel.Tense);
            Assert.True(TensionLevel.Tense < TensionLevel.Volatile);
            Assert.True(TensionLevel.Volatile < TensionLevel.Critical);
        }

        [Fact]
        public void TensionLevel_HasFiveValues()
        {
            var values = System.Enum.GetValues(typeof(TensionLevel));

            Assert.Equal(5, values.Length);
        }
    }
}
