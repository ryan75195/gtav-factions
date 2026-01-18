namespace FactionWars.Loyalty.Models
{
    /// <summary>
    /// Types of insurgency events that can occur in a zone.
    /// </summary>
    public enum InsurgencyEventType
    {
        /// <summary>
        /// Full armed uprising attempting to flip zone control.
        /// </summary>
        Uprising = 0,

        /// <summary>
        /// Sabotage of faction resources or infrastructure.
        /// </summary>
        Sabotage = 1,

        /// <summary>
        /// Civilian protest that reduces faction control.
        /// </summary>
        Protest = 2
    }
}
