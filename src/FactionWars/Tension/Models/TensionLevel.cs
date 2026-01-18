namespace FactionWars.Tension.Models
{
    /// <summary>
    /// Represents the tension level between two factions.
    /// Tension levels escalate from Calm to Critical based on the numeric tension value.
    /// </summary>
    public enum TensionLevel
    {
        /// <summary>
        /// No notable tension. Normal operations (value 0-24).
        /// </summary>
        Calm = 0,

        /// <summary>
        /// Rising tensions. Minor incidents may occur (value 25-49).
        /// </summary>
        Uneasy = 1,

        /// <summary>
        /// High tension. Border skirmishes likely (value 50-74).
        /// </summary>
        Tense = 2,

        /// <summary>
        /// Very high tension. Major confrontation imminent (value 75-89).
        /// </summary>
        Volatile = 3,

        /// <summary>
        /// Maximum tension. Full-scale conflict triggered (value 90-100).
        /// </summary>
        Critical = 4
    }
}
