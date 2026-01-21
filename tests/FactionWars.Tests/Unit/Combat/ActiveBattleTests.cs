using System.Collections.Generic;
using FactionWars.Combat.Models;
using FactionWars.Core.Models;
using Xunit;

namespace FactionWars.Tests.Unit.Combat
{
    public class ActiveBattleTests
    {
        [Fact]
        public void AddDefenderTroops_ShouldIncreaseTroopCount()
        {
            // Arrange
            var attackerTroops = new Dictionary<DefenderTier, int> { { DefenderTier.Basic, 5 } };
            var defenderTroops = new Dictionary<DefenderTier, int> { { DefenderTier.Basic, 3 } };
            var battle = new ActiveBattle("attacker", "defender", "zone1", attackerTroops, defenderTroops, 60f, 6f);

            // Act
            battle.AddDefenderTroops(DefenderTier.Basic, 2);

            // Assert
            Assert.Equal(5, battle.TotalDefenderTroops);
            Assert.Equal(5, battle.DefenderTroops[DefenderTier.Basic]);
        }

        [Fact]
        public void AddDefenderTroops_NewTier_ShouldAddTier()
        {
            // Arrange
            var attackerTroops = new Dictionary<DefenderTier, int> { { DefenderTier.Basic, 5 } };
            var defenderTroops = new Dictionary<DefenderTier, int> { { DefenderTier.Basic, 3 } };
            var battle = new ActiveBattle("attacker", "defender", "zone1", attackerTroops, defenderTroops, 60f, 6f);

            // Act
            battle.AddDefenderTroops(DefenderTier.Medium, 2);

            // Assert
            Assert.Equal(5, battle.TotalDefenderTroops);
            Assert.Equal(2, battle.DefenderTroops[DefenderTier.Medium]);
        }
    }
}
