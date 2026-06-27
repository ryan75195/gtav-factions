using System.Collections.Generic;
using System.Linq;
using FactionWars.Combat.Models;
using FactionWars.Combat.Services;
using FactionWars.Core.Interfaces;
using Xunit;

namespace FactionWars.Tests.Unit.Combat
{
    public class TargetAssignmentResolverTests
    {
        private readonly TargetAssignmentResolver _resolver = new TargetAssignmentResolver();

        private static BodyguardPosition Bg(int h, float x) => new BodyguardPosition(h, new Vector3(x, 0f, 0f));
        private static EnemyTarget En(int h, float x) => new EnemyTarget(h, new Vector3(x, 0f, 0f));

        [Fact]
        public void TwoBodyguardsTwoEnemies_SpreadAcrossDistinctTargets()
        {
            var bodyguards = new List<BodyguardPosition> { Bg(1, 0f), Bg(2, 100f) };
            var enemies = new List<EnemyTarget> { En(10, 5f), En(20, 95f) };

            var map = _resolver.Assign(bodyguards, enemies);

            Assert.Equal(10, map[1]); // nearest to bodyguard 1
            Assert.Equal(20, map[2]); // nearest to bodyguard 2
            Assert.Equal(2, map.Values.Distinct().Count());
        }

        [Fact]
        public void MoreBodyguardsThanEnemies_ExtrasDoubleUpAndAllAssigned()
        {
            var bodyguards = new List<BodyguardPosition> { Bg(1, 0f), Bg(2, 10f), Bg(3, 200f) };
            var enemies = new List<EnemyTarget> { En(10, 0f), En(20, 210f) };

            var map = _resolver.Assign(bodyguards, enemies);

            Assert.Equal(3, map.Count);
            Assert.All(map.Values, v => Assert.Contains(v, new[] { 10, 20 }));
            Assert.Equal(20, map[3]); // bodyguard 3 nearest to enemy 20
        }

        [Fact]
        public void NoEnemies_ReturnsEmptyMap()
        {
            var map = _resolver.Assign(new List<BodyguardPosition> { Bg(1, 0f) }, new List<EnemyTarget>());
            Assert.Empty(map);
        }

        [Fact]
        public void NoBodyguards_ReturnsEmptyMap()
        {
            var map = _resolver.Assign(new List<BodyguardPosition>(), new List<EnemyTarget> { En(10, 0f) });
            Assert.Empty(map);
        }
    }
}
