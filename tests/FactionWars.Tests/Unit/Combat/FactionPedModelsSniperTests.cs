using FactionWars.Combat.Models;
using FactionWars.Core.Models;
using Xunit;

namespace FactionWars.Tests.Unit.Combat
{
    public class FactionPedModelsSniperTests
    {
        [Theory]
        [InlineData("franklin")]
        [InlineData("trevor")]
        [InlineData("michael")]
        public void GetModel_Sniper_ReturnsFactionSpecificModel(string faction)
        {
            var model = FactionPedModels.GetModel(faction, DefenderRole.Sniper);

            Assert.False(string.IsNullOrEmpty(model));
            Assert.NotEqual(FactionPedModels.FallbackModel, model);
        }

        [Fact]
        public void GetModel_Sniper_UnknownFaction_ReturnsFallback()
        {
            Assert.Equal(FactionPedModels.FallbackModel, FactionPedModels.GetModel("nobody", DefenderRole.Sniper));
        }
    }
}
