using FactionWars.Core.Interfaces;
using FactionWars.Factions.Interfaces;
using FactionWars.Territory.Interfaces;
using FactionWars.UI.Interfaces;
using FactionWars.UI.Models;
using System;
using System.Collections.Generic;
using System.Linq;

namespace FactionWars.UI.Handlers
{
    /// <summary>
    /// Handles faction status-related phone commands.
    /// Processes faction_status, faction_resources, and faction_territory commands.
    /// </summary>
    public class FactionStatusCommandHandler : IPhoneCommandHandler
    {
        private static readonly HashSet<string> SupportedCommands = new HashSet<string>
        {
            "faction_status",
            "faction_resources",
            "faction_territory"
        };

        private readonly IFactionService _factionService;
        private readonly IZoneService _zoneService;
        private readonly IZoneDefenderAllocationService _allocationService;
        private readonly IStatusDisplayService _statusDisplay;

        /// <summary>
        /// Creates a new faction status command handler.
        /// </summary>
        /// <param name="factionService">The faction service for retrieving faction data.</param>
        /// <param name="zoneService">The zone service for retrieving zone data.</param>
        /// <param name="allocationService">The allocation service for retrieving troop deployments.</param>
        /// <param name="statusDisplay">The status display service for showing information.</param>
        /// <exception cref="ArgumentNullException">Thrown if any parameter is null.</exception>
        public FactionStatusCommandHandler(
            IFactionService factionService,
            IZoneService zoneService,
            IZoneDefenderAllocationService allocationService,
            IStatusDisplayService statusDisplay)
        {
            _factionService = factionService ?? throw new ArgumentNullException(nameof(factionService));
            _zoneService = zoneService ?? throw new ArgumentNullException(nameof(zoneService));
            _allocationService = allocationService ?? throw new ArgumentNullException(nameof(allocationService));
            _statusDisplay = statusDisplay ?? throw new ArgumentNullException(nameof(statusDisplay));
        }

        /// <summary>
        /// Checks if this handler can process the given command.
        /// </summary>
        /// <param name="commandId">The command ID to check.</param>
        /// <returns>True if this handler can process the command.</returns>
        public bool CanHandle(string commandId)
        {
            if (string.IsNullOrWhiteSpace(commandId))
                return false;

            return SupportedCommands.Contains(commandId);
        }

        /// <inheritdoc/>
        public void HandleCommand(PhoneCommand command)
        {
            if (command == null)
                return;

            var playerFactionId = _statusDisplay.GetPlayerFactionId();
            if (string.IsNullOrWhiteSpace(playerFactionId))
            {
                if (CanHandle(command.Id))
                {
                    _statusDisplay.ShowError("No faction assigned");
                }
                return;
            }
            var factionId = playerFactionId!;

            var faction = _factionService.GetFaction(factionId);
            if (faction == null)
            {
                if (CanHandle(command.Id))
                {
                    _statusDisplay.ShowError("Faction not found");
                }
                return;
            }

            var state = _factionService.GetFactionState(factionId);

            switch (command.Id)
            {
                case "faction_status":
                    HandleFactionStatus(faction, state);
                    break;
                case "faction_resources":
                    HandleFactionResources(state);
                    break;
                case "faction_territory":
                    HandleFactionTerritory(factionId);
                    break;
            }
        }

        private void HandleFactionStatus(Factions.Models.Faction faction, Factions.Models.FactionState? state)
        {
            // Calculate total troops: reserve pool + allocated to zones
            // This gives an accurate picture of all troops available to the player
            int reserveTroops = state?.TotalReserveTroops ?? 0;
            int allocatedTroops = _allocationService.GetTotalAllocatedTroops(faction.Id);
            int totalTroops = reserveTroops + allocatedTroops;

            // Calculate military strength based on actual troops (not legacy TroopCount)
            int weapons = state?.Weapons ?? 0;
            int militaryStrength = totalTroops + (weapons * 2); // WeaponMultiplier = 2

            var info = new FactionStatusInfo
            {
                FactionName = faction.Name,
                LeaderName = faction.Leader,
                Cash = state?.Cash ?? 0,
                TroopCount = totalTroops,
                // Use ZoneService.GetZoneCount for accurate count (FactionState.ZoneCount can be stale)
                ZoneCount = _zoneService.GetZoneCount(faction.Id),
                Weapons = weapons,
                RecruitmentPoints = state?.RecruitmentPoints ?? 0,
                MilitaryStrength = militaryStrength
            };

            _statusDisplay.ShowFactionStatus(info);
        }

        private void HandleFactionResources(Factions.Models.FactionState? state)
        {
            // Calculate total troops: reserve pool + allocated to zones
            int reserveTroops = state?.TotalReserveTroops ?? 0;
            int allocatedTroops = state != null ? _allocationService.GetTotalAllocatedTroops(state.FactionId) : 0;
            int totalTroops = reserveTroops + allocatedTroops;

            var info = new ResourceStatusInfo
            {
                Cash = state?.Cash ?? 0,
                Weapons = state?.Weapons ?? 0,
                RecruitmentPoints = state?.RecruitmentPoints ?? 0,
                TroopCount = totalTroops
            };

            _statusDisplay.ShowResourceStatus(info);
        }

        private void HandleFactionTerritory(string factionId)
        {
            // Use ZoneService for accurate zone data (FactionState can be stale)
            var ownedZones = _zoneService.GetZonesByOwner(factionId).ToList();
            var info = new TerritoryStatusInfo
            {
                ZoneCount = ownedZones.Count,
                ZoneIds = ownedZones.Select(z => z.Id).ToList()
            };

            _statusDisplay.ShowTerritoryStatus(info);
        }
    }
}
