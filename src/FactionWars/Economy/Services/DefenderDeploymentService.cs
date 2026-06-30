using System;
using FactionWars.Core.Interfaces;
using FactionWars.Core.Models;
using FactionWars.Economy.Interfaces;
using FactionWars.Economy.Models;
using FactionWars.Factions.Models;
using FactionWars.ScriptHookV.Logging;

namespace FactionWars.Economy.Services
{
    /// <summary>
    /// Default <see cref="IDefenderDeploymentService"/> that composes purchase + allocation.
    /// </summary>
    public sealed class DefenderDeploymentService : IDefenderDeploymentService
    {
        private readonly ITroopPurchaseService _purchaseService;
        private readonly IZoneDefenderAllocationService _allocationService;

        public DefenderDeploymentService(
            ITroopPurchaseService purchaseService,
            IZoneDefenderAllocationService allocationService)
        {
            _purchaseService = purchaseService ?? throw new ArgumentNullException(nameof(purchaseService));
            _allocationService = allocationService ?? throw new ArgumentNullException(nameof(allocationService));
        }

        public int GetTroopCost(DefenderRole tier) => _purchaseService.GetTroopCost(tier);

        public bool CanAfford(DefenderRole tier, int count) => _purchaseService.CanAfford(tier, count);

        public DeploymentResult BuyAndDeploy(FactionState factionState, string zoneId, DefenderRole tier, int count)
        {
            if (factionState == null) throw new ArgumentNullException(nameof(factionState));
            if (string.IsNullOrWhiteSpace(zoneId)) throw new ArgumentException("Zone id must be provided.", nameof(zoneId));
            if (count <= 0) throw new ArgumentOutOfRangeException(nameof(count));

            var cost = _purchaseService.CalculateTotalCost(tier, count);
            if (!_purchaseService.CanAfford(tier, count))
            {
                FileLogger.Info($"BuyAndDeploy: insufficient funds for {count}x {tier} (${cost}) in zone {zoneId}");
                return DeploymentResult.Unaffordable(tier, count, cost);
            }

            _purchaseService.PurchaseTroops(factionState.FactionId, tier, count);
            _allocationService.AllocateTroops(factionState, zoneId, tier, count);
            FileLogger.Info($"BuyAndDeploy: deployed {count}x {tier} to zone {zoneId} for ${cost}");
            return DeploymentResult.Deployed(tier, count, cost);
        }
    }
}
