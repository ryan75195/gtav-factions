using FactionWars.Economy.Interfaces;
using FactionWars.Economy.Models;
using FactionWars.Economy.Services;
using FactionWars.Territory.Models;
using System;
using Xunit;

namespace FactionWars.Tests.Unit.Economy
{
    /// <summary>
    /// Tests for the zone trait resource modifier system.
    /// Verifies that zone traits correctly affect resource generation rates.
    /// </summary>
    public class ZoneTraitResourceModifierTests
    {
        private readonly IZoneTraitResourceModifier _modifier;

        public ZoneTraitResourceModifierTests()
        {
            _modifier = new ZoneTraitResourceModifier();
        }

        #region GetModifier Basic Tests

        [Fact]
        public void GetModifier_NoTraits_ReturnsBaselineMultiplier()
        {
            // A zone with no traits should return 1.0 (baseline)
            var modifier = _modifier.GetModifier(ZoneTrait.None, ResourceType.Cash);

            Assert.Equal(1.0f, modifier);
        }

        [Theory]
        [InlineData(ResourceType.Cash)]
        [InlineData(ResourceType.Recruitment)]
        [InlineData(ResourceType.Weapons)]
        public void GetModifier_NoTraits_ReturnsBaselineForAllResourceTypes(ResourceType resourceType)
        {
            // All resource types should return 1.0 when no traits
            var modifier = _modifier.GetModifier(ZoneTrait.None, resourceType);

            Assert.Equal(1.0f, modifier);
        }

        #endregion

        #region Commercial Trait Tests

        [Fact]
        public void GetModifier_CommercialTrait_BoostsCashBy50Percent()
        {
            // Commercial zones provide +50% cash bonus
            var modifier = _modifier.GetModifier(ZoneTrait.Commercial, ResourceType.Cash);

            Assert.Equal(1.5f, modifier);
        }

        [Fact]
        public void GetModifier_CommercialTrait_DoesNotAffectRecruitment()
        {
            var modifier = _modifier.GetModifier(ZoneTrait.Commercial, ResourceType.Recruitment);

            Assert.Equal(1.0f, modifier);
        }

        [Fact]
        public void GetModifier_CommercialTrait_DoesNotAffectWeapons()
        {
            var modifier = _modifier.GetModifier(ZoneTrait.Commercial, ResourceType.Weapons);

            Assert.Equal(1.0f, modifier);
        }

        #endregion

        #region Residential Trait Tests

        [Fact]
        public void GetModifier_ResidentialTrait_BoostsRecruitmentBy50Percent()
        {
            // Residential zones provide +50% recruitment bonus
            var modifier = _modifier.GetModifier(ZoneTrait.Residential, ResourceType.Recruitment);

            Assert.Equal(1.5f, modifier);
        }

        [Fact]
        public void GetModifier_ResidentialTrait_DoesNotAffectCash()
        {
            var modifier = _modifier.GetModifier(ZoneTrait.Residential, ResourceType.Cash);

            Assert.Equal(1.0f, modifier);
        }

        [Fact]
        public void GetModifier_ResidentialTrait_DoesNotAffectWeapons()
        {
            var modifier = _modifier.GetModifier(ZoneTrait.Residential, ResourceType.Weapons);

            Assert.Equal(1.0f, modifier);
        }

        #endregion

        #region Industrial Trait Tests

        [Fact]
        public void GetModifier_IndustrialTrait_BoostsWeaponsBy50Percent()
        {
            // Industrial zones provide +50% weapons bonus
            var modifier = _modifier.GetModifier(ZoneTrait.Industrial, ResourceType.Weapons);

            Assert.Equal(1.5f, modifier);
        }

        [Fact]
        public void GetModifier_IndustrialTrait_DoesNotAffectCash()
        {
            var modifier = _modifier.GetModifier(ZoneTrait.Industrial, ResourceType.Cash);

            Assert.Equal(1.0f, modifier);
        }

        [Fact]
        public void GetModifier_IndustrialTrait_DoesNotAffectRecruitment()
        {
            var modifier = _modifier.GetModifier(ZoneTrait.Industrial, ResourceType.Recruitment);

            Assert.Equal(1.0f, modifier);
        }

        #endregion

        #region Port Trait Tests

        [Fact]
        public void GetModifier_PortTrait_BoostsWeaponsBy25Percent()
        {
            // Port zones provide +25% weapons (smuggling)
            var modifier = _modifier.GetModifier(ZoneTrait.Port, ResourceType.Weapons);

            Assert.Equal(1.25f, modifier);
        }

        [Fact]
        public void GetModifier_PortTrait_BoostsCashBy25Percent()
        {
            // Port zones provide +25% cash (trade)
            var modifier = _modifier.GetModifier(ZoneTrait.Port, ResourceType.Cash);

            Assert.Equal(1.25f, modifier);
        }

        [Fact]
        public void GetModifier_PortTrait_DoesNotAffectRecruitment()
        {
            var modifier = _modifier.GetModifier(ZoneTrait.Port, ResourceType.Recruitment);

            Assert.Equal(1.0f, modifier);
        }

        #endregion

        #region HighValue Trait Tests

        [Fact]
        public void GetModifier_HighValueTrait_DoublesAllResources()
        {
            // HighValue zones multiply all resources by 2x
            Assert.Equal(2.0f, _modifier.GetModifier(ZoneTrait.HighValue, ResourceType.Cash));
            Assert.Equal(2.0f, _modifier.GetModifier(ZoneTrait.HighValue, ResourceType.Recruitment));
            Assert.Equal(2.0f, _modifier.GetModifier(ZoneTrait.HighValue, ResourceType.Weapons));
        }

        #endregion

        #region Non-Resource Trait Tests

        [Theory]
        [InlineData(ZoneTrait.Fortified)]
        [InlineData(ZoneTrait.Airfield)]
        public void GetModifier_NonResourceTrait_ReturnsBaseline(ZoneTrait trait)
        {
            // Combat/mobility traits should not affect resource generation
            Assert.Equal(1.0f, _modifier.GetModifier(trait, ResourceType.Cash));
            Assert.Equal(1.0f, _modifier.GetModifier(trait, ResourceType.Recruitment));
            Assert.Equal(1.0f, _modifier.GetModifier(trait, ResourceType.Weapons));
        }

        #endregion

        #region Combined Traits Tests

        [Fact]
        public void GetModifier_CommercialAndHighValue_StacksCorrectly()
        {
            // HighValue (2x) then Commercial (+50%) = 3.0x
            var traits = ZoneTrait.Commercial | ZoneTrait.HighValue;

            var modifier = _modifier.GetModifier(traits, ResourceType.Cash);

            Assert.Equal(3.0f, modifier);
        }

        [Fact]
        public void GetModifier_IndustrialAndPort_StacksWeaponsBonuses()
        {
            // Industrial (+50%) and Port (+25%) = +75% = 1.75x
            var traits = ZoneTrait.Industrial | ZoneTrait.Port;

            var modifier = _modifier.GetModifier(traits, ResourceType.Weapons);

            Assert.Equal(1.75f, modifier);
        }

        [Fact]
        public void GetModifier_CommercialAndPort_StacksCashBonuses()
        {
            // Commercial (+50%) and Port (+25%) = +75% = 1.75x
            var traits = ZoneTrait.Commercial | ZoneTrait.Port;

            var modifier = _modifier.GetModifier(traits, ResourceType.Cash);

            Assert.Equal(1.75f, modifier);
        }

        [Fact]
        public void GetModifier_AllResourceTraits_StacksCorrectly()
        {
            // Commercial + Residential + Industrial should each apply to their specific resource
            var traits = ZoneTrait.Commercial | ZoneTrait.Residential | ZoneTrait.Industrial;

            Assert.Equal(1.5f, _modifier.GetModifier(traits, ResourceType.Cash));
            Assert.Equal(1.5f, _modifier.GetModifier(traits, ResourceType.Recruitment));
            Assert.Equal(1.5f, _modifier.GetModifier(traits, ResourceType.Weapons));
        }

        [Fact]
        public void GetModifier_HighValueWithAllResourceTraits_StacksMultiplicatively()
        {
            // HighValue (2x) then each resource trait (+50%) = 3.0x for each
            var traits = ZoneTrait.Commercial | ZoneTrait.Residential | ZoneTrait.Industrial | ZoneTrait.HighValue;

            Assert.Equal(3.0f, _modifier.GetModifier(traits, ResourceType.Cash));
            Assert.Equal(3.0f, _modifier.GetModifier(traits, ResourceType.Recruitment));
            Assert.Equal(3.0f, _modifier.GetModifier(traits, ResourceType.Weapons));
        }

        [Fact]
        public void GetModifier_IndustrialPortHighValue_StacksForWeapons()
        {
            // HighValue (2x) then Industrial (+50%) + Port (+25%) = 2x * 1.75 = 3.5x
            var traits = ZoneTrait.Industrial | ZoneTrait.Port | ZoneTrait.HighValue;

            var modifier = _modifier.GetModifier(traits, ResourceType.Weapons);

            Assert.Equal(3.5f, modifier);
        }

        [Fact]
        public void GetModifier_CombatTraitsWithResourceTraits_OnlyResourceTraitsApply()
        {
            // Fortified and Airfield shouldn't affect resource calculation
            var traits = ZoneTrait.Commercial | ZoneTrait.Fortified | ZoneTrait.Airfield;

            Assert.Equal(1.5f, _modifier.GetModifier(traits, ResourceType.Cash));
            Assert.Equal(1.0f, _modifier.GetModifier(traits, ResourceType.Recruitment));
            Assert.Equal(1.0f, _modifier.GetModifier(traits, ResourceType.Weapons));
        }

        #endregion

        #region Edge Cases

        [Fact]
        public void GetModifier_InvalidResourceType_ThrowsArgumentException()
        {
            var invalidResourceType = (ResourceType)999;

            Assert.Throws<ArgumentException>(() =>
                _modifier.GetModifier(ZoneTrait.None, invalidResourceType));
        }

        #endregion

        #region GetTotalModifier Tests

        [Fact]
        public void GetTotalModifier_ReturnsAllResourceModifiers()
        {
            var traits = ZoneTrait.Commercial | ZoneTrait.Residential | ZoneTrait.Industrial;

            var modifiers = _modifier.GetTotalModifier(traits);

            Assert.Equal(3, modifiers.Count);
            Assert.Equal(1.5f, modifiers[ResourceType.Cash]);
            Assert.Equal(1.5f, modifiers[ResourceType.Recruitment]);
            Assert.Equal(1.5f, modifiers[ResourceType.Weapons]);
        }

        [Fact]
        public void GetTotalModifier_NoTraits_ReturnsBaselineForAll()
        {
            var modifiers = _modifier.GetTotalModifier(ZoneTrait.None);

            Assert.Equal(1.0f, modifiers[ResourceType.Cash]);
            Assert.Equal(1.0f, modifiers[ResourceType.Recruitment]);
            Assert.Equal(1.0f, modifiers[ResourceType.Weapons]);
        }

        [Fact]
        public void GetTotalModifier_HighValue_DoublesAllResources()
        {
            var modifiers = _modifier.GetTotalModifier(ZoneTrait.HighValue);

            Assert.Equal(2.0f, modifiers[ResourceType.Cash]);
            Assert.Equal(2.0f, modifiers[ResourceType.Recruitment]);
            Assert.Equal(2.0f, modifiers[ResourceType.Weapons]);
        }

        #endregion

        #region HasResourceBonus Tests

        [Fact]
        public void HasResourceBonus_CommercialTrait_ReturnsTrueForCash()
        {
            Assert.True(_modifier.HasResourceBonus(ZoneTrait.Commercial, ResourceType.Cash));
        }

        [Fact]
        public void HasResourceBonus_CommercialTrait_ReturnsFalseForOtherResources()
        {
            Assert.False(_modifier.HasResourceBonus(ZoneTrait.Commercial, ResourceType.Recruitment));
            Assert.False(_modifier.HasResourceBonus(ZoneTrait.Commercial, ResourceType.Weapons));
        }

        [Fact]
        public void HasResourceBonus_HighValueTrait_ReturnsTrueForAllResources()
        {
            Assert.True(_modifier.HasResourceBonus(ZoneTrait.HighValue, ResourceType.Cash));
            Assert.True(_modifier.HasResourceBonus(ZoneTrait.HighValue, ResourceType.Recruitment));
            Assert.True(_modifier.HasResourceBonus(ZoneTrait.HighValue, ResourceType.Weapons));
        }

        [Fact]
        public void HasResourceBonus_FortifiedTrait_ReturnsFalseForAllResources()
        {
            Assert.False(_modifier.HasResourceBonus(ZoneTrait.Fortified, ResourceType.Cash));
            Assert.False(_modifier.HasResourceBonus(ZoneTrait.Fortified, ResourceType.Recruitment));
            Assert.False(_modifier.HasResourceBonus(ZoneTrait.Fortified, ResourceType.Weapons));
        }

        [Fact]
        public void HasResourceBonus_NoTraits_ReturnsFalseForAllResources()
        {
            Assert.False(_modifier.HasResourceBonus(ZoneTrait.None, ResourceType.Cash));
            Assert.False(_modifier.HasResourceBonus(ZoneTrait.None, ResourceType.Recruitment));
            Assert.False(_modifier.HasResourceBonus(ZoneTrait.None, ResourceType.Weapons));
        }

        #endregion
    }
}
