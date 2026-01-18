namespace FactionWars.Combat.Models
{
    /// <summary>
    /// Status codes for reinforcement request results.
    /// </summary>
    public enum ReinforcementResultStatus
    {
        /// <summary>
        /// All requested reinforcements were spawned successfully.
        /// </summary>
        Success,

        /// <summary>
        /// Some but not all requested reinforcements were spawned.
        /// </summary>
        PartialSuccess,

        /// <summary>
        /// Request failed because the faction is on cooldown.
        /// </summary>
        OnCooldown,

        /// <summary>
        /// Request failed due to insufficient resources.
        /// </summary>
        InsufficientResources,

        /// <summary>
        /// Request failed because the ped pool is full.
        /// </summary>
        PoolFull,

        /// <summary>
        /// Request failed because maximum active waves has been reached.
        /// </summary>
        MaxWavesReached,

        /// <summary>
        /// Request failed because the combat encounter has ended.
        /// </summary>
        EncounterEnded
    }
}
