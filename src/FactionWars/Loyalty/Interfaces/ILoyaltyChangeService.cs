using FactionWars.Loyalty.Models;
using System.Collections.Generic;

namespace FactionWars.Loyalty.Interfaces
{
    /// <summary>
    /// Service responsible for applying loyalty changes to zones.
    /// </summary>
    public interface ILoyaltyChangeService
    {
        /// <summary>
        /// Applies a single modifier to zone loyalty.
        /// </summary>
        /// <param name="zoneLoyalty">The zone loyalty to modify.</param>
        /// <param name="modifier">The modifier to apply.</param>
        void ApplyModifier(ZoneLoyalty zoneLoyalty, LoyaltyModifier modifier);

        /// <summary>
        /// Applies multiple modifiers to zone loyalty in sequence.
        /// </summary>
        /// <param name="zoneLoyalty">The zone loyalty to modify.</param>
        /// <param name="modifiers">The modifiers to apply.</param>
        void ApplyModifiers(ZoneLoyalty zoneLoyalty, IEnumerable<LoyaltyModifier> modifiers);

        /// <summary>
        /// Applies the daily loyalty change (time-based gain) and advances the day counter.
        /// </summary>
        /// <param name="zoneLoyalty">The zone loyalty to update.</param>
        void ApplyDailyChange(ZoneLoyalty zoneLoyalty);

        /// <summary>
        /// Applies the appropriate modifier based on combat result.
        /// </summary>
        /// <param name="zoneLoyalty">The zone loyalty to modify.</param>
        /// <param name="defenderWon">True if the defending faction won.</param>
        void ApplyCombatResult(ZoneLoyalty zoneLoyalty, bool defenderWon);

        /// <summary>
        /// Applies loyalty bonus from resource investment.
        /// </summary>
        /// <param name="zoneLoyalty">The zone loyalty to modify.</param>
        /// <param name="amount">The number of resource units invested.</param>
        void ApplyResourceInvestment(ZoneLoyalty zoneLoyalty, int amount);

        /// <summary>
        /// Applies loyalty penalty from oppressive actions.
        /// </summary>
        /// <param name="zoneLoyalty">The zone loyalty to modify.</param>
        /// <param name="severityMultiplier">Optional multiplier for severity (default: 1).</param>
        void ApplyOppression(ZoneLoyalty zoneLoyalty, int severityMultiplier = 1);

        /// <summary>
        /// Applies loyalty bonus from propaganda operations.
        /// </summary>
        /// <param name="zoneLoyalty">The zone loyalty to modify.</param>
        void ApplyPropaganda(ZoneLoyalty zoneLoyalty);

        /// <summary>
        /// Calculates the influence from neighboring zones.
        /// </summary>
        /// <param name="zoneLoyalty">The zone to calculate influence for.</param>
        /// <param name="neighborLoyalties">The loyalty data of adjacent zones.</param>
        /// <returns>The calculated influence value (can be positive or negative).</returns>
        int CalculateNeighborInfluence(ZoneLoyalty zoneLoyalty, IEnumerable<ZoneLoyalty> neighborLoyalties);

        /// <summary>
        /// Applies neighbor influence to zone loyalty.
        /// </summary>
        /// <param name="zoneLoyalty">The zone loyalty to modify.</param>
        /// <param name="neighborLoyalties">The loyalty data of adjacent zones.</param>
        void ApplyNeighborInfluence(ZoneLoyalty zoneLoyalty, IEnumerable<ZoneLoyalty> neighborLoyalties);

        /// <summary>
        /// Applies the penalty for recent conquest.
        /// </summary>
        /// <param name="zoneLoyalty">The zone loyalty to modify.</param>
        void ApplyConquestPenalty(ZoneLoyalty zoneLoyalty);

        /// <summary>
        /// Calculates the total value of multiple modifiers.
        /// </summary>
        /// <param name="modifiers">The modifiers to sum.</param>
        /// <returns>The total modifier value.</returns>
        int CalculateTotalModifierValue(IEnumerable<LoyaltyModifier> modifiers);

        /// <summary>
        /// Gets a description of a loyalty level change.
        /// </summary>
        /// <param name="oldLevel">The previous loyalty level.</param>
        /// <param name="newLevel">The new loyalty level.</param>
        /// <returns>A description of the change, or null if no change occurred.</returns>
        string? GetLevelChangeDescription(LoyaltyLevel oldLevel, LoyaltyLevel newLevel);
    }
}
