using System;
using FactionWars.Core.Interfaces;
using FactionWars.ScriptHookV.Logging;

namespace FactionWars.ScriptHookV
{
    /// <summary>
    /// Delegate for character switch events.
    /// </summary>
    /// <param name="oldFactionId">The faction ID of the previous character, or null if unknown.</param>
    /// <param name="newFactionId">The faction ID of the new character, or null if unknown.</param>
    public delegate void CharacterSwitchedHandler(string? oldFactionId, string? newFactionId);

    /// <summary>
    /// Detects when the player switches characters in GTA V (between Michael, Franklin, and Trevor).
    /// Monitors the player's character model and raises events when a switch is detected.
    /// </summary>
    public class CharacterSwitchDetector
    {
        private readonly IGameBridge _gameBridge;
        private readonly IPlayerFactionDetector _factionDetector;

        private string? _currentCharacterModel;
        private string? _currentFactionId;
        private string? _previousFactionId;
        private bool _isInitialized;

        /// <summary>
        /// Event raised when the player switches to a different character.
        /// </summary>
        public event CharacterSwitchedHandler? OnCharacterSwitched;

        /// <summary>
        /// Gets whether the detector has been initialized.
        /// </summary>
        public bool IsInitialized => _isInitialized;

        /// <summary>
        /// Gets the current player's faction ID based on their character model.
        /// Returns null if the character model is not a known protagonist.
        /// </summary>
        public string? CurrentFactionId => _currentFactionId;

        /// <summary>
        /// Gets the previous faction ID before the last character switch.
        /// Returns null if no switch has occurred or the previous character was not a known protagonist.
        /// </summary>
        public string? PreviousFactionId => _previousFactionId;

        /// <summary>
        /// Gets the current character model name.
        /// </summary>
        public string? CurrentCharacterModel => _currentCharacterModel;

        /// <summary>
        /// Creates a new CharacterSwitchDetector.
        /// </summary>
        /// <param name="gameBridge">The game bridge for accessing GTA V natives.</param>
        /// <param name="factionDetector">The faction detector for mapping character models to factions.</param>
        /// <exception cref="ArgumentNullException">Thrown if gameBridge or factionDetector is null.</exception>
        public CharacterSwitchDetector(IGameBridge gameBridge, IPlayerFactionDetector factionDetector)
        {
            _gameBridge = gameBridge ?? throw new ArgumentNullException(nameof(gameBridge));
            _factionDetector = factionDetector ?? throw new ArgumentNullException(nameof(factionDetector));
        }

        /// <summary>
        /// Initializes the detector by reading the current character model and faction.
        /// Must be called before CheckForSwitch() will detect any changes.
        /// </summary>
        public void Initialize()
        {
            _currentCharacterModel = _gameBridge.GetPlayerCharacterModel();
            _currentFactionId = _factionDetector.GetFactionIdFromCharacterModel(_currentCharacterModel);
            _previousFactionId = null;
            _isInitialized = true;

            FileLogger.Info($"CharacterSwitchDetector initialized: Model={_currentCharacterModel}, Faction={_currentFactionId ?? "UNKNOWN"}");
        }

        /// <summary>
        /// Checks if the player has switched to a different character since the last check.
        /// If a switch is detected, updates the current faction and raises the OnCharacterSwitched event.
        /// </summary>
        /// <returns>True if a character switch was detected, false otherwise.</returns>
        public bool CheckForSwitch()
        {
            if (!_isInitialized)
            {
                return false;
            }

            var currentModel = _gameBridge.GetPlayerCharacterModel();

            // Check if the character model has changed
            if (string.Equals(_currentCharacterModel, currentModel, StringComparison.OrdinalIgnoreCase))
            {
                return false;
            }

            // Character has switched
            var newFactionId = _factionDetector.GetFactionIdFromCharacterModel(currentModel);

            // Store previous values
            _previousFactionId = _currentFactionId;
            var oldFactionId = _currentFactionId;

            // Update current values
            _currentCharacterModel = currentModel;
            _currentFactionId = newFactionId;

            FileLogger.Separator("CHARACTER SWITCH DETECTED");
            FileLogger.Info($"Character switch: {_previousFactionId ?? "UNKNOWN"} -> {_currentFactionId ?? "UNKNOWN"}");
            FileLogger.Info($"Model changed: {oldFactionId} -> {currentModel}");

            // Raise event
            OnCharacterSwitched?.Invoke(oldFactionId, newFactionId);

            return true;
        }
    }
}
