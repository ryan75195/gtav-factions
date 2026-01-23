namespace FactionWars.Configuration
{
    /// <summary>
    /// Loads and manages game configuration from a JSON file.
    /// </summary>
    public interface IConfigLoader
    {
        /// <summary>
        /// Loads the configuration from disk.
        /// Creates a default config file if one doesn't exist.
        /// </summary>
        GameConfig Load();

        /// <summary>
        /// Gets the path to the configuration file.
        /// </summary>
        string ConfigPath { get; }
    }
}
