using FactionWars.Core.Models;
using FactionWars.Economy.Models;
using FactionWars.Factions.Models;

namespace FactionWars.Economy.Interfaces
{
    /// <summary>
    /// Orchestrates buying defenders and deploying them directly to a zone in one step.
    /// Composes <see cref="ITroopPurchaseService"/> and the zone allocation service so the
    /// player never manages a reserve pool directly.
    /// </summary>
    public interface IDefenderDeploymentService
    {
        /// <summary>
        /// Buys <paramref name="count"/> troops of <paramref name="tier"/> and deploys them to
        /// <paramref name="zoneId"/>. Validates affordability first; on insufficient funds it
        /// makes no state change.
        /// </summary>
        DeploymentResult BuyAndDeploy(FactionState factionState, string zoneId, DefenderRole tier, int count);

        /// <summary>Cost of a single troop of <paramref name="tier"/> (for menu labels).</summary>
        int GetTroopCost(DefenderRole tier);

        /// <summary>Whether the player can afford <paramref name="count"/> of <paramref name="tier"/>.</summary>
        bool CanAfford(DefenderRole tier, int count);
    }
}
