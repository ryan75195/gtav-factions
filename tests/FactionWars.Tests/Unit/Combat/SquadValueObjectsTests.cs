using FactionWars.Combat.Models;
using FactionWars.Core.Interfaces;
using Xunit;

namespace FactionWars.Tests.Unit.Combat
{
    public class SquadValueObjectsTests
    {
        [Fact]
        public void BodyguardPosition_CarriesHandleAndPosition()
        {
            var bp = new BodyguardPosition(7, new Vector3(1f, 2f, 3f));
            Assert.Equal(7, bp.Handle);
            Assert.Equal(new Vector3(1f, 2f, 3f), bp.Position);
        }

        [Fact]
        public void EnemyTarget_CarriesHandleAndPosition()
        {
            var et = new EnemyTarget(8, new Vector3(4f, 5f, 6f));
            Assert.Equal(8, et.Handle);
            Assert.Equal(new Vector3(4f, 5f, 6f), et.Position);
        }

        [Fact]
        public void AreaAnchor_CarriesCentreAndRadius()
        {
            var a = new AreaAnchor(new Vector3(9f, 9f, 9f), 25f);
            Assert.Equal(new Vector3(9f, 9f, 9f), a.Center);
            Assert.Equal(25f, a.Radius);
        }
    }
}
