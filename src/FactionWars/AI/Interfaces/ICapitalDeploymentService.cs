using FactionWars.AI.Models;
using FactionWars.Territory.Models;

namespace FactionWars.AI.Interfaces
{
    /// <summary>
    /// Service for intelligent capital deployment decisions.
    /// Helps AI factions use their cash effectively instead of hoarding.
    /// </summary>
    public interface ICapitalDeploymentService
    {
        /// <summary>
        /// Calculates how urgently a zone needs reinforcement.
        /// Higher score = more urgent defense need.
        /// </summary>
        /// <param name="zone">The zone to evaluate for defense priority.</param>
        /// <param name="context">The AI context containing faction state and world information.</param>
        /// <returns>A score from 0.0 to 1.0 indicating defense priority.</returns>
        float GetDefensePriority(Zone zone, AIContext context);

        /// <summary>
        /// Calculates how attractive a target zone is for attack.
        /// Higher score = better opportunity.
        /// </summary>
        /// <param name="target">The target zone to evaluate for attack opportunity.</param>
        /// <param name="context">The AI context containing faction state and world information.</param>
        /// <returns>A score from 0.0 to 1.0 indicating attack opportunity.</returns>
        float GetAttackOpportunity(Zone target, AIContext context);

        /// <summary>
        /// Gets the maximum troops to recruit per cycle based on cash.
        /// Formula: BaseRate(10) + (Cash / 10000), capped at 50.
        /// </summary>
        /// <param name="cash">The faction's current cash amount.</param>
        /// <returns>The maximum number of troops to recruit this cycle.</returns>
        int GetScaledRecruitmentMax(int cash);

        /// <summary>
        /// Calculates troops to commit for overwhelming force attack.
        /// Formula: Max(enemyDefenders * 3, availableTroops * 0.5)
        /// </summary>
        /// <param name="availableTroops">The total troops available for attack.</param>
        /// <param name="enemyDefenders">The estimated enemy defenders in the target zone.</param>
        /// <returns>The number of troops to commit to the attack.</returns>
        int GetOverwhelmingAttackForce(int availableTroops, int enemyDefenders);

        /// <summary>
        /// Gets the best decision (Defend, Attack, or null for Hold).
        /// Compares max defense priority vs max attack opportunity.
        /// </summary>
        /// <param name="context">The AI context containing faction state and world information.</param>
        /// <returns>The best AIDecision, or null if no action should be taken (Hold).</returns>
        AIDecision? GetBestDecision(AIContext context);
    }
}
