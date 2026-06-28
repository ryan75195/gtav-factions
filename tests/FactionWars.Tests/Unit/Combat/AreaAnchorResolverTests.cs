using FactionWars.Combat.Models;
using FactionWars.Combat.Services;
using FactionWars.Core.Interfaces;
using Xunit;

namespace FactionWars.Tests.Unit.Combat
{
    public class AreaAnchorResolverTests
    {
        private readonly AreaAnchorResolver _resolver = new AreaAnchorResolver();

        [Fact]
        public void InZone_UsesZoneCentreAndRadius()
        {
            var zoneCenter = new Vector3(10f, 20f, 30f);
            var anchor = _resolver.Resolve(zoneCenter, 150f, new Vector3(0f, 0f, 0f), 30f);
            Assert.Equal(zoneCenter, anchor.Center);
            Assert.Equal(150f, anchor.Radius);
        }

        [Fact]
        public void OutOfZone_UsesPlayerPositionAndDefaultRadius()
        {
            var playerPos = new Vector3(5f, 5f, 5f);
            var anchor = _resolver.Resolve(null, 0f, playerPos, 30f);
            Assert.Equal(playerPos, anchor.Center);
            Assert.Equal(30f, anchor.Radius);
        }
    }
}
