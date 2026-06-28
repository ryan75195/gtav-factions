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

        private static readonly Dictionary<string, Dictionary<DefenderRole, string>> Models = new Dictionary<string, Dictionary<DefenderRole, string>>
        {
            // Franklin: Street gang theme (Families, Ballas)
            ["franklin"] = new Dictionary<DefenderRole, string>
            {
                { DefenderRole.Grunt, "g_m_y_famca_01" },      // Families casual - young street thug
                { DefenderRole.Gunner, "g_m_y_famdnf_01" },    // Families DNF - established member
                { DefenderRole.Rifleman, "g_m_y_famfor_01" },     // Families OG - veteran gangster
                { DefenderRole.Rocketeer, "g_m_y_ballasout_01" },   // Ballas enforcer - gang elite
                { DefenderRole.Sniper, "g_m_y_famfor_01" }   // Families sniper - perch specialist
            },

            // Trevor: Rural/Biker theme (Lost MC, hillbillies)
            ["trevor"] = new Dictionary<DefenderRole, string>
            {
                { DefenderRole.Grunt, "a_m_m_hillbilly_01" },  // Hillbilly - rural expendable
                { DefenderRole.Gunner, "g_m_y_lost_01" },      // Lost MC member - biker
                { DefenderRole.Rifleman, "g_m_y_lost_02" },       // Lost MC veteran - experienced biker
                { DefenderRole.Rocketeer, "g_m_y_lost_03" },        // Lost MC leader - biker boss
                { DefenderRole.Sniper, "g_m_y_lost_02" }   // Lost MC sniper - perch specialist
            },

            // Michael: Professional theme (Merryweather, suits)
            ["michael"] = new Dictionary<DefenderRole, string>
            {
                { DefenderRole.Grunt, "g_m_m_armboss_01" },    // Armenian thug - low-level muscle
                { DefenderRole.Gunner, "s_m_y_blackops_01" },  // Blackops soldier - professional
                { DefenderRole.Rifleman, "s_m_y_blackops_02" },   // Blackops veteran - elite soldier
                { DefenderRole.Rocketeer, "s_m_m_highsec_01" },     // High security - top tier pro
                { DefenderRole.Sniper, "s_m_y_blackops_03" }   // Blackops sniper - perch specialist
            }
        };

        /// <summary>
        /// Gets the ped model name for a faction and defender tier.
        /// </summary>
        /// <param name="factionId">The faction ID (case-insensitive).</param>
        /// <param name="tier">The defender tier.</param>
        /// <returns>The ped model name, or FallbackModel if faction is unknown.</returns>
        public static string GetModel(string factionId, DefenderRole tier)
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
