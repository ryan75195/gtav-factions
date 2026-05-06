namespace FactionWars.Configuration
{
    /// <summary>
    /// AI faction behavior configuration.
    /// </summary>
    public class AIConfig
    {
        // Decision intervals (seconds)
        public float DecisionIntervalSeconds { get; set; } = 90f;
        public float RecruitmentIntervalSeconds { get; set; } = 90f;

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

        // Repeated active-battle reinforcements for the same faction/zone multiply
        // their deployment percentage by this value each time. 1.0 disables decay.
        public float ReinforcementDeploymentDecayMultiplier { get; set; } = 0.5f;
    }
}
