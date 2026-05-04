namespace FactionWars.Configuration
{
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
}
