using System;

namespace FactionWars.Balance.Models
{
    /// <summary>
    /// Event arguments for balance configuration changes.
    /// </summary>
    public class BalanceConfigurationChangedEventArgs : EventArgs
    {
        /// <summary>
        /// The previous configuration before the change.
        /// </summary>
        public BalanceConfiguration OldConfiguration { get; }

        /// <summary>
        /// The new configuration after the change.
        /// </summary>
        public BalanceConfiguration NewConfiguration { get; }

        /// <summary>
        /// Creates event arguments for a configuration change.
        /// </summary>
        /// <param name="oldConfiguration">The previous configuration.</param>
        /// <param name="newConfiguration">The new configuration.</param>
        public BalanceConfigurationChangedEventArgs(BalanceConfiguration oldConfiguration, BalanceConfiguration newConfiguration)
        {
            OldConfiguration = oldConfiguration ?? throw new ArgumentNullException(nameof(oldConfiguration));
            NewConfiguration = newConfiguration ?? throw new ArgumentNullException(nameof(newConfiguration));
        }
    }
}
