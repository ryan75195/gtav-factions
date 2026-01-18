using FactionWars.Factions.Interfaces;
using FactionWars.Factions.Models;
using FactionWars.Territory.Interfaces;
using FactionWars.Territory.Models;
using FactionWars.UI.Interfaces;
using FactionWars.UI.Models;
using System;
using System.Collections.Generic;
using System.Linq;

namespace FactionWars.UI.Services
{
    /// <summary>
    /// Service for managing the faction menu system.
    /// Provides menu generation and handles menu interactions.
    /// </summary>
    public class FactionMenuService : IFactionMenuService
    {
        private const string MainMenuId = "faction_main_menu";
        private const string TerritoryListMenuId = "territory_list_menu";
        private const string ZoneDetailMenuId = "zone_detail_menu";
        private const string ResourceOverviewMenuId = "resource_overview_menu";
        private const string OrdersMenuId = "orders_menu";
        private const string AttackTargetsMenuId = "attack_targets_menu";
        private const string DefendZonesMenuId = "defend_zones_menu";
        private const string SettingsMenuId = "settings_menu";

        private readonly IMenuProvider _menuProvider;
        private readonly IFactionService _factionService;
        private readonly IZoneService _zoneService;

        private string? _currentPlayerFactionId;

        /// <summary>
        /// Creates a new faction menu service.
        /// </summary>
        /// <param name="menuProvider">The menu provider for displaying menus.</param>
        /// <param name="factionService">The faction service for faction data.</param>
        /// <param name="zoneService">The zone service for zone data.</param>
        /// <exception cref="ArgumentNullException">Thrown if any parameter is null.</exception>
        public FactionMenuService(
            IMenuProvider menuProvider,
            IFactionService factionService,
            IZoneService zoneService)
        {
            _menuProvider = menuProvider ?? throw new ArgumentNullException(nameof(menuProvider));
            _factionService = factionService ?? throw new ArgumentNullException(nameof(factionService));
            _zoneService = zoneService ?? throw new ArgumentNullException(nameof(zoneService));

            // Subscribe to menu events
            _menuProvider.ItemSelected += OnMenuItemSelected;
            _menuProvider.MenuClosed += OnMenuClosed;
        }

        /// <inheritdoc/>
        public bool IsMenuVisible => _menuProvider.IsMenuVisible;

        /// <inheritdoc/>
        public void ShowMainMenu(string playerFactionId)
        {
            if (string.IsNullOrWhiteSpace(playerFactionId))
                return;

            var menuDefinition = BuildMainMenuDefinition(playerFactionId);
            if (menuDefinition == null)
                return;

            _currentPlayerFactionId = playerFactionId;
            _menuProvider.ShowMenu(menuDefinition);
        }

        /// <inheritdoc/>
        public void CloseMenu()
        {
            _menuProvider.CloseMenu();
        }

        /// <inheritdoc/>
        public MenuDefinition? BuildMainMenuDefinition(string playerFactionId)
        {
            if (string.IsNullOrWhiteSpace(playerFactionId))
                return null;

            var faction = _factionService.GetFaction(playerFactionId);
            if (faction == null)
                return null;

            var factionState = _factionService.GetFactionState(playerFactionId);
            var zoneCount = _zoneService.GetZoneCount(playerFactionId);

            var menu = new MenuDefinition(
                MainMenuId,
                faction.Name,
                "Faction Operations");

            // Territories item
            var territoriesItem = new MenuItem(
                "territories",
                "Territories",
                $"Manage your {zoneCount} controlled zone(s)");
            menu.AddItem(territoriesItem);

            // Resources item
            var cash = factionState?.Cash ?? 0;
            var troops = factionState?.TroopCount ?? 0;
            var resourcesItem = new MenuItem(
                "resources",
                "Resources",
                $"Cash: ${cash:N0} | Troops: {troops}");
            menu.AddItem(resourcesItem);

            // Orders item
            var ordersItem = new MenuItem(
                "orders",
                "Orders",
                "Issue attack and defense orders");
            menu.AddItem(ordersItem);

            // Settings item
            var settingsItem = new MenuItem(
                "settings",
                "Settings",
                "Configure faction options");
            menu.AddItem(settingsItem);

            return menu;
        }

        /// <inheritdoc/>
        public MenuDefinition? BuildTerritoryListMenuDefinition(string playerFactionId)
        {
            if (string.IsNullOrWhiteSpace(playerFactionId))
                return null;

            var zones = _zoneService.GetZonesByOwner(playerFactionId).ToList();
            var zoneCount = zones.Count;

            var menu = new MenuDefinition(
                TerritoryListMenuId,
                "Territories",
                $"{zoneCount} zone(s) controlled");

            foreach (var zone in zones)
            {
                var description = BuildZoneListDescription(zone);
                var item = new MenuItem($"zone_{zone.Id}", zone.Name, description);
                menu.AddItem(item);
            }

            return menu;
        }

        /// <inheritdoc/>
        public MenuDefinition? BuildZoneDetailMenuDefinition(string zoneId, string playerFactionId)
        {
            if (string.IsNullOrWhiteSpace(zoneId))
                return null;

            var zone = _zoneService.GetZone(zoneId);
            if (zone == null)
                return null;

            var adjacentZones = _zoneService.GetAdjacentZones(zoneId).ToList();

            var subtitle = zone.IsContested ? "CONTESTED" : "Controlled";
            var menu = new MenuDefinition(ZoneDetailMenuId, zone.Name, subtitle);

            // Control info
            var controlItem = new MenuItem(
                "control_info",
                "Control",
                $"{zone.ControlPercentage:F0}%");
            menu.AddItem(controlItem);

            // Strategic value
            var valueItem = new MenuItem(
                "strategic_value",
                "Strategic Value",
                $"{zone.StrategicValue}/10");
            menu.AddItem(valueItem);

            // Traits
            var traitsDescription = zone.Traits == ZoneTrait.None
                ? "None"
                : string.Join(", ", GetTraitNames(zone.Traits));
            var traitsItem = new MenuItem(
                "traits_info",
                "Traits",
                traitsDescription);
            menu.AddItem(traitsItem);

            // Adjacent zones
            var adjacentItem = new MenuItem(
                "adjacent_zones",
                "Adjacent Zones",
                $"{adjacentZones.Count} neighboring zone(s)");
            menu.AddItem(adjacentItem);

            // Back button
            var backItem = new MenuItem("back", "Back", "Return to territory list");
            menu.AddItem(backItem);

            return menu;
        }

        /// <inheritdoc/>
        public void ShowTerritoryListMenu(string playerFactionId)
        {
            if (string.IsNullOrWhiteSpace(playerFactionId))
                return;

            var menuDefinition = BuildTerritoryListMenuDefinition(playerFactionId);
            if (menuDefinition == null)
                return;

            _currentPlayerFactionId = playerFactionId;
            _menuProvider.ShowMenu(menuDefinition);
        }

        /// <inheritdoc/>
        public void ShowZoneDetailMenu(string zoneId, string playerFactionId)
        {
            if (string.IsNullOrWhiteSpace(zoneId))
                return;

            var menuDefinition = BuildZoneDetailMenuDefinition(zoneId, playerFactionId);
            if (menuDefinition == null)
                return;

            _currentPlayerFactionId = playerFactionId;
            _menuProvider.ShowMenu(menuDefinition);
        }

        /// <inheritdoc/>
        public MenuDefinition? BuildResourceOverviewMenuDefinition(string playerFactionId)
        {
            if (string.IsNullOrWhiteSpace(playerFactionId))
                return null;

            var factionState = _factionService.GetFactionState(playerFactionId);
            if (factionState == null)
                return null;

            var menu = new MenuDefinition(ResourceOverviewMenuId, "Resources", "Faction Resources");

            // Cash
            var cashItem = new MenuItem(
                "cash",
                "Cash",
                $"${factionState.Cash:N0}");
            menu.AddItem(cashItem);

            // Recruitment Points
            var recruitmentItem = new MenuItem(
                "recruitment",
                "Recruitment Points",
                $"{factionState.RecruitmentPoints:N0}");
            menu.AddItem(recruitmentItem);

            // Weapons
            var weaponsItem = new MenuItem(
                "weapons",
                "Weapons",
                $"{factionState.Weapons:N0}");
            menu.AddItem(weaponsItem);

            // Troops
            var troopsItem = new MenuItem(
                "troops",
                "Troops",
                $"{factionState.TroopCount:N0}");
            menu.AddItem(troopsItem);

            // Military Strength
            var strengthItem = new MenuItem(
                "military_strength",
                "Military Strength",
                $"{factionState.MilitaryStrength:N0}");
            menu.AddItem(strengthItem);

            // Back button
            var backItem = new MenuItem("back", "Back", "Return to main menu");
            menu.AddItem(backItem);

            return menu;
        }

        /// <inheritdoc/>
        public void ShowResourceOverviewMenu(string playerFactionId)
        {
            if (string.IsNullOrWhiteSpace(playerFactionId))
                return;

            var menuDefinition = BuildResourceOverviewMenuDefinition(playerFactionId);
            if (menuDefinition == null)
                return;

            _currentPlayerFactionId = playerFactionId;
            _menuProvider.ShowMenu(menuDefinition);
        }

        /// <inheritdoc/>
        public void HandleMenuSelection(string menuId, string itemId)
        {
            if (string.IsNullOrWhiteSpace(menuId) || string.IsNullOrWhiteSpace(itemId))
                return;

            switch (menuId)
            {
                case MainMenuId:
                    HandleMainMenuSelection(itemId);
                    break;
                case TerritoryListMenuId:
                    HandleTerritoryListMenuSelection(itemId);
                    break;
                case ZoneDetailMenuId:
                    HandleZoneDetailMenuSelection(itemId);
                    break;
                case ResourceOverviewMenuId:
                    HandleResourceOverviewMenuSelection(itemId);
                    break;
                case OrdersMenuId:
                    HandleOrdersMenuSelection(itemId);
                    break;
                case AttackTargetsMenuId:
                    HandleAttackTargetsMenuSelection(itemId);
                    break;
                case DefendZonesMenuId:
                    HandleDefendZonesMenuSelection(itemId);
                    break;
                case SettingsMenuId:
                    HandleSettingsMenuSelection(itemId);
                    break;
            }
        }

        /// <inheritdoc/>
        public void Update()
        {
            _menuProvider.Update();
        }

        private void HandleMainMenuSelection(string itemId)
        {
            switch (itemId)
            {
                case "territories":
                    if (_currentPlayerFactionId != null)
                        ShowTerritoryListMenu(_currentPlayerFactionId);
                    break;
                case "resources":
                    if (_currentPlayerFactionId != null)
                        ShowResourceOverviewMenu(_currentPlayerFactionId);
                    break;
                case "orders":
                    if (_currentPlayerFactionId != null)
                        ShowOrdersMenu(_currentPlayerFactionId);
                    break;
                case "settings":
                    if (_currentPlayerFactionId != null)
                        ShowSettingsMenu(_currentPlayerFactionId);
                    break;
            }
        }

        private void HandleTerritoryListMenuSelection(string itemId)
        {
            // Zone items have id format "zone_{zoneId}"
            if (itemId.StartsWith("zone_") && _currentPlayerFactionId != null)
            {
                var zoneId = itemId.Substring(5);
                ShowZoneDetailMenu(zoneId, _currentPlayerFactionId);
            }
        }

        private void HandleZoneDetailMenuSelection(string itemId)
        {
            switch (itemId)
            {
                case "back":
                    if (_currentPlayerFactionId != null)
                        ShowTerritoryListMenu(_currentPlayerFactionId);
                    break;
            }
        }

        private void HandleResourceOverviewMenuSelection(string itemId)
        {
            switch (itemId)
            {
                case "back":
                    if (_currentPlayerFactionId != null)
                        ShowMainMenu(_currentPlayerFactionId);
                    break;
            }
        }

        private void OnMenuItemSelected(object? sender, MenuItemSelectedEventArgs e)
        {
            HandleMenuSelection(e.MenuId, e.ItemId);
        }

        private void OnMenuClosed(object? sender, EventArgs e)
        {
            _currentPlayerFactionId = null;
        }

        private string BuildZoneListDescription(Zone zone)
        {
            var parts = new List<string>();

            parts.Add($"Control: {zone.ControlPercentage:F0}%");
            parts.Add($"Value: {zone.StrategicValue}");

            if (zone.IsContested)
                parts.Add("CONTESTED");

            return string.Join(" | ", parts);
        }

        private IEnumerable<string> GetTraitNames(ZoneTrait traits)
        {
            if (traits.HasFlag(ZoneTrait.Industrial))
                yield return "Industrial";
            if (traits.HasFlag(ZoneTrait.Commercial))
                yield return "Commercial";
            if (traits.HasFlag(ZoneTrait.Residential))
                yield return "Residential";
            if (traits.HasFlag(ZoneTrait.Port))
                yield return "Port";
            if (traits.HasFlag(ZoneTrait.Airfield))
                yield return "Airfield";
            if (traits.HasFlag(ZoneTrait.Fortified))
                yield return "Fortified";
            if (traits.HasFlag(ZoneTrait.HighValue))
                yield return "HighValue";
        }

        /// <inheritdoc/>
        public MenuDefinition? BuildOrdersMenuDefinition(string playerFactionId)
        {
            if (string.IsNullOrWhiteSpace(playerFactionId))
                return null;

            var menu = new MenuDefinition(OrdersMenuId, "Orders", "Issue faction orders");

            // Attack item
            var attackItem = new MenuItem(
                "attack",
                "Attack",
                "Select a zone to attack");
            menu.AddItem(attackItem);

            // Defend item
            var defendItem = new MenuItem(
                "defend",
                "Defend",
                "Reinforce zone defenses");
            menu.AddItem(defendItem);

            // Back button
            var backItem = new MenuItem("back", "Back", "Return to main menu");
            menu.AddItem(backItem);

            return menu;
        }

        /// <inheritdoc/>
        public void ShowOrdersMenu(string playerFactionId)
        {
            if (string.IsNullOrWhiteSpace(playerFactionId))
                return;

            var menuDefinition = BuildOrdersMenuDefinition(playerFactionId);
            if (menuDefinition == null)
                return;

            _currentPlayerFactionId = playerFactionId;
            _menuProvider.ShowMenu(menuDefinition);
        }

        /// <inheritdoc/>
        public MenuDefinition? BuildAttackTargetsMenuDefinition(string playerFactionId)
        {
            if (string.IsNullOrWhiteSpace(playerFactionId))
                return null;

            var menu = new MenuDefinition(AttackTargetsMenuId, "Attack Targets", "Select a target zone");

            // Get all zones owned by the player
            var playerZones = _zoneService.GetZonesByOwner(playerFactionId).ToList();

            // Find all adjacent zones that are not owned by the player
            var attackableZones = new HashSet<Zone>();
            foreach (var playerZone in playerZones)
            {
                var adjacentZones = _zoneService.GetAdjacentZones(playerZone.Id);
                foreach (var adjacentZone in adjacentZones)
                {
                    // Only add zones not owned by the player
                    if (adjacentZone.OwnerFactionId != playerFactionId)
                    {
                        attackableZones.Add(adjacentZone);
                    }
                }
            }

            // Add menu items for each attackable zone
            foreach (var zone in attackableZones)
            {
                var description = BuildAttackTargetDescription(zone);
                var item = new MenuItem($"attack_{zone.Id}", zone.Name, description);
                menu.AddItem(item);
            }

            // Back button
            var backItem = new MenuItem("back", "Back", "Return to orders menu");
            menu.AddItem(backItem);

            return menu;
        }

        /// <inheritdoc/>
        public void ShowAttackTargetsMenu(string playerFactionId)
        {
            if (string.IsNullOrWhiteSpace(playerFactionId))
                return;

            var menuDefinition = BuildAttackTargetsMenuDefinition(playerFactionId);
            if (menuDefinition == null)
                return;

            _currentPlayerFactionId = playerFactionId;
            _menuProvider.ShowMenu(menuDefinition);
        }

        /// <inheritdoc/>
        public MenuDefinition? BuildDefendZonesMenuDefinition(string playerFactionId)
        {
            if (string.IsNullOrWhiteSpace(playerFactionId))
                return null;

            var menu = new MenuDefinition(DefendZonesMenuId, "Defend Zones", "Select a zone to defend");

            // Get all zones owned by the player, prioritizing contested zones
            var playerZones = _zoneService.GetZonesByOwner(playerFactionId)
                .OrderByDescending(z => z.IsContested)
                .ThenByDescending(z => z.StrategicValue)
                .ToList();

            // Add menu items for each zone
            foreach (var zone in playerZones)
            {
                var description = BuildDefendZoneDescription(zone);
                var item = new MenuItem($"defend_{zone.Id}", zone.Name, description);
                menu.AddItem(item);
            }

            // Back button
            var backItem = new MenuItem("back", "Back", "Return to orders menu");
            menu.AddItem(backItem);

            return menu;
        }

        /// <inheritdoc/>
        public void ShowDefendZonesMenu(string playerFactionId)
        {
            if (string.IsNullOrWhiteSpace(playerFactionId))
                return;

            var menuDefinition = BuildDefendZonesMenuDefinition(playerFactionId);
            if (menuDefinition == null)
                return;

            _currentPlayerFactionId = playerFactionId;
            _menuProvider.ShowMenu(menuDefinition);
        }

        private void HandleOrdersMenuSelection(string itemId)
        {
            switch (itemId)
            {
                case "attack":
                    if (_currentPlayerFactionId != null)
                        ShowAttackTargetsMenu(_currentPlayerFactionId);
                    break;
                case "defend":
                    if (_currentPlayerFactionId != null)
                        ShowDefendZonesMenu(_currentPlayerFactionId);
                    break;
                case "back":
                    if (_currentPlayerFactionId != null)
                        ShowMainMenu(_currentPlayerFactionId);
                    break;
            }
        }

        private void HandleAttackTargetsMenuSelection(string itemId)
        {
            switch (itemId)
            {
                case "back":
                    if (_currentPlayerFactionId != null)
                        ShowOrdersMenu(_currentPlayerFactionId);
                    break;
                default:
                    // Handle attack zone selection (attack_{zoneId})
                    if (itemId.StartsWith("attack_") && _currentPlayerFactionId != null)
                    {
                        var zoneId = itemId.Substring(7);
                        // TODO: Initiate attack on zone
                    }
                    break;
            }
        }

        private void HandleDefendZonesMenuSelection(string itemId)
        {
            switch (itemId)
            {
                case "back":
                    if (_currentPlayerFactionId != null)
                        ShowOrdersMenu(_currentPlayerFactionId);
                    break;
                default:
                    // Handle defend zone selection (defend_{zoneId})
                    if (itemId.StartsWith("defend_") && _currentPlayerFactionId != null)
                    {
                        var zoneId = itemId.Substring(7);
                        // TODO: Show defense options for zone
                    }
                    break;
            }
        }

        private string BuildAttackTargetDescription(Zone zone)
        {
            var parts = new List<string>();

            // Owner info
            if (zone.OwnerFactionId == null)
            {
                parts.Add("Neutral");
            }
            else
            {
                var ownerFaction = _factionService.GetFaction(zone.OwnerFactionId);
                if (ownerFaction != null)
                {
                    parts.Add(ownerFaction.Name);
                }
                else
                {
                    parts.Add("Enemy");
                }
            }

            // Strategic value
            parts.Add($"Value: {zone.StrategicValue}");

            return string.Join(" | ", parts);
        }

        private string BuildDefendZoneDescription(Zone zone)
        {
            var parts = new List<string>();

            parts.Add($"Control: {zone.ControlPercentage:F0}%");
            parts.Add($"Value: {zone.StrategicValue}");

            if (zone.IsContested)
                parts.Add("CONTESTED");

            return string.Join(" | ", parts);
        }

        /// <inheritdoc/>
        public MenuDefinition? BuildSettingsMenuDefinition(string playerFactionId)
        {
            if (string.IsNullOrWhiteSpace(playerFactionId))
                return null;

            var menu = new MenuDefinition(SettingsMenuId, "Settings", "Configure game options");

            // Difficulty item
            var difficultyItem = new MenuItem(
                "difficulty",
                "AI Difficulty",
                "Adjust enemy faction difficulty");
            menu.AddItem(difficultyItem);

            // Auto-save item
            var autoSaveItem = new MenuItem(
                "auto_save",
                "Auto-Save",
                "Toggle automatic saving");
            menu.AddItem(autoSaveItem);

            // Back button
            var backItem = new MenuItem("back", "Back", "Return to main menu");
            menu.AddItem(backItem);

            return menu;
        }

        /// <inheritdoc/>
        public void ShowSettingsMenu(string playerFactionId)
        {
            if (string.IsNullOrWhiteSpace(playerFactionId))
                return;

            var menuDefinition = BuildSettingsMenuDefinition(playerFactionId);
            if (menuDefinition == null)
                return;

            _currentPlayerFactionId = playerFactionId;
            _menuProvider.ShowMenu(menuDefinition);
        }

        private void HandleSettingsMenuSelection(string itemId)
        {
            switch (itemId)
            {
                case "difficulty":
                    // TODO: Show difficulty selection submenu
                    break;
                case "auto_save":
                    // TODO: Toggle auto-save setting
                    break;
                case "back":
                    if (_currentPlayerFactionId != null)
                        ShowMainMenu(_currentPlayerFactionId);
                    break;
            }
        }
    }
}
