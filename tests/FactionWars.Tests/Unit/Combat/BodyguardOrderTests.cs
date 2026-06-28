using FactionWars.Combat.Models;
using FactionWars.Core.Interfaces;
using Xunit;

namespace FactionWars.Tests.Unit.Combat
{
    public class BodyguardOrderTests
    {
        [Fact]
        public void FollowPlayer_HasFollowKind()
        {
            var order = BodyguardOrder.FollowPlayer();
            Assert.Equal(BodyguardOrderKind.FollowPlayer, order.Kind);
        }

        [Fact]
        public void HoldAtPoint_CarriesPoint()
        {
            var p = new Vector3(1f, 2f, 3f);
            var order = BodyguardOrder.HoldAtPoint(p);
            Assert.Equal(BodyguardOrderKind.HoldAtPoint, order.Kind);
            Assert.Equal(p, order.Point);
        }

        [Fact]
        public void SeekInRadius_CarriesCentreAndRadius()
        {
            var c = new Vector3(5f, 6f, 7f);
            var order = BodyguardOrder.SeekInRadius(c, 40f);
            Assert.Equal(BodyguardOrderKind.SeekInRadius, order.Kind);
            Assert.Equal(c, order.Point);
            Assert.Equal(40f, order.Radius);
        }

        [Fact]
        public void AttackTarget_CarriesTargetHandle()
        {
            var order = BodyguardOrder.AttackTarget(99);
            Assert.Equal(BodyguardOrderKind.AttackTarget, order.Kind);
            Assert.Equal(99, order.TargetHandle);
        }
    }
}
