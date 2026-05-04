using System;
using System.IO;
using FactionWars.Configuration;
using Newtonsoft.Json;
using Xunit;

namespace FactionWars.Tests.Unit.Configuration
{
    public class ConfigLoaderTests : IDisposable
    {
        private readonly string _testDir;

        public ConfigLoaderTests()
        {
            _testDir = Path.Combine(Path.GetTempPath(), "FactionWarsConfigTest_" + Path.GetRandomFileName());
            Directory.CreateDirectory(_testDir);
        }

        [Fact]
        public void Load_WhenFileDoesNotExist_CreatesDefaultConfig()
        {
            var configPath = Path.Combine(_testDir, "config.json");
            var loader = new ConfigLoader(configPath);

            var config = loader.Load();

            Assert.NotNull(config);
            Assert.True(File.Exists(configPath));
            Assert.Equal(GameConfig.Default.AI.DecisionIntervalSeconds, config.AI.DecisionIntervalSeconds);
        }

        [Fact]
        public void Load_WhenFileExists_ReadsExistingConfig()
        {
            var configPath = Path.Combine(_testDir, "existing.json");
            var customConfig = new GameConfig();
            customConfig.AI.DecisionIntervalSeconds = 120f;
            customConfig.AI.TrevorAggressiveness = 0.5f;
            File.WriteAllText(configPath, JsonConvert.SerializeObject(customConfig, Formatting.Indented));

            var loader = new ConfigLoader(configPath);
            var config = loader.Load();

            Assert.Equal(120f, config.AI.DecisionIntervalSeconds);
            Assert.Equal(0.5f, config.AI.TrevorAggressiveness);
        }

        [Fact]
        public void Load_WhenFileHasPartialConfig_MergesWithDefaults()
        {
            var configPath = Path.Combine(_testDir, "partial.json");
            // Only set AI section, leave others at defaults
            File.WriteAllText(configPath, @"{
                ""AI"": {
                    ""DecisionIntervalSeconds"": 90
                }
            }");

            var loader = new ConfigLoader(configPath);
            var config = loader.Load();

            Assert.Equal(90f, config.AI.DecisionIntervalSeconds);
            // Other AI values should be defaults
            Assert.Equal(0.6f, config.AI.MichaelAggressiveness);
            // Combat should be defaults
            Assert.Equal(1.5f, config.Combat.DefenderAdvantage);
        }

        [Fact]
        public void Load_CreatesDirectoryIfNotExists()
        {
            var nestedDir = Path.Combine(_testDir, "nested", "deep");
            var configPath = Path.Combine(nestedDir, "config.json");
            var loader = new ConfigLoader(configPath);

            var config = loader.Load();

            Assert.True(Directory.Exists(nestedDir));
            Assert.True(File.Exists(configPath));
        }

        [Fact]
        public void Load_CachesConfigOnSubsequentCalls()
        {
            var configPath = Path.Combine(_testDir, "cached.json");
            var loader = new ConfigLoader(configPath);

            var config1 = loader.Load();
            var config2 = loader.Load();

            Assert.Same(config1, config2);
        }

        [Fact]
        public void ConfigPath_ReturnsProvidedPath()
        {
            var configPath = Path.Combine(_testDir, "test.json");
            var loader = new ConfigLoader(configPath);

            Assert.Equal(configPath, loader.ConfigPath);
        }

        public void Dispose()
        {
            if (Directory.Exists(_testDir))
            {
                try
                {
                    Directory.Delete(_testDir, recursive: true);
                }
                catch
                {
                    // Ignore cleanup failures in tests
                }
            }
        }
    }
}
