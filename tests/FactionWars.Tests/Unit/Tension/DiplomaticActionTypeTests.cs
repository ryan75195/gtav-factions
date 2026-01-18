using FactionWars.Tension.Models;
using Xunit;

namespace FactionWars.Tests.Unit.Tension
{
    /// <summary>
    /// Tests for the DiplomaticActionType enum which defines the types of diplomatic actions
    /// that can be performed between factions.
    /// </summary>
    public class DiplomaticActionTypeTests
    {
        [Fact]
        public void DiplomaticActionType_HasCeasefireOption()
        {
            var actionType = DiplomaticActionType.Ceasefire;
            Assert.Equal(0, (int)actionType);
        }

        [Fact]
        public void DiplomaticActionType_HasNonAggressionPactOption()
        {
            var actionType = DiplomaticActionType.NonAggressionPact;
            Assert.Equal(1, (int)actionType);
        }

        [Fact]
        public void DiplomaticActionType_HasTradeAgreementOption()
        {
            var actionType = DiplomaticActionType.TradeAgreement;
            Assert.Equal(2, (int)actionType);
        }

        [Fact]
        public void DiplomaticActionType_HasMutualDefenseOption()
        {
            var actionType = DiplomaticActionType.MutualDefense;
            Assert.Equal(3, (int)actionType);
        }

        [Fact]
        public void DiplomaticActionType_HasAllianceOption()
        {
            var actionType = DiplomaticActionType.Alliance;
            Assert.Equal(4, (int)actionType);
        }

        [Fact]
        public void DiplomaticActionType_HasDeclarationOfWarOption()
        {
            var actionType = DiplomaticActionType.DeclarationOfWar;
            Assert.Equal(5, (int)actionType);
        }

        [Fact]
        public void DiplomaticActionType_HasPeaceTreatyOption()
        {
            var actionType = DiplomaticActionType.PeaceTreaty;
            Assert.Equal(6, (int)actionType);
        }

        [Fact]
        public void DiplomaticActionType_HasTerritorialConcessionOption()
        {
            var actionType = DiplomaticActionType.TerritorialConcession;
            Assert.Equal(7, (int)actionType);
        }

        [Theory]
        [InlineData(DiplomaticActionType.Ceasefire)]
        [InlineData(DiplomaticActionType.NonAggressionPact)]
        [InlineData(DiplomaticActionType.TradeAgreement)]
        [InlineData(DiplomaticActionType.MutualDefense)]
        [InlineData(DiplomaticActionType.Alliance)]
        [InlineData(DiplomaticActionType.DeclarationOfWar)]
        [InlineData(DiplomaticActionType.PeaceTreaty)]
        [InlineData(DiplomaticActionType.TerritorialConcession)]
        public void DiplomaticActionType_AllValues_CanBeParsedFromString(DiplomaticActionType actionType)
        {
            var name = actionType.ToString();
            Assert.True(System.Enum.TryParse<DiplomaticActionType>(name, out var parsed));
            Assert.Equal(actionType, parsed);
        }
    }
}
