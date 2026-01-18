using System;
using FactionWars.Tension.Models;
using Xunit;

namespace FactionWars.Tests.Unit.Tension
{
    /// <summary>
    /// Tests for the DiplomaticActionEffect class which represents the effects
    /// of an active diplomatic action.
    /// </summary>
    public class DiplomaticActionEffectTests
    {
        private const string FactionMichael = "faction-michael";
        private const string FactionTrevor = "faction-trevor";

        #region Constructor Tests

        [Fact]
        public void Constructor_WithValidParameters_CreatesEffect()
        {
            var effect = new DiplomaticActionEffect(FactionMichael, FactionTrevor, DiplomaticActionType.Ceasefire);

            Assert.Equal(FactionMichael, effect.FactionId1);
            Assert.Equal(FactionTrevor, effect.FactionId2);
            Assert.Equal(DiplomaticActionType.Ceasefire, effect.ActionType);
        }

        [Fact]
        public void Constructor_SetsStartTime()
        {
            var before = DateTime.UtcNow;
            var effect = new DiplomaticActionEffect(FactionMichael, FactionTrevor, DiplomaticActionType.Ceasefire);
            var after = DateTime.UtcNow;

            Assert.InRange(effect.StartTime, before, after);
        }

        [Fact]
        public void Constructor_WithNullFaction1_ThrowsArgumentNullException()
        {
            Assert.Throws<ArgumentNullException>(() =>
                new DiplomaticActionEffect(null!, FactionTrevor, DiplomaticActionType.Ceasefire));
        }

        [Fact]
        public void Constructor_WithNullFaction2_ThrowsArgumentNullException()
        {
            Assert.Throws<ArgumentNullException>(() =>
                new DiplomaticActionEffect(FactionMichael, null!, DiplomaticActionType.Ceasefire));
        }

        [Fact]
        public void Constructor_WithEmptyFaction1_ThrowsArgumentException()
        {
            Assert.Throws<ArgumentException>(() =>
                new DiplomaticActionEffect("", FactionTrevor, DiplomaticActionType.Ceasefire));
        }

        [Fact]
        public void Constructor_WithEmptyFaction2_ThrowsArgumentException()
        {
            Assert.Throws<ArgumentException>(() =>
                new DiplomaticActionEffect(FactionMichael, "", DiplomaticActionType.Ceasefire));
        }

        [Fact]
        public void Constructor_WithSameFactions_ThrowsArgumentException()
        {
            Assert.Throws<ArgumentException>(() =>
                new DiplomaticActionEffect(FactionMichael, FactionMichael, DiplomaticActionType.Ceasefire));
        }

        #endregion

        #region Combat Modifier Tests

        [Fact]
        public void CombatModifier_ForCeasefire_IsZero()
        {
            var effect = new DiplomaticActionEffect(FactionMichael, FactionTrevor, DiplomaticActionType.Ceasefire);

            // Ceasefire prevents combat, so modifier is 0
            Assert.Equal(0f, effect.CombatModifier);
        }

        [Fact]
        public void CombatModifier_ForMutualDefense_IsPositive()
        {
            var effect = new DiplomaticActionEffect(FactionMichael, FactionTrevor, DiplomaticActionType.MutualDefense);

            Assert.True(effect.CombatModifier > 0);
        }

        [Fact]
        public void CombatModifier_ForAlliance_IsPositive()
        {
            var effect = new DiplomaticActionEffect(FactionMichael, FactionTrevor, DiplomaticActionType.Alliance);

            Assert.True(effect.CombatModifier > 0);
        }

        [Theory]
        [InlineData(DiplomaticActionType.Ceasefire, 0f)]
        [InlineData(DiplomaticActionType.NonAggressionPact, 0f)]
        [InlineData(DiplomaticActionType.TradeAgreement, 0f)]
        [InlineData(DiplomaticActionType.MutualDefense, 0.15f)]
        [InlineData(DiplomaticActionType.Alliance, 0.25f)]
        [InlineData(DiplomaticActionType.DeclarationOfWar, 0f)]
        [InlineData(DiplomaticActionType.PeaceTreaty, 0f)]
        [InlineData(DiplomaticActionType.TerritorialConcession, 0f)]
        public void CombatModifier_ReturnsExpectedValue(DiplomaticActionType actionType, float expectedModifier)
        {
            var effect = new DiplomaticActionEffect(FactionMichael, FactionTrevor, actionType);

            Assert.Equal(expectedModifier, effect.CombatModifier, 2);
        }

        #endregion

        #region Resource Modifier Tests

        [Fact]
        public void ResourceModifier_ForTradeAgreement_IsPositive()
        {
            var effect = new DiplomaticActionEffect(FactionMichael, FactionTrevor, DiplomaticActionType.TradeAgreement);

            Assert.True(effect.ResourceModifier > 0);
        }

        [Fact]
        public void ResourceModifier_ForAlliance_IsPositive()
        {
            var effect = new DiplomaticActionEffect(FactionMichael, FactionTrevor, DiplomaticActionType.Alliance);

            Assert.True(effect.ResourceModifier > 0);
        }

        [Theory]
        [InlineData(DiplomaticActionType.Ceasefire, 0f)]
        [InlineData(DiplomaticActionType.NonAggressionPact, 0f)]
        [InlineData(DiplomaticActionType.TradeAgreement, 0.1f)]
        [InlineData(DiplomaticActionType.MutualDefense, 0.05f)]
        [InlineData(DiplomaticActionType.Alliance, 0.15f)]
        [InlineData(DiplomaticActionType.DeclarationOfWar, 0f)]
        [InlineData(DiplomaticActionType.PeaceTreaty, 0f)]
        [InlineData(DiplomaticActionType.TerritorialConcession, 0f)]
        public void ResourceModifier_ReturnsExpectedValue(DiplomaticActionType actionType, float expectedModifier)
        {
            var effect = new DiplomaticActionEffect(FactionMichael, FactionTrevor, actionType);

            Assert.Equal(expectedModifier, effect.ResourceModifier, 2);
        }

        #endregion

        #region Tension Decay Modifier Tests

        [Fact]
        public void TensionDecayModifier_ForCeasefire_IsPositive()
        {
            var effect = new DiplomaticActionEffect(FactionMichael, FactionTrevor, DiplomaticActionType.Ceasefire);

            // Ceasefire should accelerate tension decay
            Assert.True(effect.TensionDecayModifier > 0);
        }

        [Fact]
        public void TensionDecayModifier_ForDeclarationOfWar_IsNegative()
        {
            var effect = new DiplomaticActionEffect(FactionMichael, FactionTrevor, DiplomaticActionType.DeclarationOfWar);

            // War prevents tension decay
            Assert.True(effect.TensionDecayModifier <= 0);
        }

        [Theory]
        [InlineData(DiplomaticActionType.Ceasefire, 1.5f)]
        [InlineData(DiplomaticActionType.NonAggressionPact, 1.25f)]
        [InlineData(DiplomaticActionType.TradeAgreement, 1.1f)]
        [InlineData(DiplomaticActionType.MutualDefense, 1.25f)]
        [InlineData(DiplomaticActionType.Alliance, 2.0f)]
        [InlineData(DiplomaticActionType.DeclarationOfWar, 0f)]
        [InlineData(DiplomaticActionType.PeaceTreaty, 2.5f)]
        [InlineData(DiplomaticActionType.TerritorialConcession, 1.0f)]
        public void TensionDecayModifier_ReturnsExpectedValue(DiplomaticActionType actionType, float expectedModifier)
        {
            var effect = new DiplomaticActionEffect(FactionMichael, FactionTrevor, actionType);

            Assert.Equal(expectedModifier, effect.TensionDecayModifier, 2);
        }

        #endregion

        #region Combat Restriction Tests

        [Theory]
        [InlineData(DiplomaticActionType.Ceasefire, true)]
        [InlineData(DiplomaticActionType.NonAggressionPact, true)]
        [InlineData(DiplomaticActionType.TradeAgreement, false)]
        [InlineData(DiplomaticActionType.MutualDefense, false)]
        [InlineData(DiplomaticActionType.Alliance, false)]
        [InlineData(DiplomaticActionType.DeclarationOfWar, false)]
        [InlineData(DiplomaticActionType.PeaceTreaty, true)]
        [InlineData(DiplomaticActionType.TerritorialConcession, false)]
        public void PreventsCombat_ReturnsExpectedValue(DiplomaticActionType actionType, bool expectedValue)
        {
            var effect = new DiplomaticActionEffect(FactionMichael, FactionTrevor, actionType);

            Assert.Equal(expectedValue, effect.PreventsCombat);
        }

        [Theory]
        [InlineData(DiplomaticActionType.Ceasefire, false)]
        [InlineData(DiplomaticActionType.NonAggressionPact, false)]
        [InlineData(DiplomaticActionType.MutualDefense, true)]
        [InlineData(DiplomaticActionType.Alliance, true)]
        [InlineData(DiplomaticActionType.DeclarationOfWar, false)]
        public void RequiresDefenseSupport_ReturnsExpectedValue(DiplomaticActionType actionType, bool expectedValue)
        {
            var effect = new DiplomaticActionEffect(FactionMichael, FactionTrevor, actionType);

            Assert.Equal(expectedValue, effect.RequiresDefenseSupport);
        }

        #endregion

        #region Covert Operation Restriction Tests

        [Theory]
        [InlineData(DiplomaticActionType.Ceasefire, false)]
        [InlineData(DiplomaticActionType.NonAggressionPact, true)]
        [InlineData(DiplomaticActionType.TradeAgreement, false)]
        [InlineData(DiplomaticActionType.MutualDefense, true)]
        [InlineData(DiplomaticActionType.Alliance, true)]
        [InlineData(DiplomaticActionType.PeaceTreaty, true)]
        public void PreventsCovertOperations_ReturnsExpectedValue(DiplomaticActionType actionType, bool expectedValue)
        {
            var effect = new DiplomaticActionEffect(FactionMichael, FactionTrevor, actionType);

            Assert.Equal(expectedValue, effect.PreventsCovertOperations);
        }

        #endregion

        #region Territory Access Tests

        [Theory]
        [InlineData(DiplomaticActionType.TradeAgreement, true)]
        [InlineData(DiplomaticActionType.MutualDefense, true)]
        [InlineData(DiplomaticActionType.Alliance, true)]
        [InlineData(DiplomaticActionType.Ceasefire, false)]
        [InlineData(DiplomaticActionType.NonAggressionPact, false)]
        [InlineData(DiplomaticActionType.DeclarationOfWar, false)]
        public void AllowsTerritoryAccess_ReturnsExpectedValue(DiplomaticActionType actionType, bool expectedValue)
        {
            var effect = new DiplomaticActionEffect(FactionMichael, FactionTrevor, actionType);

            Assert.Equal(expectedValue, effect.AllowsTerritoryAccess);
        }

        #endregion

        #region Duration Tests

        [Fact]
        public void IsActive_WhenDurationNotExpired_ReturnsTrue()
        {
            var effect = new DiplomaticActionEffect(FactionMichael, FactionTrevor, DiplomaticActionType.Ceasefire, 600);

            Assert.True(effect.IsActive);
        }

        [Fact]
        public void IsActive_ForPermanentEffect_ReturnsTrue()
        {
            var effect = new DiplomaticActionEffect(FactionMichael, FactionTrevor, DiplomaticActionType.Alliance, 0);

            Assert.True(effect.IsActive);
        }

        [Fact]
        public void IsPermanent_WhenDurationIsZero_ReturnsTrue()
        {
            var effect = new DiplomaticActionEffect(FactionMichael, FactionTrevor, DiplomaticActionType.Alliance, 0);

            Assert.True(effect.IsPermanent);
        }

        [Fact]
        public void IsPermanent_WhenDurationIsPositive_ReturnsFalse()
        {
            var effect = new DiplomaticActionEffect(FactionMichael, FactionTrevor, DiplomaticActionType.Ceasefire, 600);

            Assert.False(effect.IsPermanent);
        }

        [Fact]
        public void RemainingDurationSeconds_ReturnsPositiveValue()
        {
            var effect = new DiplomaticActionEffect(FactionMichael, FactionTrevor, DiplomaticActionType.Ceasefire, 600);

            Assert.True(effect.RemainingDurationSeconds > 0);
            Assert.True(effect.RemainingDurationSeconds <= 600);
        }

        [Fact]
        public void RemainingDurationSeconds_ForPermanent_ReturnsNull()
        {
            var effect = new DiplomaticActionEffect(FactionMichael, FactionTrevor, DiplomaticActionType.Alliance, 0);

            Assert.Null(effect.RemainingDurationSeconds);
        }

        #endregion

        #region Faction Query Tests

        [Fact]
        public void ContainsFaction_WithFaction1_ReturnsTrue()
        {
            var effect = new DiplomaticActionEffect(FactionMichael, FactionTrevor, DiplomaticActionType.Ceasefire);

            Assert.True(effect.ContainsFaction(FactionMichael));
        }

        [Fact]
        public void ContainsFaction_WithFaction2_ReturnsTrue()
        {
            var effect = new DiplomaticActionEffect(FactionMichael, FactionTrevor, DiplomaticActionType.Ceasefire);

            Assert.True(effect.ContainsFaction(FactionTrevor));
        }

        [Fact]
        public void ContainsFaction_WithOtherFaction_ReturnsFalse()
        {
            var effect = new DiplomaticActionEffect(FactionMichael, FactionTrevor, DiplomaticActionType.Ceasefire);

            Assert.False(effect.ContainsFaction("faction-franklin"));
        }

        [Fact]
        public void InvolvesBothFactions_WithBothInOrder_ReturnsTrue()
        {
            var effect = new DiplomaticActionEffect(FactionMichael, FactionTrevor, DiplomaticActionType.Ceasefire);

            Assert.True(effect.InvolvesBothFactions(FactionMichael, FactionTrevor));
        }

        [Fact]
        public void InvolvesBothFactions_WithBothReversed_ReturnsTrue()
        {
            var effect = new DiplomaticActionEffect(FactionMichael, FactionTrevor, DiplomaticActionType.Ceasefire);

            Assert.True(effect.InvolvesBothFactions(FactionTrevor, FactionMichael));
        }

        #endregion

        #region ToString Tests

        [Fact]
        public void ToString_ContainsActionType()
        {
            var effect = new DiplomaticActionEffect(FactionMichael, FactionTrevor, DiplomaticActionType.Ceasefire);

            var result = effect.ToString();

            Assert.Contains("Ceasefire", result);
        }

        [Fact]
        public void ToString_ContainsFactionIds()
        {
            var effect = new DiplomaticActionEffect(FactionMichael, FactionTrevor, DiplomaticActionType.Ceasefire);

            var result = effect.ToString();

            Assert.Contains(FactionMichael, result);
            Assert.Contains(FactionTrevor, result);
        }

        #endregion
    }
}
