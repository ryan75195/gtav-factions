using System.Collections.Generic;
using FactionWars.AI.Models;
using FactionWars.Territory.Models;

namespace FactionWars.AI.Interfaces
{
    /// <summary>
    /// Interface for the resource allocation service.
    /// Determines how a faction should distribute its resources (troops, cash)
    /// between attack and defense operations based on strategic priorities.
    /// </summary>
    public interface IResourceAllocationService
    {
        /// <summary>
        /// Allocates resources for an attack operation on a target zone.
        /// </summary>
        /// <param name="targetZone">The zone to attack.</param>
        /// <param name="context">The current AI context.</param>
        /// <returns>Resource allocation for the attack.</returns>
        /// <exception cref="System.ArgumentNullException">Thrown if targetZone or context is null.</exception>
        ResourceAllocation AllocateForAttack(Zone targetZone, AIContext context);

        /// <summary>
        /// Allocates resources for a defense operation on an owned zone.
        /// </summary>
        /// <param name="zone">The zone to defend.</param>
        /// <param name="context">The current AI context.</param>
        /// <returns>Resource allocation for the defense.</returns>
        /// <exception cref="System.ArgumentNullException">Thrown if zone or context is null.</exception>
        ResourceAllocation AllocateForDefense(Zone zone, AIContext context);

        /// <summary>
        /// Allocates resources across multiple AI decisions.
        /// Ensures total allocation does not exceed available resources.
        /// </summary>
        /// <param name="decisions">The AI decisions requiring resources.</param>
        /// <param name="context">The current AI context.</param>
        /// <returns>List of resource allocations for each decision.</returns>
        /// <exception cref="System.ArgumentNullException">Thrown if decisions or context is null.</exception>
        IList<ResourceAllocation> AllocateResources(IList<AIDecision> decisions, AIContext context);

        /// <summary>
        /// Calculates the effective attack strength from a resource allocation.
        /// </summary>
        /// <param name="allocation">The allocation to evaluate.</param>
        /// <returns>The calculated attack strength.</returns>
        /// <exception cref="System.ArgumentNullException">Thrown if allocation is null.</exception>
        float CalculateAttackStrength(ResourceAllocation allocation);

        /// <summary>
        /// Calculates the effective defense strength from a resource allocation.
        /// </summary>
        /// <param name="allocation">The allocation to evaluate.</param>
        /// <returns>The calculated defense strength.</returns>
        /// <exception cref="System.ArgumentNullException">Thrown if allocation is null.</exception>
        float CalculateDefenseStrength(ResourceAllocation allocation);

        /// <summary>
        /// Gets the recommended troop reserve that should be kept for emergencies.
        /// </summary>
        /// <param name="context">The current AI context.</param>
        /// <returns>Recommended reserve troop count.</returns>
        /// <exception cref="System.ArgumentNullException">Thrown if context is null.</exception>
        int GetRecommendedReserve(AIContext context);
    }
}
