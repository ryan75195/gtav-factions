using System;
using System.Collections.Generic;
using System.Linq;
using FactionWars.Combat.Interfaces;
using FactionWars.Combat.Models;
using FactionWars.Core.Interfaces;
using FactionWars.Core.Models;

namespace FactionWars.Combat.Services
{
    /// <summary>
    /// Service for processing defender casualties during combat.
    /// Identifies dead defender peds and deducts them from zone allocations.
    /// </summary>
    public class DefenderCasualtyService : IDefenderCasualtyService
    {
        private readonly IGameBridge _gameBridge;
        private readonly IPedPool _pedPool;
        private readonly IZoneDefenderAllocationRepository _allocationRepository;

        /// <summary>
        /// Creates a new DefenderCasualtyService.
        /// </summary>
        /// <param name="gameBridge">The game bridge for checking ped status.</param>
        /// <param name="pedPool">The ped pool for tracking spawned peds.</param>
        /// <param name="allocationRepository">The repository for zone defender allocations.</param>
        /// <exception cref="ArgumentNullException">Thrown if any parameter is null.</exception>
        public DefenderCasualtyService(
            IGameBridge gameBridge,
            IPedPool pedPool,
            IZoneDefenderAllocationRepository allocationRepository)
        {
            _gameBridge = gameBridge ?? throw new ArgumentNullException(nameof(gameBridge));
            _pedPool = pedPool ?? throw new ArgumentNullException(nameof(pedPool));
            _allocationRepository = allocationRepository ?? throw new ArgumentNullException(nameof(allocationRepository));
        }

        /// <inheritdoc />
        public CasualtyResult ProcessCasualties()
        {
            var casualtiesByTier = new Dictionary<DefenderTier, int>();
            var pedsToProcess = _pedPool.GetAll().ToList();
            var deadDefenders = new List<PedHandle>();

            // Identify dead defender peds
            foreach (var ped in pedsToProcess)
            {
                if (!IsValidDefender(ped))
                    continue;

                if (!_gameBridge.IsPedAlive(ped.Handle))
                {
                    deadDefenders.Add(ped);
                }
            }

            // Process each dead defender
            foreach (var ped in deadDefenders)
            {
                var tier = ped.DefenderTier!.Value;

                // Track casualty by tier
                if (!casualtiesByTier.ContainsKey(tier))
                    casualtiesByTier[tier] = 0;
                casualtiesByTier[tier]++;

                // Deduct from zone allocation
                DeductFromAllocation(ped.FactionId!, ped.ZoneId!, tier);

                // Remove from game world and pool
                _gameBridge.DeletePed(ped.Handle);
                _pedPool.Remove(ped);
            }

            return new CasualtyResult(casualtiesByTier);
        }

        /// <summary>
        /// Checks if a ped is a valid defender that can be counted as a casualty.
        /// </summary>
        private static bool IsValidDefender(PedHandle ped)
        {
            return ped != null
                && ped.IsValid
                && ped.DefenderTier.HasValue
                && !string.IsNullOrEmpty(ped.FactionId)
                && !string.IsNullOrEmpty(ped.ZoneId);
        }

        /// <summary>
        /// Deducts one troop from the zone allocation for the given faction and tier.
        /// </summary>
        private void DeductFromAllocation(string factionId, string zoneId, DefenderTier tier)
        {
            var allocation = _allocationRepository.Get(factionId, zoneId);
            if (allocation == null)
                return;

            // Only deduct if the allocation has troops of this tier
            if (allocation.HasTroops(tier, 1))
            {
                allocation.RemoveTroops(tier, 1);
                _allocationRepository.Update(allocation);
            }
        }
    }
}
