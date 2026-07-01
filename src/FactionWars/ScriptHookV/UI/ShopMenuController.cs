using FactionWars.Core.Interfaces;
using FactionWars.UI.Interfaces;
using FactionWars.UI.Models;
using System;
using System.Collections.Generic;

namespace FactionWars.ScriptHookV.UI
{
    /// <summary>
    /// Controller for the Shop submenu. Allows purchasing military vehicles.
    /// </summary>
    public class ShopMenuController
    {
        /// <summary>
        /// Menu ID for the shop menu.
        /// </summary>
        public const string ShopMenuId = "shop_menu";

        /// <summary>
        /// Item ID for cash display.
        /// </summary>
        public const string CashDisplayItemId = "cash_display";

        /// <summary>
        /// Item ID for back button.
        /// </summary>
        public const string BackItemId = "back";

        private readonly IMenuProvider _menuProvider;
        private readonly IGameBridge _gameBridge;

        // Vehicle catalog: itemId -> (displayName, modelName, price)
        private static readonly Dictionary<string, (string DisplayName, string ModelName, int Price)> VehicleCatalog = new()
        {
            { "buy_insurgent", ("Insurgent", "insurgent", 25000) },
            { "buy_technical", ("Technical", "technical", 15000) },
            { "buy_apc", ("APC", "apc", 50000) },
            { "buy_khanjali", ("Khanjali", "khanjali", 100000) },
            { "buy_buzzard", ("Buzzard", "buzzard", 75000) },
            { "buy_bati", ("Bati 801", "bati", 10000) },
            { "buy_zentorno", ("Zentorno", "zentorno", 40000) },
            { "buy_police_suv", ("FBI SUV", "fbi2", 20000) },
            { "buy_barracks", ("Barracks", "barracks", 25000) }
        };

        /// <summary>
        /// Event raised when the user selects the back option.
        /// </summary>
        public event EventHandler? BackRequested;

        /// <summary>
        /// Creates a new ShopMenuController with the specified dependencies.
        /// </summary>
        public ShopMenuController(IMenuProvider menuProvider, IGameBridge gameBridge)
        {
            _menuProvider = menuProvider ?? throw new ArgumentNullException(nameof(menuProvider));
            _gameBridge = gameBridge ?? throw new ArgumentNullException(nameof(gameBridge));

            _menuProvider.ItemSelected += OnItemSelected;
        }

        /// <summary>
        /// Shows the shop menu.
        /// </summary>
        public void Show()
        {
            var menu = new MenuDefinition(ShopMenuId, "Shop", "Military Vehicles");

            var playerMoney = _gameBridge.GetPlayerMoney();

            // Cash display
            var cashItem = new MenuItem(CashDisplayItemId, $"Cash: ${playerMoney:N0}", "Your available funds");
            cashItem.IsEnabled = false;
            menu.AddItem(cashItem);

            // Vehicle options
            AddVehicleItem(menu, "buy_insurgent", "Insurgent", 25000, "Armored SUV, seats 9", playerMoney);
            AddVehicleItem(menu, "buy_technical", "Technical", 15000, "Pickup with mounted gun", playerMoney);
            AddVehicleItem(menu, "buy_apc", "APC", 50000, "Armored carrier, amphibious", playerMoney);
            AddVehicleItem(menu, "buy_khanjali", "Khanjali", 100000, "Main battle tank", playerMoney);
            AddVehicleItem(menu, "buy_buzzard", "Buzzard", 75000, "Attack helicopter", playerMoney);
            AddVehicleItem(menu, "buy_bati", "Bati 801", 10000, "Fast sport motorcycle", playerMoney);
            AddVehicleItem(menu, "buy_zentorno", "Zentorno", 40000, "Supercar", playerMoney);
            AddVehicleItem(menu, "buy_police_suv", "FBI SUV", 20000, "FIB pursuit SUV", playerMoney);
            AddVehicleItem(menu, "buy_barracks", "Barracks", 25000, "Army troop truck, seats 9", playerMoney);

            // Back button
            menu.AddItem(new MenuItem(BackItemId, "Back", "Return to main menu"));

            _menuProvider.ShowMenu(menu);
        }

        private void AddVehicleItem(MenuDefinition menu, string id, string name, int price, string description, int playerMoney)
        {
            var item = new MenuItem(id, $"{name} (${price:N0})", description);
            item.IsEnabled = playerMoney >= price;
            menu.AddItem(item);
        }

        private void OnItemSelected(object? sender, MenuItemSelectedEventArgs e)
        {
            if (e.MenuId != ShopMenuId) return;

            if (e.ItemId == BackItemId)
            {
                _menuProvider.CloseMenu();
                BackRequested?.Invoke(this, EventArgs.Empty);
                return;
            }

            // Handle vehicle purchase
            if (VehicleCatalog.TryGetValue(e.ItemId, out var vehicleInfo))
            {
                PurchaseVehicle(vehicleInfo.DisplayName, vehicleInfo.ModelName, vehicleInfo.Price);
            }
        }

        private void PurchaseVehicle(string displayName, string modelName, int price)
        {
            var playerMoney = _gameBridge.GetPlayerMoney();
            if (playerMoney < price)
            {
                return; // Can't afford
            }

            // Deduct money
            _gameBridge.AddPlayerMoney(-price);

            // Spawn vehicle on nearest road
            var playerPos = _gameBridge.GetPlayerPosition();
            var spawnPos = _gameBridge.GetNearestRoadPosition(playerPos);
            var vehicleHandle = _gameBridge.CreateVehicle(modelName, spawnPos);

            if (vehicleHandle != -1)
            {
                // Create yellow blip for the vehicle
                var blipHandle = _gameBridge.CreateBlipForVehicle(vehicleHandle);
                if (blipHandle != -1)
                {
                    _gameBridge.SetBlipColor(blipHandle, BlipColor.Yellow);
                }

                // Show notification
                _gameBridge.ShowNotification($"~g~{displayName} delivered! Check your map.");
            }

            // Refresh menu to show updated cash
            Show();
        }
    }
}
