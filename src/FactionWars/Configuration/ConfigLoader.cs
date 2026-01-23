using System;
using System.IO;
using Newtonsoft.Json;
using FactionWars.ScriptHookV.Logging;

namespace FactionWars.Configuration
{
    /// <summary>
    /// Loads game configuration from a JSON file.
    /// Creates default config if file doesn't exist.
    /// </summary>
    public class ConfigLoader : IConfigLoader
    {
        private readonly string _configPath;
        private GameConfig? _cachedConfig;

        public ConfigLoader(string configPath)
        {
            _configPath = configPath ?? throw new ArgumentNullException(nameof(configPath));
        }

        public string ConfigPath => _configPath;

        public GameConfig Load()
        {
            if (_cachedConfig != null)
            {
                return _cachedConfig;
            }

            try
            {
                EnsureDirectoryExists();

                if (File.Exists(_configPath))
                {
                    _cachedConfig = LoadFromFile();
                    FileLogger.Info($"Loaded config from {_configPath}");
                }
                else
                {
                    _cachedConfig = CreateDefaultConfig();
                    FileLogger.Info($"Created default config at {_configPath}");
                }
            }
            catch (Exception ex)
            {
                FileLogger.Error("Failed to load config, using defaults", ex);
                _cachedConfig = GameConfig.Default;
            }

            return _cachedConfig;
        }

        private void EnsureDirectoryExists()
        {
            var directory = Path.GetDirectoryName(_configPath);
            if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
            {
                Directory.CreateDirectory(directory);
            }
        }

        private GameConfig LoadFromFile()
        {
            var json = File.ReadAllText(_configPath);
            var config = JsonConvert.DeserializeObject<GameConfig>(json);
            return config ?? GameConfig.Default;
        }

        private GameConfig CreateDefaultConfig()
        {
            var config = GameConfig.Default;
            var json = JsonConvert.SerializeObject(config, Formatting.Indented);
            File.WriteAllText(_configPath, json);
            return config;
        }
    }
}
