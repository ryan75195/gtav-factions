using FactionWars.Core.Interfaces;
using FactionWars.Core.Models;
using FactionWars.Factions.Interfaces;
using FactionWars.Territory.Interfaces;
using FactionWars.UI.Interfaces;
using FactionWars.UI.Models;
using System;
using System.Linq;

namespace FactionWars.ScriptHookV.UI
{
    /// <summary>
    /// Controller for the Overview submenu. Displays faction stats and victory progress.
    /// </summary>
    public class OverviewMenuController
    {
        /// <summary>
        /// Menu ID for the overview menu.
        /// </summary>
        public const string OverviewMenuId = "overview_menu";

        /// <summary>
        /// Item ID for the zones owned display.
        /// </summary>
        public const string ZonesOwnedItemId = "zones_owned";

        /// <summary>
        /// Item ID for the victory progress display.
        /// </summary>
        public const string VictoryProgressItemId = "victory_progress";

        /// <summary>
        /// Item ID for the cash display.
        /// </summary>
        public const string CashItemId = "cash";

        /// <summary>
        /// Item ID for the total troops display.
        /// </summary>
        public const string TotalTroopsItemId = "total_troops";

        /// <summary>
        /// Item ID for the reserve by tier display.
        /// </summary>
        public const string ReserveByTierItemId = "reserve_by_tier";

        /// <summary>
        /// Item ID for the military strength display.
        /// </summary>
        public const string MilitaryStrengthItemId = "military_strength";

        /// <summary>
        /// Item ID for the back navigation item.
        /// </summary>
        public const string BackItemId = "back";

        private readonly IMenuProvider _menuProvider;
        private readonly IFactionService _factionService;
        private readonly IZoneService _zoneService;
        private readonly IPlayerContext _playerContext;

        /// <summary>
        /// Event raised when the user selects the back option.
        /// </summary>
        public event EventHandler? BackRequested;

        /// <summary>
        /// Creates a new OverviewMenuController with the specified dependencies.
        /// </summary>
        /// <param name="menuProvider">The menu provider for displaying menus.</param>
        /// <param name="factionService">The faction service for retrieving faction data.</param>
        /// <param name="zoneService">The zone service for retrieving zone data.</param>
        /// <param name="playerContext">The player context for determining the current faction.</param>
        /// <exception cref="ArgumentNullException">Thrown if any parameter is null.</exception>
        public OverviewMenuController(
            IMenuProvider menuProvider,
            IFactionService factionService,
            IZoneService zoneService,
            IPlayerContext playerContext)
        {
            _menuProvider = menuProvider ?? throw new ArgumentNullException(nameof(menuProvider));
            _factionService = factionService ?? throw new ArgumentNullException(nameof(factionService));
            _zoneService = zoneService ?? throw new ArgumentNullException(nameof(zoneService));
            _playerContext = playerContext ?? throw new ArgumentNullException(nameof(playerContext));

            // Subscribe to menu item selection events
            _menuProvider.ItemSelected += OnItemSelected;
        }

        /// <summary>
        /// Shows the overview menu with current faction stats.
        /// </summary>
        public void Show()
        {
            var factionId = _playerContext.CurrentFactionId;
            var faction = factionId != null ? _factionService.GetFaction(factionId) : null;
            var factionState = factionId != null ? _factionService.GetFactionState(factionId) : null;

            var factionName = faction?.Name ?? "Unknown";
            var totalZones = _zoneService.GetAllZones().Count();
            // Use ZoneService.GetZoneCount for accurate count (FactionState.ZoneCount can be stale)
            var zonesOwned = factionId != null ? _zoneService.GetZoneCount(factionId) : 0;
            var cash = factionState?.Cash ?? 0;
            var totalReserveTroops = factionState?.TotalReserveTroops ?? 0;
            var militaryStrength = factionState?.MilitaryStrength ?? 0;

            // Calculate victory progress
            var victoryPercent = totalZones > 0 ? (int)Math.Round((double)zonesOwned / totalZones * 100) : 0;

            // Get reserve troops by tier
            var basicReserve = factionState?.GetReserveTroops(DefenderTier.Basic) ?? 0;
            var mediumReserve = factionState?.GetReserveTroops(DefenderTier.Medium) ?? 0;
            var heavyReserve = factionState?.GetReserveTroops(DefenderTier.Heavy) ?? 0;

            var menu = new MenuDefinition(OverviewMenuId, "Overview", factionName);

            // Add info items (display-only, disabled)
            var zonesItem = new MenuItem(
                ZonesOwnedItemId,
                $"Zones: {zonesOwned} / {totalZones}",
                "Number of territories controlled");
            zonesItem.IsEnabled = false;
            menu.AddItem(zonesItem);

            var victoryItem = new MenuItem(
                VictoryProgressItemId,
                $"Victory Progress: {victoryPercent}%",
                "Capture 100% of zones to win");
            victoryItem.IsEnabled = false;
            menu.AddItem(victoryItem);

            var cashItem = new MenuItem(
                CashItemId,
                $"Cash: ${cash:N0}",
                "Available funds for purchases");
            cashItem.IsEnabled = false;
            menu.AddItem(cashItem);

            var troopsItem = new MenuItem(
                TotalTroopsItemId,
                $"Total Reserve: {totalReserveTroops}",
                "Troops available to deploy");
            troopsItem.IsEnabled = false;
            menu.AddItem(troopsItem);

            var reserveItem = new MenuItem(
                ReserveByTierItemId,
                "Reserve by Tier",
                $"Basic: {basicReserve}, Medium: {mediumReserve}, Heavy: {heavyReserve}");
            reserveItem.IsEnabled = false;
            menu.AddItem(reserveItem);

            var strengthItem = new MenuItem(
                MilitaryStrengthItemId,
                $"Military Strength: {militaryStrength}",
                "Combined power of troops and weapons");
            strengthItem.IsEnabled = false;
            menu.AddItem(strengthItem);

            // Add navigation item
            var backItem = new MenuItem(
                BackItemId,
                "Back",
                "Return to main menu");
            menu.AddItem(backItem);

            _menuProvider.ShowMenu(menu);
        }

        /// <summary>
        /// Handles menu item selection events.
        /// </summary>
        private void OnItemSelected(object? sender, MenuItemSelectedEventArgs e)
        {
            if (e.MenuId != OverviewMenuId)
                return;

            if (e.ItemId == BackItemId)
            {
                _menuProvider.CloseMenu();
                BackRequested?.Invoke(this, EventArgs.Empty);
            }
        }
    }
}
