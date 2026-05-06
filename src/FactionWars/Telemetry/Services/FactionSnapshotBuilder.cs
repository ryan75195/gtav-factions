using System;
using System.Collections.Generic;
using FactionWars.Core.Interfaces;
using FactionWars.Core.Models;
using FactionWars.Factions.Interfaces;
using FactionWars.Factions.Models;
using FactionWars.Telemetry.Interfaces;
using FactionWars.Telemetry.Models;
using FactionWars.Territory.Interfaces;

namespace FactionWars.Telemetry.Services
{
    /// <summary>
    /// Builds FactionSnapshot rows from current faction and zone state.
    /// Pure: no I/O, no side effects.
    /// </summary>
    public sealed class FactionSnapshotBuilder : IFactionSnapshotBuilder
    {
        private readonly IFactionService _factionService;
        private readonly IZoneService _zoneService;
        private readonly IZoneDefenderAllocationService? _allocationService;
        private readonly Func<string?>? _getPlayerFactionId;
        private readonly Func<int>? _getPlayerMoney;

        public FactionSnapshotBuilder(IFactionService factionService, IZoneService zoneService)
            : this(factionService, zoneService, null, null, null)
        {
        }

        public FactionSnapshotBuilder(
            IFactionService factionService,
            IZoneService zoneService,
            IZoneDefenderAllocationService? allocationService,
            Func<string?>? getPlayerFactionId,
            Func<int>? getPlayerMoney)
        {
            _factionService = factionService ?? throw new ArgumentNullException(nameof(factionService));
            _zoneService = zoneService ?? throw new ArgumentNullException(nameof(zoneService));
            _allocationService = allocationService;
            _getPlayerFactionId = getPlayerFactionId;
            _getPlayerMoney = getPlayerMoney;
        }

        public IReadOnlyList<FactionSnapshot> Build(DateTime timestamp, long playTimeSeconds)
        {
            var rows = new List<FactionSnapshot>();
            foreach (var faction in _factionService.GetAllFactions())
            {
                var state = _factionService.GetFactionState(faction.Id);
                if (state == null) continue;

                var reserveByTier = GetReserveByTier(state);
                var deployedByTier = GetDeployedByTier(faction.Id);
                var totalByTier = SumByTier(reserveByTier, deployedByTier);
                int reserve = SumTroops(reserveByTier);
                int deployed = SumTroops(deployedByTier);
                int total = reserve + deployed;

                rows.Add(new FactionSnapshot(
                    timestamp, playTimeSeconds, faction.Id,
                    GetSnapshotCash(faction.Id, state.Cash), total,
                    _zoneService.GetZoneCount(faction.Id),
                    totalByTier[DefenderTier.Basic],
                    totalByTier[DefenderTier.Medium],
                    totalByTier[DefenderTier.Heavy],
                    totalByTier[DefenderTier.Elite],
                    reserve, deployed));
            }
            return rows;
        }

        private static Dictionary<DefenderTier, int> GetReserveByTier(FactionState state)
        {
            return new Dictionary<DefenderTier, int>
            {
                { DefenderTier.Basic, state.GetReserveTroops(DefenderTier.Basic) },
                { DefenderTier.Medium, state.GetReserveTroops(DefenderTier.Medium) },
                { DefenderTier.Heavy, state.GetReserveTroops(DefenderTier.Heavy) },
                { DefenderTier.Elite, state.GetReserveTroops(DefenderTier.Elite) }
            };
        }

        private Dictionary<DefenderTier, int> GetDeployedByTier(string factionId)
        {
            var deployed = EmptyTierCounts();
            if (_allocationService == null)
                return deployed;

            foreach (var allocation in _allocationService.GetAllocationsForFaction(factionId))
            {
                deployed[DefenderTier.Basic] += allocation.GetTroopCount(DefenderTier.Basic);
                deployed[DefenderTier.Medium] += allocation.GetTroopCount(DefenderTier.Medium);
                deployed[DefenderTier.Heavy] += allocation.GetTroopCount(DefenderTier.Heavy);
                deployed[DefenderTier.Elite] += allocation.GetTroopCount(DefenderTier.Elite);
            }

            return deployed;
        }

        private static Dictionary<DefenderTier, int> SumByTier(
            Dictionary<DefenderTier, int> reserve,
            Dictionary<DefenderTier, int> deployed)
        {
            var total = EmptyTierCounts();
            foreach (var tier in new[] { DefenderTier.Basic, DefenderTier.Medium, DefenderTier.Heavy, DefenderTier.Elite })
            {
                total[tier] = reserve[tier] + deployed[tier];
            }

            return total;
        }

        private static Dictionary<DefenderTier, int> EmptyTierCounts()
        {
            return new Dictionary<DefenderTier, int>
            {
                { DefenderTier.Basic, 0 },
                { DefenderTier.Medium, 0 },
                { DefenderTier.Heavy, 0 },
                { DefenderTier.Elite, 0 }
            };
        }

        private static int SumTroops(Dictionary<DefenderTier, int> troopsByTier)
        {
            int total = 0;
            foreach (var count in troopsByTier.Values)
            {
                total += count;
            }

            return total;
        }

        private int GetSnapshotCash(string factionId, int stateCash)
        {
            if (_getPlayerFactionId == null || _getPlayerMoney == null)
                return stateCash;

            var playerFactionId = _getPlayerFactionId();
            if (!string.Equals(playerFactionId, factionId, StringComparison.OrdinalIgnoreCase))
                return stateCash;

            return Math.Max(0, _getPlayerMoney());
        }
    }
}
