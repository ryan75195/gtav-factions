using FactionWars.Combat.Interfaces;
using FactionWars.Combat.Services;
using FactionWars.Core.Models;
using Xunit;

namespace FactionWars.Tests.Unit.Combat
{
    public class EngageRangeProviderTests
    {
        private readonly IEngageRangeProvider _provider = new EngageRangeProvider();

        [Theory]
        [InlineData(DefenderRole.Sniper, 80f)]
        [InlineData(DefenderRole.Rocketeer, 45f)]
        [InlineData(DefenderRole.Rifleman, 45f)]
        [InlineData(DefenderRole.Gunner, 35f)]
        [InlineData(DefenderRole.Grunt, 18f)]
        public void For_KnownRole_ReturnsTableValue(DefenderRole role, float expected)
        {
            Assert.Equal(expected, _provider.For(role));
        }

        [Fact]
        public void For_UnmappedRole_ReturnsFallback()
        {
            // An out-of-range enum value (e.g. a future role not yet in the table) falls back to 30m.
            Assert.Equal(30f, _provider.For((DefenderRole)999));
        }
    }
}
