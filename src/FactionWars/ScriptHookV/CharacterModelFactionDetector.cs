using System;
using System.Collections.Generic;
using FactionWars.Core.Interfaces;

namespace FactionWars.ScriptHookV
{
    /// <summary>
    /// Detects the player's faction based on their GTA V character model.
    /// Maps the three protagonists to their respective factions:
    /// - Michael (player_zero) -> Blue faction
    /// - Franklin (player_one) -> Green faction
    /// - Trevor (player_two) -> Orange faction
    /// </summary>
    public class CharacterModelFactionDetector : IPlayerFactionDetector
    {
        /// <summary>
        /// Michael's faction ID.
        /// </summary>
        public const string MichaelFactionId = "michael";

        /// <summary>
        /// Franklin's faction ID.
        /// </summary>
        public const string FranklinFactionId = "franklin";

        /// <summary>
        /// Trevor's faction ID.
        /// </summary>
        public const string TrevorFactionId = "trevor";

        /// <summary>
        /// Michael's ped model name in GTA V.
        /// </summary>
        public const string MichaelModelName = "player_zero";

        /// <summary>
        /// Franklin's ped model name in GTA V.
        /// </summary>
        public const string FranklinModelName = "player_one";

        /// <summary>
        /// Trevor's ped model name in GTA V.
        /// </summary>
        public const string TrevorModelName = "player_two";

        private readonly Dictionary<string, string> _modelToFaction;
        private readonly Dictionary<string, string> _factionToModel;

        /// <summary>
        /// Creates a new CharacterModelFactionDetector with the standard GTA V protagonist mappings.
        /// </summary>
        public CharacterModelFactionDetector()
        {
            _modelToFaction = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
            {
                { MichaelModelName, MichaelFactionId },
                { FranklinModelName, FranklinFactionId },
                { TrevorModelName, TrevorFactionId }
            };

            _factionToModel = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
            {
                { MichaelFactionId, MichaelModelName },
                { FranklinFactionId, FranklinModelName },
                { TrevorFactionId, TrevorModelName }
            };
        }

        /// <inheritdoc />
        public string? GetFactionIdFromCharacterModel(string modelName)
        {
            if (string.IsNullOrEmpty(modelName))
                return null;

            return _modelToFaction.TryGetValue(modelName, out var factionId) ? factionId : null;
        }

        /// <inheritdoc />
        public string? GetCharacterModelForFaction(string factionId)
        {
            if (string.IsNullOrEmpty(factionId))
                return null;

            return _factionToModel.TryGetValue(factionId, out var modelName) ? modelName : null;
        }
    }
}
