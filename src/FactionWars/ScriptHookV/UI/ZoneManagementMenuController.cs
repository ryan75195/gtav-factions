using FactionWars.Core.Interfaces;
using FactionWars.Core.Models;
using FactionWars.Factions.Interfaces;
using FactionWars.Factions.Models;
using FactionWars.Territory.Interfaces;
using FactionWars.UI.Interfaces;
using FactionWars.UI.Models;
using System;
using System.Linq;

namespace FactionWars.ScriptHookV.UI
{
    /// <summary>
    /// Controller for the Zone Management submenu. Allows viewing zones,
    /// allocating troops by tier, and withdrawing troops.
    /// </summary>
    public class ZoneManagementMenuController
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
        /// Item ID for the reserve pool summary display.
        /// </summary>
        public const string ReserveSummaryItemId = "reserve_summary";

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
        /// Item ID for allocate basic troops action.
        /// </summary>
        public const string AllocateBasicItemId = "allocate_basic";

        /// <summary>
        /// Item ID for allocate medium troops action.
        /// </summary>
        public const string AllocateMediumItemId = "allocate_medium";

        /// <summary>
        /// Item ID for allocate heavy troops action.
        /// </summary>
        public const string AllocateHeavyItemId = "allocate_heavy";

        /// <summary>
        /// Item ID for withdraw basic troops action.
        /// </summary>
        public const string WithdrawBasicItemId = "withdraw_basic";

        /// <summary>
        /// Item ID for withdraw medium troops action.
        /// </summary>
        public const string WithdrawMediumItemId = "withdraw_medium";

        /// <summary>
        /// Item ID for withdraw heavy troops action.
        /// </summary>
        public const string WithdrawHeavyItemId = "withdraw_heavy";

        private readonly IMenuProvider _menuProvider;
        private readonly IFactionService _factionService;
        private readonly IZoneService _zoneService;
        private readonly IPlayerContext _playerContext;
        private readonly IZoneDefenderAllocationService _allocationService;

        private string? _selectedZoneId;

        /// <summary>
        /// Event raised when the user selects the back option from the main zone management menu.
        /// </summary>
        public event EventHandler? BackRequested;

        /// <summary>
        /// Creates a new ZoneManagementMenuController with the specified dependencies.
        /// </summary>
        /// <param name="menuProvider">The menu provider for displaying menus.</param>
        /// <param name="factionService">The faction service for retrieving faction data.</param>
        /// <param name="zoneService">The zone service for retrieving zone data.</param>
        /// <param name="playerContext">The player context for determining the current faction.</param>
        /// <param name="allocationService">The service for managing troop allocations.</param>
        /// <exception cref="ArgumentNullException">Thrown if any parameter is null.</exception>
        public ZoneManagementMenuController(
            IMenuProvider menuProvider,
            IFactionService factionService,
            IZoneService zoneService,
            IPlayerContext playerContext,
            IZoneDefenderAllocationService allocationService)
        {
            _menuProvider = menuProvider ?? throw new ArgumentNullException(nameof(menuProvider));
            _factionService = factionService ?? throw new ArgumentNullException(nameof(factionService));
            _zoneService = zoneService ?? throw new ArgumentNullException(nameof(zoneService));
            _playerContext = playerContext ?? throw new ArgumentNullException(nameof(playerContext));
            _allocationService = allocationService ?? throw new ArgumentNullException(nameof(allocationService));

            // Subscribe to menu item selection events
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

        /// <summary>
        /// Shows the zone list menu.
        /// </summary>
        private void ShowZoneListMenu()
        {
            var factionId = _playerContext.CurrentFactionId;
            var faction = factionId != null ? _factionService.GetFaction(factionId) : null;
            var factionState = factionId != null ? _factionService.GetFactionState(factionId) : null;

            var factionName = faction?.Name ?? "Unknown";

            var menu = new MenuDefinition(ZoneManagementMenuId, "Zone Management", factionName);

            // Add reserve pool summary (display only)
            var basicReserve = factionState?.GetReserveTroops(DefenderTier.Basic) ?? 0;
            var mediumReserve = factionState?.GetReserveTroops(DefenderTier.Medium) ?? 0;
            var heavyReserve = factionState?.GetReserveTroops(DefenderTier.Heavy) ?? 0;

            var reserveItem = new MenuItem(
                ReserveSummaryItemId,
                $"Reserves: B:{basicReserve} M:{mediumReserve} H:{heavyReserve}",
                "Available troops to deploy from reserve pool");
            reserveItem.IsEnabled = false;
            menu.AddItem(reserveItem);

            // Get owned zones
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
                // Add each zone as a selectable item
                foreach (var zone in ownedZones)
                {
                    var allocation = factionId != null ? _allocationService.GetAllocation(factionId, zone.Id) : null;
                    var basic = allocation?.GetTroopCount(DefenderTier.Basic) ?? 0;
                    var medium = allocation?.GetTroopCount(DefenderTier.Medium) ?? 0;
                    var heavy = allocation?.GetTroopCount(DefenderTier.Heavy) ?? 0;
                    var total = basic + medium + heavy;

                    var zoneItem = new MenuItem(
                        zone.Id,
                        $"{zone.Name} ({total} troops)",
                        $"B:{basic} M:{medium} H:{heavy} | Value: {zone.StrategicValue}");
                    menu.AddItem(zoneItem);
                }
            }

            // Add back navigation
            var backItem = new MenuItem(
                BackItemId,
                "Back",
                "Return to main menu");
            menu.AddItem(backItem);

            _menuProvider.ShowMenu(menu);
        }

        /// <summary>
        /// Shows the zone detail menu for a specific zone.
        /// </summary>
        /// <param name="zoneId">The zone ID to show details for.</param>
        /// <param name="selectedItemId">Optional item ID to select after menu is shown.</param>
        private void ShowZoneDetailMenu(string zoneId, string? selectedItemId = null)
        {
            _selectedZoneId = zoneId;

            var factionId = _playerContext.CurrentFactionId;
            var factionState = factionId != null ? _factionService.GetFactionState(factionId) : null;
            var zone = _zoneService.GetZone(zoneId);
            var zoneName = zone?.Name ?? "Unknown Zone";

            var menu = new MenuDefinition(ZoneDetailMenuId, zoneName, "Manage troops");

            // Get current allocation
            var allocation = factionId != null ? _allocationService.GetAllocation(factionId, zoneId) : null;
            var currentBasic = allocation?.GetTroopCount(DefenderTier.Basic) ?? 0;
            var currentMedium = allocation?.GetTroopCount(DefenderTier.Medium) ?? 0;
            var currentHeavy = allocation?.GetTroopCount(DefenderTier.Heavy) ?? 0;

            // Get reserve counts
            var reserveBasic = factionState?.GetReserveTroops(DefenderTier.Basic) ?? 0;
            var reserveMedium = factionState?.GetReserveTroops(DefenderTier.Medium) ?? 0;
            var reserveHeavy = factionState?.GetReserveTroops(DefenderTier.Heavy) ?? 0;

            // Current allocation display (disabled info items)
            var basicItem = new MenuItem(
                CurrentBasicItemId,
                $"Basic: {currentBasic}",
                "Currently allocated Basic tier troops");
            basicItem.IsEnabled = false;
            menu.AddItem(basicItem);

            var mediumItem = new MenuItem(
                CurrentMediumItemId,
                $"Medium: {currentMedium}",
                "Currently allocated Medium tier troops");
            mediumItem.IsEnabled = false;
            menu.AddItem(mediumItem);

            var heavyItem = new MenuItem(
                CurrentHeavyItemId,
                $"Heavy: {currentHeavy}",
                "Currently allocated Heavy tier troops");
            heavyItem.IsEnabled = false;
            menu.AddItem(heavyItem);

            // Allocate actions
            var allocateBasicItem = new MenuItem(
                AllocateBasicItemId,
                $"+ Allocate Basic (Reserve: {reserveBasic})",
                "Allocate one Basic troop from reserve");
            allocateBasicItem.IsEnabled = reserveBasic > 0;
            menu.AddItem(allocateBasicItem);

            var allocateMediumItem = new MenuItem(
                AllocateMediumItemId,
                $"+ Allocate Medium (Reserve: {reserveMedium})",
                "Allocate one Medium troop from reserve");
            allocateMediumItem.IsEnabled = reserveMedium > 0;
            menu.AddItem(allocateMediumItem);

            var allocateHeavyItem = new MenuItem(
                AllocateHeavyItemId,
                $"+ Allocate Heavy (Reserve: {reserveHeavy})",
                "Allocate one Heavy troop from reserve");
            allocateHeavyItem.IsEnabled = reserveHeavy > 0;
            menu.AddItem(allocateHeavyItem);

            // Withdraw actions
            var withdrawBasicItem = new MenuItem(
                WithdrawBasicItemId,
                $"- Withdraw Basic (Allocated: {currentBasic})",
                "Withdraw one Basic troop back to reserve");
            withdrawBasicItem.IsEnabled = currentBasic > 0;
            menu.AddItem(withdrawBasicItem);

            var withdrawMediumItem = new MenuItem(
                WithdrawMediumItemId,
                $"- Withdraw Medium (Allocated: {currentMedium})",
                "Withdraw one Medium troop back to reserve");
            withdrawMediumItem.IsEnabled = currentMedium > 0;
            menu.AddItem(withdrawMediumItem);

            var withdrawHeavyItem = new MenuItem(
                WithdrawHeavyItemId,
                $"- Withdraw Heavy (Allocated: {currentHeavy})",
                "Withdraw one Heavy troop back to reserve");
            withdrawHeavyItem.IsEnabled = currentHeavy > 0;
            menu.AddItem(withdrawHeavyItem);

            // Back navigation
            var backItem = new MenuItem(
                DetailBackItemId,
                "Back",
                "Return to zone list");
            menu.AddItem(backItem);

            _menuProvider.ShowMenu(menu, selectedItemId);
            _menuProvider.HoldToRepeatEnabled = true; // Enable hold-to-repeat for allocation/withdrawal
        }

        /// <summary>
        /// Handles menu item selection events.
        /// </summary>
        private void OnItemSelected(object? sender, MenuItemSelectedEventArgs e)
        {
            if (e.MenuId == ZoneManagementMenuId)
            {
                HandleZoneListSelection(e.ItemId);
            }
            else if (e.MenuId == ZoneDetailMenuId)
            {
                HandleZoneDetailSelection(e.ItemId);
            }
        }

        /// <summary>
        /// Handles item selection in the zone list menu.
        /// </summary>
        private void HandleZoneListSelection(string itemId)
        {
            if (itemId == BackItemId)
            {
                _menuProvider.CloseMenu();
                BackRequested?.Invoke(this, EventArgs.Empty);
                return;
            }

            // Zone item was selected - show zone detail
            if (itemId.StartsWith("zone_") || _zoneService.GetZone(itemId) != null)
            {
                ShowZoneDetailMenu(itemId);
            }
        }

        /// <summary>
        /// Handles item selection in the zone detail menu.
        /// </summary>
        private void HandleZoneDetailSelection(string itemId)
        {
            if (itemId == DetailBackItemId)
            {
                ShowZoneListMenu();
                return;
            }

            var factionId = _playerContext.CurrentFactionId;
            var factionState = factionId != null ? _factionService.GetFactionState(factionId) : null;

            if (factionState == null || _selectedZoneId == null)
                return;

            switch (itemId)
            {
                case AllocateBasicItemId:
                    _allocationService.AllocateTroops(factionState, _selectedZoneId, DefenderTier.Basic, 1);
                    ShowZoneDetailMenu(_selectedZoneId, AllocateBasicItemId);
                    break;

                case AllocateMediumItemId:
                    _allocationService.AllocateTroops(factionState, _selectedZoneId, DefenderTier.Medium, 1);
                    ShowZoneDetailMenu(_selectedZoneId, AllocateMediumItemId);
                    break;

                case AllocateHeavyItemId:
                    _allocationService.AllocateTroops(factionState, _selectedZoneId, DefenderTier.Heavy, 1);
                    ShowZoneDetailMenu(_selectedZoneId, AllocateHeavyItemId);
                    break;

                case WithdrawBasicItemId:
                    _allocationService.WithdrawTroops(factionState, _selectedZoneId, DefenderTier.Basic, 1);
                    ShowZoneDetailMenu(_selectedZoneId, WithdrawBasicItemId);
                    break;

                case WithdrawMediumItemId:
                    _allocationService.WithdrawTroops(factionState, _selectedZoneId, DefenderTier.Medium, 1);
                    ShowZoneDetailMenu(_selectedZoneId, WithdrawMediumItemId);
                    break;

                case WithdrawHeavyItemId:
                    _allocationService.WithdrawTroops(factionState, _selectedZoneId, DefenderTier.Heavy, 1);
                    ShowZoneDetailMenu(_selectedZoneId, WithdrawHeavyItemId);
                    break;
            }
        }
    }
}
