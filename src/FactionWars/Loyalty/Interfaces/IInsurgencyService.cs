using FactionWars.Loyalty.Models;

namespace FactionWars.Loyalty.Interfaces
{
    /// <summary>
    /// Service for managing insurgency risk and events in zones.
    /// </summary>
    public interface IInsurgencyService
    {
        /// <summary>
        /// Calculates the risk increase based on zone loyalty.
        /// </summary>
        /// <param name="loyalty">The zone's loyalty state.</param>
        /// <returns>The risk increase amount.</returns>
        int CalculateRiskFromLoyalty(ZoneLoyalty loyalty);

        /// <summary>
        /// Calculates risk reduction based on high loyalty.
        /// </summary>
        /// <param name="loyalty">The zone's loyalty state.</param>
        /// <returns>The risk reduction amount.</returns>
        int CalculateRiskReduction(ZoneLoyalty loyalty);

        /// <summary>
        /// Updates the daily risk for a zone based on its loyalty.
        /// </summary>
        /// <param name="risk">The insurgency risk to update.</param>
        /// <param name="loyalty">The zone's loyalty state.</param>
        void UpdateDailyRisk(InsurgencyRisk risk, ZoneLoyalty loyalty);

        /// <summary>
        /// Checks whether an uprising should occur.
        /// </summary>
        /// <param name="risk">The insurgency risk to check.</param>
        /// <param name="rollValue">A random value between 0 and 1 for determining outcome.</param>
        /// <returns>True if an uprising is triggered, false otherwise.</returns>
        bool CheckForUprising(InsurgencyRisk risk, float rollValue);

        /// <summary>
        /// Applies a suppression effect to reduce insurgency risk.
        /// </summary>
        /// <param name="risk">The insurgency risk to affect.</param>
        /// <param name="suppressionStrength">The strength of the suppression (positive value).</param>
        void ApplySuppressionEffect(InsurgencyRisk risk, int suppressionStrength);

        /// <summary>
        /// Creates an uprising event from the current risk state.
        /// </summary>
        /// <param name="risk">The insurgency risk that triggered the uprising.</param>
        /// <returns>A new insurgency event.</returns>
        InsurgencyEvent CreateUprisingEvent(InsurgencyRisk risk);

        /// <summary>
        /// Calculates the strength of an uprising based on risk level.
        /// </summary>
        /// <param name="risk">The insurgency risk.</param>
        /// <returns>The uprising strength value.</returns>
        int CalculateUprisingStrength(InsurgencyRisk risk);
    }
}
