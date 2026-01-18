using System;
using FactionWars.AI.Models;
using FactionWars.Balance.Interfaces;
using FactionWars.Balance.Models;

namespace FactionWars.Balance.Services
{
    /// <summary>
    /// Implementation of balance configuration management.
    /// Handles preset application, custom configuration, and configuration change events.
    /// </summary>
    public class BalanceConfigurationService : IBalanceConfigurationService
    {
        private BalanceConfiguration _currentConfiguration;

        /// <inheritdoc />
        public BalanceConfiguration CurrentConfiguration => _currentConfiguration.Clone();

        /// <inheritdoc />
        public event EventHandler<BalanceConfigurationChangedEventArgs> ConfigurationChanged;

        /// <summary>
        /// Creates a new balance configuration service with default configuration.
        /// </summary>
        public BalanceConfigurationService()
        {
            _currentConfiguration = new BalanceConfiguration();
        }

        /// <summary>
        /// Creates a new balance configuration service with the specified configuration.
        /// </summary>
        /// <param name="initialConfiguration">The initial configuration to use.</param>
        /// <exception cref="ArgumentNullException">Thrown if initialConfiguration is null.</exception>
        public BalanceConfigurationService(BalanceConfiguration initialConfiguration)
        {
            _currentConfiguration = initialConfiguration?.Clone()
                ?? throw new ArgumentNullException(nameof(initialConfiguration));
        }

        /// <inheritdoc />
        public void ApplyPreset(AIDifficulty difficulty)
        {
            var oldConfig = _currentConfiguration.Clone();
            var newConfig = BalancePresets.ForDifficulty(difficulty);

            _currentConfiguration = newConfig;
            OnConfigurationChanged(oldConfig, newConfig.Clone());
        }

        /// <inheritdoc />
        public void UpdateConfiguration(BalanceConfiguration configuration)
        {
            if (configuration == null)
                throw new ArgumentNullException(nameof(configuration));

            var validationResult = configuration.Validate();
            if (!validationResult.IsValid)
            {
                throw new InvalidOperationException(
                    $"Configuration validation failed: {string.Join("; ", validationResult.Errors)}");
            }

            var oldConfig = _currentConfiguration.Clone();
            _currentConfiguration = configuration.Clone();
            OnConfigurationChanged(oldConfig, _currentConfiguration.Clone());
        }

        /// <inheritdoc />
        public void Reset()
        {
            var oldConfig = _currentConfiguration.Clone();
            _currentConfiguration = new BalanceConfiguration();
            OnConfigurationChanged(oldConfig, _currentConfiguration.Clone());
        }

        /// <inheritdoc />
        public float GetEffectiveResourceMultiplier(bool isPlayerFaction)
        {
            return isPlayerFaction ? _currentConfiguration.PlayerResourceMultiplier : 1.0f;
        }

        /// <inheritdoc />
        public float GetEffectiveCombatMultiplier(bool isPlayerFaction)
        {
            return isPlayerFaction ? _currentConfiguration.PlayerCombatMultiplier : 1.0f;
        }

        /// <inheritdoc />
        public float GetEffectiveDefenseMultiplier(bool isPlayerFaction)
        {
            return isPlayerFaction ? _currentConfiguration.PlayerDefenseMultiplier : 1.0f;
        }

        /// <inheritdoc />
        public BalanceConfiguration CreateSnapshot()
        {
            return _currentConfiguration.Clone();
        }

        /// <inheritdoc />
        public void RestoreSnapshot(BalanceConfiguration snapshot)
        {
            if (snapshot == null)
                throw new ArgumentNullException(nameof(snapshot));

            var oldConfig = _currentConfiguration.Clone();
            _currentConfiguration = snapshot.Clone();
            OnConfigurationChanged(oldConfig, _currentConfiguration.Clone());
        }

        /// <summary>
        /// Raises the ConfigurationChanged event.
        /// </summary>
        /// <param name="oldConfig">The previous configuration.</param>
        /// <param name="newConfig">The new configuration.</param>
        protected virtual void OnConfigurationChanged(BalanceConfiguration oldConfig, BalanceConfiguration newConfig)
        {
            ConfigurationChanged?.Invoke(this, new BalanceConfigurationChangedEventArgs(oldConfig, newConfig));
        }
    }
}
