using System;
using System.Collections.Generic;

namespace FactionWars.Factions.Models
{
    /// <summary>
    /// Provides detailed information and bonuses for each faction type.
    /// Uses a cached dictionary for efficient lookups.
    /// </summary>
    public class FactionTypeInfo
    {
        private static readonly Dictionary<FactionType, FactionTypeInfo> _infoCache =
            new Dictionary<FactionType, FactionTypeInfo>();

        static FactionTypeInfo()
        {
            InitializeCache();
        }

        /// <summary>
        /// The name of the faction leader.
        /// </summary>
        public string LeaderName { get; }

        /// <summary>
        /// The display name for this faction.
        /// </summary>
        public string FactionName { get; }

        /// <summary>
        /// A description of this faction's characteristics and playstyle.
        /// </summary>
        public string Description { get; }

        /// <summary>
        /// The color used to represent this faction on the map and UI.
        /// </summary>
        public FactionColor Color { get; }

        /// <summary>
        /// Multiplier for income generation (1.0 = normal).
        /// </summary>
        public float IncomeBonus { get; }

        /// <summary>
        /// Multiplier for combat effectiveness (1.0 = normal).
        /// </summary>
        public float CombatBonus { get; }

        /// <summary>
        /// Multiplier for defensive capabilities (1.0 = normal).
        /// </summary>
        public float DefenseBonus { get; }

        /// <summary>
        /// Multiplier for unit movement and reinforcement speed (1.0 = normal).
        /// </summary>
        public float MobilityBonus { get; }

        /// <summary>
        /// Multiplier for recruitment rate (1.0 = normal).
        /// </summary>
        public float RecruitmentBonus { get; }

        private FactionTypeInfo(
            string leaderName,
            string factionName,
            string description,
            FactionColor color,
            float incomeBonus,
            float combatBonus,
            float defenseBonus,
            float mobilityBonus,
            float recruitmentBonus)
        {
            LeaderName = leaderName;
            FactionName = factionName;
            Description = description;
            Color = color;
            IncomeBonus = incomeBonus;
            CombatBonus = combatBonus;
            DefenseBonus = defenseBonus;
            MobilityBonus = mobilityBonus;
            RecruitmentBonus = recruitmentBonus;
        }

        /// <summary>
        /// Gets the faction type information for the specified type.
        /// Returns a cached instance for efficiency.
        /// </summary>
        /// <param name="type">The faction type to get info for.</param>
        /// <returns>The faction type information.</returns>
        /// <exception cref="ArgumentOutOfRangeException">Thrown if the faction type is invalid.</exception>
        public static FactionTypeInfo GetInfo(FactionType type)
        {
            if (_infoCache.TryGetValue(type, out var info))
            {
                return info;
            }

            throw new ArgumentOutOfRangeException(nameof(type), type, "Invalid faction type.");
        }

        private static void InitializeCache()
        {
            // Michael De Santa - Calculated strategist
            // Focus: Income and Defense, methodical approach
            _infoCache[FactionType.Michael] = new FactionTypeInfo(
                leaderName: "Michael De Santa",
                factionName: "De Santa Family",
                description: "A calculated criminal empire focused on high-value operations and strategic defense. Michael's experience brings financial expertise and tactical planning.",
                color: new FactionColor(0, 100, 255), // Blue - sophisticated, professional
                incomeBonus: 1.25f,     // +25% income - heist expertise
                combatBonus: 1.0f,      // Neutral combat
                defenseBonus: 1.15f,    // +15% defense - strategic mindset
                mobilityBonus: 1.0f,    // Neutral mobility
                recruitmentBonus: 1.0f  // Neutral recruitment
            );

            // Trevor Philips - Aggressive berserker
            // Focus: Combat and Recruitment, chaotic offense
            _infoCache[FactionType.Trevor] = new FactionTypeInfo(
                leaderName: "Trevor Philips",
                factionName: "Trevor Philips Industries",
                description: "An aggressive and unpredictable operation that thrives on chaos and violence. Trevor's rage-fueled approach excels in combat but struggles with discipline.",
                color: new FactionColor(255, 128, 0), // Orange - chaotic, aggressive
                incomeBonus: 0.9f,      // -10% income - chaotic operations
                combatBonus: 1.35f,     // +35% combat - rage and violence
                defenseBonus: 0.85f,    // -15% defense - offense over defense
                mobilityBonus: 1.0f,    // Neutral mobility
                recruitmentBonus: 1.2f  // +20% recruitment - attracts violent followers
            );

            // Franklin Clinton - Opportunistic adapter
            // Focus: Balance and Mobility, adaptable approach
            _infoCache[FactionType.Franklin] = new FactionTypeInfo(
                leaderName: "Franklin Clinton",
                factionName: "Clinton Organization",
                description: "An opportunistic organization with balanced capabilities and exceptional mobility. Franklin's street smarts and driving skills enable rapid response and adaptable tactics.",
                color: new FactionColor(0, 200, 100), // Green - growth, balance
                incomeBonus: 1.1f,      // +10% income - opportunistic
                combatBonus: 1.1f,      // +10% combat - capable
                defenseBonus: 1.1f,     // +10% defense - well-rounded
                mobilityBonus: 1.25f,   // +25% mobility - driving expertise
                recruitmentBonus: 1.05f // +5% recruitment - charismatic
            );
        }
    }
}
