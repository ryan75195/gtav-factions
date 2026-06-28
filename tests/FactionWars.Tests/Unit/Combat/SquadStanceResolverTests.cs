using FactionWars.Combat.Models;
using FactionWars.Combat.Services;
using FactionWars.Core.Interfaces;
using Xunit;

namespace FactionWars.Tests.Unit.Combat
{
    public class SquadStanceResolverTests
    {
        private readonly SquadStanceResolver _resolver = new SquadStanceResolver();
        private readonly Vector3 _center = new Vector3(100f, 200f, 30f);

        [Fact]
        public void Escort_ResolvesToFollowPlayer()
        {
            var order = _resolver.Resolve(SquadStance.Escort, _center, 50f, 0, 3);
            Assert.Equal(BodyguardOrderKind.FollowPlayer, order.Kind);
        }

        [Fact]
        public void SearchAndDestroy_ResolvesToSeekInRadiusWithAnchor()
        {
            var order = _resolver.Resolve(SquadStance.SearchAndDestroy, _center, 50f, 0, 3);
            Assert.Equal(BodyguardOrderKind.SeekInRadius, order.Kind);
            Assert.Equal(_center, order.Point);
            Assert.Equal(50f, order.Radius);
        }

        [Fact]
        public void HoldArea_GivesDistinctPointsPerIndexWithinRadius()
        {
            var first = _resolver.Resolve(SquadStance.HoldArea, _center, 50f, 0, 3);
            var second = _resolver.Resolve(SquadStance.HoldArea, _center, 50f, 1, 3);

            Assert.Equal(BodyguardOrderKind.HoldAtPoint, first.Kind);
            Assert.NotEqual(first.Point, second.Point);
            Assert.True(_center.DistanceTo2D(first.Point) <= 50f);
            Assert.True(_center.DistanceTo2D(second.Point) <= 50f);
        }
    }
}
