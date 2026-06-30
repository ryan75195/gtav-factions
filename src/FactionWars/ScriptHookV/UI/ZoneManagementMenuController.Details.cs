using FactionWars.Core.Models;
using FactionWars.UI.Models;

namespace FactionWars.ScriptHookV.UI
{
    public partial class ZoneManagementMenuController
    {
        private void ShowZoneDetailMenu(string zoneId, string? selectedItemId = null)
        {
            _selectedZoneId = zoneId;

            var factionId = _playerContext.CurrentFactionId;
            var zone = _zoneService.GetZone(zoneId);
            var zoneName = zone?.Name ?? "Unknown Zone";

            var menu = new MenuDefinition(ZoneDetailMenuId, zoneName, "Deploy defenders");

            var allocation = factionId != null ? _allocationService.GetAllocation(factionId, zoneId) : null;
            AddCurrentAllocationItems(menu, allocation);
            AddDeployItems(menu);
            AddDetailBackItem(menu);

            _menuProvider.ShowMenu(menu, selectedItemId);
            _menuProvider.HoldToRepeatEnabled = true;
        }

        private static void AddCurrentAllocationItems(MenuDefinition menu, ZoneDefenderAllocation? allocation)
        {
            AddCurrentItem(menu, CurrentBasicItemId, "Basic", allocation, DefenderRole.Grunt);
            AddCurrentItem(menu, CurrentMediumItemId, "Medium", allocation, DefenderRole.Gunner);
            AddCurrentItem(menu, CurrentHeavyItemId, "Heavy", allocation, DefenderRole.Rifleman);
            AddCurrentItem(menu, CurrentEliteItemId, "Elite", allocation, DefenderRole.Rocketeer);
            AddCurrentItem(menu, CurrentSniperItemId, "Sniper", allocation, DefenderRole.Sniper);
        }

        private static void AddCurrentItem(
            MenuDefinition menu,
            string itemId,
            string label,
            ZoneDefenderAllocation? allocation,
            DefenderRole tier)
        {
            var count = allocation?.GetTroopCount(tier) ?? 0;
            var item = new MenuItem(itemId, $"{label}: {count}", $"Currently deployed {label} tier defenders");
            item.IsEnabled = false;
            menu.AddItem(item);
        }

        private void AddDeployItems(MenuDefinition menu)
        {
            AddDeployItem(menu, DeployBasicItemId, "Basic", DefenderRole.Grunt);
            AddDeployItem(menu, DeployMediumItemId, "Medium", DefenderRole.Gunner);
            AddDeployItem(menu, DeployHeavyItemId, "Heavy", DefenderRole.Rifleman);
            AddDeployItem(menu, DeployEliteItemId, "Elite", DefenderRole.Rocketeer);
            AddDeployItem(menu, DeploySniperItemId, "Sniper", DefenderRole.Sniper);
        }

        private void AddDeployItem(MenuDefinition menu, string itemId, string label, DefenderRole tier)
        {
            var cost = _deploymentService.GetTroopCost(tier);
            var item = new MenuItem(itemId, $"Deploy {label} — ${cost}", $"Buy and deploy one {label} defender to this zone");
            item.IsEnabled = _deploymentService.CanAfford(tier, 1);
            menu.AddItem(item);
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
