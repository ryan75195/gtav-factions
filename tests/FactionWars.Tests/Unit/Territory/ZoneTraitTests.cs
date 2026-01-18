using FactionWars.Territory.Models;
using System;
using System.Linq;
using Xunit;

namespace FactionWars.Tests.Unit.Territory
{
    public class ZoneTraitTests
    {
        #region ZoneTrait Enum Values

        [Fact]
        public void ZoneTrait_ShouldHaveNoneValue()
        {
            // Arrange & Act
            var trait = ZoneTrait.None;

            // Assert
            Assert.Equal(0, (int)trait);
        }

        [Fact]
        public void ZoneTrait_ShouldHaveIndustrialValue()
        {
            // Industrial zones generate more resources (money/weapons)
            var trait = ZoneTrait.Industrial;
            Assert.True(Enum.IsDefined(typeof(ZoneTrait), trait));
        }

        [Fact]
        public void ZoneTrait_ShouldHaveCommercialValue()
        {
            // Commercial zones generate more cash income
            var trait = ZoneTrait.Commercial;
            Assert.True(Enum.IsDefined(typeof(ZoneTrait), trait));
        }

        [Fact]
        public void ZoneTrait_ShouldHaveResidentialValue()
        {
            // Residential zones provide recruitment bonus
            var trait = ZoneTrait.Residential;
            Assert.True(Enum.IsDefined(typeof(ZoneTrait), trait));
        }

        [Fact]
        public void ZoneTrait_ShouldHavePortValue()
        {
            // Port zones enable supply line access and smuggling
            var trait = ZoneTrait.Port;
            Assert.True(Enum.IsDefined(typeof(ZoneTrait), trait));
        }

        [Fact]
        public void ZoneTrait_ShouldHaveAirfieldValue()
        {
            // Airfield zones provide rapid deployment bonus
            var trait = ZoneTrait.Airfield;
            Assert.True(Enum.IsDefined(typeof(ZoneTrait), trait));
        }

        [Fact]
        public void ZoneTrait_ShouldHaveFortifiedValue()
        {
            // Fortified zones provide defense bonus
            var trait = ZoneTrait.Fortified;
            Assert.True(Enum.IsDefined(typeof(ZoneTrait), trait));
        }

        [Fact]
        public void ZoneTrait_ShouldHaveHighValueValue()
        {
            // High-value zones multiply all resource generation
            var trait = ZoneTrait.HighValue;
            Assert.True(Enum.IsDefined(typeof(ZoneTrait), trait));
        }

        [Fact]
        public void ZoneTrait_ShouldBeFlagsEnum()
        {
            // ZoneTrait should support combinations (e.g., Industrial | Fortified)
            var combined = ZoneTrait.Industrial | ZoneTrait.Fortified;
            Assert.True(combined.HasFlag(ZoneTrait.Industrial));
            Assert.True(combined.HasFlag(ZoneTrait.Fortified));
            Assert.False(combined.HasFlag(ZoneTrait.Commercial));
        }

        [Fact]
        public void ZoneTrait_CombinedFlags_ShouldMaintainIndividualFlags()
        {
            // Arrange
            var combined = ZoneTrait.Commercial | ZoneTrait.HighValue | ZoneTrait.Fortified;

            // Act & Assert
            Assert.True(combined.HasFlag(ZoneTrait.Commercial));
            Assert.True(combined.HasFlag(ZoneTrait.HighValue));
            Assert.True(combined.HasFlag(ZoneTrait.Fortified));
            Assert.False(combined.HasFlag(ZoneTrait.Industrial));
            Assert.False(combined.HasFlag(ZoneTrait.Residential));
        }

        #endregion

        #region ZoneTraitEffects - Cash Modifier

        [Fact]
        public void GetCashModifier_ShouldReturnBaselineForNone()
        {
            // Arrange
            var effects = new ZoneTraitEffects();

            // Act
            var modifier = effects.GetCashModifier(ZoneTrait.None);

            // Assert - No trait means base multiplier of 1.0
            Assert.Equal(1.0f, modifier);
        }

        [Fact]
        public void GetCashModifier_ShouldReturnBonusForCommercial()
        {
            // Arrange
            var effects = new ZoneTraitEffects();

            // Act
            var modifier = effects.GetCashModifier(ZoneTrait.Commercial);

            // Assert - Commercial zones should boost cash generation
            Assert.True(modifier > 1.0f);
        }

        [Fact]
        public void GetCashModifier_ShouldReturnBonusForHighValue()
        {
            // Arrange
            var effects = new ZoneTraitEffects();

            // Act
            var modifier = effects.GetCashModifier(ZoneTrait.HighValue);

            // Assert - High-value zones should boost all resources including cash
            Assert.True(modifier > 1.0f);
        }

        [Fact]
        public void GetCashModifier_ShouldStackForCombinedTraits()
        {
            // Arrange
            var effects = new ZoneTraitEffects();
            var combinedTraits = ZoneTrait.Commercial | ZoneTrait.HighValue;

            // Act
            var combinedModifier = effects.GetCashModifier(combinedTraits);
            var commercialOnly = effects.GetCashModifier(ZoneTrait.Commercial);
            var highValueOnly = effects.GetCashModifier(ZoneTrait.HighValue);

            // Assert - Combined traits should provide greater bonus than either alone
            Assert.True(combinedModifier > commercialOnly);
            Assert.True(combinedModifier > highValueOnly);
        }

        #endregion

        #region ZoneTraitEffects - Recruitment Modifier

        [Fact]
        public void GetRecruitmentModifier_ShouldReturnBaselineForNone()
        {
            // Arrange
            var effects = new ZoneTraitEffects();

            // Act
            var modifier = effects.GetRecruitmentModifier(ZoneTrait.None);

            // Assert
            Assert.Equal(1.0f, modifier);
        }

        [Fact]
        public void GetRecruitmentModifier_ShouldReturnBonusForResidential()
        {
            // Arrange
            var effects = new ZoneTraitEffects();

            // Act
            var modifier = effects.GetRecruitmentModifier(ZoneTrait.Residential);

            // Assert - Residential zones should boost recruitment
            Assert.True(modifier > 1.0f);
        }

        [Fact]
        public void GetRecruitmentModifier_ShouldReturnBonusForHighValue()
        {
            // Arrange
            var effects = new ZoneTraitEffects();

            // Act
            var modifier = effects.GetRecruitmentModifier(ZoneTrait.HighValue);

            // Assert - High-value should boost recruitment too
            Assert.True(modifier > 1.0f);
        }

        #endregion

        #region ZoneTraitEffects - Weapons Modifier

        [Fact]
        public void GetWeaponsModifier_ShouldReturnBaselineForNone()
        {
            // Arrange
            var effects = new ZoneTraitEffects();

            // Act
            var modifier = effects.GetWeaponsModifier(ZoneTrait.None);

            // Assert
            Assert.Equal(1.0f, modifier);
        }

        [Fact]
        public void GetWeaponsModifier_ShouldReturnBonusForIndustrial()
        {
            // Arrange
            var effects = new ZoneTraitEffects();

            // Act
            var modifier = effects.GetWeaponsModifier(ZoneTrait.Industrial);

            // Assert - Industrial zones should boost weapons production
            Assert.True(modifier > 1.0f);
        }

        [Fact]
        public void GetWeaponsModifier_ShouldReturnBonusForPort()
        {
            // Arrange
            var effects = new ZoneTraitEffects();

            // Act
            var modifier = effects.GetWeaponsModifier(ZoneTrait.Port);

            // Assert - Ports enable smuggling which boosts weapons availability
            Assert.True(modifier > 1.0f);
        }

        #endregion

        #region ZoneTraitEffects - Defense Modifier

        [Fact]
        public void GetDefenseModifier_ShouldReturnBaselineForNone()
        {
            // Arrange
            var effects = new ZoneTraitEffects();

            // Act
            var modifier = effects.GetDefenseModifier(ZoneTrait.None);

            // Assert
            Assert.Equal(1.0f, modifier);
        }

        [Fact]
        public void GetDefenseModifier_ShouldReturnBonusForFortified()
        {
            // Arrange
            var effects = new ZoneTraitEffects();

            // Act
            var modifier = effects.GetDefenseModifier(ZoneTrait.Fortified);

            // Assert - Fortified zones should provide significant defense bonus
            Assert.True(modifier > 1.0f);
        }

        [Fact]
        public void GetDefenseModifier_ShouldReturnSignificantBonusForFortified()
        {
            // Arrange
            var effects = new ZoneTraitEffects();

            // Act
            var modifier = effects.GetDefenseModifier(ZoneTrait.Fortified);

            // Assert - Defense bonus should be at least 25%
            Assert.True(modifier >= 1.25f);
        }

        #endregion

        #region ZoneTraitEffects - Reinforcement Speed Modifier

        [Fact]
        public void GetReinforcementSpeedModifier_ShouldReturnBaselineForNone()
        {
            // Arrange
            var effects = new ZoneTraitEffects();

            // Act
            var modifier = effects.GetReinforcementSpeedModifier(ZoneTrait.None);

            // Assert
            Assert.Equal(1.0f, modifier);
        }

        [Fact]
        public void GetReinforcementSpeedModifier_ShouldReturnBonusForAirfield()
        {
            // Arrange
            var effects = new ZoneTraitEffects();

            // Act
            var modifier = effects.GetReinforcementSpeedModifier(ZoneTrait.Airfield);

            // Assert - Airfields enable rapid deployment
            Assert.True(modifier > 1.0f);
        }

        [Fact]
        public void GetReinforcementSpeedModifier_ShouldReturnBonusForPort()
        {
            // Arrange
            var effects = new ZoneTraitEffects();

            // Act
            var modifier = effects.GetReinforcementSpeedModifier(ZoneTrait.Port);

            // Assert - Ports also speed up reinforcements via sea
            Assert.True(modifier > 1.0f);
        }

        #endregion

        #region ZoneTraitEffects - Description

        [Fact]
        public void GetDescription_ShouldReturnEmptyForNone()
        {
            // Arrange
            var effects = new ZoneTraitEffects();

            // Act
            var description = effects.GetDescription(ZoneTrait.None);

            // Assert
            Assert.Equal(string.Empty, description);
        }

        [Fact]
        public void GetDescription_ShouldReturnNonEmptyForValidTrait()
        {
            // Arrange
            var effects = new ZoneTraitEffects();

            // Act
            var description = effects.GetDescription(ZoneTrait.Industrial);

            // Assert
            Assert.False(string.IsNullOrEmpty(description));
        }

        [Fact]
        public void GetDescription_ShouldReturnCombinedForMultipleTraits()
        {
            // Arrange
            var effects = new ZoneTraitEffects();
            var combinedTraits = ZoneTrait.Industrial | ZoneTrait.Fortified;

            // Act
            var description = effects.GetDescription(combinedTraits);

            // Assert - Should contain information about both traits
            Assert.Contains("Industrial", description, StringComparison.OrdinalIgnoreCase);
        }

        #endregion

        #region ZoneTraitEffects - GetActiveTraits

        [Fact]
        public void GetActiveTraits_ShouldReturnEmptyForNone()
        {
            // Arrange
            var effects = new ZoneTraitEffects();

            // Act
            var activeTraits = effects.GetActiveTraits(ZoneTrait.None);

            // Assert
            Assert.Empty(activeTraits);
        }

        [Fact]
        public void GetActiveTraits_ShouldReturnSingleTraitForSingleFlag()
        {
            // Arrange
            var effects = new ZoneTraitEffects();

            // Act
            var activeTraits = effects.GetActiveTraits(ZoneTrait.Industrial);

            // Assert
            Assert.Single(activeTraits);
            Assert.Contains(ZoneTrait.Industrial, activeTraits);
        }

        [Fact]
        public void GetActiveTraits_ShouldReturnAllTraitsForCombinedFlags()
        {
            // Arrange
            var effects = new ZoneTraitEffects();
            var combined = ZoneTrait.Industrial | ZoneTrait.Fortified | ZoneTrait.Commercial;

            // Act
            var activeTraits = effects.GetActiveTraits(combined);

            // Assert
            Assert.Equal(3, activeTraits.Count());
            Assert.Contains(ZoneTrait.Industrial, activeTraits);
            Assert.Contains(ZoneTrait.Fortified, activeTraits);
            Assert.Contains(ZoneTrait.Commercial, activeTraits);
        }

        #endregion

        #region Zone Integration with Traits

        [Fact]
        public void Zone_ShouldHaveTraitsProperty()
        {
            // Arrange
            var zone = new Zone("zone_1", "Downtown", new FactionWars.Core.Interfaces.Vector3(0, 0, 0));

            // Act & Assert - Zone should have a Traits property defaulting to None
            Assert.Equal(ZoneTrait.None, zone.Traits);
        }

        [Fact]
        public void Zone_ShouldAllowSettingTraits()
        {
            // Arrange
            var zone = new Zone("zone_1", "Downtown", new FactionWars.Core.Interfaces.Vector3(0, 0, 0));

            // Act
            zone.Traits = ZoneTrait.Industrial | ZoneTrait.Fortified;

            // Assert
            Assert.True(zone.Traits.HasFlag(ZoneTrait.Industrial));
            Assert.True(zone.Traits.HasFlag(ZoneTrait.Fortified));
        }

        #endregion
    }
}
