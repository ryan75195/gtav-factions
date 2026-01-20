using System.Collections.Generic;
using System.Linq;
using FactionWars.AI.Models;
using FactionWars.AI.Strategies;
using FactionWars.Factions.Models;
using FactionWars.Territory.Models;
using FactionWars.Core.Interfaces;
using Xunit;

namespace FactionWars.Tests.Unit.AI
{
    public class ProbabilityTargetSelectionTests
    {
        [Fact]
        public void CalculateTargetScore_UnguardedZone_HasHigherScore()
        {
            // Arrange - zone with 0 defenders should score higher than zone with 5
            var unguardedZone = new Zone("zone1", "Unguarded", new Vector3(0, 0, 0), 100f, strategicValue: 5);
            unguardedZone.OwnerFactionId = "enemy";

            var guardedZone = new Zone("zone2", "Guarded", new Vector3(0, 0, 0), 100f, strategicValue: 5);
            guardedZone.OwnerFactionId = "enemy";

            var defenderCounts = new Dictionary<string, int>
            {
                { "zone1", 0 },  // Unguarded
                { "zone2", 5 }   // Guarded
            };

            // Act
            var unguardedScore = TestableStrategy.CalculateTargetScorePublic(unguardedZone, defenderCounts);
            var guardedScore = TestableStrategy.CalculateTargetScorePublic(guardedZone, defenderCounts);

            // Assert - unguarded should have 3x multiplier
            Assert.True(unguardedScore > guardedScore);
        }

        [Fact]
        public void CalculateTargetScore_NeutralZone_HasBonus()
        {
            var neutralZone = new Zone("zone1", "Neutral", new Vector3(0, 0, 0), 100f, strategicValue: 5);
            neutralZone.OwnerFactionId = null;  // Neutral

            var enemyZone = new Zone("zone2", "Enemy", new Vector3(0, 0, 0), 100f, strategicValue: 5);
            enemyZone.OwnerFactionId = "enemy";

            var defenderCounts = new Dictionary<string, int>
            {
                { "zone1", 0 },
                { "zone2", 0 }
            };

            var neutralScore = TestableStrategy.CalculateTargetScorePublic(neutralZone, defenderCounts);
            var enemyScore = TestableStrategy.CalculateTargetScorePublic(enemyZone, defenderCounts);

            // Neutral should have 1.5x bonus
            Assert.True(neutralScore > enemyScore);
        }

        // Test helper to expose protected method
        private class TestableStrategy : BaseAIStrategy
        {
            public TestableStrategy() : base(FactionType.Michael, 0.5f, 0.5f) { }

            public static float CalculateTargetScorePublic(Zone zone, IDictionary<string, int> defenderCounts)
            {
                return CalculateTargetScore(zone, defenderCounts);
            }
        }
    }
}
