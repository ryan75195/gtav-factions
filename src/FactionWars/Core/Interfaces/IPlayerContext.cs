namespace FactionWars.Core.Interfaces
{
    /// <summary>
    /// Provides information about the current player's context within the game.
    /// This includes which faction they belong to based on the character they're playing.
    /// </summary>
    public interface IPlayerContext
    {
        /// <summary>
        /// Gets the current player's faction ID based on their character model.
        /// Returns null if the player is not controlling a known protagonist.
        /// </summary>
        /// <remarks>
        /// In GTA V:
        /// - Michael (player_zero) = "michael"
        /// - Franklin (player_one) = "franklin"
        /// - Trevor (player_two) = "trevor"
        /// </remarks>
        string? CurrentFactionId { get; }
    }
}
