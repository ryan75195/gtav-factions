namespace FactionWars.Economy.Models
{
    /// <summary>
    /// Represents the different types of resources that factions can generate and use.
    /// Each resource type serves a different purpose in the faction warfare system.
    /// </summary>
    public enum ResourceType
    {
        /// <summary>
        /// Cash is the primary currency used for purchasing operations,
        /// paying troops, and general faction expenses.
        /// </summary>
        Cash = 0,

        /// <summary>
        /// Recruitment points used to hire new troops into the faction's army.
        /// Generated primarily from residential zones.
        /// </summary>
        Recruitment = 1,

        /// <summary>
        /// Weapons stockpile that enhances military strength.
        /// Generated primarily from industrial zones and ports (smuggling).
        /// </summary>
        Weapons = 2
    }
}
