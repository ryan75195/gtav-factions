using System.Collections.Generic;
using FactionWars.AI.Models;
using FactionWars.Factions.Models;
using FactionWars.Territory.Models;

namespace FactionWars.AI.Interfaces
{
    /// <summary>
    /// Interface for AI faction strategies. Each faction type (Michael, Trevor, Franklin)
    /// has a unique strategy that determines how it evaluates targets and makes decisions.
    /// </summary>
    public interface IAIStrategy
    {
        /// <summary>
        /// The faction type this strategy is designed for.
        /// </summary>
        FactionType FactionType { get; }

        /// <summary>
        /// Evaluates a zone's attractiveness as a target for this strategy.
        /// </summary>
        /// <param name="zone">The zone to evaluate.</param>
        /// <param name="context">The current AI context.</param>
        /// <returns>A score between 0 and 1, where higher is more attractive.</returns>
        /// <exception cref="System.ArgumentNullException">Thrown if zone or context is null.</exception>
        float EvaluateZone(Zone zone, AIContext context);

        /// <summary>
        /// Makes strategic decisions based on the current game state.
        /// </summary>
        /// <param name="context">The current AI context.</param>
        /// <returns>A list of decisions to execute, ordered by priority.</returns>
        /// <exception cref="System.ArgumentNullException">Thrown if context is null.</exception>
        IList<AIDecision> MakeDecisions(AIContext context);

        /// <summary>
        /// Determines whether the faction should attack a specific zone.
        /// </summary>
        /// <param name="zone">The zone to potentially attack.</param>
        /// <param name="context">The current AI context.</param>
        /// <returns>True if the faction should attack, false otherwise.</returns>
        /// <exception cref="System.ArgumentNullException">Thrown if zone or context is null.</exception>
        bool ShouldAttack(Zone zone, AIContext context);

        /// <summary>
        /// Determines whether the faction should defend a specific zone.
        /// </summary>
        /// <param name="zone">The zone to potentially defend.</param>
        /// <param name="context">The current AI context.</param>
        /// <returns>True if the faction should defend, false otherwise.</returns>
        /// <exception cref="System.ArgumentNullException">Thrown if zone or context is null.</exception>
        bool ShouldDefend(Zone zone, AIContext context);

        /// <summary>
        /// Calculates how many troops to commit to a specific action.
        /// </summary>
        /// <param name="decision">The decision requiring troops.</param>
        /// <param name="context">The current AI context.</param>
        /// <returns>The number of troops to commit.</returns>
        /// <exception cref="System.ArgumentNullException">Thrown if decision or context is null.</exception>
        int GetTroopsForAction(AIDecision decision, AIContext context);

        /// <summary>
        /// Gets the aggressiveness level of this strategy (0 = passive, 1 = very aggressive).
        /// Affects the balance between attack and defense priorities.
        /// </summary>
        /// <returns>A value between 0 and 1.</returns>
        float GetAggressiveness();

        /// <summary>
        /// Gets the risk tolerance of this strategy (0 = risk-averse, 1 = risk-seeking).
        /// Affects willingness to attack when outnumbered.
        /// </summary>
        /// <returns>A value between 0 and 1.</returns>
        float GetRiskTolerance();
    }
}
