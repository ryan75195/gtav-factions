namespace FactionWars.Escalation.Models
{
    /// <summary>
    /// Represents the escalation tier for a faction.
    /// Escalation tiers determine what weapons and vehicles are available to a faction.
    /// Higher tiers unlock more powerful equipment.
    /// </summary>
    public enum EscalationTier
    {
        /// <summary>
        /// Tier 1 - Basic equipment (pistols, basic cars).
        /// Default starting tier for all factions.
        /// </summary>
        Tier1 = 0,

        /// <summary>
        /// Tier 2 - Standard equipment (SMGs, sedans).
        /// Requires 1000+ escalation points.
        /// </summary>
        Tier2 = 1,

        /// <summary>
        /// Tier 3 - Advanced equipment (assault rifles, SUVs).
        /// Requires 3000+ escalation points.
        /// </summary>
        Tier3 = 2,

        /// <summary>
        /// Tier 4 - Heavy equipment (LMGs, armored vehicles).
        /// Requires 6000+ escalation points.
        /// </summary>
        Tier4 = 3,

        /// <summary>
        /// Tier 5 - Military grade (explosives, military vehicles).
        /// Requires 9000+ escalation points.
        /// </summary>
        Tier5 = 4
    }
}
