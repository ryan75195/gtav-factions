using FactionWars.Combat.Models;
using FactionWars.Core.Models;
using Xunit;

namespace FactionWars.Tests.Unit.Combat
{
    public class FactionPedModelsTests
    {
        [Theory]
        [InlineData("franklin", DefenderRole.Grunt, "g_m_y_famca_01")]
        [InlineData("franklin", DefenderRole.Gunner, "g_m_y_famdnf_01")]
        [InlineData("franklin", DefenderRole.Rifleman, "g_m_y_famfor_01")]
        [InlineData("franklin", DefenderRole.Rocketeer, "g_m_y_ballasout_01")]
        [InlineData("trevor", DefenderRole.Grunt, "a_m_m_hillbilly_01")]
        [InlineData("trevor", DefenderRole.Gunner, "g_m_y_lost_01")]
        [InlineData("trevor", DefenderRole.Rifleman, "g_m_y_lost_02")]
        [InlineData("trevor", DefenderRole.Rocketeer, "g_m_y_lost_03")]
        [InlineData("michael", DefenderRole.Grunt, "g_m_m_armboss_01")]
        [InlineData("michael", DefenderRole.Gunner, "s_m_y_blackops_01")]
        [InlineData("michael", DefenderRole.Rifleman, "s_m_y_blackops_02")]
        [InlineData("michael", DefenderRole.Rocketeer, "s_m_m_highsec_01")]
        public void GetModel_ReturnsCorrectModelForFactionAndTier(string factionId, DefenderRole tier, string expectedModel)
        {
            var model = FactionPedModels.GetModel(factionId, tier);

            Assert.Equal(expectedModel, model);
        }

        [Theory]
        [InlineData("FRANKLIN", DefenderRole.Grunt)]
        [InlineData("Franklin", DefenderRole.Gunner)]
        [InlineData("TREVOR", DefenderRole.Rifleman)]
        [InlineData("Michael", DefenderRole.Rocketeer)]
        public void GetModel_IsCaseInsensitive(string factionId, DefenderRole tier)
        {
            var model = FactionPedModels.GetModel(factionId, tier);

            Assert.NotEqual(FactionPedModels.FallbackModel, model);
        }

        [Theory]
        [InlineData(null, DefenderRole.Grunt)]
        [InlineData("", DefenderRole.Gunner)]
        [InlineData("unknown_faction", DefenderRole.Rifleman)]
        public void GetModel_ReturnsFallbackForUnknownFaction(string? factionId, DefenderRole tier)
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
