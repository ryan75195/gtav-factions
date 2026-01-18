using FactionWars.Factions.Interfaces;
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
        private readonly IStatusDisplayService _statusDisplay;

        /// <summary>
        /// Creates a new faction status command handler.
        /// </summary>
        /// <param name="factionService">The faction service for retrieving faction data.</param>
        /// <param name="statusDisplay">The status display service for showing information.</param>
        /// <exception cref="ArgumentNullException">Thrown if any parameter is null.</exception>
        public FactionStatusCommandHandler(
            IFactionService factionService,
            IStatusDisplayService statusDisplay)
        {
            _factionService = factionService ?? throw new ArgumentNullException(nameof(factionService));
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

            var faction = _factionService.GetFaction(playerFactionId);
            if (faction == null)
            {
                if (CanHandle(command.Id))
                {
                    _statusDisplay.ShowError("Faction not found");
                }
                return;
            }

            var state = _factionService.GetFactionState(playerFactionId);

            switch (command.Id)
            {
                case "faction_status":
                    HandleFactionStatus(faction, state);
                    break;
                case "faction_resources":
                    HandleFactionResources(state);
                    break;
                case "faction_territory":
                    HandleFactionTerritory(state);
                    break;
            }
        }

        private void HandleFactionStatus(Factions.Models.Faction faction, Factions.Models.FactionState? state)
        {
            var info = new FactionStatusInfo
            {
                FactionName = faction.Name,
                LeaderName = faction.Leader,
                Cash = state?.Cash ?? 0,
                TroopCount = state?.TroopCount ?? 0,
                ZoneCount = state?.ZoneCount ?? 0,
                Weapons = state?.Weapons ?? 0,
                RecruitmentPoints = state?.RecruitmentPoints ?? 0,
                MilitaryStrength = state?.MilitaryStrength ?? 0
            };

            _statusDisplay.ShowFactionStatus(info);
        }

        private void HandleFactionResources(Factions.Models.FactionState? state)
        {
            var info = new ResourceStatusInfo
            {
                Cash = state?.Cash ?? 0,
                Weapons = state?.Weapons ?? 0,
                RecruitmentPoints = state?.RecruitmentPoints ?? 0,
                TroopCount = state?.TroopCount ?? 0
            };

            _statusDisplay.ShowResourceStatus(info);
        }

        private void HandleFactionTerritory(Factions.Models.FactionState? state)
        {
            var info = new TerritoryStatusInfo
            {
                ZoneCount = state?.ZoneCount ?? 0,
                ZoneIds = state?.OwnedZoneIds.ToList() ?? new List<string>()
            };

            _statusDisplay.ShowTerritoryStatus(info);
        }
    }
}
