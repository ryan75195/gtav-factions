using FactionWars.Core.Interfaces;
using FactionWars.ScriptHookV.Utils;
using Xunit;

namespace FactionWars.Tests.Unit.ScriptHookV.Utils
{
    public class FactionBlipColorTests
    {
        [Theory]
        [InlineData("michael", BlipColor.MichaelBlue)]
        [InlineData("Michael", BlipColor.MichaelBlue)]
        [InlineData("MICHAEL", BlipColor.MichaelBlue)]
        [InlineData("trevor", BlipColor.TrevorOrange)]
        [InlineData("franklin", BlipColor.FranklinGreen)]
        public void ForFactionId_KnownFaction_ReturnsCharacterColor(string factionId, BlipColor expected)
        {
            Assert.Equal(expected, FactionBlipColor.ForFactionId(factionId));
        }

        [Fact]
        public void ForFactionId_UnknownFaction_ReturnsRedFallback()
        {
            // Hostile-by-default for unknown factions matches pre-Plan-2 behaviour
            // where all enemy peds rendered red.
            Assert.Equal(BlipColor.Red, FactionBlipColor.ForFactionId("unknown"));
        }

        [Fact]
        public void ForFactionId_Null_ReturnsWhite()
        {
            Assert.Equal(BlipColor.White, FactionBlipColor.ForFactionId(null));
        }
    }
}
