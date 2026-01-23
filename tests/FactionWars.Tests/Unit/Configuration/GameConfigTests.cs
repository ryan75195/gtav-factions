using FactionWars.Configuration;
using Xunit;

namespace FactionWars.Tests.Unit.Configuration
{
    public class GameConfigTests
    {
        [Fact]
        public void Default_HasValidAISettings()
        {
            var config = GameConfig.Default;

            Assert.Equal(60f, config.AI.DecisionIntervalSeconds);
            Assert.Equal(0.3f, config.AI.MichaelAggressiveness);
            Assert.Equal(0.85f, config.AI.TrevorAggressiveness);
            Assert.Equal(0.6f, config.AI.FranklinAggressiveness);
        }

        [Fact]
        public void Default_HasValidCombatSettings()
        {
            var config = GameConfig.Default;

            Assert.Equal(1.5f, config.Combat.DefenderAdvantage);
            Assert.Equal(12, config.Combat.MaxSpawnedPedsPerSide);
            Assert.Equal(30, config.Combat.MaxTotalPeds);
        }

        [Fact]
        public void Default_HasValidEconomySettings()
        {
            var config = GameConfig.Default;

            Assert.Equal(100, config.Economy.CashBaseRate);
            Assert.Equal(10, config.Economy.RecruitmentBaseRate);
            Assert.Equal(5, config.Economy.WeaponsBaseRate);
            Assert.Equal(60, config.Economy.ResourceTickIntervalSeconds);
        }

        [Fact]
        public void Default_HasValidInitializationSettings()
        {
            var config = GameConfig.Default;

            Assert.Equal(5000, config.Initialization.StartingCash);
            Assert.Equal(5, config.Initialization.StartingTroopsPerZone);
            Assert.Equal(3, config.Initialization.StartingZonesPerFaction);
        }

        [Fact]
        public void Default_HasValidPersistenceSettings()
        {
            var config = GameConfig.Default;

            Assert.Equal(300, config.Persistence.AutoSaveIntervalSeconds);
            Assert.Equal(10, config.Persistence.MaxSaveSlots);
        }

        [Fact]
        public void Default_CreatesNewInstanceEachTime()
        {
            var config1 = GameConfig.Default;
            var config2 = GameConfig.Default;

            // Modifying one shouldn't affect the other
            config1.AI.DecisionIntervalSeconds = 999f;

            Assert.NotEqual(config1.AI.DecisionIntervalSeconds, config2.AI.DecisionIntervalSeconds);
        }
    }
}
