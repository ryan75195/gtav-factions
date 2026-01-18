using System;
using FactionWars.AI.Models;
using FactionWars.Balance.Models;

namespace FactionWars.Balance.Interfaces
{
    /// <summary>
    /// Service for managing game balance configuration.
    /// Provides access to current settings and allows applying presets or custom configurations.
    /// </summary>
    public interface IBalanceConfigurationService
    {
        /// <summary>
        /// Gets the current balance configuration.
        /// Returns a clone to prevent unintended modifications.
        /// </summary>
        BalanceConfiguration CurrentConfiguration { get; }

        /// <summary>
        /// Event raised when the configuration changes.
        /// </summary>
        event EventHandler<BalanceConfigurationChangedEventArgs> ConfigurationChanged;

        /// <summary>
        /// Applies a preset configuration for the specified difficulty.
        /// </summary>
        /// <param name="difficulty">The difficulty level to apply.</param>
        void ApplyPreset(AIDifficulty difficulty);

        /// <summary>
        /// Updates the configuration with custom values.
        /// </summary>
        /// <param name="configuration">The new configuration to apply.</param>
        /// <exception cref="ArgumentNullException">Thrown if configuration is null.</exception>
        /// <exception cref="InvalidOperationException">Thrown if configuration is invalid.</exception>
        void UpdateConfiguration(BalanceConfiguration configuration);

        /// <summary>
        /// Resets the configuration to default values.
        /// </summary>
        void Reset();

        /// <summary>
        /// Gets the effective resource multiplier for a faction.
        /// </summary>
        /// <param name="isPlayerFaction">Whether the faction is player-controlled.</param>
        /// <returns>The resource generation multiplier.</returns>
        float GetEffectiveResourceMultiplier(bool isPlayerFaction);

        /// <summary>
        /// Gets the effective combat multiplier for a faction.
        /// </summary>
        /// <param name="isPlayerFaction">Whether the faction is player-controlled.</param>
        /// <returns>The combat effectiveness multiplier.</returns>
        float GetEffectiveCombatMultiplier(bool isPlayerFaction);

        /// <summary>
        /// Gets the effective defense multiplier for a faction.
        /// </summary>
        /// <param name="isPlayerFaction">Whether the faction is player-controlled.</param>
        /// <returns>The defense multiplier.</returns>
        float GetEffectiveDefenseMultiplier(bool isPlayerFaction);

        /// <summary>
        /// Creates a snapshot of the current configuration.
        /// </summary>
        /// <returns>A copy of the current configuration.</returns>
        BalanceConfiguration CreateSnapshot();

        /// <summary>
        /// Restores a previously saved configuration snapshot.
        /// </summary>
        /// <param name="snapshot">The configuration to restore.</param>
        /// <exception cref="ArgumentNullException">Thrown if snapshot is null.</exception>
        void RestoreSnapshot(BalanceConfiguration snapshot);
    }
}
