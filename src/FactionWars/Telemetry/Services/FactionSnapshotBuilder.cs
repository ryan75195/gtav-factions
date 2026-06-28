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
                    totalByTier[DefenderRole.Grunt],
                    totalByTier[DefenderRole.Gunner],
                    totalByTier[DefenderRole.Rifleman],
                    totalByTier[DefenderRole.Rocketeer],
                    reserve, deployed));
            }
            return rows;
        }

        private static Dictionary<DefenderRole, int> GetReserveByTier(FactionState state)
        {
            return new Dictionary<DefenderRole, int>
            {
                { DefenderRole.Grunt, state.GetReserveTroops(DefenderRole.Grunt) },
                { DefenderRole.Gunner, state.GetReserveTroops(DefenderRole.Gunner) },
                { DefenderRole.Rifleman, state.GetReserveTroops(DefenderRole.Rifleman) },
                { DefenderRole.Rocketeer, state.GetReserveTroops(DefenderRole.Rocketeer) }
            };
        }

        private Dictionary<DefenderRole, int> GetDeployedByTier(string factionId)
        {
            var deployed = EmptyTierCounts();
            if (_allocationService == null)
                return deployed;

            foreach (var allocation in _allocationService.GetAllocationsForFaction(factionId))
            {
                deployed[DefenderRole.Grunt] += allocation.GetTroopCount(DefenderRole.Grunt);
                deployed[DefenderRole.Gunner] += allocation.GetTroopCount(DefenderRole.Gunner);
                deployed[DefenderRole.Rifleman] += allocation.GetTroopCount(DefenderRole.Rifleman);
                deployed[DefenderRole.Rocketeer] += allocation.GetTroopCount(DefenderRole.Rocketeer);
            }

            return deployed;
        }

        private static Dictionary<DefenderRole, int> SumByTier(
            Dictionary<DefenderRole, int> reserve,
            Dictionary<DefenderRole, int> deployed)
        {
            var total = EmptyTierCounts();
            foreach (var tier in new[] { DefenderRole.Grunt, DefenderRole.Gunner, DefenderRole.Rifleman, DefenderRole.Rocketeer })
            {
                total[tier] = reserve[tier] + deployed[tier];
            }

            return total;
        }

        private static Dictionary<DefenderRole, int> EmptyTierCounts()
        {
            return new Dictionary<DefenderRole, int>
            {
                { DefenderRole.Grunt, 0 },
                { DefenderRole.Gunner, 0 },
                { DefenderRole.Rifleman, 0 },
                { DefenderRole.Rocketeer, 0 }
            };
        }

        private static int SumTroops(Dictionary<DefenderRole, int> troopsByTier)
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
