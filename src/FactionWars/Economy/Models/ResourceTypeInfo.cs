using System;
using System.Collections.Generic;

namespace FactionWars.Economy.Models
{
    /// <summary>
    /// Provides metadata and configuration information for each resource type.
    /// Includes display names, descriptions, symbols, base generation rates, and storage caps.
    /// </summary>
    public class ResourceTypeInfo
    {
        // Cached instances for each resource type
        private static readonly Dictionary<ResourceType, ResourceTypeInfo> CachedInfos = new Dictionary<ResourceType, ResourceTypeInfo>
        {
            { ResourceType.Cash, new ResourceTypeInfo(ResourceType.Cash) },
            { ResourceType.Recruitment, new ResourceTypeInfo(ResourceType.Recruitment) },
            { ResourceType.Weapons, new ResourceTypeInfo(ResourceType.Weapons) }
        };

        // Base generation rates per tick (5 minutes) per zone
        private const int CashBaseRate = 100;
        private const int RecruitmentBaseRate = 10;
        private const int WeaponsBaseRate = 5;

        // Default storage caps
        private const int CashDefaultCap = 100000;
        private const int RecruitmentDefaultCap = 1000;
        private const int WeaponsDefaultCap = 500;

        /// <summary>
        /// The resource type this info describes.
        /// </summary>
        public ResourceType ResourceType { get; }

        /// <summary>
        /// Display name for UI presentation.
        /// </summary>
        public string DisplayName { get; }

        /// <summary>
        /// Detailed description of what this resource does.
        /// </summary>
        public string Description { get; }

        /// <summary>
        /// Symbol used for display (e.g., "$" for cash).
        /// Empty string if no symbol applies.
        /// </summary>
        public string Symbol { get; }

        /// <summary>
        /// Base generation rate per tick per zone (before modifiers).
        /// </summary>
        public int BaseGenerationRate { get; }

        /// <summary>
        /// Default maximum storage capacity for this resource.
        /// </summary>
        public int DefaultCap { get; }

        /// <summary>
        /// Creates a new ResourceTypeInfo for the specified resource type.
        /// </summary>
        /// <param name="resourceType">The resource type to describe.</param>
        public ResourceTypeInfo(ResourceType resourceType)
        {
            ResourceType = resourceType;

            switch (resourceType)
            {
                case ResourceType.Cash:
                    DisplayName = "Cash";
                    Description = "Primary currency for faction operations, troop payments, and purchases.";
                    Symbol = "$";
                    BaseGenerationRate = CashBaseRate;
                    DefaultCap = CashDefaultCap;
                    break;

                case ResourceType.Recruitment:
                    DisplayName = "Recruitment Points";
                    Description = "Points used to recruit new soldiers into the faction's army.";
                    Symbol = string.Empty;
                    BaseGenerationRate = RecruitmentBaseRate;
                    DefaultCap = RecruitmentDefaultCap;
                    break;

                case ResourceType.Weapons:
                    DisplayName = "Weapons";
                    Description = "Military hardware that enhances troop effectiveness in combat.";
                    Symbol = string.Empty;
                    BaseGenerationRate = WeaponsBaseRate;
                    DefaultCap = WeaponsDefaultCap;
                    break;

                default:
                    throw new ArgumentOutOfRangeException(nameof(resourceType), resourceType,
                        "Unknown resource type.");
            }
        }

        /// <summary>
        /// Gets the cached ResourceTypeInfo for the specified resource type.
        /// Uses caching for efficiency.
        /// </summary>
        /// <param name="resourceType">The resource type to get info for.</param>
        /// <returns>The ResourceTypeInfo for the specified type.</returns>
        public static ResourceTypeInfo GetInfo(ResourceType resourceType)
        {
            if (CachedInfos.TryGetValue(resourceType, out var info))
            {
                return info;
            }

            throw new ArgumentOutOfRangeException(nameof(resourceType), resourceType,
                "Unknown resource type.");
        }

        public override string ToString()
        {
            return $"{DisplayName} (Base: {BaseGenerationRate}/tick, Cap: {DefaultCap})";
        }
    }
}
