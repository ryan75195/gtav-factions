namespace FactionWars.Core.Models
{
    /// <summary>
    /// Represents the difficulty level for the faction wars game.
    /// Affects AI income generation and income tick intervals.
    /// </summary>
    public enum Difficulty
    {
        /// <summary>
        /// Easy difficulty - AI earns less income, longer intervals between ticks.
        /// </summary>
        Easy,

        /// <summary>
        /// Normal difficulty - balanced income and tick intervals.
        /// </summary>
        Normal,

        /// <summary>
        /// Hard difficulty - AI earns more income, shorter intervals between ticks.
        /// </summary>
        Hard
    }
}
