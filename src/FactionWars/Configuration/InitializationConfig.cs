namespace FactionWars.Configuration
{
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
}
