namespace FactionWars.Loyalty.Models
{
    /// <summary>
    /// Possible outcomes of an insurgency event.
    /// </summary>
    public enum InsurgencyOutcome
    {
        /// <summary>
        /// The insurgency was suppressed by the controlling faction.
        /// </summary>
        Suppressed = 0,

        /// <summary>
        /// The insurgency succeeded and the zone flipped to the insurgent faction.
        /// </summary>
        ZoneFlipped = 1,

        /// <summary>
        /// The insurgency was resolved through negotiation.
        /// </summary>
        Negotiated = 2
    }
}
