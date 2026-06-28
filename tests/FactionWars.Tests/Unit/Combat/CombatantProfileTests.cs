using FactionWars.Combat.Models;
using FactionWars.Core.Interfaces;
using Xunit;

namespace FactionWars.Tests.Unit.Combat
{
    public class CombatantProfileTests
    {
        [Fact]
        public void Constructor_ExposesAllProperties()
        {
            var profile = new CombatantProfile("MICHAEL", BlipColor.MichaelBlue, Allegiance.Friendly);

            Assert.Equal("MICHAEL", profile.RelationshipGroup);
            Assert.Equal(BlipColor.MichaelBlue, profile.BlipColor);
            Assert.Equal(Allegiance.Friendly, profile.Allegiance);
        }
    }
}
