using FactionWars.Combat.Interfaces;
using FactionWars.Combat.Models;
using FactionWars.Combat.Services;
using Xunit;

namespace FactionWars.Tests.Unit.Combat
{
    public class ControlPercentageCalculatorTests
    {
        private readonly IControlPercentageCalculator _calculator;

        public ControlPercentageCalculatorTests()
        {
            _calculator = new ControlPercentageCalculator();
        }

        #region Basic Calculation

        [Fact]
        public void Calculate_WithNoPeds_ShouldReturn50ForBoth()
        {
            // Arrange
            int attackerCount = 0;
            int defenderCount = 0;

            // Act
            var result = _calculator.Calculate(attackerCount, defenderCount);

            // Assert - returns 50/50 neutral state when no peds to prevent immediate victory
            Assert.Equal(50f, result.AttackerPercentage);
            Assert.Equal(50f, result.DefenderPercentage);
        }

        [Fact]
        public void Calculate_WithOnlyAttackers_ShouldReturn100ForAttacker()
        {
            // Arrange
            int attackerCount = 10;
            int defenderCount = 0;

            // Act
            var result = _calculator.Calculate(attackerCount, defenderCount);

            // Assert
            Assert.Equal(100f, result.AttackerPercentage);
            Assert.Equal(0f, result.DefenderPercentage);
        }

        [Fact]
        public void Calculate_WithOnlyDefenders_ShouldReturn100ForDefender()
        {
            // Arrange
            int attackerCount = 0;
            int defenderCount = 10;

            // Act
            var result = _calculator.Calculate(attackerCount, defenderCount);

            // Assert
            Assert.Equal(0f, result.AttackerPercentage);
            Assert.Equal(100f, result.DefenderPercentage);
        }

        [Fact]
        public void Calculate_WithEqualPeds_ShouldReturn50ForBoth()
        {
            // Arrange
            int attackerCount = 10;
            int defenderCount = 10;

            // Act
            var result = _calculator.Calculate(attackerCount, defenderCount);

            // Assert
            Assert.Equal(50f, result.AttackerPercentage);
            Assert.Equal(50f, result.DefenderPercentage);
        }

        [Fact]
        public void Calculate_WithMoreAttackers_ShouldReturnHigherAttackerPercentage()
        {
            // Arrange
            int attackerCount = 15;
            int defenderCount = 5;

            // Act
            var result = _calculator.Calculate(attackerCount, defenderCount);

            // Assert
            Assert.Equal(75f, result.AttackerPercentage);
            Assert.Equal(25f, result.DefenderPercentage);
        }

        [Fact]
        public void Calculate_WithMoreDefenders_ShouldReturnHigherDefenderPercentage()
        {
            // Arrange
            int attackerCount = 5;
            int defenderCount = 15;

            // Act
            var result = _calculator.Calculate(attackerCount, defenderCount);

            // Assert
            Assert.Equal(25f, result.AttackerPercentage);
            Assert.Equal(75f, result.DefenderPercentage);
        }

        #endregion

        #region Edge Cases

        [Fact]
        public void Calculate_WithNegativeAttackerCount_ShouldTreatAsZero()
        {
            // Arrange
            int attackerCount = -5;
            int defenderCount = 10;

            // Act
            var result = _calculator.Calculate(attackerCount, defenderCount);

            // Assert
            Assert.Equal(0f, result.AttackerPercentage);
            Assert.Equal(100f, result.DefenderPercentage);
        }

        [Fact]
        public void Calculate_WithNegativeDefenderCount_ShouldTreatAsZero()
        {
            // Arrange
            int attackerCount = 10;
            int defenderCount = -5;

            // Act
            var result = _calculator.Calculate(attackerCount, defenderCount);

            // Assert
            Assert.Equal(100f, result.AttackerPercentage);
            Assert.Equal(0f, result.DefenderPercentage);
        }

        [Fact]
        public void Calculate_WithBothNegative_ShouldReturn50ForBoth()
        {
            // Arrange
            int attackerCount = -5;
            int defenderCount = -10;

            // Act
            var result = _calculator.Calculate(attackerCount, defenderCount);

            // Assert - negative values clamped to zero, resulting in 50/50 neutral state
            Assert.Equal(50f, result.AttackerPercentage);
            Assert.Equal(50f, result.DefenderPercentage);
        }

        [Fact]
        public void Calculate_WithSingleAttacker_ShouldReturn100ForAttacker()
        {
            // Arrange
            int attackerCount = 1;
            int defenderCount = 0;

            // Act
            var result = _calculator.Calculate(attackerCount, defenderCount);

            // Assert
            Assert.Equal(100f, result.AttackerPercentage);
            Assert.Equal(0f, result.DefenderPercentage);
        }

        [Fact]
        public void Calculate_WithSingleDefender_ShouldReturn100ForDefender()
        {
            // Arrange
            int attackerCount = 0;
            int defenderCount = 1;

            // Act
            var result = _calculator.Calculate(attackerCount, defenderCount);

            // Assert
            Assert.Equal(0f, result.AttackerPercentage);
            Assert.Equal(100f, result.DefenderPercentage);
        }

        #endregion

        #region Percentage Sum

        [Theory]
        [InlineData(1, 1)]
        [InlineData(5, 10)]
        [InlineData(10, 5)]
        [InlineData(7, 13)]
        [InlineData(3, 17)]
        [InlineData(100, 50)]
        public void Calculate_PercentagesShouldSumTo100(int attackerCount, int defenderCount)
        {
            // Act
            var result = _calculator.Calculate(attackerCount, defenderCount);

            // Assert
            Assert.Equal(100f, result.AttackerPercentage + result.DefenderPercentage, precision: 2);
        }

        [Fact]
        public void Calculate_WithZeroPeds_PercentagesShouldSumTo100()
        {
            // Act
            var result = _calculator.Calculate(0, 0);

            // Assert - returns 50/50 neutral state (sums to 100)
            Assert.Equal(100f, result.AttackerPercentage + result.DefenderPercentage);
        }

        #endregion

        #region Rounding Behavior

        [Fact]
        public void Calculate_WithOneThirdSplit_ShouldHandleDecimalsCorrectly()
        {
            // Arrange - 1 attacker vs 2 defenders = 33.33% vs 66.67%
            int attackerCount = 1;
            int defenderCount = 2;

            // Act
            var result = _calculator.Calculate(attackerCount, defenderCount);

            // Assert - Allow for floating point precision
            Assert.InRange(result.AttackerPercentage, 33.3f, 33.4f);
            Assert.InRange(result.DefenderPercentage, 66.6f, 66.7f);
        }

        [Fact]
        public void Calculate_WithOddSplit_ShouldMaintain100Total()
        {
            // Arrange - 3 attackers vs 7 defenders = 30% vs 70%
            int attackerCount = 3;
            int defenderCount = 7;

            // Act
            var result = _calculator.Calculate(attackerCount, defenderCount);

            // Assert
            Assert.Equal(30f, result.AttackerPercentage, precision: 2);
            Assert.Equal(70f, result.DefenderPercentage, precision: 2);
        }

        #endregion

        #region Result Object

        [Fact]
        public void Calculate_ShouldReturnControlPercentageResultType()
        {
            // Act
            var result = _calculator.Calculate(5, 5);

            // Assert
            Assert.IsType<ControlPercentageResult>(result);
        }

        [Fact]
        public void ControlPercentageResult_ShouldExposeAttackerPercentage()
        {
            // Act
            var result = _calculator.Calculate(7, 3);

            // Assert
            Assert.Equal(70f, result.AttackerPercentage);
        }

        [Fact]
        public void ControlPercentageResult_ShouldExposeDefenderPercentage()
        {
            // Act
            var result = _calculator.Calculate(7, 3);

            // Assert
            Assert.Equal(30f, result.DefenderPercentage, precision: 2);
        }

        [Fact]
        public void ControlPercentageResult_ShouldExposeTotalPeds()
        {
            // Act
            var result = _calculator.Calculate(7, 3);

            // Assert
            Assert.Equal(10, result.TotalPeds);
        }

        #endregion

        #region Encounter Integration

        [Fact]
        public void CalculateForEncounter_ShouldUseEncounterPedCounts()
        {
            // Arrange
            var encounter = new CombatEncounter("enc_1", "zone_1", "faction_a", "faction_b");
            encounter.AttackerPedCount = 8;
            encounter.DefenderPedCount = 2;

            // Act
            var result = _calculator.CalculateForEncounter(encounter);

            // Assert
            Assert.Equal(80f, result.AttackerPercentage);
            Assert.Equal(20f, result.DefenderPercentage);
        }

        [Fact]
        public void CalculateForEncounter_WithZeroPeds_ShouldReturn50ForBoth()
        {
            // Arrange
            var encounter = new CombatEncounter("enc_1", "zone_1", "faction_a", "faction_b");

            // Act
            var result = _calculator.CalculateForEncounter(encounter);

            // Assert - returns 50/50 neutral state when no peds
            Assert.Equal(50f, result.AttackerPercentage);
            Assert.Equal(50f, result.DefenderPercentage);
        }

        [Fact]
        public void CalculateForEncounter_ShouldThrowOnNullEncounter()
        {
            // Act & Assert
            Assert.Throws<System.ArgumentNullException>(() => _calculator.CalculateForEncounter(null!));
        }

        #endregion

        #region Apply To Encounter

        [Fact]
        public void ApplyToEncounter_ShouldUpdateEncounterControlPercentages()
        {
            // Arrange
            var encounter = new CombatEncounter("enc_1", "zone_1", "faction_a", "faction_b");
            encounter.AttackerPedCount = 6;
            encounter.DefenderPedCount = 4;

            // Act
            _calculator.ApplyToEncounter(encounter);

            // Assert
            Assert.Equal(60f, encounter.AttackerControlPercentage, precision: 2);
            Assert.Equal(40f, encounter.DefenderControlPercentage, precision: 2);
        }

        [Fact]
        public void ApplyToEncounter_WithNoPeds_ShouldSetBothTo50()
        {
            // Arrange
            var encounter = new CombatEncounter("enc_1", "zone_1", "faction_a", "faction_b");
            // Manually set percentages to non-standard values initially
            encounter.AttackerControlPercentage = 75f;
            encounter.DefenderControlPercentage = 25f;

            // Act
            _calculator.ApplyToEncounter(encounter);

            // Assert - returns 50/50 neutral state when no peds
            Assert.Equal(50f, encounter.AttackerControlPercentage);
            Assert.Equal(50f, encounter.DefenderControlPercentage);
        }

        [Fact]
        public void ApplyToEncounter_ShouldThrowOnNullEncounter()
        {
            // Act & Assert
            Assert.Throws<System.ArgumentNullException>(() => _calculator.ApplyToEncounter(null!));
        }

        [Fact]
        public void ApplyToEncounter_ShouldNotApplyToEndedEncounter()
        {
            // Arrange
            var encounter = new CombatEncounter("enc_1", "zone_1", "faction_a", "faction_b");
            encounter.AttackerPedCount = 10;
            encounter.DefenderPedCount = 0;
            encounter.End(CombatStatus.AttackerVictory);

            // Preserve the percentages at time of ending
            float originalAttacker = encounter.AttackerControlPercentage;
            float originalDefender = encounter.DefenderControlPercentage;

            // Act & Assert - Should throw because encounter is ended
            Assert.Throws<System.InvalidOperationException>(() => _calculator.ApplyToEncounter(encounter));
        }

        #endregion
    }
}
