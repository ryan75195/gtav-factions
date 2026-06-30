using FactionWars.Core.Interfaces;
using FactionWars.Core.Models;
using FactionWars.Economy.Interfaces;
using FactionWars.Factions.Interfaces;
using FactionWars.Factions.Models;
using FactionWars.ScriptHookV.Models;
using FactionWars.Territory.Interfaces;
using FactionWars.UI.Interfaces;
using FactionWars.UI.Models;
using System;
using System.Linq;

namespace FactionWars.ScriptHookV.UI
{
    /// <summary>
    /// Controller for the Zone Management submenu. Allows viewing zones
    /// and buying/deploying defenders directly to each zone by tier.
    /// </summary>
    public partial class ZoneManagementMenuController
    {
        /// <summary>
        /// Menu ID for the zone management menu.
        /// </summary>
        public const string ZoneManagementMenuId = "zone_management_menu";

        /// <summary>
        /// Menu ID for the zone detail menu.
        /// </summary>
        public const string ZoneDetailMenuId = "zone_detail_menu";

        /// <summary>
        /// Item ID for when no zones are owned.
        /// </summary>
        public const string NoZonesItemId = "no_zones";

        /// <summary>
        /// Item ID for the back navigation item in main menu.
        /// </summary>
        public const string BackItemId = "back";

        /// <summary>
        /// Item ID for the back navigation item in zone detail menu.
        /// </summary>
        public const string DetailBackItemId = "detail_back";

        /// <summary>
        /// Item ID for current basic troops display.
        /// </summary>
        public const string CurrentBasicItemId = "current_basic";

        /// <summary>
        /// Item ID for current medium troops display.
        /// </summary>
        public const string CurrentMediumItemId = "current_medium";

        /// <summary>
        /// Item ID for current heavy troops display.
        /// </summary>
        public const string CurrentHeavyItemId = "current_heavy";

        /// <summary>
        /// Item ID for current elite troops display.
        /// </summary>
        public const string CurrentEliteItemId = "current_elite";

        /// <summary>
        /// Item ID for current sniper troops display.
        /// </summary>
        public const string CurrentSniperItemId = "current_sniper";

        /// <summary>
        /// Item ID for buy-and-deploy basic troops action.
        /// </summary>
        public const string DeployBasicItemId = "deploy_basic";

        /// <summary>
        /// Item ID for buy-and-deploy medium troops action.
        /// </summary>
        public const string DeployMediumItemId = "deploy_medium";

        /// <summary>
        /// Item ID for buy-and-deploy heavy troops action.
        /// </summary>
        public const string DeployHeavyItemId = "deploy_heavy";

        /// <summary>
        /// Item ID for buy-and-deploy elite troops action.
        /// </summary>
        public const string DeployEliteItemId = "deploy_elite";

        /// <summary>
        /// Item ID for buy-and-deploy sniper troops action.
        /// </summary>
        public const string DeploySniperItemId = "deploy_sniper";

        private readonly IMenuProvider _menuProvider;
        private readonly IFactionService _factionService;
        private readonly IZoneService _zoneService;
        private readonly IPlayerContext _playerContext;
        private readonly IZoneDefenderAllocationService _allocationService;
        private readonly IDefenderDeploymentService _deploymentService;

        private string? _selectedZoneId;

        /// <summary>
        /// Event raised when the user selects the back option from the main zone management menu.
        /// </summary>
        public event EventHandler? BackRequested;

        /// <summary>
        /// Creates a new ZoneManagementMenuController with the specified dependencies bundle.
        /// </summary>
        /// <param name="dependencies">The dependencies bundle.</param>
        /// <exception cref="ArgumentNullException">Thrown if dependencies or any required property is null.</exception>
        public ZoneManagementMenuController(ZoneManagementMenuControllerDependencies dependencies)
        {
            if (dependencies == null) throw new ArgumentNullException(nameof(dependencies));
            _menuProvider = dependencies.MenuProvider ?? throw new ArgumentNullException(nameof(dependencies.MenuProvider));
            _factionService = dependencies.FactionService ?? throw new ArgumentNullException(nameof(dependencies.FactionService));
            _zoneService = dependencies.ZoneService ?? throw new ArgumentNullException(nameof(dependencies.ZoneService));
            _playerContext = dependencies.PlayerContext ?? throw new ArgumentNullException(nameof(dependencies.PlayerContext));
            _allocationService = dependencies.AllocationService ?? throw new ArgumentNullException(nameof(dependencies.AllocationService));
            _deploymentService = dependencies.DeploymentService ?? throw new ArgumentNullException(nameof(dependencies.DeploymentService));

            _menuProvider.ItemSelected += OnItemSelected;
        }

        /// <summary>
        /// Shows the zone management menu with current zone list.
        /// </summary>
        public void Show()
        {
            _selectedZoneId = null;
            ShowZoneListMenu();
        }

        private void ShowZoneListMenu()
        {
            var factionId = _playerContext.CurrentFactionId;
            var faction = factionId != null ? _factionService.GetFaction(factionId) : null;
            var factionName = faction?.Name ?? "Unknown";

            var menu = new MenuDefinition(ZoneManagementMenuId, "Zone Management", factionName);
            AddOwnedZoneItems(menu, factionId);
            AddZoneListBackItem(menu);
            _menuProvider.ShowMenu(menu);
        }

        private void AddOwnedZoneItems(MenuDefinition menu, string? factionId)
        {
            var ownedZones = factionId != null
                ? _zoneService.GetZonesByOwner(factionId).ToList()
                : new System.Collections.Generic.List<Territory.Models.Zone>();

            if (ownedZones.Count == 0)
            {
                var noZonesItem = new MenuItem(
                    NoZonesItemId,
                    "No zones owned",
                    "Capture zones to manage their defenses");
                noZonesItem.IsEnabled = false;
                menu.AddItem(noZonesItem);
            }
            else
            {
                foreach (var zone in ownedZones)
                {
                    var allocation = factionId != null ? _allocationService.GetAllocation(factionId, zone.Id) : null;
                    var total = allocation == null ? 0 :
                        allocation.GetTroopCount(DefenderRole.Grunt)
                        + allocation.GetTroopCount(DefenderRole.Gunner)
                        + allocation.GetTroopCount(DefenderRole.Rifleman)
                        + allocation.GetTroopCount(DefenderRole.Rocketeer)
                        + allocation.GetTroopCount(DefenderRole.Sniper);

                    var zoneItem = new MenuItem(
                        zone.Id,
                        $"{zone.Name} ({total} troops)",
                        $"Value: {zone.StrategicValue}");
                    menu.AddItem(zoneItem);
                }
            }
        }

        private static void AddZoneListBackItem(MenuDefinition menu)
        {
            var backItem = new MenuItem(
                BackItemId,
                "Back",
                "Return to main menu");
            menu.AddItem(backItem);
        }

        /// <summary>
        /// Shows the zone detail menu for a specific zone.
        /// </summary>
        /// <param name="zoneId">The zone ID to show details for.</param>
        /// <param name="selectedItemId">Optional item ID to select after menu is shown.</param>
    }
}
