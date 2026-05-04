namespace FactionWars.Configuration
{
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
}
