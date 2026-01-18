using FactionWars.Economy.Models;
using FactionWars.Economy.Services;
using FactionWars.Territory.Models;
using FactionWars.Core.Interfaces;
using System;
using Xunit;

namespace FactionWars.Tests.Unit.Economy
{
    public class ResourceGenerationCalculatorTests
    {
        private readonly ResourceGenerationCalculator _calculator;

        public ResourceGenerationCalculatorTests()
        {
            _calculator = new ResourceGenerationCalculator();
        }

        #region Basic Generation Tests

        [Fact]
        public void CalculateGeneration_NullZone_ThrowsArgumentNullException()
        {
            // A null zone should throw an exception
            Assert.Throws<ArgumentNullException>(() =>
                _calculator.CalculateGeneration(null!, ResourceType.Cash));
        }

        [Fact]
        public void CalculateGeneration_ZoneWithNoTraits_ReturnsBaseRate()
        {
            // A zone with no special traits should generate at base rate
            var zone = CreateTestZone(traits: ZoneTrait.None, strategicValue: 1);

            var cashGeneration = _calculator.CalculateGeneration(zone, ResourceType.Cash);

            Assert.Equal(100, cashGeneration); // Base rate for cash is 100
        }

        [Theory]
        [InlineData(ResourceType.Cash, 100)]
        [InlineData(ResourceType.Recruitment, 10)]
        [InlineData(ResourceType.Weapons, 5)]
        public void CalculateGeneration_ZoneWithNoTraits_ReturnsCorrectBaseRates(
            ResourceType resourceType, int expectedBase)
        {
            // Each resource type has its own base generation rate
            var zone = CreateTestZone(traits: ZoneTrait.None, strategicValue: 1);

            var generation = _calculator.CalculateGeneration(zone, resourceType);

            Assert.Equal(expectedBase, generation);
        }

        #endregion

        #region Strategic Value Multiplier Tests

        [Theory]
        [InlineData(1, 100)]
        [InlineData(2, 200)]
        [InlineData(5, 500)]
        [InlineData(10, 1000)]
        public void CalculateGeneration_StrategicValue_MultipliesBaseRate(
            int strategicValue, int expectedCash)
        {
            // Strategic value acts as a multiplier for resource generation
            var zone = CreateTestZone(traits: ZoneTrait.None, strategicValue: strategicValue);

            var generation = _calculator.CalculateGeneration(zone, ResourceType.Cash);

            Assert.Equal(expectedCash, generation);
        }

        [Fact]
        public void CalculateGeneration_StrategicValue_AffectsAllResourceTypes()
        {
            // Strategic value should multiply all resource types
            var zone = CreateTestZone(traits: ZoneTrait.None, strategicValue: 3);

            Assert.Equal(300, _calculator.CalculateGeneration(zone, ResourceType.Cash));
            Assert.Equal(30, _calculator.CalculateGeneration(zone, ResourceType.Recruitment));
            Assert.Equal(15, _calculator.CalculateGeneration(zone, ResourceType.Weapons));
        }

        #endregion

        #region Zone Trait Bonus Tests - Commercial

        [Fact]
        public void CalculateGeneration_CommercialTrait_BoostsCashGeneration()
        {
            // Commercial zones provide a 50% bonus to cash generation
            var zone = CreateTestZone(traits: ZoneTrait.Commercial, strategicValue: 1);

            var generation = _calculator.CalculateGeneration(zone, ResourceType.Cash);

            Assert.Equal(150, generation); // 100 base + 50% bonus = 150
        }

        [Fact]
        public void CalculateGeneration_CommercialTrait_DoesNotAffectOtherResources()
        {
            // Commercial trait only boosts cash, not other resources
            var zone = CreateTestZone(traits: ZoneTrait.Commercial, strategicValue: 1);

            Assert.Equal(10, _calculator.CalculateGeneration(zone, ResourceType.Recruitment));
            Assert.Equal(5, _calculator.CalculateGeneration(zone, ResourceType.Weapons));
        }

        #endregion

        #region Zone Trait Bonus Tests - Residential

        [Fact]
        public void CalculateGeneration_ResidentialTrait_BoostsRecruitmentGeneration()
        {
            // Residential zones provide a 50% bonus to recruitment
            var zone = CreateTestZone(traits: ZoneTrait.Residential, strategicValue: 1);

            var generation = _calculator.CalculateGeneration(zone, ResourceType.Recruitment);

            Assert.Equal(15, generation); // 10 base + 50% bonus = 15
        }

        [Fact]
        public void CalculateGeneration_ResidentialTrait_DoesNotAffectOtherResources()
        {
            // Residential trait only boosts recruitment, not other resources
            var zone = CreateTestZone(traits: ZoneTrait.Residential, strategicValue: 1);

            Assert.Equal(100, _calculator.CalculateGeneration(zone, ResourceType.Cash));
            Assert.Equal(5, _calculator.CalculateGeneration(zone, ResourceType.Weapons));
        }

        #endregion

        #region Zone Trait Bonus Tests - Industrial

        [Fact]
        public void CalculateGeneration_IndustrialTrait_BoostsWeaponsGeneration()
        {
            // Industrial zones provide a 50% bonus to weapons production
            var zone = CreateTestZone(traits: ZoneTrait.Industrial, strategicValue: 1);

            var generation = _calculator.CalculateGeneration(zone, ResourceType.Weapons);

            Assert.Equal(7, generation); // 5 base + 50% (rounded down) = 7
        }

        [Fact]
        public void CalculateGeneration_IndustrialTrait_DoesNotAffectOtherResources()
        {
            // Industrial trait only boosts weapons, not other resources
            var zone = CreateTestZone(traits: ZoneTrait.Industrial, strategicValue: 1);

            Assert.Equal(100, _calculator.CalculateGeneration(zone, ResourceType.Cash));
            Assert.Equal(10, _calculator.CalculateGeneration(zone, ResourceType.Recruitment));
        }

        #endregion

        #region Zone Trait Bonus Tests - Port

        [Fact]
        public void CalculateGeneration_PortTrait_BoostsWeaponsGeneration()
        {
            // Port zones provide a 25% bonus to weapons (smuggling)
            var zone = CreateTestZone(traits: ZoneTrait.Port, strategicValue: 1);

            var generation = _calculator.CalculateGeneration(zone, ResourceType.Weapons);

            Assert.Equal(6, generation); // 5 base + 25% (rounded down) = 6
        }

        [Fact]
        public void CalculateGeneration_PortTrait_BoostsCashGeneration()
        {
            // Port zones also provide a 25% bonus to cash (trade)
            var zone = CreateTestZone(traits: ZoneTrait.Port, strategicValue: 1);

            var generation = _calculator.CalculateGeneration(zone, ResourceType.Cash);

            Assert.Equal(125, generation); // 100 base + 25% = 125
        }

        #endregion

        #region Zone Trait Bonus Tests - HighValue

        [Fact]
        public void CalculateGeneration_HighValueTrait_BoostsAllResources()
        {
            // High-value zones multiply all resource generation by 2x
            var zone = CreateTestZone(traits: ZoneTrait.HighValue, strategicValue: 1);

            Assert.Equal(200, _calculator.CalculateGeneration(zone, ResourceType.Cash));
            Assert.Equal(20, _calculator.CalculateGeneration(zone, ResourceType.Recruitment));
            Assert.Equal(10, _calculator.CalculateGeneration(zone, ResourceType.Weapons));
        }

        #endregion

        #region Multiple Traits Combined Tests

        [Fact]
        public void CalculateGeneration_CommercialAndHighValue_StackBonuses()
        {
            // Traits should stack: Commercial (+50%) and HighValue (2x)
            // Order: Base 100 -> HighValue (x2) = 200 -> Commercial (+50%) = 300
            var zone = CreateTestZone(
                traits: ZoneTrait.Commercial | ZoneTrait.HighValue,
                strategicValue: 1);

            var generation = _calculator.CalculateGeneration(zone, ResourceType.Cash);

            Assert.Equal(300, generation);
        }

        [Fact]
        public void CalculateGeneration_IndustrialAndPort_StackWeaponsBonuses()
        {
            // Industrial (+50%) and Port (+25%) should stack for weapons
            // Base 5 -> Industrial (+50%) = 7 -> Port (+25%) = 8 (rounded)
            var zone = CreateTestZone(
                traits: ZoneTrait.Industrial | ZoneTrait.Port,
                strategicValue: 1);

            var generation = _calculator.CalculateGeneration(zone, ResourceType.Weapons);

            Assert.Equal(8, generation);
        }

        [Fact]
        public void CalculateGeneration_AllResourceBonusTraits_ApplyCorrectly()
        {
            // A zone with Commercial, Residential, and Industrial
            var zone = CreateTestZone(
                traits: ZoneTrait.Commercial | ZoneTrait.Residential | ZoneTrait.Industrial,
                strategicValue: 1);

            // Cash: 100 + 50% = 150
            Assert.Equal(150, _calculator.CalculateGeneration(zone, ResourceType.Cash));
            // Recruitment: 10 + 50% = 15
            Assert.Equal(15, _calculator.CalculateGeneration(zone, ResourceType.Recruitment));
            // Weapons: 5 + 50% = 7
            Assert.Equal(7, _calculator.CalculateGeneration(zone, ResourceType.Weapons));
        }

        #endregion

        #region Strategic Value and Traits Combined Tests

        [Fact]
        public void CalculateGeneration_StrategicValueAndTraits_CombineCorrectly()
        {
            // Strategic value 2 with Commercial trait
            // Base 100 * 2 (strategic) = 200 -> +50% (commercial) = 300
            var zone = CreateTestZone(
                traits: ZoneTrait.Commercial,
                strategicValue: 2);

            var generation = _calculator.CalculateGeneration(zone, ResourceType.Cash);

            Assert.Equal(300, generation);
        }

        [Fact]
        public void CalculateGeneration_HighStrategicValueAndHighValueTrait()
        {
            // Strategic value 5 with HighValue trait
            // Base 100 * 5 = 500 -> x2 (HighValue) = 1000
            var zone = CreateTestZone(
                traits: ZoneTrait.HighValue,
                strategicValue: 5);

            var generation = _calculator.CalculateGeneration(zone, ResourceType.Cash);

            Assert.Equal(1000, generation);
        }

        #endregion

        #region Non-Resource Affecting Traits Tests

        [Fact]
        public void CalculateGeneration_FortifiedTrait_DoesNotAffectResourceGeneration()
        {
            // Fortified is a combat trait, shouldn't affect resources
            var zone = CreateTestZone(traits: ZoneTrait.Fortified, strategicValue: 1);

            Assert.Equal(100, _calculator.CalculateGeneration(zone, ResourceType.Cash));
            Assert.Equal(10, _calculator.CalculateGeneration(zone, ResourceType.Recruitment));
            Assert.Equal(5, _calculator.CalculateGeneration(zone, ResourceType.Weapons));
        }

        [Fact]
        public void CalculateGeneration_AirfieldTrait_DoesNotAffectResourceGeneration()
        {
            // Airfield is a mobility trait, shouldn't affect resources
            var zone = CreateTestZone(traits: ZoneTrait.Airfield, strategicValue: 1);

            Assert.Equal(100, _calculator.CalculateGeneration(zone, ResourceType.Cash));
            Assert.Equal(10, _calculator.CalculateGeneration(zone, ResourceType.Recruitment));
            Assert.Equal(5, _calculator.CalculateGeneration(zone, ResourceType.Weapons));
        }

        #endregion

        #region CalculateAllGeneration Tests

        [Fact]
        public void CalculateAllGeneration_NullZone_ThrowsArgumentNullException()
        {
            Assert.Throws<ArgumentNullException>(() =>
                _calculator.CalculateAllGeneration(null!));
        }

        [Fact]
        public void CalculateAllGeneration_ReturnsAllResourceTypes()
        {
            // Should return a result for each resource type
            var zone = CreateTestZone(traits: ZoneTrait.None, strategicValue: 1);

            var result = _calculator.CalculateAllGeneration(zone);

            Assert.Equal(3, result.Count);
            Assert.True(result.ContainsKey(ResourceType.Cash));
            Assert.True(result.ContainsKey(ResourceType.Recruitment));
            Assert.True(result.ContainsKey(ResourceType.Weapons));
        }

        [Fact]
        public void CalculateAllGeneration_BasicZone_ReturnsBaseRates()
        {
            var zone = CreateTestZone(traits: ZoneTrait.None, strategicValue: 1);

            var result = _calculator.CalculateAllGeneration(zone);

            Assert.Equal(100, result[ResourceType.Cash]);
            Assert.Equal(10, result[ResourceType.Recruitment]);
            Assert.Equal(5, result[ResourceType.Weapons]);
        }

        [Fact]
        public void CalculateAllGeneration_WithTraits_AppliesCorrectBonuses()
        {
            var zone = CreateTestZone(
                traits: ZoneTrait.Commercial | ZoneTrait.Residential | ZoneTrait.Industrial,
                strategicValue: 1);

            var result = _calculator.CalculateAllGeneration(zone);

            Assert.Equal(150, result[ResourceType.Cash]);
            Assert.Equal(15, result[ResourceType.Recruitment]);
            Assert.Equal(7, result[ResourceType.Weapons]);
        }

        #endregion

        #region Edge Cases

        [Fact]
        public void CalculateGeneration_InvalidResourceType_ThrowsArgumentException()
        {
            var zone = CreateTestZone(traits: ZoneTrait.None, strategicValue: 1);
            var invalidResourceType = (ResourceType)999;

            Assert.Throws<ArgumentException>(() =>
                _calculator.CalculateGeneration(zone, invalidResourceType));
        }

        [Fact]
        public void CalculateGeneration_ContestedZone_ReturnsNormalGeneration()
        {
            // Contested zones still generate resources (control affects this later)
            var zone = CreateTestZone(traits: ZoneTrait.None, strategicValue: 1);
            zone.IsContested = true;

            var generation = _calculator.CalculateGeneration(zone, ResourceType.Cash);

            Assert.Equal(100, generation);
        }

        #endregion

        #region Helper Methods

        private Zone CreateTestZone(ZoneTrait traits, int strategicValue)
        {
            var zone = new Zone(
                id: "test-zone",
                name: "Test Zone",
                center: new Vector3(0, 0, 0),
                radius: 100f,
                strategicValue: strategicValue);
            zone.Traits = traits;
            return zone;
        }

        #endregion
    }
}
