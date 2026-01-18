namespace FactionWars.AI.Models
{
    /// <summary>
    /// Represents the difficulty level for AI factions.
    /// Higher difficulty levels result in more aggressive and capable AI opponents.
    /// </summary>
    public enum AIDifficulty
    {
        /// <summary>
        /// Easy difficulty: AI is less aggressive, generates fewer resources,
        /// and reacts more slowly to player actions.
        /// </summary>
        Easy = 0,

        /// <summary>
        /// Normal difficulty: Standard AI behavior with balanced resource generation,
        /// attack frequency, and reaction times.
        /// </summary>
        Normal = 1,

        /// <summary>
        /// Hard difficulty: AI is more aggressive, generates more resources,
        /// and makes better tactical decisions.
        /// </summary>
        Hard = 2,

        /// <summary>
        /// Veteran difficulty: Maximum AI challenge with aggressive behavior,
        /// fast reactions, and optimal resource management.
        /// </summary>
        Veteran = 3
    }
}
