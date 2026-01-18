using FactionWars.Lieutenants.Models;

namespace FactionWars.Lieutenants.Interfaces
{
    /// <summary>
    /// Service for handling lieutenant defection mechanics.
    /// </summary>
    public interface IDefectionService
    {
        /// <summary>
        /// Calculates the chance of a lieutenant defecting.
        /// </summary>
        /// <param name="lieutenant">The lieutenant to evaluate.</param>
        /// <param name="bribeAmount">Optional bribe amount to increase the chance.</param>
        /// <returns>A value between 0.0 (no chance) and 1.0 (guaranteed).</returns>
        double CalculateDefectionChance(Lieutenant lieutenant, int bribeAmount = 0);

        /// <summary>
        /// Attempts to make a lieutenant defect to a new faction.
        /// </summary>
        /// <param name="lieutenant">The lieutenant to attempt to flip.</param>
        /// <param name="targetFactionId">The faction to defect to.</param>
        /// <param name="bribeAmount">Optional bribe amount to increase the chance.</param>
        /// <returns>The result of the defection attempt.</returns>
        DefectionResult AttemptDefection(Lieutenant lieutenant, string targetFactionId, int bribeAmount = 0);

        /// <summary>
        /// Checks whether a defection attempt can be made on a lieutenant.
        /// </summary>
        /// <param name="lieutenant">The lieutenant to check.</param>
        /// <param name="targetFactionId">The faction that would attempt to flip them.</param>
        /// <returns>True if a defection attempt is valid.</returns>
        bool CanAttemptDefection(Lieutenant? lieutenant, string targetFactionId);

        /// <summary>
        /// Calculates the bribe amount required to guarantee a successful defection.
        /// </summary>
        /// <param name="lieutenant">The lieutenant to evaluate.</param>
        /// <returns>The required bribe amount in dollars.</returns>
        int GetRequiredBribeForGuaranteedDefection(Lieutenant lieutenant);

        /// <summary>
        /// Checks if a lieutenant is a former member of the specified faction.
        /// </summary>
        /// <param name="lieutenant">The lieutenant to check.</param>
        /// <param name="factionId">The faction ID to check against.</param>
        /// <returns>True if the lieutenant was originally from this faction but has defected.</returns>
        bool IsFormerMember(Lieutenant? lieutenant, string factionId);
    }
}
