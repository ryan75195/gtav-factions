using FactionWars.Loyalty.Models;

namespace FactionWars.Loyalty.Interfaces
{
    /// <summary>
    /// Service responsible for managing zone integration after capture.
    /// </summary>
    public interface IZoneIntegrationService
    {
        /// <summary>
        /// Calculates the integration difficulty based on loyalty and transfer history.
        /// </summary>
        /// <param name="loyalty">The current zone loyalty state.</param>
        /// <param name="transferCount">Number of times control has been transferred.</param>
        /// <returns>The calculated integration difficulty.</returns>
        IntegrationDifficulty CalculateDifficulty(ZoneLoyalty loyalty, int transferCount);

        /// <summary>
        /// Calculates the daily integration progress based on difficulty.
        /// </summary>
        /// <param name="state">The zone integration state.</param>
        /// <returns>The amount of progress to add.</returns>
        int CalculateDailyProgress(ZoneIntegrationState state);

        /// <summary>
        /// Applies daily integration progress to the state and advances the day counter.
        /// </summary>
        /// <param name="state">The zone integration state to update.</param>
        void ApplyDailyProgress(ZoneIntegrationState state);

        /// <summary>
        /// Calculates the resource production penalty based on integration progress.
        /// </summary>
        /// <param name="state">The zone integration state.</param>
        /// <returns>A multiplier (0.25-1.0) for resource production.</returns>
        float CalculateResourcePenalty(ZoneIntegrationState state);

        /// <summary>
        /// Applies an integration setback due to insurgency.
        /// </summary>
        /// <param name="state">The zone integration state.</param>
        /// <param name="insurgencyLevel">The level of the insurgency.</param>
        void ApplyInsurgencySetback(ZoneIntegrationState state, InsurgencyLevel insurgencyLevel);

        /// <summary>
        /// Creates an integration state from a zone loyalty record.
        /// </summary>
        /// <param name="loyalty">The zone loyalty data.</param>
        /// <returns>A new zone integration state.</returns>
        ZoneIntegrationState CreateIntegrationState(ZoneLoyalty loyalty);

        /// <summary>
        /// Updates zone loyalty based on integration progress.
        /// </summary>
        /// <param name="loyalty">The zone loyalty to update.</param>
        /// <param name="state">The current integration state.</param>
        void UpdateLoyaltyFromIntegration(ZoneLoyalty loyalty, ZoneIntegrationState state);

        /// <summary>
        /// Calculates the defense bonus/penalty based on integration progress.
        /// </summary>
        /// <param name="state">The zone integration state.</param>
        /// <returns>A bonus (positive) or penalty (negative) percentage.</returns>
        int CalculateDefenseBonus(ZoneIntegrationState state);
    }
}
