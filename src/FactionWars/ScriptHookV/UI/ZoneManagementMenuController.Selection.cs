using System;
using FactionWars.Core.Models;
using FactionWars.UI.Interfaces;
using FactionWars.UI.Models;

namespace FactionWars.ScriptHookV.UI
{
    public partial class ZoneManagementMenuController
    {
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

            var tier = DeployTierFor(itemId);
            if (tier == null) return;

            _deploymentService.BuyAndDeploy(factionState, _selectedZoneId, tier.Value, 1);
            ShowZoneDetailMenu(_selectedZoneId, itemId);
        }

        private static DefenderRole? DeployTierFor(string itemId)
        {
            switch (itemId)
            {
                case DeployBasicItemId: return DefenderRole.Grunt;
                case DeployMediumItemId: return DefenderRole.Gunner;
                case DeployHeavyItemId: return DefenderRole.Rifleman;
                case DeployEliteItemId: return DefenderRole.Rocketeer;
                case DeploySniperItemId: return DefenderRole.Sniper;
                default: return null;
            }
        }
    }
}
