using FactionWars.Core.Interfaces;
using FactionWars.Persistence.Models;
using Newtonsoft.Json;
using System;
using System.IO;
using System.Threading.Tasks;

namespace FactionWars.Persistence
{
    /// <summary>
    /// JSON-based implementation of IPersistenceService.
    /// Handles saving and loading game state to/from JSON files.
    /// </summary>
    public class JsonPersistenceService : IPersistenceService
    {
        private static readonly JsonSerializerSettings SerializerSettings = new JsonSerializerSettings
        {
            Formatting = Formatting.Indented,
            NullValueHandling = NullValueHandling.Include,
            DateFormatHandling = DateFormatHandling.IsoDateFormat
        };

        /// <summary>
        /// Saves the game state to the specified path as JSON.
        /// </summary>
        /// <param name="gameState">The game state to save.</param>
        /// <param name="filePath">Path to save the file to.</param>
        public void Save(GameState gameState, string filePath)
        {
            ValidateSaveParameters(gameState, filePath);

            try
            {
                var json = JsonConvert.SerializeObject(gameState, SerializerSettings);
                File.WriteAllText(filePath, json);
            }
            catch (Exception ex) when (ex is not ArgumentNullException && ex is not ArgumentException)
            {
                throw new InvalidOperationException($"Failed to save game state to '{filePath}'.", ex);
            }
        }

        /// <summary>
        /// Loads a game state from the specified path.
        /// </summary>
        /// <param name="filePath">Path to load the file from.</param>
        /// <returns>The loaded game state.</returns>
        public GameState Load(string filePath)
        {
            ValidateLoadParameters(filePath);

            if (!File.Exists(filePath))
            {
                throw new FileNotFoundException($"Save file not found: '{filePath}'", filePath);
            }

            try
            {
                var json = File.ReadAllText(filePath);
                var gameState = JsonConvert.DeserializeObject<GameState>(json, SerializerSettings);
                return gameState ?? throw new InvalidOperationException("Deserialized game state was null.");
            }
            catch (JsonException ex)
            {
                throw new InvalidOperationException($"Failed to parse save file '{filePath}'. The file may be corrupted.", ex);
            }
        }

        /// <summary>
        /// Saves the game state asynchronously.
        /// </summary>
        /// <param name="gameState">The game state to save.</param>
        /// <param name="filePath">Path to save the file to.</param>
        public async Task SaveAsync(GameState gameState, string filePath)
        {
            ValidateSaveParameters(gameState, filePath);

            try
            {
                var json = JsonConvert.SerializeObject(gameState, SerializerSettings);
                using var writer = new StreamWriter(filePath);
                await writer.WriteAsync(json);
            }
            catch (Exception ex) when (ex is not ArgumentNullException && ex is not ArgumentException)
            {
                throw new InvalidOperationException($"Failed to save game state to '{filePath}'.", ex);
            }
        }

        /// <summary>
        /// Loads a game state asynchronously.
        /// </summary>
        /// <param name="filePath">Path to load the file from.</param>
        /// <returns>The loaded game state.</returns>
        public async Task<GameState> LoadAsync(string filePath)
        {
            ValidateLoadParameters(filePath);

            if (!File.Exists(filePath))
            {
                throw new FileNotFoundException($"Save file not found: '{filePath}'", filePath);
            }

            try
            {
                using var reader = new StreamReader(filePath);
                var json = await reader.ReadToEndAsync();
                var gameState = JsonConvert.DeserializeObject<GameState>(json, SerializerSettings);
                return gameState ?? throw new InvalidOperationException("Deserialized game state was null.");
            }
            catch (JsonException ex)
            {
                throw new InvalidOperationException($"Failed to parse save file '{filePath}'. The file may be corrupted.", ex);
            }
        }

        /// <summary>
        /// Checks if a save file exists at the specified path.
        /// </summary>
        /// <param name="filePath">Path to check.</param>
        /// <returns>True if the file exists.</returns>
        public bool Exists(string filePath)
        {
            if (string.IsNullOrEmpty(filePath))
            {
                return false;
            }

            return File.Exists(filePath);
        }

        /// <summary>
        /// Deletes a save file at the specified path.
        /// </summary>
        /// <param name="filePath">Path to delete.</param>
        public void Delete(string filePath)
        {
            if (string.IsNullOrEmpty(filePath))
            {
                return;
            }

            if (File.Exists(filePath))
            {
                File.Delete(filePath);
            }
        }

        private static void ValidateSaveParameters(GameState gameState, string filePath)
        {
            if (gameState == null)
            {
                throw new ArgumentNullException(nameof(gameState));
            }

            if (filePath == null)
            {
                throw new ArgumentNullException(nameof(filePath));
            }

            if (string.IsNullOrEmpty(filePath))
            {
                throw new ArgumentException("File path cannot be empty.", nameof(filePath));
            }
        }

        private static void ValidateLoadParameters(string filePath)
        {
            if (filePath == null)
            {
                throw new ArgumentNullException(nameof(filePath));
            }

            if (string.IsNullOrEmpty(filePath))
            {
                throw new ArgumentException("File path cannot be empty.", nameof(filePath));
            }
        }
    }
}
