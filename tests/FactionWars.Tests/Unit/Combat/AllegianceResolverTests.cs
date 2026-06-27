using FactionWars.Combat.Models;
using FactionWars.Combat.Services;
using FactionWars.Core.Interfaces;
using Xunit;

namespace FactionWars.Tests.Unit.Combat
{
    public class AllegianceResolverTests
    {
        private readonly AllegianceResolver _resolver = new AllegianceResolver();

        [Fact]
        public void Resolve_SameFactionAsPlayer_IsFriendlyWithOwnColourAndFactionGroup()
        {
            var profile = _resolver.Resolve("michael", "michael");

            Assert.Equal(Allegiance.Friendly, profile.Allegiance);
            Assert.Equal("MICHAEL", profile.RelationshipGroup);
            Assert.Equal(BlipColor.MichaelBlue, profile.BlipColor);
        }

        [Fact]
        public void Resolve_DifferentFactionFromPlayer_IsHostileWithItsOwnColour()
        {
            var profile = _resolver.Resolve("franklin", "michael");

            Assert.Equal(Allegiance.Hostile, profile.Allegiance);
            Assert.Equal("FRANKLIN", profile.RelationshipGroup);
            Assert.Equal(BlipColor.FranklinGreen, profile.BlipColor);
        }

        [Fact]
        public void Resolve_IsCaseInsensitiveOnFactionMatch()
        {
            var profile = _resolver.Resolve("Michael", "michael");

            Assert.Equal(Allegiance.Friendly, profile.Allegiance);
        }
    }
}
