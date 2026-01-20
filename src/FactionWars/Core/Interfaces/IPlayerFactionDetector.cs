namespace FactionWars.Core.Interfaces
{
    /// <summary>
    /// Detects the player's faction based on their character model.
    /// In GTA V, Michael, Trevor, and Franklin each lead their own faction.
    /// </summary>
    public interface IPlayerFactionDetector
    {
        /// <summary>
        /// Gets the faction ID associated with a character model name.
        /// </summary>
        /// <param name="modelName">The ped model name (e.g., "player_zero" for Michael).</param>
        /// <returns>The faction ID if the model is a known protagonist, null otherwise.</returns>
        string? GetFactionIdFromCharacterModel(string modelName);

        /// <summary>
        /// Gets the character model name associated with a faction ID.
        /// </summary>
        /// <param name="factionId">The faction ID (e.g., "michael", "trevor", "franklin").</param>
        /// <returns>The ped model name if the faction is known, null otherwise.</returns>
        string? GetCharacterModelForFaction(string factionId);
    }
}
