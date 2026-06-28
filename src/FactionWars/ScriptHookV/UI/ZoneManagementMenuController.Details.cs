using FactionWars.Core.Models;
using FactionWars.Factions.Models;
using FactionWars.UI.Models;
using System;

namespace FactionWars.ScriptHookV.UI
{
    public partial class ZoneManagementMenuController
    {
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
            var currentBasic = allocation?.GetTroopCount(DefenderRole.Grunt) ?? 0;
            var currentMedium = allocation?.GetTroopCount(DefenderRole.Gunner) ?? 0;
            var currentHeavy = allocation?.GetTroopCount(DefenderRole.Rifleman) ?? 0;

            // Get reserve counts
            var reserveBasic = factionState?.GetReserveTroops(DefenderRole.Grunt) ?? 0;
            var reserveMedium = factionState?.GetReserveTroops(DefenderRole.Gunner) ?? 0;
            var reserveHeavy = factionState?.GetReserveTroops(DefenderRole.Rifleman) ?? 0;
            AddCurrentAllocationItems(menu, currentBasic, currentMedium, currentHeavy);
            AddAllocateItems(menu, reserveBasic, reserveMedium, reserveHeavy);
            AddWithdrawItems(menu, currentBasic, currentMedium, currentHeavy);
            AddDetailBackItem(menu);

            _menuProvider.ShowMenu(menu, selectedItemId);
            _menuProvider.HoldToRepeatEnabled = true; // Enable hold-to-repeat for allocation/withdrawal
        }

        private static void AddCurrentAllocationItems(
            MenuDefinition menu,
            int currentBasic,
            int currentMedium,
            int currentHeavy)
        {
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
        }

        private static void AddAllocateItems(
            MenuDefinition menu,
            int reserveBasic,
            int reserveMedium,
            int reserveHeavy)
        {
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
        }

        private static void AddWithdrawItems(
            MenuDefinition menu,
            int currentBasic,
            int currentMedium,
            int currentHeavy)
        {
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
        }

        private static void AddDetailBackItem(MenuDefinition menu)
        {
            var backItem = new MenuItem(
                DetailBackItemId,
                "Back",
                "Return to zone list");
            menu.AddItem(backItem);
        }

        /// <summary>
        /// Handles menu item selection events.
        /// </summary>
    }
}
