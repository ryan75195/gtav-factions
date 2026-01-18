using FactionWars.Loyalty.Interfaces;
using FactionWars.Loyalty.Models;
using FactionWars.Loyalty.Services;
using Moq;
using System;
using System.Collections.Generic;
using System.Linq;
using Xunit;

namespace FactionWars.Tests.Unit.Loyalty
{
    public class LoyaltyChangeServiceTests
    {
        private readonly LoyaltyChangeService _service;

        public LoyaltyChangeServiceTests()
        {
            _service = new LoyaltyChangeService();
        }

        #region Interface Compliance

        [Fact]
        public void LoyaltyChangeService_ShouldImplementILoyaltyChangeService()
        {
            // Assert
            Assert.IsAssignableFrom<ILoyaltyChangeService>(_service);
        }

        #endregion

        #region ApplyModifier - Basic Application

        [Fact]
        public void ApplyModifier_ShouldIncreaseZoneLoyalty_WhenModifierIsPositive()
        {
            // Arrange
            var zoneLoyalty = new ZoneLoyalty("zone_downtown", "faction_michael", initialLoyalty: 50);
            var modifier = LoyaltyModifier.CreateCombatVictory(); // +7

            // Act
            _service.ApplyModifier(zoneLoyalty, modifier);

            // Assert
            Assert.Equal(57, zoneLoyalty.LoyaltyValue);
        }

        [Fact]
        public void ApplyModifier_ShouldDecreaseZoneLoyalty_WhenModifierIsNegative()
        {
            // Arrange
            var zoneLoyalty = new ZoneLoyalty("zone_downtown", "faction_michael", initialLoyalty: 50);
            var modifier = LoyaltyModifier.CreateCombatDefeat(); // -5

            // Act
            _service.ApplyModifier(zoneLoyalty, modifier);

            // Assert
            Assert.Equal(45, zoneLoyalty.LoyaltyValue);
        }

        [Fact]
        public void ApplyModifier_ShouldThrowArgumentNullException_WhenZoneLoyaltyIsNull()
        {
            // Arrange
            var modifier = LoyaltyModifier.CreateTimeBasedGain();

            // Act & Assert
            Assert.Throws<ArgumentNullException>(() => _service.ApplyModifier(null!, modifier));
        }

        [Fact]
        public void ApplyModifier_ShouldThrowArgumentNullException_WhenModifierIsNull()
        {
            // Arrange
            var zoneLoyalty = new ZoneLoyalty("zone_downtown", "faction_michael");

            // Act & Assert
            Assert.Throws<ArgumentNullException>(() => _service.ApplyModifier(zoneLoyalty, null!));
        }

        [Fact]
        public void ApplyModifier_ShouldClampLoyaltyToMaximum()
        {
            // Arrange
            var zoneLoyalty = new ZoneLoyalty("zone_downtown", "faction_michael", initialLoyalty: 98);
            var modifier = LoyaltyModifier.CreateCombatVictory(); // +7

            // Act
            _service.ApplyModifier(zoneLoyalty, modifier);

            // Assert - Clamped to 100
            Assert.Equal(100, zoneLoyalty.LoyaltyValue);
        }

        [Fact]
        public void ApplyModifier_ShouldClampLoyaltyToMinimum()
        {
            // Arrange
            var zoneLoyalty = new ZoneLoyalty("zone_downtown", "faction_michael", initialLoyalty: 3);
            var modifier = LoyaltyModifier.CreateCombatDefeat(); // -5

            // Act
            _service.ApplyModifier(zoneLoyalty, modifier);

            // Assert - Clamped to 0
            Assert.Equal(0, zoneLoyalty.LoyaltyValue);
        }

        #endregion

        #region ApplyModifiers - Multiple Modifiers

        [Fact]
        public void ApplyModifiers_ShouldApplyAllModifiersInSequence()
        {
            // Arrange
            var zoneLoyalty = new ZoneLoyalty("zone_downtown", "faction_michael", initialLoyalty: 50);
            var modifiers = new List<LoyaltyModifier>
            {
                LoyaltyModifier.CreateTimeBasedGain(),     // +2
                LoyaltyModifier.CreateCombatVictory(),    // +7
                LoyaltyModifier.CreateResourceInvestment() // +3
            };

            // Act
            _service.ApplyModifiers(zoneLoyalty, modifiers);

            // Assert - 50 + 2 + 7 + 3 = 62
            Assert.Equal(62, zoneLoyalty.LoyaltyValue);
        }

        [Fact]
        public void ApplyModifiers_ShouldHandleMixedPositiveAndNegative()
        {
            // Arrange
            var zoneLoyalty = new ZoneLoyalty("zone_downtown", "faction_michael", initialLoyalty: 50);
            var modifiers = new List<LoyaltyModifier>
            {
                LoyaltyModifier.CreateCombatVictory(),   // +7
                LoyaltyModifier.CreateOppression(),      // -2
                LoyaltyModifier.CreateCombatDefeat()     // -5
            };

            // Act
            _service.ApplyModifiers(zoneLoyalty, modifiers);

            // Assert - 50 + 7 - 2 - 5 = 50
            Assert.Equal(50, zoneLoyalty.LoyaltyValue);
        }

        [Fact]
        public void ApplyModifiers_ShouldThrowArgumentNullException_WhenZoneLoyaltyIsNull()
        {
            // Arrange
            var modifiers = new List<LoyaltyModifier> { LoyaltyModifier.CreateTimeBasedGain() };

            // Act & Assert
            Assert.Throws<ArgumentNullException>(() => _service.ApplyModifiers(null!, modifiers));
        }

        [Fact]
        public void ApplyModifiers_ShouldThrowArgumentNullException_WhenModifiersIsNull()
        {
            // Arrange
            var zoneLoyalty = new ZoneLoyalty("zone_downtown", "faction_michael");

            // Act & Assert
            Assert.Throws<ArgumentNullException>(() => _service.ApplyModifiers(zoneLoyalty, null!));
        }

        [Fact]
        public void ApplyModifiers_ShouldDoNothing_WhenModifiersCollectionIsEmpty()
        {
            // Arrange
            var zoneLoyalty = new ZoneLoyalty("zone_downtown", "faction_michael", initialLoyalty: 50);
            var modifiers = new List<LoyaltyModifier>();

            // Act
            _service.ApplyModifiers(zoneLoyalty, modifiers);

            // Assert
            Assert.Equal(50, zoneLoyalty.LoyaltyValue);
        }

        #endregion

        #region ApplyDailyChange

        [Fact]
        public void ApplyDailyChange_ShouldApplyTimeBasedGain()
        {
            // Arrange
            var zoneLoyalty = new ZoneLoyalty("zone_downtown", "faction_michael", initialLoyalty: 50);

            // Act
            _service.ApplyDailyChange(zoneLoyalty);

            // Assert - Default time-based gain is +2
            Assert.Equal(52, zoneLoyalty.LoyaltyValue);
        }

        [Fact]
        public void ApplyDailyChange_ShouldAdvanceDayCounter()
        {
            // Arrange
            var zoneLoyalty = new ZoneLoyalty("zone_downtown", "faction_michael", initialLoyalty: 50);
            var initialDays = zoneLoyalty.DaysUnderControl;

            // Act
            _service.ApplyDailyChange(zoneLoyalty);

            // Assert
            Assert.Equal(initialDays + 1, zoneLoyalty.DaysUnderControl);
        }

        [Fact]
        public void ApplyDailyChange_ShouldThrowArgumentNullException_WhenZoneLoyaltyIsNull()
        {
            // Act & Assert
            Assert.Throws<ArgumentNullException>(() => _service.ApplyDailyChange(null!));
        }

        [Fact]
        public void ApplyDailyChange_ShouldNotExceedMaxLoyalty()
        {
            // Arrange
            var zoneLoyalty = new ZoneLoyalty("zone_downtown", "faction_michael", initialLoyalty: 99);

            // Act
            _service.ApplyDailyChange(zoneLoyalty);

            // Assert - Should cap at 100
            Assert.Equal(100, zoneLoyalty.LoyaltyValue);
        }

        #endregion

        #region ApplyCombatResult

        [Fact]
        public void ApplyCombatResult_ShouldApplyVictoryBonus_WhenDefenderWon()
        {
            // Arrange
            var zoneLoyalty = new ZoneLoyalty("zone_downtown", "faction_michael", initialLoyalty: 50);

            // Act
            _service.ApplyCombatResult(zoneLoyalty, defenderWon: true);

            // Assert - Default combat victory bonus is +7
            Assert.Equal(57, zoneLoyalty.LoyaltyValue);
        }

        [Fact]
        public void ApplyCombatResult_ShouldApplyDefeatPenalty_WhenDefenderLost()
        {
            // Arrange
            var zoneLoyalty = new ZoneLoyalty("zone_downtown", "faction_michael", initialLoyalty: 50);

            // Act
            _service.ApplyCombatResult(zoneLoyalty, defenderWon: false);

            // Assert - Default combat defeat penalty is -5
            Assert.Equal(45, zoneLoyalty.LoyaltyValue);
        }

        [Fact]
        public void ApplyCombatResult_ShouldThrowArgumentNullException_WhenZoneLoyaltyIsNull()
        {
            // Act & Assert
            Assert.Throws<ArgumentNullException>(() => _service.ApplyCombatResult(null!, true));
        }

        #endregion

        #region ApplyResourceInvestment

        [Fact]
        public void ApplyResourceInvestment_ShouldApplyBonusBasedOnAmount()
        {
            // Arrange
            var zoneLoyalty = new ZoneLoyalty("zone_downtown", "faction_michael", initialLoyalty: 50);

            // Act - Invest 1 unit of resources
            _service.ApplyResourceInvestment(zoneLoyalty, amount: 1);

            // Assert - Default investment bonus is +3 per unit
            Assert.Equal(53, zoneLoyalty.LoyaltyValue);
        }

        [Fact]
        public void ApplyResourceInvestment_ShouldScaleWithAmount()
        {
            // Arrange
            var zoneLoyalty = new ZoneLoyalty("zone_downtown", "faction_michael", initialLoyalty: 50);

            // Act - Invest 3 units of resources
            _service.ApplyResourceInvestment(zoneLoyalty, amount: 3);

            // Assert - 3 * 3 = 9 bonus
            Assert.Equal(59, zoneLoyalty.LoyaltyValue);
        }

        [Fact]
        public void ApplyResourceInvestment_ShouldThrowArgumentNullException_WhenZoneLoyaltyIsNull()
        {
            // Act & Assert
            Assert.Throws<ArgumentNullException>(() => _service.ApplyResourceInvestment(null!, 1));
        }

        [Fact]
        public void ApplyResourceInvestment_ShouldThrowArgumentOutOfRangeException_WhenAmountIsZeroOrNegative()
        {
            // Arrange
            var zoneLoyalty = new ZoneLoyalty("zone_downtown", "faction_michael");

            // Act & Assert
            Assert.Throws<ArgumentOutOfRangeException>(() => _service.ApplyResourceInvestment(zoneLoyalty, amount: 0));
            Assert.Throws<ArgumentOutOfRangeException>(() => _service.ApplyResourceInvestment(zoneLoyalty, amount: -1));
        }

        #endregion

        #region ApplyOppression

        [Fact]
        public void ApplyOppression_ShouldApplyPenalty()
        {
            // Arrange
            var zoneLoyalty = new ZoneLoyalty("zone_downtown", "faction_michael", initialLoyalty: 50);

            // Act
            _service.ApplyOppression(zoneLoyalty);

            // Assert - Default oppression penalty is -2
            Assert.Equal(48, zoneLoyalty.LoyaltyValue);
        }

        [Fact]
        public void ApplyOppression_ShouldApplyPenaltyWithSeverity()
        {
            // Arrange
            var zoneLoyalty = new ZoneLoyalty("zone_downtown", "faction_michael", initialLoyalty: 50);

            // Act - Severe oppression (3x multiplier)
            _service.ApplyOppression(zoneLoyalty, severityMultiplier: 3);

            // Assert - -2 * 3 = -6
            Assert.Equal(44, zoneLoyalty.LoyaltyValue);
        }

        [Fact]
        public void ApplyOppression_ShouldThrowArgumentNullException_WhenZoneLoyaltyIsNull()
        {
            // Act & Assert
            Assert.Throws<ArgumentNullException>(() => _service.ApplyOppression(null!));
        }

        [Fact]
        public void ApplyOppression_ShouldThrowArgumentOutOfRangeException_WhenSeverityIsZeroOrNegative()
        {
            // Arrange
            var zoneLoyalty = new ZoneLoyalty("zone_downtown", "faction_michael");

            // Act & Assert
            Assert.Throws<ArgumentOutOfRangeException>(() => _service.ApplyOppression(zoneLoyalty, severityMultiplier: 0));
            Assert.Throws<ArgumentOutOfRangeException>(() => _service.ApplyOppression(zoneLoyalty, severityMultiplier: -1));
        }

        #endregion

        #region ApplyPropaganda

        [Fact]
        public void ApplyPropaganda_ShouldApplyBonus()
        {
            // Arrange
            var zoneLoyalty = new ZoneLoyalty("zone_downtown", "faction_michael", initialLoyalty: 50);

            // Act
            _service.ApplyPropaganda(zoneLoyalty);

            // Assert - Default propaganda bonus is +5
            Assert.Equal(55, zoneLoyalty.LoyaltyValue);
        }

        [Fact]
        public void ApplyPropaganda_ShouldThrowArgumentNullException_WhenZoneLoyaltyIsNull()
        {
            // Act & Assert
            Assert.Throws<ArgumentNullException>(() => _service.ApplyPropaganda(null!));
        }

        #endregion

        #region ApplyNeighborInfluence

        [Fact]
        public void ApplyNeighborInfluence_ShouldApplyPositiveInfluence_WhenNeighborHasHighLoyalty()
        {
            // Arrange
            var zoneLoyalty = new ZoneLoyalty("zone_downtown", "faction_michael", initialLoyalty: 50);
            var neighborLoyalties = new List<ZoneLoyalty>
            {
                new ZoneLoyalty("zone_vinewood", "faction_michael", initialLoyalty: 80),  // +6 from 50
                new ZoneLoyalty("zone_rockford", "faction_michael", initialLoyalty: 90)   // +8 from 50
            };

            // Act
            var influence = _service.CalculateNeighborInfluence(zoneLoyalty, neighborLoyalties);

            // Assert - Positive influence when neighbors have higher loyalty
            Assert.True(influence > 0);
        }

        [Fact]
        public void ApplyNeighborInfluence_ShouldApplyNegativeInfluence_WhenNeighborHasLowLoyalty()
        {
            // Arrange
            var zoneLoyalty = new ZoneLoyalty("zone_downtown", "faction_michael", initialLoyalty: 50);
            var neighborLoyalties = new List<ZoneLoyalty>
            {
                new ZoneLoyalty("zone_vinewood", "faction_michael", initialLoyalty: 20),
                new ZoneLoyalty("zone_rockford", "faction_michael", initialLoyalty: 15)
            };

            // Act
            var influence = _service.CalculateNeighborInfluence(zoneLoyalty, neighborLoyalties);

            // Assert - Negative influence when neighbors have lower loyalty
            Assert.True(influence < 0);
        }

        [Fact]
        public void ApplyNeighborInfluence_ShouldReturnZero_WhenNoNeighbors()
        {
            // Arrange
            var zoneLoyalty = new ZoneLoyalty("zone_downtown", "faction_michael", initialLoyalty: 50);
            var neighborLoyalties = new List<ZoneLoyalty>();

            // Act
            var influence = _service.CalculateNeighborInfluence(zoneLoyalty, neighborLoyalties);

            // Assert
            Assert.Equal(0, influence);
        }

        [Fact]
        public void ApplyNeighborInfluence_ShouldOnlyConsiderSameFactionNeighbors()
        {
            // Arrange
            var zoneLoyalty = new ZoneLoyalty("zone_downtown", "faction_michael", initialLoyalty: 50);
            var neighborLoyalties = new List<ZoneLoyalty>
            {
                new ZoneLoyalty("zone_vinewood", "faction_michael", initialLoyalty: 80),  // Same faction
                new ZoneLoyalty("zone_rockford", "faction_trevor", initialLoyalty: 90),   // Different faction
                new ZoneLoyalty("zone_del_perro", "faction_franklin", initialLoyalty: 10) // Different faction
            };

            // Act
            var influence = _service.CalculateNeighborInfluence(zoneLoyalty, neighborLoyalties);

            // Assert - Only faction_michael neighbor should be considered
            Assert.True(influence > 0);
        }

        [Fact]
        public void ApplyNeighborInfluence_ShouldThrowArgumentNullException_WhenZoneLoyaltyIsNull()
        {
            // Arrange
            var neighborLoyalties = new List<ZoneLoyalty>();

            // Act & Assert
            Assert.Throws<ArgumentNullException>(() => _service.CalculateNeighborInfluence(null!, neighborLoyalties));
        }

        [Fact]
        public void ApplyNeighborInfluence_ShouldThrowArgumentNullException_WhenNeighborsIsNull()
        {
            // Arrange
            var zoneLoyalty = new ZoneLoyalty("zone_downtown", "faction_michael");

            // Act & Assert
            Assert.Throws<ArgumentNullException>(() => _service.CalculateNeighborInfluence(zoneLoyalty, null!));
        }

        [Fact]
        public void ApplyNeighborInfluence_ShouldApplyCalculatedInfluence()
        {
            // Arrange
            var zoneLoyalty = new ZoneLoyalty("zone_downtown", "faction_michael", initialLoyalty: 50);
            var neighborLoyalties = new List<ZoneLoyalty>
            {
                new ZoneLoyalty("zone_vinewood", "faction_michael", initialLoyalty: 80),
                new ZoneLoyalty("zone_rockford", "faction_michael", initialLoyalty: 70)
            };

            // Act
            _service.ApplyNeighborInfluence(zoneLoyalty, neighborLoyalties);

            // Assert - Loyalty should increase due to high-loyalty neighbors
            Assert.True(zoneLoyalty.LoyaltyValue > 50);
        }

        #endregion

        #region ApplyConquestPenalty

        [Fact]
        public void ApplyConquestPenalty_ShouldApplyPenalty()
        {
            // Arrange
            var zoneLoyalty = new ZoneLoyalty("zone_downtown", "faction_michael", initialLoyalty: 50);

            // Act
            _service.ApplyConquestPenalty(zoneLoyalty);

            // Assert - Default conquest penalty is -10
            Assert.Equal(40, zoneLoyalty.LoyaltyValue);
        }

        [Fact]
        public void ApplyConquestPenalty_ShouldThrowArgumentNullException_WhenZoneLoyaltyIsNull()
        {
            // Act & Assert
            Assert.Throws<ArgumentNullException>(() => _service.ApplyConquestPenalty(null!));
        }

        #endregion

        #region CalculateTotalModifierValue

        [Fact]
        public void CalculateTotalModifierValue_ShouldSumAllModifiers()
        {
            // Arrange
            var modifiers = new List<LoyaltyModifier>
            {
                LoyaltyModifier.CreateTimeBasedGain(),     // +2
                LoyaltyModifier.CreateCombatVictory(),    // +7
                LoyaltyModifier.CreateCombatDefeat()      // -5
            };

            // Act
            var total = _service.CalculateTotalModifierValue(modifiers);

            // Assert - 2 + 7 - 5 = 4
            Assert.Equal(4, total);
        }

        [Fact]
        public void CalculateTotalModifierValue_ShouldReturnZero_WhenEmpty()
        {
            // Arrange
            var modifiers = new List<LoyaltyModifier>();

            // Act
            var total = _service.CalculateTotalModifierValue(modifiers);

            // Assert
            Assert.Equal(0, total);
        }

        [Fact]
        public void CalculateTotalModifierValue_ShouldThrowArgumentNullException_WhenNull()
        {
            // Act & Assert
            Assert.Throws<ArgumentNullException>(() => _service.CalculateTotalModifierValue(null!));
        }

        #endregion

        #region GetLevelChangeDescription

        [Fact]
        public void GetLevelChangeDescription_ShouldReturnDescription_WhenLevelChanges()
        {
            // Arrange
            var oldLevel = LoyaltyLevel.Resistant;
            var newLevel = LoyaltyLevel.Neutral;

            // Act
            var description = _service.GetLevelChangeDescription(oldLevel, newLevel);

            // Assert
            Assert.NotNull(description);
            Assert.Contains("Resistant", description);
            Assert.Contains("Neutral", description);
        }

        [Fact]
        public void GetLevelChangeDescription_ShouldReturnNull_WhenLevelDoesNotChange()
        {
            // Arrange
            var oldLevel = LoyaltyLevel.Neutral;
            var newLevel = LoyaltyLevel.Neutral;

            // Act
            var description = _service.GetLevelChangeDescription(oldLevel, newLevel);

            // Assert
            Assert.Null(description);
        }

        [Fact]
        public void GetLevelChangeDescription_ShouldIndicateImprovement_WhenLevelIncreases()
        {
            // Arrange
            var oldLevel = LoyaltyLevel.Hostile;
            var newLevel = LoyaltyLevel.Supportive;

            // Act
            var description = _service.GetLevelChangeDescription(oldLevel, newLevel);

            // Assert
            Assert.Contains("improved", description, StringComparison.OrdinalIgnoreCase);
        }

        [Fact]
        public void GetLevelChangeDescription_ShouldIndicateDecline_WhenLevelDecreases()
        {
            // Arrange
            var oldLevel = LoyaltyLevel.Fanatical;
            var newLevel = LoyaltyLevel.Resistant;

            // Act
            var description = _service.GetLevelChangeDescription(oldLevel, newLevel);

            // Assert
            Assert.Contains("declined", description, StringComparison.OrdinalIgnoreCase);
        }

        #endregion
    }
}
