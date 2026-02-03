using FactionWars.Combat.Models;
using FactionWars.Core.Models;
using Xunit;

namespace FactionWars.Tests.Unit.Combat
{
    public class FactionPedModelsTests
    {
        [Theory]
        [InlineData("franklin", DefenderTier.Basic, "g_m_y_famca_01")]
        [InlineData("franklin", DefenderTier.Medium, "g_m_y_famdnf_01")]
        [InlineData("franklin", DefenderTier.Heavy, "g_m_y_famfor_01")]
        [InlineData("franklin", DefenderTier.Elite, "g_m_y_ballasout_01")]
        [InlineData("trevor", DefenderTier.Basic, "a_m_m_hillbilly_01")]
        [InlineData("trevor", DefenderTier.Medium, "g_m_y_lost_01")]
        [InlineData("trevor", DefenderTier.Heavy, "g_m_y_lost_02")]
        [InlineData("trevor", DefenderTier.Elite, "g_m_y_lost_03")]
        [InlineData("michael", DefenderTier.Basic, "g_m_m_armboss_01")]
        [InlineData("michael", DefenderTier.Medium, "s_m_y_blackops_01")]
        [InlineData("michael", DefenderTier.Heavy, "s_m_y_blackops_02")]
        [InlineData("michael", DefenderTier.Elite, "s_m_m_highsec_01")]
        public void GetModel_ReturnsCorrectModelForFactionAndTier(string factionId, DefenderTier tier, string expectedModel)
        {
            var model = FactionPedModels.GetModel(factionId, tier);

            Assert.Equal(expectedModel, model);
        }

        [Theory]
        [InlineData("FRANKLIN", DefenderTier.Basic)]
        [InlineData("Franklin", DefenderTier.Medium)]
        [InlineData("TREVOR", DefenderTier.Heavy)]
        [InlineData("Michael", DefenderTier.Elite)]
        public void GetModel_IsCaseInsensitive(string factionId, DefenderTier tier)
        {
            var model = FactionPedModels.GetModel(factionId, tier);

            Assert.NotEqual(FactionPedModels.FallbackModel, model);
        }

        [Theory]
        [InlineData(null, DefenderTier.Basic)]
        [InlineData("", DefenderTier.Medium)]
        [InlineData("unknown_faction", DefenderTier.Heavy)]
        public void GetModel_ReturnsFallbackForUnknownFaction(string? factionId, DefenderTier tier)
        {
            var model = FactionPedModels.GetModel(factionId!, tier);

            Assert.Equal(FactionPedModels.FallbackModel, model);
        }

        [Fact]
        public void FallbackModel_IsValidPedModel()
        {
            Assert.Equal("a_m_m_business_01", FactionPedModels.FallbackModel);
        }
    }
}
