using System;
using FactionWars.Core.Interfaces;

namespace FactionWars.ScriptHookV
{
    /// <summary>
    /// Implementation of IPlayerContext that provides the current player's faction
    /// based on their character model.
    /// </summary>
    public class PlayerContext : IPlayerContext
    {
        private readonly IGameBridge _gameBridge;
        private readonly IPlayerFactionDetector _factionDetector;

        /// <summary>
        /// Creates a new PlayerContext.
        /// </summary>
        /// <param name="gameBridge">The game bridge for native calls.</param>
        /// <param name="factionDetector">The detector for mapping characters to factions.</param>
        /// <exception cref="ArgumentNullException">Thrown if any parameter is null.</exception>
        public PlayerContext(IGameBridge gameBridge, IPlayerFactionDetector factionDetector)
        {
            _gameBridge = gameBridge ?? throw new ArgumentNullException(nameof(gameBridge));
            _factionDetector = factionDetector ?? throw new ArgumentNullException(nameof(factionDetector));
        }

        /// <inheritdoc />
        public string? CurrentFactionId
        {
            get
            {
                var characterModel = _gameBridge.GetPlayerCharacterModel();
                return _factionDetector.GetFactionIdFromCharacterModel(characterModel);
            }
        }
    }
}
