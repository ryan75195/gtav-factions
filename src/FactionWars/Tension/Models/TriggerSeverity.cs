namespace FactionWars.Tension.Models
{
    /// <summary>
    /// Defines the severity level of a tension escalation trigger,
    /// which modifies the base tension increase.
    /// </summary>
    public enum TriggerSeverity
    {
        /// <summary>
        /// Minor incident - 0.5x multiplier on base tension increase.
        /// </summary>
        Minor = 0,

        /// <summary>
        /// Normal incident - 1.0x multiplier on base tension increase.
        /// </summary>
        Normal = 1,

        /// <summary>
        /// Major incident - 1.5x multiplier on base tension increase.
        /// </summary>
        Major = 2,

        /// <summary>
        /// Critical incident - 2.0x multiplier on base tension increase.
        /// </summary>
        Critical = 3
    }
}
