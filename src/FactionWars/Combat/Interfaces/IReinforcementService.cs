using FactionWars.Combat.Models;

namespace FactionWars.Combat.Interfaces
{
    /// <summary>
    /// Service interface for managing reinforcement mechanics during combat.
    /// Handles spawning additional peds based on faction requests, cooldowns, and resource constraints.
    /// </summary>
    public interface IReinforcementService
    {
        /// <summary>
        /// Requests reinforcements for a combat encounter.
        /// </summary>
        /// <param name="request">The reinforcement request details.</param>
        /// <param name="encounter">The combat encounter to reinforce.</param>
        /// <returns>A result indicating success or failure with details.</returns>
        /// <exception cref="System.ArgumentNullException">Thrown if request or encounter is null.</exception>
        ReinforcementResult RequestReinforcements(ReinforcementRequest request, CombatEncounter encounter);

        /// <summary>
        /// Requests reinforcements using the request's encounter ID to look up the encounter.
        /// </summary>
        /// <param name="request">The reinforcement request details.</param>
        /// <returns>A result indicating success or failure with details.</returns>
        /// <exception cref="System.ArgumentNullException">Thrown if request is null.</exception>
        ReinforcementResult RequestReinforcements(ReinforcementRequest request);

        /// <summary>
        /// Checks if a faction can currently request reinforcements for an encounter.
        /// </summary>
        /// <param name="factionId">The faction ID to check.</param>
        /// <param name="encounter">The combat encounter.</param>
        /// <returns>True if the faction can request reinforcements.</returns>
        /// <exception cref="System.ArgumentNullException">Thrown if factionId or encounter is null.</exception>
        bool CanRequestReinforcements(string factionId, CombatEncounter encounter);

        /// <summary>
        /// Gets the remaining cooldown time for a faction in an encounter.
        /// </summary>
        /// <param name="factionId">The faction ID to check.</param>
        /// <param name="encounter">The combat encounter.</param>
        /// <returns>Remaining cooldown in seconds, or 0 if no cooldown active.</returns>
        /// <exception cref="System.ArgumentNullException">Thrown if factionId or encounter is null.</exception>
        float GetRemainingCooldown(string factionId, CombatEncounter encounter);

        /// <summary>
        /// Gets the number of active reinforcement waves for a faction in an encounter.
        /// </summary>
        /// <param name="factionId">The faction ID to check.</param>
        /// <param name="encounter">The combat encounter.</param>
        /// <returns>The number of active waves.</returns>
        int GetActiveWaveCount(string factionId, CombatEncounter encounter);

        /// <summary>
        /// Resets the cooldown for a faction in an encounter.
        /// </summary>
        /// <param name="factionId">The faction ID to reset.</param>
        /// <param name="encounter">The combat encounter.</param>
        void ResetCooldown(string factionId, CombatEncounter encounter);

        /// <summary>
        /// Gets the current reinforcement configuration.
        /// </summary>
        /// <returns>A copy of the current configuration.</returns>
        ReinforcementConfig GetCurrentConfig();
    }
}
