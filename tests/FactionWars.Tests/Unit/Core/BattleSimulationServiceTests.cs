using System;
using FactionWars.Core.Interfaces;
using FactionWars.Core.Models;
using FactionWars.Core.Services;
using Xunit;

namespace FactionWars.Tests.Unit.Core
{
    /// <summary>
    /// Tests for IBattleSimulationService.
    /// The battle simulation service handles AI vs AI combat when the player isn't present.
    /// It calculates combat outcomes based on attacker/defender troop counts and tiers.
    /// </summary>
    public class BattleSimulationServiceTests
    {
        #region Interface Tests

        [Fact]
        public void IBattleSimulationService_SimulateBattle_IsDefinedWithCorrectSignature()
        {
            // This test verifies the interface method exists with correct signature
            // The interface should have a SimulateBattle method that takes:
            // - attackerFactionId: string
            // - defenderFactionId: string
            // - zoneId: string
            // - attackerTroops: TroopComposition
            // - defenderTroops: TroopComposition
            // And returns: BattleSimulationResult
            var interfaceType = typeof(IBattleSimulationService);
            var method = interfaceType.GetMethod("SimulateBattle");

            Assert.NotNull(method);
            Assert.Equal(typeof(BattleSimulationResult), method!.ReturnType);

            var parameters = method.GetParameters();
            Assert.Equal(5, parameters.Length);
            Assert.Equal("attackerFactionId", parameters[0].Name);
            Assert.Equal(typeof(string), parameters[0].ParameterType);
            Assert.Equal("defenderFactionId", parameters[1].Name);
            Assert.Equal(typeof(string), parameters[1].ParameterType);
            Assert.Equal("zoneId", parameters[2].Name);
            Assert.Equal(typeof(string), parameters[2].ParameterType);
            Assert.Equal("attackerTroops", parameters[3].Name);
            Assert.Equal(typeof(TroopComposition), parameters[3].ParameterType);
            Assert.Equal("defenderTroops", parameters[4].Name);
            Assert.Equal(typeof(TroopComposition), parameters[4].ParameterType);
        }

        [Fact]
        public void IBattleSimulationService_CalculateWinProbability_IsDefinedWithCorrectSignature()
        {
            // This test verifies the interface method exists with correct signature
            // The interface should have a CalculateWinProbability method that takes:
            // - attackerTroops: TroopComposition
            // - defenderTroops: TroopComposition
            // And returns: float (probability between 0 and 1)
            var interfaceType = typeof(IBattleSimulationService);
            var method = interfaceType.GetMethod("CalculateWinProbability");

            Assert.NotNull(method);
            Assert.Equal(typeof(float), method!.ReturnType);

            var parameters = method.GetParameters();
            Assert.Equal(2, parameters.Length);
            Assert.Equal("attackerTroops", parameters[0].Name);
            Assert.Equal(typeof(TroopComposition), parameters[0].ParameterType);
            Assert.Equal("defenderTroops", parameters[1].Name);
            Assert.Equal(typeof(TroopComposition), parameters[1].ParameterType);
        }

        [Fact]
        public void IBattleSimulationService_CalculateCasualties_IsDefinedWithCorrectSignature()
        {
            // This test verifies the interface method exists with correct signature
            // The interface should have a CalculateCasualties method that takes:
            // - troops: TroopComposition
            // - opposingStrength: float
            // And returns: TroopComposition (casualties suffered)
            var interfaceType = typeof(IBattleSimulationService);
            var method = interfaceType.GetMethod("CalculateCasualties");

            Assert.NotNull(method);
            Assert.Equal(typeof(TroopComposition), method!.ReturnType);

            var parameters = method.GetParameters();
            Assert.Equal(2, parameters.Length);
            Assert.Equal("troops", parameters[0].Name);
            Assert.Equal(typeof(TroopComposition), parameters[0].ParameterType);
            Assert.Equal("opposingStrength", parameters[1].Name);
            Assert.Equal(typeof(float), parameters[1].ParameterType);
        }

        #endregion

        #region BattleSimulationResult Model Tests

        [Fact]
        public void BattleSimulationResult_HasRequiredProperties()
        {
            // Verify BattleSimulationResult has all required properties
            var resultType = typeof(BattleSimulationResult);

            Assert.NotNull(resultType.GetProperty("AttackerFactionId"));
            Assert.NotNull(resultType.GetProperty("DefenderFactionId"));
            Assert.NotNull(resultType.GetProperty("ZoneId"));
            Assert.NotNull(resultType.GetProperty("AttackerWon"));
            Assert.NotNull(resultType.GetProperty("AttackerCasualties"));
            Assert.NotNull(resultType.GetProperty("DefenderCasualties"));
            Assert.NotNull(resultType.GetProperty("NewOwnerFactionId"));
        }

        [Fact]
        public void BattleSimulationResult_AttackerVictory_SetsCorrectProperties()
        {
            // Arrange
            var attackerCasualties = new TroopComposition(2, 1, 0);
            var defenderCasualties = new TroopComposition(5, 3, 1);

            // Act
            var result = BattleSimulationResult.AttackerVictory(
                attackerFactionId: "faction-michael",
                defenderFactionId: "faction-trevor",
                zoneId: "zone-downtown",
                attackerCasualties: attackerCasualties,
                defenderCasualties: defenderCasualties);

            // Assert
            Assert.Equal("faction-michael", result.AttackerFactionId);
            Assert.Equal("faction-trevor", result.DefenderFactionId);
            Assert.Equal("zone-downtown", result.ZoneId);
            Assert.True(result.AttackerWon);
            Assert.Equal("faction-michael", result.NewOwnerFactionId);
            Assert.Equal(attackerCasualties, result.AttackerCasualties);
            Assert.Equal(defenderCasualties, result.DefenderCasualties);
        }

        [Fact]
        public void BattleSimulationResult_DefenderVictory_SetsCorrectProperties()
        {
            // Arrange
            var attackerCasualties = new TroopComposition(8, 4, 2);
            var defenderCasualties = new TroopComposition(2, 1, 0);

            // Act
            var result = BattleSimulationResult.DefenderVictory(
                attackerFactionId: "faction-michael",
                defenderFactionId: "faction-trevor",
                zoneId: "zone-downtown",
                attackerCasualties: attackerCasualties,
                defenderCasualties: defenderCasualties);

            // Assert
            Assert.Equal("faction-michael", result.AttackerFactionId);
            Assert.Equal("faction-trevor", result.DefenderFactionId);
            Assert.Equal("zone-downtown", result.ZoneId);
            Assert.False(result.AttackerWon);
            Assert.Equal("faction-trevor", result.NewOwnerFactionId);
            Assert.Equal(attackerCasualties, result.AttackerCasualties);
            Assert.Equal(defenderCasualties, result.DefenderCasualties);
        }

        #endregion

        #region TroopComposition Model Tests

        [Fact]
        public void TroopComposition_Constructor_SetsCorrectValues()
        {
            // Act
            var composition = new TroopComposition(basic: 10, medium: 5, heavy: 2);

            // Assert
            Assert.Equal(10, composition.Basic);
            Assert.Equal(5, composition.Medium);
            Assert.Equal(2, composition.Heavy);
        }

        [Fact]
        public void TroopComposition_TotalCount_ReturnsSum()
        {
            // Arrange
            var composition = new TroopComposition(basic: 10, medium: 5, heavy: 2);

            // Act
            var total = composition.TotalCount;

            // Assert
            Assert.Equal(17, total);
        }

        [Fact]
        public void TroopComposition_TotalStrength_CalculatesWithModifiers()
        {
            // Arrange: Basic=1.0, Medium=1.5, Heavy=2.0
            var composition = new TroopComposition(basic: 10, medium: 4, heavy: 3);

            // Act
            var strength = composition.TotalStrength;

            // Assert: 10*1.0 + 4*1.5 + 3*2.0 = 10 + 6 + 6 = 22
            Assert.Equal(22f, strength, 2);
        }

        [Fact]
        public void TroopComposition_Empty_HasZeroCountAndStrength()
        {
            // Act
            var composition = TroopComposition.Empty;

            // Assert
            Assert.Equal(0, composition.Basic);
            Assert.Equal(0, composition.Medium);
            Assert.Equal(0, composition.Heavy);
            Assert.Equal(0, composition.TotalCount);
            Assert.Equal(0f, composition.TotalStrength, 2);
        }

        [Fact]
        public void TroopComposition_IsEmpty_ReturnsTrueWhenAllZero()
        {
            // Arrange
            var empty = new TroopComposition(0, 0, 0);
            var notEmpty = new TroopComposition(1, 0, 0);

            // Assert
            Assert.True(empty.IsEmpty);
            Assert.False(notEmpty.IsEmpty);
        }

        [Fact]
        public void TroopComposition_Constructor_ThrowsOnNegativeValues()
        {
            // Act & Assert
            Assert.Throws<ArgumentOutOfRangeException>(() => new TroopComposition(-1, 0, 0));
            Assert.Throws<ArgumentOutOfRangeException>(() => new TroopComposition(0, -1, 0));
            Assert.Throws<ArgumentOutOfRangeException>(() => new TroopComposition(0, 0, -1));
        }

        [Fact]
        public void TroopComposition_Subtract_ReturnsCorrectDifference()
        {
            // Arrange
            var original = new TroopComposition(10, 5, 3);
            var casualties = new TroopComposition(3, 2, 1);

            // Act
            var remaining = original.Subtract(casualties);

            // Assert
            Assert.Equal(7, remaining.Basic);
            Assert.Equal(3, remaining.Medium);
            Assert.Equal(2, remaining.Heavy);
        }

        [Fact]
        public void TroopComposition_Subtract_ClampsToZero()
        {
            // Arrange
            var original = new TroopComposition(5, 2, 1);
            var casualties = new TroopComposition(10, 5, 3);

            // Act
            var remaining = original.Subtract(casualties);

            // Assert
            Assert.Equal(0, remaining.Basic);
            Assert.Equal(0, remaining.Medium);
            Assert.Equal(0, remaining.Heavy);
        }

        [Fact]
        public void TroopComposition_Add_ReturnsCorrectSum()
        {
            // Arrange
            var first = new TroopComposition(10, 5, 3);
            var second = new TroopComposition(5, 3, 2);

            // Act
            var total = first.Add(second);

            // Assert
            Assert.Equal(15, total.Basic);
            Assert.Equal(8, total.Medium);
            Assert.Equal(5, total.Heavy);
        }

        #endregion

        #region BattleSimulationService Implementation Tests

        // Note: These tests require BattleSimulationService to be implemented
        // Following TDD, we write these tests first, then implement the service

        [Fact]
        public void SimulateBattle_AttackerHasOverwhelmingStrength_AttackerWins()
        {
            // Arrange: attacker has 100 heavy (strength=200), defender has 10 basic (strength=10)
            var service = CreateService();
            var attackerTroops = new TroopComposition(0, 0, 100);
            var defenderTroops = new TroopComposition(10, 0, 0);

            // Act
            var result = service.SimulateBattle(
                "faction_blue",
                "faction_orange",
                "zone_downtown",
                attackerTroops,
                defenderTroops);

            // Assert
            Assert.True(result.AttackerWon);
            Assert.Equal("faction_blue", result.AttackerFactionId);
            Assert.Equal("faction_orange", result.DefenderFactionId);
            Assert.Equal("zone_downtown", result.ZoneId);
            Assert.Equal("faction_blue", result.NewOwnerFactionId);
        }

        [Fact]
        public void SimulateBattle_DefenderHasOverwhelmingStrength_DefenderWins()
        {
            // Arrange: attacker has 10 basic (strength=10), defender has 100 heavy (strength=200)
            var service = CreateService();
            var attackerTroops = new TroopComposition(10, 0, 0);
            var defenderTroops = new TroopComposition(0, 0, 100);

            // Act
            var result = service.SimulateBattle(
                "faction_blue",
                "faction_orange",
                "zone_vinewood",
                attackerTroops,
                defenderTroops);

            // Assert
            Assert.False(result.AttackerWon);
            Assert.Equal("faction_orange", result.NewOwnerFactionId);
        }

        [Fact]
        public void SimulateBattle_ReturnsCasualties_ForBothSides()
        {
            // Arrange
            var service = CreateService();
            var attackerTroops = new TroopComposition(20, 10, 5);
            var defenderTroops = new TroopComposition(15, 8, 3);

            // Act
            var result = service.SimulateBattle(
                "faction_blue",
                "faction_orange",
                "zone_airport",
                attackerTroops,
                defenderTroops);

            // Assert - both sides should have casualties
            Assert.NotNull(result.AttackerCasualties);
            Assert.NotNull(result.DefenderCasualties);
            Assert.True(result.AttackerCasualties.TotalCount > 0 || result.DefenderCasualties.TotalCount > 0);
        }

        [Fact]
        public void SimulateBattle_AttackerWithNoTroops_DefenderWins()
        {
            // Arrange
            var service = CreateService();
            var attackerTroops = TroopComposition.Empty;
            var defenderTroops = new TroopComposition(10, 5, 2);

            // Act
            var result = service.SimulateBattle(
                "faction_blue",
                "faction_orange",
                "zone_port",
                attackerTroops,
                defenderTroops);

            // Assert
            Assert.False(result.AttackerWon);
            Assert.Equal("faction_orange", result.NewOwnerFactionId);
        }

        [Fact]
        public void SimulateBattle_DefenderWithNoTroops_AttackerWins()
        {
            // Arrange
            var service = CreateService();
            var attackerTroops = new TroopComposition(10, 5, 2);
            var defenderTroops = TroopComposition.Empty;

            // Act
            var result = service.SimulateBattle(
                "faction_blue",
                "faction_orange",
                "zone_sandy_shores",
                attackerTroops,
                defenderTroops);

            // Assert
            Assert.True(result.AttackerWon);
            Assert.Equal("faction_blue", result.NewOwnerFactionId);
        }

        [Fact]
        public void SimulateBattle_BothHaveNoTroops_DefenderWins()
        {
            // Arrange - tie breaker goes to defender (they hold position)
            var service = CreateService();
            var attackerTroops = TroopComposition.Empty;
            var defenderTroops = TroopComposition.Empty;

            // Act
            var result = service.SimulateBattle(
                "faction_blue",
                "faction_orange",
                "zone_paleto",
                attackerTroops,
                defenderTroops);

            // Assert - defender advantage in ties
            Assert.False(result.AttackerWon);
            Assert.Equal("faction_orange", result.NewOwnerFactionId);
        }

        [Fact]
        public void SimulateBattle_Casualties_DoNotExceedOriginalTroops()
        {
            // Arrange
            var service = CreateService();
            var attackerTroops = new TroopComposition(10, 5, 2);
            var defenderTroops = new TroopComposition(8, 4, 1);

            // Act
            var result = service.SimulateBattle(
                "faction_blue",
                "faction_orange",
                "zone_grove",
                attackerTroops,
                defenderTroops);

            // Assert
            Assert.True(result.AttackerCasualties.Basic <= attackerTroops.Basic);
            Assert.True(result.AttackerCasualties.Medium <= attackerTroops.Medium);
            Assert.True(result.AttackerCasualties.Heavy <= attackerTroops.Heavy);
            Assert.True(result.DefenderCasualties.Basic <= defenderTroops.Basic);
            Assert.True(result.DefenderCasualties.Medium <= defenderTroops.Medium);
            Assert.True(result.DefenderCasualties.Heavy <= defenderTroops.Heavy);
        }

        [Fact]
        public void SimulateBattle_NullAttackerId_ThrowsArgumentNullException()
        {
            // Arrange
            var service = CreateService();
            var attackerTroops = new TroopComposition(10, 0, 0);
            var defenderTroops = new TroopComposition(10, 0, 0);

            // Act & Assert
            Assert.Throws<ArgumentNullException>(() => service.SimulateBattle(
                null!,
                "faction_orange",
                "zone_test",
                attackerTroops,
                defenderTroops));
        }

        [Fact]
        public void SimulateBattle_NullDefenderId_ThrowsArgumentNullException()
        {
            // Arrange
            var service = CreateService();
            var attackerTroops = new TroopComposition(10, 0, 0);
            var defenderTroops = new TroopComposition(10, 0, 0);

            // Act & Assert
            Assert.Throws<ArgumentNullException>(() => service.SimulateBattle(
                "faction_blue",
                null!,
                "zone_test",
                attackerTroops,
                defenderTroops));
        }

        [Fact]
        public void SimulateBattle_NullZoneId_ThrowsArgumentNullException()
        {
            // Arrange
            var service = CreateService();
            var attackerTroops = new TroopComposition(10, 0, 0);
            var defenderTroops = new TroopComposition(10, 0, 0);

            // Act & Assert
            Assert.Throws<ArgumentNullException>(() => service.SimulateBattle(
                "faction_blue",
                "faction_orange",
                null!,
                attackerTroops,
                defenderTroops));
        }

        [Fact]
        public void SimulateBattle_NullAttackerTroops_ThrowsArgumentNullException()
        {
            // Arrange
            var service = CreateService();
            var defenderTroops = new TroopComposition(10, 0, 0);

            // Act & Assert
            Assert.Throws<ArgumentNullException>(() => service.SimulateBattle(
                "faction_blue",
                "faction_orange",
                "zone_test",
                null!,
                defenderTroops));
        }

        [Fact]
        public void SimulateBattle_NullDefenderTroops_ThrowsArgumentNullException()
        {
            // Arrange
            var service = CreateService();
            var attackerTroops = new TroopComposition(10, 0, 0);

            // Act & Assert
            Assert.Throws<ArgumentNullException>(() => service.SimulateBattle(
                "faction_blue",
                "faction_orange",
                "zone_test",
                attackerTroops,
                null!));
        }

        [Fact]
        public void CalculateWinProbability_EqualStrength_ReturnsDefenderAdvantage()
        {
            // Arrange - equal strength
            var service = CreateService();
            var attackerTroops = new TroopComposition(10, 0, 0);
            var defenderTroops = new TroopComposition(10, 0, 0);

            // Act
            var probability = service.CalculateWinProbability(attackerTroops, defenderTroops);

            // Assert - equal strength should favor defender slightly
            Assert.True(probability >= 0.4f && probability <= 0.5f,
                $"Expected probability 0.4-0.5 but got {probability}");
        }

        [Fact]
        public void CalculateWinProbability_AttackerMuchStronger_ReturnsHighProbability()
        {
            // Arrange - attacker 5x stronger
            var service = CreateService();
            var attackerTroops = new TroopComposition(50, 0, 0);
            var defenderTroops = new TroopComposition(10, 0, 0);

            // Act
            var probability = service.CalculateWinProbability(attackerTroops, defenderTroops);

            // Assert
            Assert.True(probability >= 0.8f, $"Expected probability >= 0.8 but got {probability}");
        }

        [Fact]
        public void CalculateWinProbability_DefenderMuchStronger_ReturnsLowProbability()
        {
            // Arrange - defender 5x stronger
            var service = CreateService();
            var attackerTroops = new TroopComposition(10, 0, 0);
            var defenderTroops = new TroopComposition(50, 0, 0);

            // Act
            var probability = service.CalculateWinProbability(attackerTroops, defenderTroops);

            // Assert
            Assert.True(probability <= 0.2f, $"Expected probability <= 0.2 but got {probability}");
        }

        [Fact]
        public void CalculateWinProbability_AttackerEmpty_ReturnsZero()
        {
            // Arrange
            var service = CreateService();
            var attackerTroops = TroopComposition.Empty;
            var defenderTroops = new TroopComposition(10, 0, 0);

            // Act
            var probability = service.CalculateWinProbability(attackerTroops, defenderTroops);

            // Assert
            Assert.Equal(0f, probability);
        }

        [Fact]
        public void CalculateWinProbability_DefenderEmpty_ReturnsOne()
        {
            // Arrange
            var service = CreateService();
            var attackerTroops = new TroopComposition(10, 0, 0);
            var defenderTroops = TroopComposition.Empty;

            // Act
            var probability = service.CalculateWinProbability(attackerTroops, defenderTroops);

            // Assert
            Assert.Equal(1f, probability);
        }

        [Fact]
        public void CalculateWinProbability_BothEmpty_ReturnsZero()
        {
            // Arrange
            var service = CreateService();
            var attackerTroops = TroopComposition.Empty;
            var defenderTroops = TroopComposition.Empty;

            // Act
            var probability = service.CalculateWinProbability(attackerTroops, defenderTroops);

            // Assert - defender advantage, attacker can't win
            Assert.Equal(0f, probability);
        }

        [Fact]
        public void CalculateWinProbability_AlwaysReturnsBetween0And1()
        {
            // Arrange - various combinations
            var service = CreateService();
            var testCases = new[]
            {
                (new TroopComposition(100, 0, 0), new TroopComposition(1, 0, 0)),
                (new TroopComposition(1, 0, 0), new TroopComposition(100, 0, 0)),
                (new TroopComposition(50, 50, 50), new TroopComposition(50, 50, 50)),
            };

            foreach (var (attacker, defender) in testCases)
            {
                // Act
                var probability = service.CalculateWinProbability(attacker, defender);

                // Assert
                Assert.True(probability >= 0f && probability <= 1f,
                    $"Probability {probability} out of range for attacker={attacker}, defender={defender}");
            }
        }

        [Fact]
        public void CalculateWinProbability_HeavyTroopsCountMore_ThanBasic()
        {
            // Arrange - 10 heavy vs 10 basic
            var service = CreateService();
            var heavyAttacker = new TroopComposition(0, 0, 10);
            var basicDefender = new TroopComposition(10, 0, 0);

            // And 10 basic vs 10 heavy
            var basicAttacker = new TroopComposition(10, 0, 0);
            var heavyDefender = new TroopComposition(0, 0, 10);

            // Act
            var heavyAttackerProb = service.CalculateWinProbability(heavyAttacker, basicDefender);
            var basicAttackerProb = service.CalculateWinProbability(basicAttacker, heavyDefender);

            // Assert - heavy attacker should have higher win chance than basic attacker
            Assert.True(heavyAttackerProb > basicAttackerProb,
                $"Heavy attacker prob {heavyAttackerProb} should be > basic attacker prob {basicAttackerProb}");
        }

        [Fact]
        public void CalculateCasualties_OpposingStrengthZero_ReturnsZeroCasualties()
        {
            // Arrange
            var service = CreateService();
            var troops = new TroopComposition(10, 5, 2);

            // Act
            var casualties = service.CalculateCasualties(troops, 0f);

            // Assert
            Assert.Equal(0, casualties.TotalCount);
        }

        [Fact]
        public void CalculateCasualties_HighOpposingStrength_ReturnsHighCasualties()
        {
            // Arrange
            var service = CreateService();
            var troops = new TroopComposition(10, 5, 2);

            // Act
            var casualties = service.CalculateCasualties(troops, 100f);

            // Assert - should have significant casualties
            Assert.True(casualties.TotalCount > 0, "Expected some casualties");
        }

        [Fact]
        public void CalculateCasualties_NeverExceedsOriginalTroops()
        {
            // Arrange
            var service = CreateService();
            var troops = new TroopComposition(10, 5, 2);

            // Act - very high opposing strength
            var casualties = service.CalculateCasualties(troops, 10000f);

            // Assert
            Assert.True(casualties.Basic <= troops.Basic);
            Assert.True(casualties.Medium <= troops.Medium);
            Assert.True(casualties.Heavy <= troops.Heavy);
        }

        [Fact]
        public void CalculateCasualties_EmptyTroops_ReturnsEmpty()
        {
            // Arrange
            var service = CreateService();
            var troops = TroopComposition.Empty;

            // Act
            var casualties = service.CalculateCasualties(troops, 50f);

            // Assert
            Assert.True(casualties.IsEmpty);
        }

        [Fact]
        public void CalculateCasualties_BasicTroopsLostFirst_ThenMedium_ThenHeavy()
        {
            // Arrange - mixed troops with moderate opposing strength
            var service = CreateService();
            var troops = new TroopComposition(10, 10, 10);

            // Act - moderate opposing strength that causes some casualties
            var casualties = service.CalculateCasualties(troops, 20f);

            // Assert - Basic troops should have highest casualty rate, then medium, then heavy
            // Heavy troops are more resilient
            float basicRate = troops.Basic > 0 ? (float)casualties.Basic / troops.Basic : 0f;
            float mediumRate = troops.Medium > 0 ? (float)casualties.Medium / troops.Medium : 0f;
            float heavyRate = troops.Heavy > 0 ? (float)casualties.Heavy / troops.Heavy : 0f;

            Assert.True(basicRate >= mediumRate, $"Basic rate {basicRate} should be >= Medium rate {mediumRate}");
            Assert.True(mediumRate >= heavyRate, $"Medium rate {mediumRate} should be >= Heavy rate {heavyRate}");
        }

        [Fact]
        public void CalculateCasualties_NullTroops_ThrowsArgumentNullException()
        {
            // Arrange
            var service = CreateService();

            // Act & Assert
            Assert.Throws<ArgumentNullException>(() => service.CalculateCasualties(null!, 10f));
        }

        [Fact]
        public void CalculateCasualties_NegativeOpposingStrength_TreatedAsZero()
        {
            // Arrange
            var service = CreateService();
            var troops = new TroopComposition(10, 5, 2);

            // Act
            var casualties = service.CalculateCasualties(troops, -10f);

            // Assert - negative strength should be treated as zero
            Assert.Equal(0, casualties.TotalCount);
        }

        private IBattleSimulationService CreateService()
        {
            return new BattleSimulationService();
        }

        #endregion
    }
}
