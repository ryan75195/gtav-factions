using FactionWars.Core.Interfaces;
using FactionWars.Economy.Interfaces;
using FactionWars.Economy.Models;
using FactionWars.Factions.Interfaces;
using FactionWars.Territory.Interfaces;
using FactionWars.Territory.Models;
using FactionWars.UI.Interfaces;
using FactionWars.UI.Models;
using System;
using System.Collections.Generic;
using System.Linq;

namespace FactionWars.ScriptHookV.UI
{
    /// <summary>
    /// Controller for the Resources submenu. Displays income breakdown by resource type and zone.
    /// </summary>
    public class ResourcesMenuController
    {
        /// <summary>
        /// Menu ID for the resources menu.
        /// </summary>
        public const string ResourcesMenuId = "resources_menu";

        /// <summary>
        /// Item ID for the next tick display.
        /// </summary>
        public const string NextTickItemId = "next_tick";

        /// <summary>
        /// Item ID for the total cash income display.
        /// </summary>
        public const string TotalCashItemId = "total_cash";

        /// <summary>
        /// Item ID for the total recruitment income display.
        /// </summary>
        public const string TotalRecruitmentItemId = "total_recruitment";

        /// <summary>
        /// Item ID for the total weapons income display.
        /// </summary>
        public const string TotalWeaponsItemId = "total_weapons";

        /// <summary>
        /// Item ID for the zone breakdown header.
        /// </summary>
        public const string ZoneBreakdownHeaderItemId = "zone_breakdown_header";

        /// <summary>
        /// Item ID for the back navigation item.
        /// </summary>
        public const string BackItemId = "back";

        private readonly IMenuProvider _menuProvider;
        private readonly IFactionService _factionService;
        private readonly IZoneService _zoneService;
        private readonly IPlayerContext _playerContext;
        private readonly IResourceTickService _resourceTickService;
        private readonly IZoneTraitResourceModifier _resourceModifier;
        private readonly ISupplyLineService _supplyLineService;

        /// <summary>
        /// Event raised when the user selects the back option.
        /// </summary>
        public event EventHandler? BackRequested;

        /// <summary>
        /// Creates a new ResourcesMenuController with the specified dependencies.
        /// </summary>
        /// <param name="menuProvider">The menu provider for displaying menus.</param>
        /// <param name="factionService">The faction service for retrieving faction data.</param>
        /// <param name="zoneService">The zone service for retrieving zone data.</param>
        /// <param name="playerContext">The player context for determining the current faction.</param>
        /// <param name="resourceTickService">The resource tick service for timing information.</param>
        /// <param name="resourceModifier">The resource modifier for trait-based bonuses.</param>
        /// <param name="supplyLineService">The supply line service for efficiency calculations.</param>
        /// <exception cref="ArgumentNullException">Thrown if any parameter is null.</exception>
        public ResourcesMenuController(
            IMenuProvider menuProvider,
            IFactionService factionService,
            IZoneService zoneService,
            IPlayerContext playerContext,
            IResourceTickService resourceTickService,
            IZoneTraitResourceModifier resourceModifier,
            ISupplyLineService supplyLineService)
        {
            _menuProvider = menuProvider ?? throw new ArgumentNullException(nameof(menuProvider));
            _factionService = factionService ?? throw new ArgumentNullException(nameof(factionService));
            _zoneService = zoneService ?? throw new ArgumentNullException(nameof(zoneService));
            _playerContext = playerContext ?? throw new ArgumentNullException(nameof(playerContext));
            _resourceTickService = resourceTickService ?? throw new ArgumentNullException(nameof(resourceTickService));
            _resourceModifier = resourceModifier ?? throw new ArgumentNullException(nameof(resourceModifier));
            _supplyLineService = supplyLineService ?? throw new ArgumentNullException(nameof(supplyLineService));

            // Subscribe to menu item selection events
            _menuProvider.ItemSelected += OnItemSelected;
        }

        /// <summary>
        /// Shows the resources menu with income breakdown.
        /// </summary>
        public void Show()
        {
            var factionId = _playerContext.CurrentFactionId;
            var faction = factionId != null ? _factionService.GetFaction(factionId) : null;
            var factionName = faction?.Name ?? "Unknown";

            // Get zones owned by player
            var zones = factionId != null
                ? _zoneService.GetZonesByOwner(factionId).ToList()
                : new List<Zone>();

            // Calculate total income per resource type
            var income = CalculateTotalIncome(factionId, zones);

            var menu = new MenuDefinition(ResourcesMenuId, "Resources", factionName);

            // Add next tick timer
            var timeUntilTick = _resourceTickService.TimeUntilNextTick;
            var minutes = (int)(timeUntilTick / 60);
            var seconds = (int)(timeUntilTick % 60);
            var nextTickItem = new MenuItem(
                NextTickItemId,
                $"Next Tick: {minutes}:{seconds:D2}",
                $"Resources generate every {_resourceTickService.TickIntervalSeconds / 60} minutes");
            nextTickItem.IsEnabled = false;
            menu.AddItem(nextTickItem);

            // Add total income items
            var cashItem = new MenuItem(
                TotalCashItemId,
                $"Cash: ${income.TotalCash:N0}/tick",
                "Total cash income from all zones");
            cashItem.IsEnabled = false;
            menu.AddItem(cashItem);

            var recruitmentItem = new MenuItem(
                TotalRecruitmentItemId,
                $"Recruitment: {income.TotalRecruitment}/tick",
                "Total recruitment points from all zones");
            recruitmentItem.IsEnabled = false;
            menu.AddItem(recruitmentItem);

            var weaponsItem = new MenuItem(
                TotalWeaponsItemId,
                $"Weapons: {income.TotalWeapons}/tick",
                "Total weapons production from all zones");
            weaponsItem.IsEnabled = false;
            menu.AddItem(weaponsItem);

            // Add zone breakdown header
            var headerItem = new MenuItem(
                ZoneBreakdownHeaderItemId,
                "--- Zone Breakdown ---",
                "Income breakdown by territory");
            headerItem.IsEnabled = false;
            menu.AddItem(headerItem);

            // Add zone income items
            foreach (var zoneIncome in income.ZoneIncomes)
            {
                var efficiencyNote = zoneIncome.Efficiency < 1.0f
                    ? $" ({(int)(zoneIncome.Efficiency * 100)}% efficiency)"
                    : "";

                var zoneItem = new MenuItem(
                    $"zone_{zoneIncome.ZoneId}",
                    $"{zoneIncome.ZoneName}: ${zoneIncome.Cash}/tick",
                    $"${zoneIncome.Cash} Cash, {zoneIncome.Recruitment} Recruit, {zoneIncome.Weapons} Wpn{efficiencyNote}");
                zoneItem.IsEnabled = false;
                menu.AddItem(zoneItem);
            }

            // Add navigation item
            var backItem = new MenuItem(
                BackItemId,
                "Back",
                "Return to main menu");
            menu.AddItem(backItem);

            _menuProvider.ShowMenu(menu);
        }

        /// <summary>
        /// Calculates total income for all zones owned by the faction.
        /// </summary>
        private IncomeSummary CalculateTotalIncome(string? factionId, List<Zone> zones)
        {
            int totalCash = 0;
            int totalRecruitment = 0;
            int totalWeapons = 0;
            var zoneIncomes = new List<ZoneIncomeInfo>();

            if (factionId == null || zones.Count == 0)
            {
                return new IncomeSummary(totalCash, totalRecruitment, totalWeapons, zoneIncomes);
            }

            var cashInfo = ResourceTypeInfo.GetInfo(ResourceType.Cash);
            var recruitmentInfo = ResourceTypeInfo.GetInfo(ResourceType.Recruitment);
            var weaponsInfo = ResourceTypeInfo.GetInfo(ResourceType.Weapons);

            foreach (var zone in zones)
            {
                // Calculate base generation with strategic value
                float baseCash = cashInfo.BaseGenerationRate * zone.StrategicValue;
                float baseRecruitment = recruitmentInfo.BaseGenerationRate * zone.StrategicValue;
                float baseWeapons = weaponsInfo.BaseGenerationRate * zone.StrategicValue;

                // Apply trait modifiers
                float cashModifier = _resourceModifier.GetModifier(zone.Traits, ResourceType.Cash);
                float recruitmentModifier = _resourceModifier.GetModifier(zone.Traits, ResourceType.Recruitment);
                float weaponsModifier = _resourceModifier.GetModifier(zone.Traits, ResourceType.Weapons);

                // Apply supply line efficiency
                float efficiency = _supplyLineService.GetSupplyLineEfficiency(factionId, zone.Id);

                int zoneCash = (int)(baseCash * cashModifier * efficiency);
                int zoneRecruitment = (int)(baseRecruitment * recruitmentModifier * efficiency);
                int zoneWeapons = (int)(baseWeapons * weaponsModifier * efficiency);

                totalCash += zoneCash;
                totalRecruitment += zoneRecruitment;
                totalWeapons += zoneWeapons;

                zoneIncomes.Add(new ZoneIncomeInfo
                {
                    ZoneId = zone.Id,
                    ZoneName = zone.Name,
                    Cash = zoneCash,
                    Recruitment = zoneRecruitment,
                    Weapons = zoneWeapons,
                    Efficiency = efficiency
                });
            }

            return new IncomeSummary(totalCash, totalRecruitment, totalWeapons, zoneIncomes);
        }

        /// <summary>
        /// Handles menu item selection events.
        /// </summary>
        private void OnItemSelected(object? sender, MenuItemSelectedEventArgs e)
        {
            if (e.MenuId != ResourcesMenuId)
                return;

            if (e.ItemId == BackItemId)
            {
                _menuProvider.CloseMenu();
                BackRequested?.Invoke(this, EventArgs.Empty);
            }
        }

        /// <summary>
        /// Internal class to hold zone income information.
        /// </summary>
        private class ZoneIncomeInfo
        {
            public string ZoneId { get; set; } = string.Empty;
            public string ZoneName { get; set; } = string.Empty;
            public int Cash { get; set; }
            public int Recruitment { get; set; }
            public int Weapons { get; set; }
            public float Efficiency { get; set; }
        }

        private sealed class IncomeSummary
        {
            public IncomeSummary(int totalCash, int totalRecruitment, int totalWeapons, List<ZoneIncomeInfo> zoneIncomes)
            {
                TotalCash = totalCash;
                TotalRecruitment = totalRecruitment;
                TotalWeapons = totalWeapons;
                ZoneIncomes = zoneIncomes;
            }

            public int TotalCash { get; }
            public int TotalRecruitment { get; }
            public int TotalWeapons { get; }
            public List<ZoneIncomeInfo> ZoneIncomes { get; }
        }
    }
}
