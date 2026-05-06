using System;
using System.Collections.Generic;
using FactionWars.AI.Models;
using FactionWars.Territory.Models;

namespace FactionWars.AI.Services
{
    public partial class CapitalDeploymentService
    {
        private const float ContestedAttackMultiplier = 0.45f;
        private const float LowResourceExpansionBonus = 0.25f;
        private const float TrevorNorthernExpansionBonus = 0.2f;
        private const int LowResourceTroopThreshold = 20;

        private static readonly ISet<string> TrevorNorthernExpansionZones = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            "alamo_sea",
            "grapeseed",
            "paleto_forest",
            "chiliad_wilderness",
            "paleto_bay",
            "grand_senora_desert"
        };

        private static float GetExpansionBonus(Zone target, AIContext context, int enemyDefenders)
        {
            float bonus = 0f;

            if (IsCheapExpansionTarget(target, enemyDefenders) && context.FactionState.TroopCount <= LowResourceTroopThreshold)
            {
                bonus += LowResourceExpansionBonus;
            }

            if (IsTrevorNorthernExpansion(context, target))
            {
                bonus += TrevorNorthernExpansionBonus;
            }

            return bonus;
        }

        private static float GetAttritionMultiplier(Zone target)
        {
            return target.IsContested ? ContestedAttackMultiplier : 1f;
        }

        private static bool IsCheapExpansionTarget(Zone target, int enemyDefenders)
        {
            return enemyDefenders == 0 || target.OwnerFactionId == null;
        }

        private static bool IsTrevorNorthernExpansion(AIContext context, Zone target)
        {
            return string.Equals(context.Faction.Id, "trevor", StringComparison.OrdinalIgnoreCase)
                && TrevorNorthernExpansionZones.Contains(target.Id);
        }
    }
}
