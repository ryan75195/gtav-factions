using FactionWars.AI.Interfaces;
using FactionWars.AI.Models;
using FactionWars.Core.Interfaces;
using FactionWars.Core.Models;
using FactionWars.Factions.Interfaces;
using FactionWars.Factions.Models;

namespace FactionWars.AI.Services
{
    /// <summary>
    /// Service for responding to vehicle threats by deploying Elite/RPG units.
    /// When enemy vehicles are detected in a battle zone, this service coordinates
    /// the deployment of anti-vehicle troops from the faction's reserve pool or
    /// through emergency purchases.
    /// </summary>
    public class AntiVehicleResponseService : IAntiVehicleResponseService
    {
        private readonly IFactionService _factionService;
        private readonly IZoneDefenderAllocationService _allocationService;
        private readonly IVehicleThreatService _vehicleThreatService;
        private readonly IDefenderRoleService _tierService;

        /// <summary>
        /// Creates a new AntiVehicleResponseService.
        /// </summary>
        /// <param name="factionService">Service for accessing faction state and resources.</param>
        /// <param name="allocationService">Service for allocating troops to zones.</param>
        /// <param name="vehicleThreatService">Service for determining RPG requirements by threat level.</param>
        /// <param name="tierService">Service for getting defender tier costs.</param>
        public AntiVehicleResponseService(
            IFactionService factionService,
            IZoneDefenderAllocationService allocationService,
            IVehicleThreatService vehicleThreatService,
            IDefenderRoleService tierService)
        {
            _factionService = factionService;
            _allocationService = allocationService;
            _vehicleThreatService = vehicleThreatService;
            _tierService = tierService;
        }

        /// <inheritdoc/>
        public int RespondToVehicleThreat(string factionId, string zoneId, VehicleThreatLevel threatLevel)
        {
            // Validate inputs
            if (string.IsNullOrEmpty(factionId) || string.IsNullOrEmpty(zoneId))
            {
                return 0;
            }

            // Get required RPG count for this threat level
            int requiredRpgCount = _vehicleThreatService.GetRequiredRpgCount(threatLevel);
            if (requiredRpgCount <= 0)
            {
                return 0;
            }

            // Get faction state
            var factionState = _factionService.GetFactionState(factionId);
            if (factionState == null)
            {
                return 0;
            }

            // Check available Elite units in reserve
            int availableElite = factionState.GetReserveTroops(DefenderRole.Rocketeer);
            int eliteCost = _tierService.GetCost(DefenderRole.Rocketeer);
            int eliteToDeploy = GetEliteToDeploy(factionId, factionState, requiredRpgCount, availableElite, eliteCost);

            return AllocateEliteDefenders(factionId, zoneId, eliteToDeploy);
        }

        private int GetEliteToDeploy(
            string factionId,
            FactionState factionState,
            int requiredRpgCount,
            int availableElite,
            int eliteCost)
        {
            int eliteNeeded = requiredRpgCount;
            int eliteToDeploy = 0;
            int fromReserve = System.Math.Min(availableElite, eliteNeeded);
            eliteToDeploy += fromReserve;
            eliteNeeded -= fromReserve;

            while (eliteNeeded > 0)
            {
                if (!factionState.CanAfford(eliteCost))
                    break;

                if (_factionService.SpendCash(factionId, eliteCost) &&
                    _factionService.AddReserveTroops(factionId, DefenderRole.Rocketeer, 1))
                {
                    eliteToDeploy++;
                    eliteNeeded--;
                }
                else
                {
                    break;
                }
            }

            return eliteToDeploy;
        }

        private int AllocateEliteDefenders(string factionId, string zoneId, int eliteToDeploy)
        {
            if (eliteToDeploy <= 0)
                return 0;

            var factionState = _factionService.GetFactionState(factionId);
            if (factionState == null)
                return 0;

            return _allocationService.AllocateTroops(factionState, zoneId, DefenderRole.Rocketeer, eliteToDeploy)
                ? eliteToDeploy
                : 0;
        }
    }
}
