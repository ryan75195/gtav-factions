using FactionWars.Lieutenants.Models;

namespace FactionWars.Lieutenants.Interfaces
{
    /// <summary>
    /// Calculates zone bonuses provided by lieutenants based on their traits and level.
    /// </summary>
    public interface ILieutenantZoneEffects
    {
        /// <summary>
        /// Gets the attack power bonus for a zone commanded by this lieutenant.
        /// </summary>
        /// <param name="lieutenant">The lieutenant, or null for baseline.</param>
        /// <returns>Multiplier where 1.0 = no bonus.</returns>
        float GetAttackBonus(Lieutenant? lieutenant);

        /// <summary>
        /// Gets the defense bonus for a zone commanded by this lieutenant.
        /// </summary>
        /// <param name="lieutenant">The lieutenant, or null for baseline.</param>
        /// <returns>Multiplier where 1.0 = no bonus.</returns>
        float GetDefenseBonus(Lieutenant? lieutenant);

        /// <summary>
        /// Gets the resource generation bonus for a zone commanded by this lieutenant.
        /// </summary>
        /// <param name="lieutenant">The lieutenant, or null for baseline.</param>
        /// <returns>Multiplier where 1.0 = no bonus.</returns>
        float GetResourceBonus(Lieutenant? lieutenant);

        /// <summary>
        /// Gets the population loyalty gain bonus for a zone commanded by this lieutenant.
        /// </summary>
        /// <param name="lieutenant">The lieutenant, or null for baseline.</param>
        /// <returns>Multiplier where 1.0 = no bonus.</returns>
        float GetLoyaltyBonus(Lieutenant? lieutenant);

        /// <summary>
        /// Gets the intelligence gathering bonus for a zone commanded by this lieutenant.
        /// </summary>
        /// <param name="lieutenant">The lieutenant, or null for baseline.</param>
        /// <returns>Multiplier where 1.0 = no bonus.</returns>
        float GetIntelligenceBonus(Lieutenant? lieutenant);

        /// <summary>
        /// Gets the covert operations effectiveness bonus for a zone commanded by this lieutenant.
        /// </summary>
        /// <param name="lieutenant">The lieutenant, or null for baseline.</param>
        /// <returns>Multiplier where 1.0 = no bonus.</returns>
        float GetCovertOpsBonus(Lieutenant? lieutenant);

        /// <summary>
        /// Gets the attack deterrence bonus (reduces chance of enemy attacks).
        /// </summary>
        /// <param name="lieutenant">The lieutenant, or null for baseline.</param>
        /// <returns>Multiplier where 1.0 = no bonus.</returns>
        float GetAttackDeterrenceBonus(Lieutenant? lieutenant);

        /// <summary>
        /// Gets the experience gain bonus for this lieutenant when assigned to a zone.
        /// </summary>
        /// <param name="lieutenant">The lieutenant, or null for baseline.</param>
        /// <returns>Multiplier where 1.0 = no bonus.</returns>
        float GetExperienceGainBonus(Lieutenant? lieutenant);

        /// <summary>
        /// Gets all zone effects as a summary object.
        /// </summary>
        /// <param name="lieutenant">The lieutenant, or null for baseline.</param>
        /// <returns>Summary of all zone effects.</returns>
        ZoneEffectsSummary GetAllZoneEffects(Lieutenant? lieutenant);
    }
}
