using System;
using FactionWars.AI.Models;
using FactionWars.Territory.Models;

namespace FactionWars.AI.Strategies
{
    public abstract partial class BaseAIStrategy
    {
        public virtual bool ShouldDefend(Zone zone, AIContext context)
        {
            if (zone == null)
                throw new ArgumentNullException(nameof(zone));
            if (context == null)
                throw new ArgumentNullException(nameof(context));

            // Can only defend owned zones
            if (zone.OwnerFactionId != context.Faction.Id)
            {
                return false;
            }

            // Contested zones always need defense
            if (zone.IsContested)
            {
                return true;
            }

            // High value zones might need preemptive defense
            float defenseThreshold = 0.7f * (1f - _aggressiveness); // Less aggressive = more defensive
            float zoneValue = zone.StrategicValue / MaxStrategicValue;

            return zoneValue >= defenseThreshold;
        }

        /// <summary>
        /// Calculates how many troops to commit to a specific action.
        /// </summary>
    }
}
