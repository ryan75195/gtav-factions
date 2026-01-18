using FactionWars.Persistence.Models;

namespace FactionWars.Core.Interfaces
{
    /// <summary>
    /// Interface for validating save file game state data.
    /// Checks for data integrity, version compatibility, and referential integrity.
    /// </summary>
    public interface ISaveFileValidator
    {
        /// <summary>
        /// Validates a game state and returns the validation result.
        /// </summary>
        /// <param name="gameState">The game state to validate.</param>
        /// <returns>A validation result containing any errors found.</returns>
        SaveFileValidationResult Validate(GameState gameState);
    }
}
