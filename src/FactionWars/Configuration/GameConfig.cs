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

    /// <summary>
    /// AI faction behavior configuration.
    /// </summary>
    public class AIConfig
    {
        // Decision intervals (seconds)
        public float DecisionIntervalSeconds { get; set; } = 60f;
        public float RecruitmentIntervalSeconds { get; set; } = 60f;

        // Per-faction aggressiveness (0.0 = passive, 1.0 = very aggressive)
        public float MichaelAggressiveness { get; set; } = 0.6f;
        public float TrevorAggressiveness { get; set; } = 0.6f;
        public float FranklinAggressiveness { get; set; } = 0.6f;

        // Per-faction risk tolerance (0.0 = risk-averse, 1.0 = risk-seeking)
        public float MichaelRiskTolerance { get; set; } = 0.6f;
        public float TrevorRiskTolerance { get; set; } = 0.6f;
        public float FranklinRiskTolerance { get; set; } = 0.6f;

        // AI costs
        public int RecruitCostPerTroop { get; set; } = 200;
        public int AttackCostPerTroop { get; set; } = 50;
        public int MaxRecruitPerCycle { get; set; } = 5;
    }

    /// <summary>
    /// Combat mechanics configuration.
    /// </summary>
    public class CombatConfig
    {
        // Battle mechanics
        public float DefenderAdvantage { get; set; } = 1.5f;
        public float BaseCasualtyRate { get; set; } = 0.3f;

        // Troop strength by tier
        public float BasicTroopStrength { get; set; } = 1.0f;
        public float MediumTroopStrength { get; set; } = 1.5f;
        public float HeavyTroopStrength { get; set; } = 2.0f;

        // Troop resilience (lower = more resilient, affects casualties taken)
        public float BasicResilienceModifier { get; set; } = 1.0f;
        public float MediumResilienceModifier { get; set; } = 0.75f;
        public float HeavyResilienceModifier { get; set; } = 0.5f;

        // Battle duration (seconds)
        public float MinBattleDurationSeconds { get; set; } = 60f;
        public float MaxBattleDurationSeconds { get; set; } = 300f;

        // Spawning limits
        public int MaxSpawnedPedsPerSide { get; set; } = 12;
        public int MaxTotalPeds { get; set; } = 30;
        public int TroopsPerSpawnedPed { get; set; } = 5;

        // Takeover thresholds (control percentage)
        public float AttackerVictoryThreshold { get; set; } = 100f;
        public float DefenderVictoryThreshold { get; set; } = 0f;
    }

    /// <summary>
    /// Economy and resource generation configuration.
    /// </summary>
    public class EconomyConfig
    {
        // Base resource generation per zone per tick
        public int CashBaseRate { get; set; } = 100;
        public int RecruitmentBaseRate { get; set; } = 10;
        public int WeaponsBaseRate { get; set; } = 5;

        // Resource caps
        public int CashCap { get; set; } = 100000;
        public int RecruitmentCap { get; set; } = 1000;
        public int WeaponsCap { get; set; } = 500;

        // Tick interval (seconds)
        public int ResourceTickIntervalSeconds { get; set; } = 60;

        // Zone trait bonuses (additive multipliers: 0.5 = +50%)
        public float CommercialCashBonus { get; set; } = 0.50f;
        public float ResidentialRecruitmentBonus { get; set; } = 0.50f;
        public float IndustrialWeaponsBonus { get; set; } = 0.50f;
        public float PortCashBonus { get; set; } = 0.25f;
        public float PortWeaponsBonus { get; set; } = 0.25f;
        public float HighValueMultiplier { get; set; } = 2.0f;

        // Supply line efficiency when disconnected from HQ
        public float DisconnectedSupplyEfficiency { get; set; } = 0.5f;
    }

    /// <summary>
    /// Game initialization configuration.
    /// </summary>
    public class InitializationConfig
    {
        public int StartingCash { get; set; } = 5000;
        public int StartingTroopsPerZone { get; set; } = 5;
        public int StartingZonesPerFaction { get; set; } = 3;
        public int StartingReserveTroops { get; set; } = 10;
    }

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
