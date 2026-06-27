namespace FactionWars.Core.Models
{
    /// <summary>
    /// The combat role of a defender troop. Each role has a distinct weapon,
    /// cost, and survivability profile. Integer values are a persistence
    /// contract and must never be reordered.
    /// </summary>
    public enum DefenderRole
    {
        /// <summary>Pistol, cheap and fragile. (Formerly Basic.)</summary>
        Grunt = 0,

        /// <summary>SMG, close/mid spray. (Formerly Medium.)</summary>
        Gunner = 1,

        /// <summary>Carbine, reliable line infantry. (Formerly Heavy.)</summary>
        Rifleman = 2,

        /// <summary>RPG anti-vehicle specialist. (Formerly Elite.)</summary>
        Rocketeer = 3
    }
}
