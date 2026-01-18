namespace FactionWars.Tension.Models
{
    /// <summary>
    /// Defines the types of bribery operations that can be conducted.
    /// Each type has different costs, risks, and effects.
    /// </summary>
    public enum BriberyTargetType
    {
        /// <summary>
        /// Bribe an intelligence asset to gain information about enemy faction.
        /// Provides visibility into enemy movements and plans.
        /// </summary>
        IntelligenceAsset = 0,

        /// <summary>
        /// Bribe someone to divert faction resources.
        /// Siphons a percentage of enemy resource generation.
        /// </summary>
        ResourceDiversion = 1,

        /// <summary>
        /// Bribe a faction member to defect.
        /// Recruits an enemy member to your faction permanently.
        /// </summary>
        DefectorRecruitment = 2,

        /// <summary>
        /// Bribe intermediaries to reduce tensions.
        /// Directly reduces the tension level between factions.
        /// </summary>
        TensionReduction = 3
    }
}
