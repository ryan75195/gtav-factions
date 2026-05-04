namespace FactionWars.Configuration
{
    /// <summary>
    /// Save/load and persistence configuration.
    /// </summary>
    public class PersistenceConfig
    {
        public int AutoSaveIntervalSeconds { get; set; } = 300;
        public int MaxSaveSlots { get; set; } = 10;
        public string SaveDirectoryName { get; set; } = "FactionWars";
    }
}
