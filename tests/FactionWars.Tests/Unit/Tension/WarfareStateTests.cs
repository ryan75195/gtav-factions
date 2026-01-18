using FactionWars.Tension.Models;
using Xunit;

namespace FactionWars.Tests.Unit.Tension
{
    /// <summary>
    /// Tests for the WarfareState enum which represents the state of conflict between factions.
    /// </summary>
    public class WarfareStateTests
    {
        [Fact]
        public void WarfareState_HasPeaceState()
        {
            // Arrange & Act
            var state = WarfareState.Peace;

            // Assert
            Assert.Equal(0, (int)state);
        }

        [Fact]
        public void WarfareState_HasColdWarState()
        {
            // Arrange & Act
            var state = WarfareState.ColdWar;

            // Assert
            Assert.Equal(1, (int)state);
        }

        [Fact]
        public void WarfareState_HasBorderSkirmishesState()
        {
            // Arrange & Act
            var state = WarfareState.BorderSkirmishes;

            // Assert
            Assert.Equal(2, (int)state);
        }

        [Fact]
        public void WarfareState_HasOpenWarfareState()
        {
            // Arrange & Act
            var state = WarfareState.OpenWarfare;

            // Assert
            Assert.Equal(3, (int)state);
        }

        [Fact]
        public void WarfareState_HasTotalWarState()
        {
            // Arrange & Act
            var state = WarfareState.TotalWar;

            // Assert
            Assert.Equal(4, (int)state);
        }

        [Fact]
        public void WarfareState_HasExactlyFiveStates()
        {
            // Arrange & Act
            var values = System.Enum.GetValues(typeof(WarfareState));

            // Assert
            Assert.Equal(5, values.Length);
        }

        [Theory]
        [InlineData(WarfareState.Peace, "Peace")]
        [InlineData(WarfareState.ColdWar, "ColdWar")]
        [InlineData(WarfareState.BorderSkirmishes, "BorderSkirmishes")]
        [InlineData(WarfareState.OpenWarfare, "OpenWarfare")]
        [InlineData(WarfareState.TotalWar, "TotalWar")]
        public void WarfareState_HasCorrectNames(WarfareState state, string expectedName)
        {
            // Assert
            Assert.Equal(expectedName, state.ToString());
        }

        [Theory]
        [InlineData(WarfareState.Peace, false)]
        [InlineData(WarfareState.ColdWar, false)]
        [InlineData(WarfareState.BorderSkirmishes, true)]
        [InlineData(WarfareState.OpenWarfare, true)]
        [InlineData(WarfareState.TotalWar, true)]
        public void WarfareState_StatesAboveColdWarAreActiveCombat(WarfareState state, bool isActiveCombat)
        {
            // Active combat states are BorderSkirmishes and above
            bool result = (int)state >= (int)WarfareState.BorderSkirmishes;
            Assert.Equal(isActiveCombat, result);
        }

        [Theory]
        [InlineData(WarfareState.Peace, false)]
        [InlineData(WarfareState.ColdWar, true)]
        [InlineData(WarfareState.BorderSkirmishes, true)]
        [InlineData(WarfareState.OpenWarfare, true)]
        [InlineData(WarfareState.TotalWar, true)]
        public void WarfareState_StatesAbovePeaceAreHostile(WarfareState state, bool isHostile)
        {
            // Hostile states are ColdWar and above
            bool result = (int)state >= (int)WarfareState.ColdWar;
            Assert.Equal(isHostile, result);
        }
    }
}
