using FactionWars.Persistence.Models;
using System.Threading.Tasks;

namespace FactionWars.Core.Interfaces
{
    /// <summary>
    /// Interface for persisting and loading game state.
    /// Abstracts the storage mechanism for testability.
    /// </summary>
    public interface IPersistenceService
    {
        /// <summary>
        /// Saves the game state to the specified path.
        /// </summary>
        /// <param name="gameState">The game state to save.</param>
        /// <param name="filePath">Path to save the file to.</param>
        void Save(GameState gameState, string filePath);

        /// <summary>
        /// Loads a game state from the specified path.
        /// </summary>
        /// <param name="filePath">Path to load the file from.</param>
        /// <returns>The loaded game state.</returns>
        GameState Load(string filePath);

        /// <summary>
        /// Saves the game state asynchronously.
        /// </summary>
        /// <param name="gameState">The game state to save.</param>
        /// <param name="filePath">Path to save the file to.</param>
        Task SaveAsync(GameState gameState, string filePath);

        /// <summary>
        /// Loads a game state asynchronously.
        /// </summary>
        /// <param name="filePath">Path to load the file from.</param>
        /// <returns>The loaded game state.</returns>
        Task<GameState> LoadAsync(string filePath);

        /// <summary>
        /// Checks if a save file exists at the specified path.
        /// </summary>
        /// <param name="filePath">Path to check.</param>
        /// <returns>True if the file exists.</returns>
        bool Exists(string filePath);

        /// <summary>
        /// Deletes a save file at the specified path.
        /// </summary>
        /// <param name="filePath">Path to delete.</param>
        void Delete(string filePath);
    }
}
