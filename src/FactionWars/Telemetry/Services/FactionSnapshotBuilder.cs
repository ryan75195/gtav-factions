using System;
using System.Collections.Generic;
using FactionWars.Core.Models;
using FactionWars.Factions.Interfaces;
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

        public FactionSnapshotBuilder(IFactionService factionService, IZoneService zoneService)
        {
            _factionService = factionService ?? throw new ArgumentNullException(nameof(factionService));
            _zoneService = zoneService ?? throw new ArgumentNullException(nameof(zoneService));
        }

        public IReadOnlyList<FactionSnapshot> Build(DateTime timestamp, long playTimeSeconds)
        {
            var rows = new List<FactionSnapshot>();
            foreach (var faction in _factionService.GetAllFactions())
            {
                var state = _factionService.GetFactionState(faction.Id);
                if (state == null) continue;

                int basic = state.GetReserveTroops(DefenderTier.Basic);
                int medium = state.GetReserveTroops(DefenderTier.Medium);
                int heavy = state.GetReserveTroops(DefenderTier.Heavy);
                int elite = state.GetReserveTroops(DefenderTier.Elite);
                int reserve = basic + medium + heavy + elite;
                int total = state.TroopCount;
                int deployed = Math.Max(0, total - reserve);

                rows.Add(new FactionSnapshot(
                    timestamp, playTimeSeconds, faction.Id,
                    state.Cash, total,
                    _zoneService.GetZoneCount(faction.Id),
                    basic, medium, heavy, elite,
                    reserve, deployed));
            }
            return rows;
        }
    }
}
