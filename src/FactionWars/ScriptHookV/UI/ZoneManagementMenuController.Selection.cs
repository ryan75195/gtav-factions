using System;
using FactionWars.Core.Models;
using FactionWars.ScriptHookV.Logging;
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
                    _allocationService.AllocateTroops(factionState, _selectedZoneId, DefenderRole.Grunt, 1);
                    ShowZoneDetailMenu(_selectedZoneId, AllocateBasicItemId);
                    break;

                case AllocateMediumItemId:
                    _allocationService.AllocateTroops(factionState, _selectedZoneId, DefenderRole.Gunner, 1);
                    ShowZoneDetailMenu(_selectedZoneId, AllocateMediumItemId);
                    break;

                case AllocateHeavyItemId:
                    _allocationService.AllocateTroops(factionState, _selectedZoneId, DefenderRole.Rifleman, 1);
                    ShowZoneDetailMenu(_selectedZoneId, AllocateHeavyItemId);
                    break;

                case WithdrawBasicItemId:
                    _allocationService.WithdrawTroops(factionState, _selectedZoneId, DefenderRole.Grunt, 1);
                    ShowZoneDetailMenu(_selectedZoneId, WithdrawBasicItemId);
                    break;

                case WithdrawMediumItemId:
                    _allocationService.WithdrawTroops(factionState, _selectedZoneId, DefenderRole.Gunner, 1);
                    ShowZoneDetailMenu(_selectedZoneId, WithdrawMediumItemId);
                    break;

                case WithdrawHeavyItemId:
                    _allocationService.WithdrawTroops(factionState, _selectedZoneId, DefenderRole.Rifleman, 1);
                    ShowZoneDetailMenu(_selectedZoneId, WithdrawHeavyItemId);
                    break;
            }
        }
    }
}
