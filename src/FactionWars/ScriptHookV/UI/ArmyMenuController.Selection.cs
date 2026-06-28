using System;
using FactionWars.Core.Interfaces;
using FactionWars.Core.Models;
using FactionWars.Factions.Models;
using FactionWars.ScriptHookV.Managers;
using FactionWars.UI.Interfaces;

namespace FactionWars.ScriptHookV.UI
{
    public partial class ArmyMenuController
    {
        private void OnItemSelected(object? sender, MenuItemSelectedEventArgs e)
        {
            if (e.MenuId == ArmyMenuId)
            {
                HandleArmyMenuSelection(e.ItemId);
            }
            else if (e.MenuId == FollowerListMenuId)
            {
                HandleFollowerListSelection(e.ItemId);
            }
            else if (e.MenuId == FollowerDetailMenuId)
            {
                HandleFollowerDetailSelection(e.ItemId);
            }
        }

        /// <summary>
        /// Handles item selection in the main army menu.
        /// </summary>
        private void HandleArmyMenuSelection(string itemId)
        {
            var factionId = _playerContext.CurrentFactionId;

            // Store the selected item ID for cursor retention on menu refresh
            _lastSelectedItemId = itemId;

            switch (itemId)
            {
                case BackItemId:
                    _lastSelectedItemId = null; // Clear on navigation away
                    _menuProvider.CloseMenu();
                    BackRequested?.Invoke(this, EventArgs.Empty);
                    break;

                case PurchaseBasicItemId:
                    PurchaseTroop(factionId, DefenderRole.Grunt);
                    break;

                case PurchaseMediumItemId:
                    PurchaseTroop(factionId, DefenderRole.Gunner);
                    break;

                case PurchaseHeavyItemId:
                    PurchaseTroop(factionId, DefenderRole.Rifleman);
                    break;

                case RecruitBasicItemId:
                    RecruitFollowerIfFactionSelected(factionId, DefenderRole.Grunt);
                    break;

                case RecruitMediumItemId:
                    RecruitFollowerIfFactionSelected(factionId, DefenderRole.Gunner);
                    break;

                case RecruitHeavyItemId:
                    RecruitFollowerIfFactionSelected(factionId, DefenderRole.Rifleman);
                    break;

                case ManageFollowersItemId:
                    _lastSelectedItemId = null; // Clear when navigating to submenu
                    ShowFollowerListMenu();
                    break;
            }
        }

        private void PurchaseTroop(string? factionId, DefenderRole tier)
        {
            if (factionId == null)
                return;

            _purchaseService.PurchaseTroops(factionId, tier, 1);
            ShowArmyMenu();
        }

        private void RecruitFollowerIfFactionSelected(string? factionId, DefenderRole tier)
        {
            if (factionId != null)
                RecruitFollower(factionId, tier);
        }

        /// <summary>
        /// Handles item selection in the follower list menu.
        /// </summary>
        private void HandleFollowerListSelection(string itemId)
        {
            if (itemId == FollowerListBackItemId)
            {
                ShowArmyMenu();
                return;
            }

            // Check if it's a follower item
            if (itemId.StartsWith("follower_"))
            {
                var followerIdStr = itemId.Substring("follower_".Length);
                if (Guid.TryParse(followerIdStr, out var followerId))
                {
                    ShowFollowerDetailMenu(followerId);
                }
            }
        }

        /// <summary>
        /// Handles item selection in the follower detail menu.
        /// </summary>
        private void HandleFollowerDetailSelection(string itemId)
        {
            switch (itemId)
            {
                case FollowerDetailBackItemId:
                    ShowFollowerListMenu();
                    break;

                case DismissFollowerItemId:
                    if (_selectedFollowerId.HasValue)
                    {
                        _followerService.DismissFollower(_selectedFollowerId.Value);
                        _selectedFollowerId = null;
                        ShowFollowerListMenu();
                    }
                    break;
            }
        }

        /// <summary>
        /// Recruits a follower of the specified tier.
        /// Uses FollowerManager if available, which handles both domain and game world spawning.
        /// </summary>
        private void RecruitFollower(string factionId, DefenderRole tier)
        {
            // Use FollowerManager if available (handles both domain and game world spawning)
            if (_followerManager != null)
            {
                var result = _followerManager.RecruitFollower(factionId, tier);

                if (result.Success)
                {
                    _gameBridge?.ShowNotification($"~g~Recruited {tier} follower");
                }
                else
                {
                    var errorMsg = result.FailureReason switch
                    {
                        FollowerRecruitFailureReason.MaxFollowersReached => "Max followers reached",
                        FollowerRecruitFailureReason.InsufficientFunds => "Insufficient funds",
                        FollowerRecruitFailureReason.InvalidFaction => "Invalid faction",
                        FollowerRecruitFailureReason.SpawnFailed => "Spawn failed",
                        _ => "Unknown error"
                    };
                    _gameBridge?.ShowNotification($"~r~Failed: {errorMsg}");
                }
            }
            else
            {
                // Fallback to domain-only recruitment (no actual ped spawning)
                var purchaseResult = _purchaseService.PurchaseTroops(factionId, tier, 1);
                if (purchaseResult.Success)
                {
                    var result = _followerService.Recruit(factionId, tier);
                    if (!result.Success)
                    {
                        // If recruitment fails, troop goes to reserve as a fallback
                    }
                }
            }
            ShowArmyMenu();
        }

        /// <summary>
        /// Formats a time span into a human-readable string.
        /// </summary>
        private static string FormatServiceTime(TimeSpan time)
        {
            if (time.TotalDays >= 1)
            {
                return $"{(int)time.TotalDays}d";
            }
            if (time.TotalHours >= 1)
            {
                return $"{(int)time.TotalHours}h";
            }
            if (time.TotalMinutes >= 1)
            {
                return $"{(int)time.TotalMinutes}m";
            }
            return $"{(int)time.TotalSeconds}s";
        }
    }
}
