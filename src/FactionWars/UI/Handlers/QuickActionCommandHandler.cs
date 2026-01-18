using FactionWars.Combat.Interfaces;
using FactionWars.Combat.Models;
using FactionWars.Core.Interfaces;
using FactionWars.Factions.Interfaces;
using FactionWars.Territory.Interfaces;
using FactionWars.UI.Interfaces;
using FactionWars.UI.Models;
using System;
using System.Collections.Generic;

namespace FactionWars.UI.Handlers
{
    /// <summary>
    /// Handles quick action phone commands.
    /// Processes quick_reinforcements, quick_rally, quick_attack, and quick_defend commands.
    /// </summary>
    public class QuickActionCommandHandler : IPhoneCommandHandler
    {
        private static readonly HashSet<string> SupportedCommands = new HashSet<string>
        {
            "quick_reinforcements",
            "quick_rally",
            "quick_attack",
            "quick_defend"
        };

        private const int DefaultReinforcementCount = 5;

        private readonly IFactionService _factionService;
        private readonly IZoneService _zoneService;
        private readonly IReinforcementService _reinforcementService;
        private readonly IQuickActionDisplayService _displayService;
        private readonly IGameBridge _gameBridge;

        /// <summary>
        /// Creates a new quick action command handler.
        /// </summary>
        /// <param name="factionService">The faction service for retrieving faction data.</param>
        /// <param name="zoneService">The zone service for retrieving zone data.</param>
        /// <param name="reinforcementService">The reinforcement service for requesting reinforcements.</param>
        /// <param name="displayService">The display service for showing feedback.</param>
        /// <param name="gameBridge">The game bridge for accessing game state.</param>
        /// <exception cref="ArgumentNullException">Thrown if any parameter is null.</exception>
        public QuickActionCommandHandler(
            IFactionService factionService,
            IZoneService zoneService,
            IReinforcementService reinforcementService,
            IQuickActionDisplayService displayService,
            IGameBridge gameBridge)
        {
            _factionService = factionService ?? throw new ArgumentNullException(nameof(factionService));
            _zoneService = zoneService ?? throw new ArgumentNullException(nameof(zoneService));
            _reinforcementService = reinforcementService ?? throw new ArgumentNullException(nameof(reinforcementService));
            _displayService = displayService ?? throw new ArgumentNullException(nameof(displayService));
            _gameBridge = gameBridge ?? throw new ArgumentNullException(nameof(gameBridge));
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

            if (!CanHandle(command.Id))
                return;

            var playerFactionId = _displayService.GetPlayerFactionId();
            if (string.IsNullOrWhiteSpace(playerFactionId))
            {
                _displayService.ShowError("No faction assigned");
                return;
            }

            var faction = _factionService.GetFaction(playerFactionId);
            if (faction == null)
            {
                _displayService.ShowError("Faction not found");
                return;
            }

            switch (command.Id)
            {
                case "quick_reinforcements":
                    HandleQuickReinforcements(playerFactionId);
                    break;
                case "quick_rally":
                    HandleQuickRally();
                    break;
                case "quick_attack":
                    HandleQuickAttack(playerFactionId);
                    break;
                case "quick_defend":
                    HandleQuickDefend(playerFactionId);
                    break;
            }
        }

        private void HandleQuickReinforcements(string playerFactionId)
        {
            var playerPos = _gameBridge.GetPlayerPosition();
            var zone = _zoneService.GetZoneAtPosition(playerPos);

            if (zone == null)
            {
                _displayService.ShowError("Not in a controlled zone");
                return;
            }

            if (zone.OwnerFactionId != playerFactionId)
            {
                _displayService.ShowError("Cannot call reinforcements in enemy territory");
                return;
            }

            var request = new ReinforcementRequest(
                encounterId: $"quick_{zone.Id}_{DateTime.UtcNow.Ticks}",
                factionId: playerFactionId,
                zoneId: zone.Id,
                requestedCount: DefaultReinforcementCount,
                spawnPosition: playerPos);

            var result = _reinforcementService.RequestReinforcements(request);

            switch (result.Status)
            {
                case ReinforcementResultStatus.Success:
                    _displayService.ShowReinforcementsRequested(result.SpawnedCount);
                    break;
                case ReinforcementResultStatus.OnCooldown:
                    _displayService.ShowError("Reinforcements on cooldown");
                    break;
                case ReinforcementResultStatus.InsufficientResources:
                    _displayService.ShowError("Insufficient resources for reinforcements");
                    break;
                default:
                    _displayService.ShowError("Unable to request reinforcements");
                    break;
            }
        }

        private void HandleQuickRally()
        {
            var playerPos = _gameBridge.GetPlayerPosition();
            _displayService.ShowRallyOrdered(playerPos);
        }

        private void HandleQuickAttack(string playerFactionId)
        {
            var playerPos = _gameBridge.GetPlayerPosition();
            var zone = _zoneService.GetZoneAtPosition(playerPos);

            if (zone == null)
            {
                _displayService.ShowError("No target zone nearby");
                return;
            }

            if (zone.OwnerFactionId == playerFactionId)
            {
                _displayService.ShowError("Already control this zone");
                return;
            }

            _displayService.ShowAttackInitiated(zone.Id, zone.Name);
        }

        private void HandleQuickDefend(string playerFactionId)
        {
            var playerPos = _gameBridge.GetPlayerPosition();
            var zone = _zoneService.GetZoneAtPosition(playerPos);

            if (zone == null)
            {
                _displayService.ShowError("Not in a controlled zone");
                return;
            }

            if (zone.OwnerFactionId != playerFactionId)
            {
                _displayService.ShowError("Cannot defend enemy territory");
                return;
            }

            _displayService.ShowDefenseInitiated(zone.Id, zone.Name);
        }
    }
}
