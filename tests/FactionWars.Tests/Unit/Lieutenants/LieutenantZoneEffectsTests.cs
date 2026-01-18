using System;
using System.Collections.Generic;
using FactionWars.Lieutenants.Interfaces;
using FactionWars.Lieutenants.Models;
using FactionWars.Lieutenants.Services;
using Xunit;

namespace FactionWars.Tests.Unit.Lieutenants
{
    /// <summary>
    /// Tests for LieutenantZoneEffects which calculates zone bonuses
    /// provided by lieutenants based on their traits and level.
    /// </summary>
    public class LieutenantZoneEffectsTests
    {
        private readonly ILieutenantZoneEffects _effects;

        public LieutenantZoneEffectsTests()
        {
            _effects = new LieutenantZoneEffects();
        }

        #region Constructor and Initialization Tests

        [Fact]
        public void Constructor_CreatesInstance()
        {
            var effects = new LieutenantZoneEffects();
            Assert.NotNull(effects);
        }

        #endregion

        #region GetAttackBonus Tests

        [Fact]
        public void GetAttackBonus_NullLieutenant_ReturnsBaselineModifier()
        {
            float result = _effects.GetAttackBonus(null);
            Assert.Equal(1.0f, result);
        }

        [Fact]
        public void GetAttackBonus_LieutenantWithNoTraits_ReturnsBaselineModifier()
        {
            var lieutenant = new Lieutenant("lt-1", "Test Commander", "faction-1");
            float result = _effects.GetAttackBonus(lieutenant);
            Assert.Equal(1.0f, result);
        }

        [Fact]
        public void GetAttackBonus_LieutenantWithAggressiveTrait_ReturnsIncreasedModifier()
        {
            var lieutenant = new Lieutenant("lt-1", "Test Commander", "faction-1");
            lieutenant.AddTrait(LieutenantTrait.Aggressive);

            float result = _effects.GetAttackBonus(lieutenant);

            Assert.True(result > 1.0f);
        }

        [Fact]
        public void GetAttackBonus_LieutenantWithVeteranTrait_ReturnsIncreasedModifier()
        {
            var lieutenant = new Lieutenant("lt-1", "Test Commander", "faction-1");
            lieutenant.AddTrait(LieutenantTrait.Veteran);

            float result = _effects.GetAttackBonus(lieutenant);

            Assert.True(result > 1.0f);
        }

        [Fact]
        public void GetAttackBonus_LieutenantWithRuthlessTrait_ReturnsIncreasedModifier()
        {
            var lieutenant = new Lieutenant("lt-1", "Test Commander", "faction-1");
            lieutenant.AddTrait(LieutenantTrait.Ruthless);

            float result = _effects.GetAttackBonus(lieutenant);

            Assert.True(result > 1.0f);
        }

        [Fact]
        public void GetAttackBonus_MultipleAttackTraits_StacksAdditively()
        {
            var lieutenant = new Lieutenant("lt-1", "Test Commander", "faction-1");
            lieutenant.AddTrait(LieutenantTrait.Aggressive);
            lieutenant.AddTrait(LieutenantTrait.Ruthless);

            float singleTraitResult = _effects.GetAttackBonus(CreateLieutenantWithTrait(LieutenantTrait.Aggressive));
            float doubleTraitResult = _effects.GetAttackBonus(lieutenant);

            Assert.True(doubleTraitResult > singleTraitResult);
        }

        [Fact]
        public void GetAttackBonus_HigherLevel_IncreasesBonusMagnitude()
        {
            var lowLevelLt = new Lieutenant("lt-1", "Rookie", "faction-1");
            lowLevelLt.AddTrait(LieutenantTrait.Aggressive);

            var highLevelLt = new Lieutenant("lt-2", "Veteran", "faction-1");
            highLevelLt.AddTrait(LieutenantTrait.Aggressive);
            // Add enough experience to reach level 5 (1000 XP per level)
            highLevelLt.GainExperience(4000);

            float lowLevelResult = _effects.GetAttackBonus(lowLevelLt);
            float highLevelResult = _effects.GetAttackBonus(highLevelLt);

            Assert.True(highLevelResult > lowLevelResult);
        }

        #endregion

        #region GetDefenseBonus Tests

        [Fact]
        public void GetDefenseBonus_NullLieutenant_ReturnsBaselineModifier()
        {
            float result = _effects.GetDefenseBonus(null);
            Assert.Equal(1.0f, result);
        }

        [Fact]
        public void GetDefenseBonus_LieutenantWithNoTraits_ReturnsBaselineModifier()
        {
            var lieutenant = new Lieutenant("lt-1", "Test Commander", "faction-1");
            float result = _effects.GetDefenseBonus(lieutenant);
            Assert.Equal(1.0f, result);
        }

        [Fact]
        public void GetDefenseBonus_LieutenantWithDefensiveTrait_ReturnsIncreasedModifier()
        {
            var lieutenant = new Lieutenant("lt-1", "Test Commander", "faction-1");
            lieutenant.AddTrait(LieutenantTrait.Defensive);

            float result = _effects.GetDefenseBonus(lieutenant);

            Assert.True(result > 1.0f);
        }

        [Fact]
        public void GetDefenseBonus_LieutenantWithVeteranTrait_ReturnsIncreasedModifier()
        {
            var lieutenant = new Lieutenant("lt-1", "Test Commander", "faction-1");
            lieutenant.AddTrait(LieutenantTrait.Veteran);

            float result = _effects.GetDefenseBonus(lieutenant);

            Assert.True(result > 1.0f);
        }

        [Fact]
        public void GetDefenseBonus_HigherLevel_IncreasesBonusMagnitude()
        {
            var lowLevelLt = new Lieutenant("lt-1", "Rookie", "faction-1");
            lowLevelLt.AddTrait(LieutenantTrait.Defensive);

            var highLevelLt = new Lieutenant("lt-2", "Veteran", "faction-1");
            highLevelLt.AddTrait(LieutenantTrait.Defensive);
            highLevelLt.GainExperience(4000);

            float lowLevelResult = _effects.GetDefenseBonus(lowLevelLt);
            float highLevelResult = _effects.GetDefenseBonus(highLevelLt);

            Assert.True(highLevelResult > lowLevelResult);
        }

        #endregion

        #region GetResourceBonus Tests

        [Fact]
        public void GetResourceBonus_NullLieutenant_ReturnsBaselineModifier()
        {
            float result = _effects.GetResourceBonus(null);
            Assert.Equal(1.0f, result);
        }

        [Fact]
        public void GetResourceBonus_LieutenantWithNoTraits_ReturnsBaselineModifier()
        {
            var lieutenant = new Lieutenant("lt-1", "Test Commander", "faction-1");
            float result = _effects.GetResourceBonus(lieutenant);
            Assert.Equal(1.0f, result);
        }

        [Fact]
        public void GetResourceBonus_LieutenantWithResourcefulTrait_ReturnsIncreasedModifier()
        {
            var lieutenant = new Lieutenant("lt-1", "Test Commander", "faction-1");
            lieutenant.AddTrait(LieutenantTrait.Resourceful);

            float result = _effects.GetResourceBonus(lieutenant);

            Assert.True(result > 1.0f);
        }

        [Fact]
        public void GetResourceBonus_LieutenantWithCorruptTrait_ReturnsIncreasedCashModifier()
        {
            var lieutenant = new Lieutenant("lt-1", "Test Commander", "faction-1");
            lieutenant.AddTrait(LieutenantTrait.Corrupt);

            float result = _effects.GetResourceBonus(lieutenant);

            // Corrupt trait generates more cash (but affects bribery)
            Assert.True(result > 1.0f);
        }

        [Fact]
        public void GetResourceBonus_HigherLevel_IncreasesBonusMagnitude()
        {
            var lowLevelLt = new Lieutenant("lt-1", "Rookie", "faction-1");
            lowLevelLt.AddTrait(LieutenantTrait.Resourceful);

            var highLevelLt = new Lieutenant("lt-2", "Veteran", "faction-1");
            highLevelLt.AddTrait(LieutenantTrait.Resourceful);
            highLevelLt.GainExperience(4000);

            float lowLevelResult = _effects.GetResourceBonus(lowLevelLt);
            float highLevelResult = _effects.GetResourceBonus(highLevelLt);

            Assert.True(highLevelResult > lowLevelResult);
        }

        #endregion

        #region GetLoyaltyBonus Tests

        [Fact]
        public void GetLoyaltyBonus_NullLieutenant_ReturnsBaselineModifier()
        {
            float result = _effects.GetLoyaltyBonus(null);
            Assert.Equal(1.0f, result);
        }

        [Fact]
        public void GetLoyaltyBonus_LieutenantWithNoTraits_ReturnsBaselineModifier()
        {
            var lieutenant = new Lieutenant("lt-1", "Test Commander", "faction-1");
            float result = _effects.GetLoyaltyBonus(lieutenant);
            Assert.Equal(1.0f, result);
        }

        [Fact]
        public void GetLoyaltyBonus_LieutenantWithCharismaticTrait_ReturnsIncreasedModifier()
        {
            var lieutenant = new Lieutenant("lt-1", "Test Commander", "faction-1");
            lieutenant.AddTrait(LieutenantTrait.Charismatic);

            float result = _effects.GetLoyaltyBonus(lieutenant);

            Assert.True(result > 1.0f);
        }

        [Fact]
        public void GetLoyaltyBonus_LieutenantWithLoyalTrait_ReturnsIncreasedModifier()
        {
            var lieutenant = new Lieutenant("lt-1", "Test Commander", "faction-1");
            lieutenant.AddTrait(LieutenantTrait.Loyal);

            float result = _effects.GetLoyaltyBonus(lieutenant);

            Assert.True(result > 1.0f);
        }

        #endregion

        #region GetIntelligenceBonus Tests

        [Fact]
        public void GetIntelligenceBonus_NullLieutenant_ReturnsBaselineModifier()
        {
            float result = _effects.GetIntelligenceBonus(null);
            Assert.Equal(1.0f, result);
        }

        [Fact]
        public void GetIntelligenceBonus_LieutenantWithNoTraits_ReturnsBaselineModifier()
        {
            var lieutenant = new Lieutenant("lt-1", "Test Commander", "faction-1");
            float result = _effects.GetIntelligenceBonus(lieutenant);
            Assert.Equal(1.0f, result);
        }

        [Fact]
        public void GetIntelligenceBonus_LieutenantWithConnectedTrait_ReturnsIncreasedModifier()
        {
            var lieutenant = new Lieutenant("lt-1", "Test Commander", "faction-1");
            lieutenant.AddTrait(LieutenantTrait.Connected);

            float result = _effects.GetIntelligenceBonus(lieutenant);

            Assert.True(result > 1.0f);
        }

        [Fact]
        public void GetIntelligenceBonus_LieutenantWithCunningTrait_ReturnsIncreasedModifier()
        {
            var lieutenant = new Lieutenant("lt-1", "Test Commander", "faction-1");
            lieutenant.AddTrait(LieutenantTrait.Cunning);

            float result = _effects.GetIntelligenceBonus(lieutenant);

            Assert.True(result > 1.0f);
        }

        #endregion

        #region GetCovertOpsBonus Tests

        [Fact]
        public void GetCovertOpsBonus_NullLieutenant_ReturnsBaselineModifier()
        {
            float result = _effects.GetCovertOpsBonus(null);
            Assert.Equal(1.0f, result);
        }

        [Fact]
        public void GetCovertOpsBonus_LieutenantWithCunningTrait_ReturnsIncreasedModifier()
        {
            var lieutenant = new Lieutenant("lt-1", "Test Commander", "faction-1");
            lieutenant.AddTrait(LieutenantTrait.Cunning);

            float result = _effects.GetCovertOpsBonus(lieutenant);

            Assert.True(result > 1.0f);
        }

        #endregion

        #region GetAttackDeterrenceBonus Tests

        [Fact]
        public void GetAttackDeterrenceBonus_NullLieutenant_ReturnsBaselineModifier()
        {
            float result = _effects.GetAttackDeterrenceBonus(null);
            Assert.Equal(1.0f, result);
        }

        [Fact]
        public void GetAttackDeterrenceBonus_LieutenantWithIntimidatingTrait_ReturnsIncreasedModifier()
        {
            var lieutenant = new Lieutenant("lt-1", "Test Commander", "faction-1");
            lieutenant.AddTrait(LieutenantTrait.Intimidating);

            float result = _effects.GetAttackDeterrenceBonus(lieutenant);

            Assert.True(result > 1.0f);
        }

        [Fact]
        public void GetAttackDeterrenceBonus_LieutenantWithRuthlessTrait_ReturnsIncreasedModifier()
        {
            var lieutenant = new Lieutenant("lt-1", "Test Commander", "faction-1");
            lieutenant.AddTrait(LieutenantTrait.Ruthless);

            float result = _effects.GetAttackDeterrenceBonus(lieutenant);

            Assert.True(result > 1.0f);
        }

        #endregion

        #region GetExperienceGainBonus Tests

        [Fact]
        public void GetExperienceGainBonus_NullLieutenant_ReturnsBaselineModifier()
        {
            float result = _effects.GetExperienceGainBonus(null);
            Assert.Equal(1.0f, result);
        }

        [Fact]
        public void GetExperienceGainBonus_LieutenantWithVeteranTrait_ReturnsIncreasedModifier()
        {
            var lieutenant = new Lieutenant("lt-1", "Test Commander", "faction-1");
            lieutenant.AddTrait(LieutenantTrait.Veteran);

            float result = _effects.GetExperienceGainBonus(lieutenant);

            Assert.True(result > 1.0f);
        }

        #endregion

        #region GetAllZoneEffects Tests

        [Fact]
        public void GetAllZoneEffects_NullLieutenant_ReturnsAllBaselineModifiers()
        {
            var effects = _effects.GetAllZoneEffects(null);

            Assert.NotNull(effects);
            Assert.Equal(1.0f, effects.AttackBonus);
            Assert.Equal(1.0f, effects.DefenseBonus);
            Assert.Equal(1.0f, effects.ResourceBonus);
            Assert.Equal(1.0f, effects.LoyaltyBonus);
            Assert.Equal(1.0f, effects.IntelligenceBonus);
            Assert.Equal(1.0f, effects.CovertOpsBonus);
            Assert.Equal(1.0f, effects.AttackDeterrenceBonus);
            Assert.Equal(1.0f, effects.ExperienceGainBonus);
        }

        [Fact]
        public void GetAllZoneEffects_LieutenantWithMultipleTraits_ReturnsCorrectBonuses()
        {
            var lieutenant = new Lieutenant("lt-1", "Test Commander", "faction-1");
            lieutenant.AddTrait(LieutenantTrait.Aggressive);
            lieutenant.AddTrait(LieutenantTrait.Resourceful);
            lieutenant.AddTrait(LieutenantTrait.Connected);

            var effects = _effects.GetAllZoneEffects(lieutenant);

            Assert.True(effects.AttackBonus > 1.0f);
            Assert.True(effects.ResourceBonus > 1.0f);
            Assert.True(effects.IntelligenceBonus > 1.0f);
        }

        #endregion

        #region DeceasedLieutenant Tests

        [Fact]
        public void GetAttackBonus_DeceasedLieutenant_ReturnsBaselineModifier()
        {
            var lieutenant = new Lieutenant("lt-1", "Test Commander", "faction-1");
            lieutenant.AddTrait(LieutenantTrait.Aggressive);
            lieutenant.Kill();

            float result = _effects.GetAttackBonus(lieutenant);

            Assert.Equal(1.0f, result);
        }

        [Fact]
        public void GetAllZoneEffects_DeceasedLieutenant_ReturnsAllBaselineModifiers()
        {
            var lieutenant = new Lieutenant("lt-1", "Test Commander", "faction-1");
            lieutenant.AddTrait(LieutenantTrait.Aggressive);
            lieutenant.AddTrait(LieutenantTrait.Resourceful);
            lieutenant.Kill();

            var effects = _effects.GetAllZoneEffects(lieutenant);

            Assert.Equal(1.0f, effects.AttackBonus);
            Assert.Equal(1.0f, effects.ResourceBonus);
        }

        #endregion

        #region CapturedLieutenant Tests

        [Fact]
        public void GetAttackBonus_CapturedLieutenant_ReturnsBaselineModifier()
        {
            var lieutenant = new Lieutenant("lt-1", "Test Commander", "faction-1");
            lieutenant.AddTrait(LieutenantTrait.Aggressive);
            lieutenant.Capture("faction-2");

            float result = _effects.GetAttackBonus(lieutenant);

            Assert.Equal(1.0f, result);
        }

        #endregion

        #region BonusMagnitude Tests

        [Fact]
        public void GetAttackBonus_AggressiveTrait_ReturnsExpectedBonusValue()
        {
            var lieutenant = new Lieutenant("lt-1", "Test Commander", "faction-1");
            lieutenant.AddTrait(LieutenantTrait.Aggressive);

            float result = _effects.GetAttackBonus(lieutenant);

            // Level 1 Aggressive should give base bonus of 15%
            Assert.Equal(1.15f, result, 2);
        }

        [Fact]
        public void GetDefenseBonus_DefensiveTrait_ReturnsExpectedBonusValue()
        {
            var lieutenant = new Lieutenant("lt-1", "Test Commander", "faction-1");
            lieutenant.AddTrait(LieutenantTrait.Defensive);

            float result = _effects.GetDefenseBonus(lieutenant);

            // Level 1 Defensive should give base bonus of 15%
            Assert.Equal(1.15f, result, 2);
        }

        [Fact]
        public void GetResourceBonus_ResourcefulTrait_ReturnsExpectedBonusValue()
        {
            var lieutenant = new Lieutenant("lt-1", "Test Commander", "faction-1");
            lieutenant.AddTrait(LieutenantTrait.Resourceful);

            float result = _effects.GetResourceBonus(lieutenant);

            // Level 1 Resourceful should give base bonus of 20%
            Assert.Equal(1.20f, result, 2);
        }

        [Fact]
        public void GetAttackBonus_Level5Lieutenant_ReturnsScaledBonus()
        {
            var lieutenant = new Lieutenant("lt-1", "Test Commander", "faction-1");
            lieutenant.AddTrait(LieutenantTrait.Aggressive);
            lieutenant.GainExperience(4000); // Level 5

            float result = _effects.GetAttackBonus(lieutenant);

            // Level 5 should scale bonus by 1.4x (1 + 0.1 * (5-1))
            // 0.15 * 1.4 = 0.21 => 1.21
            Assert.Equal(1.21f, result, 2);
        }

        [Fact]
        public void GetAttackBonus_Level10Lieutenant_ReturnsMaxScaledBonus()
        {
            var lieutenant = new Lieutenant("lt-1", "Test Commander", "faction-1");
            lieutenant.AddTrait(LieutenantTrait.Aggressive);
            lieutenant.GainExperience(9000); // Level 10

            float result = _effects.GetAttackBonus(lieutenant);

            // Level 10 should scale bonus by 1.9x (1 + 0.1 * (10-1))
            // 0.15 * 1.9 = 0.285 => 1.285
            Assert.InRange(result, 1.28f, 1.29f);
        }

        #endregion

        #region Helper Methods

        private Lieutenant CreateLieutenantWithTrait(LieutenantTrait trait)
        {
            var lieutenant = new Lieutenant("lt-helper", "Helper", "faction-1");
            lieutenant.AddTrait(trait);
            return lieutenant;
        }

        #endregion
    }
}
