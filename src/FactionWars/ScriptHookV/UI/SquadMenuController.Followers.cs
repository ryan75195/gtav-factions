using System;
using System.Linq;
using FactionWars.Core.Models;
using FactionWars.UI.Interfaces;
using FactionWars.UI.Models;

namespace FactionWars.ScriptHookV.UI
{
    public partial class SquadMenuController
    {
        private void ShowFollowerListMenu()
        {
            var factionId = _playerContext.CurrentFactionId;
            var followers = factionId != null
                ? _followerService.GetFollowers(factionId)
                : Array.Empty<Follower>();

            var menu = new MenuDefinition(FollowerListMenuId, "Squad", "Manage your bodyguards");

            if (followers.Count == 0)
            {
                var noFollowersItem = new MenuItem(
                    NoFollowersItemId,
                    "No bodyguards",
                    "Recruit bodyguards from the Squad menu");
                noFollowersItem.IsEnabled = false;
                menu.AddItem(noFollowersItem);
            }
            else
            {
                foreach (var follower in followers)
                {
                    var followerItem = new MenuItem(
                        $"follower_{follower.Id}",
                        $"{follower.Tier} Bodyguard",
                        $"Recruited {FormatServiceTime(follower.GetServiceTime())} ago");
                    menu.AddItem(followerItem);
                }
            }

            // Back navigation
            var backItem = new MenuItem(
                FollowerListBackItemId,
                "Back",
                "Return to squad menu");
            menu.AddItem(backItem);

            _menuProvider.ShowMenu(menu);
        }

        private void ShowFollowerDetailMenu(Guid followerId)
        {
            _selectedFollowerId = followerId;
            var follower = _followerService.GetFollowerById(followerId);

            if (follower == null)
            {
                ShowFollowerListMenu();
                return;
            }

            var menu = new MenuDefinition(FollowerDetailMenuId, $"{follower.Tier} Bodyguard", "Bodyguard details");

            // Follower info (disabled display items)
            var tierItem = new MenuItem(
                "tier_info",
                $"Tier: {follower.Tier}",
                "Combat tier determines equipment and effectiveness");
            tierItem.IsEnabled = false;
            menu.AddItem(tierItem);

            var serviceItem = new MenuItem(
                "service_info",
                $"Service time: {FormatServiceTime(follower.GetServiceTime())}",
                "Time since recruitment");
            serviceItem.IsEnabled = false;
            menu.AddItem(serviceItem);

            // Dismiss option
            var dismissItem = new MenuItem(
                DismissFollowerItemId,
                "Dismiss Bodyguard",
                "Send this bodyguard away (no refund)");
            menu.AddItem(dismissItem);

            // Back navigation
            var backItem = new MenuItem(
                FollowerDetailBackItemId,
                "Back",
                "Return to squad list");
            menu.AddItem(backItem);

            _menuProvider.ShowMenu(menu);
        }

        private void OnItemSelected(object? sender, MenuItemSelectedEventArgs e)
        {
            if (e.MenuId == MenuId)
            {
                HandleSquadMenuSelection(e.ItemId);
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

        private void HandleSquadMenuSelection(string itemId)
        {
            var factionId = _playerContext.CurrentFactionId;
            _lastSelectedItemId = itemId;

            switch (itemId)
            {
                case BackItemId:
                    _lastSelectedItemId = null;
                    _menuProvider.CloseMenu();
                    BackRequested?.Invoke(this, EventArgs.Empty);
                    break;

                case RecruitBasicItemId:
                    if (factionId != null) RecruitFollower(factionId, DefenderRole.Grunt);
                    break;

                case RecruitMediumItemId:
                    if (factionId != null) RecruitFollower(factionId, DefenderRole.Gunner);
                    break;

                case RecruitHeavyItemId:
                    if (factionId != null) RecruitFollower(factionId, DefenderRole.Rifleman);
                    break;

                case RecruitEliteItemId:
                    if (factionId != null) RecruitFollower(factionId, DefenderRole.Rocketeer);
                    break;

                case RecruitSniperItemId:
                    if (factionId != null) RecruitFollower(factionId, DefenderRole.Sniper);
                    break;

                case ManageFollowersItemId:
                    _lastSelectedItemId = null;
                    ShowFollowerListMenu();
                    break;
            }
        }

        private void HandleFollowerListSelection(string itemId)
        {
            if (itemId == FollowerListBackItemId)
            {
                ShowSquadMenu();
                return;
            }

            if (itemId.StartsWith("follower_"))
            {
                var followerIdStr = itemId.Substring("follower_".Length);
                if (Guid.TryParse(followerIdStr, out var followerId))
                {
                    ShowFollowerDetailMenu(followerId);
                }
            }
        }

    }
}
