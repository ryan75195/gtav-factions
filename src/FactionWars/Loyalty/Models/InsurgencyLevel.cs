namespace FactionWars.Loyalty.Models
{
    /// <summary>
    /// Represents the severity level of insurgency risk in a zone.
    /// Higher levels indicate greater probability of an uprising.
    /// </summary>
    public enum InsurgencyLevel
    {
        /// <summary>
        /// Risk level 0-24. No significant insurgency threat.
        /// </summary>
        None = 0,

        /// <summary>
        /// Risk level 25-49. Minor unrest, 5% chance of uprising.
        /// </summary>
        Low = 1,

        /// <summary>
        /// Risk level 50-74. Moderate unrest, 15% chance of uprising.
        /// </summary>
        Medium = 2,

        /// <summary>
        /// Risk level 75-89. Significant unrest, 30% chance of uprising.
        /// </summary>
        High = 3,

        /// <summary>
        /// Risk level 90-100. Critical unrest, 50% chance of uprising.
        /// </summary>
        Critical = 4
    }
}
