using System;
using FactionWars.AI.Models;
using FactionWars.Territory.Models;

namespace FactionWars.AI.Strategies
{
    public abstract partial class BaseAIStrategy
    {
        public virtual int GetTroopsForAction(AIDecision decision, AIContext context)
        {
            if (decision == null)
                throw new ArgumentNullException(nameof(decision));
            if (context == null)
                throw new ArgumentNullException(nameof(context));

            int availableTroops = context.FactionState.TroopCount;
            if (availableTroops == 0)
            {
                return 0;
            }

            // Base allocation based on priority (30% to 70% of troops based on priority)
            float baseAllocation = 0.3f + (decision.Priority * 0.4f);
            int troops = (int)(availableTroops * baseAllocation);

            // Apply risk tolerance for attacks
            if (decision.DecisionType == AIDecisionType.Attack)
            {
                troops = (int)(troops * (0.5f + (_riskTolerance * 0.5f)));
            }

            // Ensure minimum troops for meaningful action
            int minimum = decision.DecisionType == AIDecisionType.Defend
                ? MinimumDefenseTroops
                : MinimumAttackTroops;

            if (troops < minimum && availableTroops >= minimum)
            {
                troops = minimum;
            }

            // Never exceed available troops
            return Math.Min(troops, availableTroops);
        }

    }
}
