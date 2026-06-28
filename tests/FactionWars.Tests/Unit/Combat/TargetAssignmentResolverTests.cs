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
        private static readonly IReadOnlyDictionary<int, int> NoPrevious = new Dictionary<int, int>();

        private static BodyguardPosition Bg(int h, float x) => new BodyguardPosition(h, new Vector3(x, 0f, 0f));
        private static EnemyTarget En(int h, float x) => new EnemyTarget(h, new Vector3(x, 0f, 0f));

        [Fact]
        public void TwoBodyguardsTwoEnemies_SpreadAcrossDistinctTargets()
        {
            var bodyguards = new List<BodyguardPosition> { Bg(1, 0f), Bg(2, 100f) };
            var enemies = new List<EnemyTarget> { En(10, 5f), En(20, 95f) };

            var map = _resolver.Assign(bodyguards, enemies, NoPrevious);

            Assert.Equal(10, map[1]); // nearest to bodyguard 1
            Assert.Equal(20, map[2]); // distinct, nearest to bodyguard 2
            Assert.Equal(2, map.Values.Distinct().Count());
        }

        [Fact]
        public void MoreBodyguardsThanEnemies_ExtrasDoubleUpAndAllAssigned()
        {
            var bodyguards = new List<BodyguardPosition> { Bg(1, 0f), Bg(2, 10f), Bg(3, 200f) };
            var enemies = new List<EnemyTarget> { En(10, 0f), En(20, 210f) };

            var map = _resolver.Assign(bodyguards, enemies, NoPrevious);

            Assert.Equal(3, map.Count);
            Assert.All(map.Values, v => Assert.Contains(v, new[] { 10, 20 }));
            Assert.Equal(20, map[3]); // bodyguard 3 nearest to enemy 20
        }

        [Fact]
        public void NoEnemies_ReturnsEmptyMap()
        {
            var map = _resolver.Assign(new List<BodyguardPosition> { Bg(1, 0f) }, new List<EnemyTarget>(), NoPrevious);
            Assert.Empty(map);
        }

        [Fact]
        public void NoBodyguards_ReturnsEmptyMap()
        {
            var map = _resolver.Assign(new List<BodyguardPosition>(), new List<EnemyTarget> { En(10, 0f) }, NoPrevious);
            Assert.Empty(map);
        }

        [Fact]
        public void KeepsPreviousTarget_WhenStillValid()
        {
            // Bodyguard sits at 0; its old target (10) is now FAR while a new enemy (20) is closer.
            // Sticky assignment must keep the committed target rather than chase the nearer one.
            var bodyguards = new List<BodyguardPosition> { Bg(1, 0f) };
            var enemies = new List<EnemyTarget> { En(10, 100f), En(20, 5f) };
            var previous = new Dictionary<int, int> { { 1, 10 } };

            var map = _resolver.Assign(bodyguards, enemies, previous);

            Assert.Equal(10, map[1]);
        }

        [Fact]
        public void ReassignsBodyguard_WhenPreviousTargetGone()
        {
            // Previous target 99 is no longer among the live enemies, so the bodyguard must retarget.
            var bodyguards = new List<BodyguardPosition> { Bg(1, 0f) };
            var enemies = new List<EnemyTarget> { En(10, 0f) };
            var previous = new Dictionary<int, int> { { 1, 99 } };

            var map = _resolver.Assign(bodyguards, enemies, previous);

            Assert.Equal(10, map[1]);
        }

        [Fact]
        public void NewAssignments_SpreadToFarEnemies()
        {
            // Two bodyguards clustered near the origin; one near enemy and one far across the zone.
            // After the first takes the near enemy, the second should fan out to the far one
            // (dispersion) rather than pile onto the nearby enemy (20).
            var bodyguards = new List<BodyguardPosition> { Bg(1, 0f), Bg(2, 1f) };
            var enemies = new List<EnemyTarget> { En(10, 2f), En(20, 3f), En(30, 100f) };

            var map = _resolver.Assign(bodyguards, enemies, NoPrevious);

            Assert.Equal(10, map[1]); // first seeds on the nearest enemy
            Assert.Equal(30, map[2]); // second disperses to the far enemy
        }
    }
}
