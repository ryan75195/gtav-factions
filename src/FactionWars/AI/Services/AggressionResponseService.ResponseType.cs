using FactionWars.AI.Models;
using FactionWars.Factions.Models;

namespace FactionWars.AI.Services
{
    public partial class AggressionResponseService
    {
        private AggressionResponseType DetermineResponseType(float threatLevel, AIContext context)
        {
            int availableTroops = context.FactionState.TroopCount;
            var factionType = GetFactionType(context);

            // Very low threat or no troops = no response
            if (threatLevel < LowThreatThreshold * 0.5f || availableTroops < MinimumDefenseTroops)
            {
                return AggressionResponseType.None;
            }

            // Adjust thresholds based on faction type
            float aggressionModifier = GetAggressionModifier(factionType);
            float adjustedHighThreshold = HighThreatThreshold * (1f - aggressionModifier * 0.3f);

            // High threat and enough troops for counter-attack
            if (threatLevel >= adjustedHighThreshold && availableTroops >= MinimumTroopsForRetaliation)
            {
                // Trevor escalates quickly
                if (factionType == FactionType.Trevor && threatLevel >= adjustedHighThreshold * 0.8f)
                {
                    return AggressionResponseType.Retaliation;
                }

                return AggressionResponseType.Retaliation;
            }

            // Medium threat or limited troops = defensive
            if (threatLevel >= LowThreatThreshold)
            {
                return AggressionResponseType.Defensive;
            }

            return AggressionResponseType.None;
        }

        private FactionType GetFactionType(AIContext context)
        {
            // Try to determine faction type from context
            var factionName = context.Faction.Name.ToLowerInvariant();

            if (factionName.Contains("trevor") || factionName.Contains("philips"))
                return FactionType.Trevor;
            if (factionName.Contains("michael") || factionName.Contains("santa"))
                return FactionType.Michael;
            if (factionName.Contains("franklin") || factionName.Contains("clinton"))
                return FactionType.Franklin;

            // Default to balanced (Michael)
            return FactionType.Michael;
        }

        private float GetAggressionModifier(FactionType type)
        {
            switch (type)
            {
                case FactionType.Trevor:
                    return 0.8f; // Very aggressive
                case FactionType.Franklin:
                    return 0.5f; // Balanced
                case FactionType.Michael:
                default:
                    return 0.3f; // More defensive
            }
        }

    }
}
