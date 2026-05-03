using System;

namespace FactionWars.Telemetry.Models
{
    public sealed class FactionSnapshot
    {
        public DateTime Timestamp { get; }
        public long PlayTimeSeconds { get; }
        public string FactionId { get; }
        public int Cash { get; }
        public int TotalTroops { get; }
        public int ZonesOwned { get; }
        public int Basic { get; }
        public int Medium { get; }
        public int Heavy { get; }
        public int Elite { get; }
        public int ReserveTroops { get; }
        public int DeployedTroops { get; }

        public FactionSnapshot(DateTime timestamp, long playTimeSeconds, string factionId,
            int cash, int totalTroops, int zonesOwned,
            int basic, int medium, int heavy, int elite,
            int reserveTroops, int deployedTroops)
        {
            Timestamp = timestamp;
            PlayTimeSeconds = playTimeSeconds;
            FactionId = factionId ?? throw new ArgumentNullException(nameof(factionId));
            Cash = cash;
            TotalTroops = totalTroops;
            ZonesOwned = zonesOwned;
            Basic = basic;
            Medium = medium;
            Heavy = heavy;
            Elite = elite;
            ReserveTroops = reserveTroops;
            DeployedTroops = deployedTroops;
        }
    }
}
