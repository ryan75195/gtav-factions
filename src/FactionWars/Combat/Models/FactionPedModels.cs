using System.Collections.Generic;
using FactionWars.Core.Models;

namespace FactionWars.Combat.Models
{
    /// <summary>
    /// Provides faction-specific ped models for each defender tier.
    /// Each faction has a distinct visual theme with tier-based progression.
    /// </summary>
    public static class FactionPedModels
    {
        /// <summary>
        /// Fallback model used when faction is unknown or null.
        /// </summary>
        public const string FallbackModel = "a_m_m_business_01";

        private static readonly Dictionary<string, Dictionary<DefenderTier, string>> Models = new Dictionary<string, Dictionary<DefenderTier, string>>
        {
            // Franklin: Street gang theme (Families, Ballas)
            ["franklin"] = new Dictionary<DefenderTier, string>
            {
                { DefenderTier.Basic, "g_m_y_famca_01" },      // Families casual - young street thug
                { DefenderTier.Medium, "g_m_y_famdnf_01" },    // Families DNF - established member
                { DefenderTier.Heavy, "g_m_y_famfor_01" },     // Families OG - veteran gangster
                { DefenderTier.Elite, "g_m_y_ballasout_01" }   // Ballas enforcer - gang elite
            },

            // Trevor: Rural/Biker theme (Lost MC, hillbillies)
            ["trevor"] = new Dictionary<DefenderTier, string>
            {
                { DefenderTier.Basic, "a_m_m_hillbilly_01" },  // Hillbilly - rural expendable
                { DefenderTier.Medium, "g_m_y_lost_01" },      // Lost MC member - biker
                { DefenderTier.Heavy, "g_m_y_lost_02" },       // Lost MC veteran - experienced biker
                { DefenderTier.Elite, "g_m_y_lost_03" }        // Lost MC leader - biker boss
            },

            // Michael: Professional theme (Merryweather, suits)
            ["michael"] = new Dictionary<DefenderTier, string>
            {
                { DefenderTier.Basic, "g_m_m_armboss_01" },    // Armenian thug - low-level muscle
                { DefenderTier.Medium, "s_m_y_blackops_01" },  // Blackops soldier - professional
                { DefenderTier.Heavy, "s_m_y_blackops_02" },   // Blackops veteran - elite soldier
                { DefenderTier.Elite, "s_m_m_highsec_01" }     // High security - top tier pro
            }
        };

        /// <summary>
        /// Gets the ped model name for a faction and defender tier.
        /// </summary>
        /// <param name="factionId">The faction ID (case-insensitive).</param>
        /// <param name="tier">The defender tier.</param>
        /// <returns>The ped model name, or FallbackModel if faction is unknown.</returns>
        public static string GetModel(string factionId, DefenderTier tier)
        {
            if (string.IsNullOrEmpty(factionId))
                return FallbackModel;

            if (!Models.TryGetValue(factionId.ToLowerInvariant(), out var tierModels))
                return FallbackModel;

            if (!tierModels.TryGetValue(tier, out var model))
                return FallbackModel;

            return model;
        }
    }
}
