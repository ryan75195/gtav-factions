namespace FactionWars.AI.Models
{
    /// <summary>
    /// Represents the threat level of a vehicle for RPG response calculation.
    /// </summary>
    public enum VehicleThreatLevel
    {
        /// <summary>
        /// No threat - civilian vehicles, motorcycles (e.g., Bati).
        /// No RPG response required.
        /// </summary>
        None,

        /// <summary>
        /// Light threat - armed technicals, sports cars (e.g., Technical, Zentorno).
        /// Requires 1 RPG unit response.
        /// </summary>
        Light,

        /// <summary>
        /// Heavy threat - armored vehicles, helicopters, tanks (e.g., Insurgent, APC, Buzzard, Khanjali).
        /// Requires 2 RPG units response.
        /// </summary>
        Heavy
    }
}
