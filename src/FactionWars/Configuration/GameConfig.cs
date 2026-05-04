namespace FactionWars.Configuration
{
    /// <summary>
    /// Root configuration object for FactionWars mod.
    /// All gameplay constants can be tuned via config.json.
    /// </summary>
    public class GameConfig
    {
        public AIConfig AI { get; set; } = new AIConfig();
        public CombatConfig Combat { get; set; } = new CombatConfig();
        public EconomyConfig Economy { get; set; } = new EconomyConfig();
        public InitializationConfig Initialization { get; set; } = new InitializationConfig();
        public PersistenceConfig Persistence { get; set; } = new PersistenceConfig();

        /// <summary>
        /// Creates a GameConfig with all default values.
        /// </summary>
        public static GameConfig Default => new GameConfig();
    }
}
